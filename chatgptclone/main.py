import asyncio
import json
import os
import sys
from pathlib import Path
from typing import Any
from urllib import error, request

conversation_history: list[dict[str, str]] = []
config: dict[str, Any] = {}


def load_config() -> dict[str, Any]:
    settings_path = Path(__file__).resolve().parent / "appsettings.json"
    loaded: dict[str, Any] = {}

    if settings_path.exists():
        with settings_path.open("r", encoding="utf-8") as f:
            loaded = json.load(f)

    openai = loaded.get("OpenAI", {})
    app = loaded.get("App", {})

    return {
        "OpenAI": {
            "ApiKey": os.getenv("OPENAI_API_KEY", openai.get("ApiKey", "")),
            "Model": openai.get("Model", "gpt-3.5-turbo"),
            "MaxTokens": int(openai.get("MaxTokens", 1000)),
            "Temperature": float(openai.get("Temperature", 0.7)),
        },
        "App": {
            "ConversationHistoryFile": app.get("ConversationHistoryFile", "conversation_history.json"),
            "MaxHistoryMessages": int(app.get("MaxHistoryMessages", 20)),
        },
    }


def get_history_path() -> Path:
    history_file = config["App"]["ConversationHistoryFile"]
    path = Path(history_file)
    return path if path.is_absolute() else Path.cwd() / path


async def load_conversation_history() -> None:
    global conversation_history
    try:
        history_path = get_history_path()
        if history_path.exists():
            content = await asyncio.to_thread(history_path.read_text, encoding="utf-8")
            payload = json.loads(content)
            conversation_history = payload.get("Messages", [])
            print(f"Loaded {len(conversation_history)} messages from conversation history.")
    except Exception as ex:
        print(f"Warning: Could not load conversation history: {ex}")
        conversation_history = []


async def save_conversation_history() -> None:
    try:
        history_path = get_history_path()
        payload = {"Messages": conversation_history}
        content = json.dumps(payload, indent=2)
        await asyncio.to_thread(history_path.write_text, content, encoding="utf-8")
    except Exception as ex:
        print(f"Warning: Could not save conversation history: {ex}")


def get_ai_response() -> str:
    api_key = config["OpenAI"]["ApiKey"]
    if not api_key or api_key == "your-api-key-here":
        raise RuntimeError("OpenAI API key not found. Set OPENAI_API_KEY or OpenAI.ApiKey in appsettings JSON")

    max_history_messages = config["App"]["MaxHistoryMessages"]
    messages = conversation_history[-max_history_messages:]

    payload = {
        "model": config["OpenAI"]["Model"],
        "messages": messages,
        "max_tokens": config["OpenAI"]["MaxTokens"],
        "temperature": config["OpenAI"]["Temperature"],
    }

    body = json.dumps(payload).encode("utf-8")
    req = request.Request(
        "https://api.openai.com/v1/chat/completions",
        data=body,
        headers={
            "Authorization": "{} {}".format("Bearer", api_key),
            "Content-Type": "application/json",
        },
        method="POST",
    )

    try:
        with request.urlopen(req) as response:
            data = json.loads(response.read().decode("utf-8"))
    except error.HTTPError as ex:
        details = ex.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"API request failed: {ex.code} - {details}") from ex

    choices = data.get("choices", [])
    if not choices:
        raise RuntimeError("No response received from AI")

    return choices[0].get("message", {}).get("content", "")


async def process_user_message(user_input: str) -> None:
    conversation_history.append({"role": "user", "content": user_input})
    ai_response = await asyncio.to_thread(get_ai_response)
    conversation_history.append({"role": "assistant", "content": ai_response})

    print(f"\nAI: {ai_response}")
    await save_conversation_history()


async def process_test_message(test_message: str) -> None:
    print(f"\nTest Mode - Processing message: {test_message}")
    try:
        await process_user_message(test_message)
        print("\nTest completed successfully!")
    except Exception as ex:
        print(f"Test failed with error: {ex}")


async def main() -> None:
    global config
    config = load_config()

    await load_conversation_history()

    print("ChatGPT Clone - Type 'exit' to quit, 'clear' to clear history")
    print("================================================")

    if len(sys.argv) > 1:
        await process_test_message(sys.argv[1])
        return

    while True:
        user_input = input("\nYou: ").strip()
        if not user_input:
            continue

        if user_input.lower() == "exit":
            await save_conversation_history()
            print("Goodbye!")
            break

        if user_input.lower() == "clear":
            conversation_history.clear()
            await save_conversation_history()
            print("Conversation history cleared!")
            continue

        try:
            await process_user_message(user_input)
        except Exception as ex:
            print(f"Error: {ex}")


if __name__ == "__main__":
    asyncio.run(main())
