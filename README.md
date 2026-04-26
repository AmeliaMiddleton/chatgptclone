# ChatGPT Clone (Console App) — .NET 8

A simple **C#/.NET 8** console “ChatGPT clone” that sends your messages to the OpenAI API and prints the assistant response back in the terminal. It also saves a local conversation log so your chat history can persist between runs.

## Features

- Console chat loop with:
  - `exit` to quit (and save history)
  - `clear` to wipe saved history
- Uses OpenAI **Chat Completions** endpoint (`/v1/chat/completions`)
- Maintains a rolling in-memory history (configurable limit) to provide conversation context
- Saves conversation history to a JSON file (default: `conversation_history.json`)
- Configuration via **.NET User Secrets** (recommended for API key)

## Tech Stack

- .NET **8.0** console application
- `HttpClient` for API requests
- `Microsoft.Extensions.Configuration` (+ UserSecrets)
- `Newtonsoft.Json` for JSON serialization

## Project Structure

- `chatgptclone/Program.cs` — main application + request/response models + history persistence
- `chatgptclone/appsettings.json` — non-secret defaults (model, max tokens, temperature, history settings)
- `chatgptclone/chatgptclone.csproj` — project file targeting `net8.0`
- `chatgptclone.sln` — solution file

## Prerequisites

- .NET SDK 8 installed
- An OpenAI API key

## Setup (Recommended: User Secrets)

This project is configured to load secrets using `AddUserSecrets<Program>()`.

1. Open a terminal in the project folder that contains `chatgptclone.csproj`
2. Initialize user secrets (if needed):
   ```bash
   dotnet user-secrets init