# Research Agent

This project is a simple Python research agent that supports OpenAI-compatible providers such as Groq, OpenAI, and a local fallback mode.

## Setup

1. Create a `.env` file in the project root.
2. Add your preferred provider configuration:

### Groq

```env
GROQ_API_KEY=gsk-your-key
OPENAI_API_BASE=https://api.groq.com/openai/v1
MODEL=llama-3.3-70b-versatile
```

### OpenAI

```env
OPENAI_API_KEY=sk-your-key
OPENAI_API_BASE=https://api.openai.com/v1
MODEL=gpt-4-0613
```

### Local fallback mode

Use this when you want the app to run without a live API call:

```env
LLM_PROVIDER=local
```

3. Install dependencies:

```bash
python -m pip install -r requirements.txt
```

4. Run the app:

```bash
python app.py
```

Or use the old entrypoint name:

```bash
python main.py
```

### Additional usage

Run with an inline query:

```bash
python app.py --query "what are sharks"
```

Pipe a query from stdin:

```bash
echo "what are sharks" | python app.py
```

### Interactive session with history

If you run the app without `--query`, it will keep conversation history during that session:

```bash
python app.py
```

Then type your questions and press Enter. The agent will remember the previous messages while the session stays open.

To end the session, type `exit`, `quit`, or press Enter on a blank line.

### Run the ResearchHelp website

Start the retro website with:

```bash
python web.py
```

Then open your browser to:

```text
http://localhost:8080
```

The page is called ResearchHelp and shows an answer box plus a Sources section for each result.

> The website keeps history during the browser session only and does not save it permanently.
> Remove `LLM_PROVIDER=local` from `.env` if you want the site to use Groq or OpenAI instead of the local fallback.

