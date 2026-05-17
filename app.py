from __future__ import annotations

import argparse
import os
import sys
from dataclasses import dataclass
from dotenv import load_dotenv, find_dotenv
from pathlib import Path
from openai import OpenAI

DEFAULT_GROQ_BASE = "https://api.groq.com/openai/v1"
DEFAULT_OPENAI_BASE = "https://api.openai.com/v1"


@dataclass
class AgentConfig:
    provider: str
    api_key: str | None
    base_url: str | None
    model_name: str


def load_environment() -> Path:
    project_root = Path(__file__).parent
    
    # 1. If we are on Render, do NOT load sample.env. Trust the Render dashboard!
    if os.environ.get("RENDER"):
        print("Running on Render. Trusting dashboard environment variables.")
        return project_root
        
    # 2. Local development fallback flow
    env_file = find_dotenv(".env")
    if env_file:
        load_dotenv(env_file)
        print(f"Loaded environment from: {env_file}")
        return Path(env_file)

    sample_env_path = project_root / "sample.env"
    if sample_env_path.exists():
        load_dotenv(sample_env_path)
        print(f"Loaded environment from sample file: {sample_env_path}")
        return sample_env_path

    raise FileNotFoundError(
        "No .env file found. Create a .env file in the project root or add sample.env."
    )

def get_config() -> AgentConfig:
    load_environment()
    if os.getenv("LLM_PROVIDER", "").lower() == "local":
        return AgentConfig(provider="local", api_key=None, base_url=None, model_name="local")

    groq_key = os.getenv("GROQ_API_KEY", "").strip()
    openai_key = os.getenv("OPENAI_API_KEY", "").strip()
    if groq_key:
        base_url = os.getenv("OPENAI_API_BASE", DEFAULT_GROQ_BASE).strip()
        return AgentConfig(provider="groq", api_key=groq_key, base_url=base_url, model_name=os.getenv("MODEL", "llama-3.3-70b-versatile"))

    if openai_key:
        base_url = os.getenv("OPENAI_API_BASE", DEFAULT_OPENAI_BASE).strip()
        return AgentConfig(provider="openai", api_key=openai_key, base_url=base_url, model_name=os.getenv("MODEL", "gpt-4-0613"))

    raise RuntimeError(
        "Missing API key. Set GROQ_API_KEY or OPENAI_API_KEY in your .env file or environment, or set LLM_PROVIDER=local."
    )


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Run the PythonAI research agent.")
    parser.add_argument("--query", "-q", help="Query text to send to the agent.")
    return parser.parse_args()


def read_query(cli_query: str | None) -> str:
    if cli_query:
        return cli_query.strip()

    if not sys.stdin.isatty():
        text = sys.stdin.read().strip()
        if text:
            return text

    return input("What can I help you research? ").strip()


def run_agent(messages: list[dict[str, str]], config: AgentConfig) -> str:
    if len(messages) <= 1:
        raise ValueError("No query provided. Enter text, use --query, or pipe a query through stdin.")

    if config.provider == "local":
        print("Using local fallback mode.")
        last_user = next((m["content"] for m in reversed(messages) if m["role"] == "user"), "")
        return f"This is a concise local summary for: {last_user}"

    print(f"Using provider: {config.provider} ({config.base_url})")
    client = OpenAI(base_url=config.base_url, api_key=config.api_key)
    response = client.chat.completions.create(
        model=config.model_name,
        messages=messages,
    )

    if response and hasattr(response, "choices") and len(response.choices) > 0:
        message = response.choices[0].message
        if isinstance(message, dict):
            content = message.get("content")
        else:
            content = getattr(message, "content", None)
        return content if content is not None else str(message)

    return str(response)


def interactive_session(config: AgentConfig) -> None:
    messages = [{"role": "system", "content": "You are a helpful research assistant."}]
    print("Interactive session started. Type 'exit' or 'quit' to end.")

    while True:
        query = input("You: ").strip()
        if not query or query.lower() in {"exit", "quit", "bye"}:
            print("Session ended.")
            break

        messages.append({"role": "user", "content": query})
        output = run_agent(messages, config)
        print("--- MODEL RESPONSE ---")
        print(output)
        messages.append({"role": "assistant", "content": output})


def main() -> None:
    args = parse_args()
    config = get_config()

    if args.query:
        messages = [
            {"role": "system", "content": "You are a helpful research assistant."},
            {"role": "user", "content": read_query(args.query)},
        ]
        output = run_agent(messages, config)
        print("--- MODEL RESPONSE ---")
        print(output)
        return

    if not sys.stdin.isatty():
        query = read_query(None)
        messages = [
            {"role": "system", "content": "You are a helpful research assistant."},
            {"role": "user", "content": query},
        ]
        output = run_agent(messages, config)
        print("--- MODEL RESPONSE ---")
        print(output)
        return

    interactive_session(config)


if __name__ == "__main__":
    main()
