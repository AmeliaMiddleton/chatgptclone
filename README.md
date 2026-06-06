# ChatGPT Clone (Console App) — Python

A simple **Python** console “ChatGPT clone” that sends your messages to the OpenAI API and prints the assistant response in the terminal. It also saves a local conversation log so your chat history can persist between runs.

## Features

- Console chat loop with:
  - `exit` to quit (and save history)
  - `clear` to wipe saved history
- Uses OpenAI **Chat Completions** endpoint (`/v1/chat/completions`)
- Maintains a rolling in-memory history (configurable limit) to provide conversation context
- Saves conversation history to a JSON file (default: `conversation_history.json`)
- Configuration via `appsettings.json` plus `OPENAI_API_KEY` environment variable

## Tech Stack

- Python 3.10+
- Standard library only (`urllib`, `json`, `asyncio`, `pathlib`)

## Project Structure

- `chatgptclone/main.py` — main application + API call + history persistence
- `chatgptclone/appsettings.json` — non-secret defaults (model, max tokens, temperature, history settings)

## Prerequisites

- Python 3.10+
- An OpenAI API key

## Setup

1. Set your OpenAI key as an environment variable:

   ```bash
   export OPENAI_API_KEY="your_api_key_here"
   ```

2. (Optional) Update `chatgptclone/appsettings.json`.

## Run

From the repository root:

```bash
python3 chatgptclone/main.py
```

## Test Mode

Pass a single message argument to run once and exit:

```bash
python3 chatgptclone/main.py "Hello"
```
