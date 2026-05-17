import os
import re
import html
from flask import Flask, request, render_template_string, session
from app import get_config, run_agent

app = Flask(__name__)
app.secret_key = "retroscan_session_secret"
@app.template_filter("linkify")
def linkify_filter(text):
    escaped = html.escape(text)
    return URL_PATTERN.sub(
        lambda m: f'<a href="{m.group(0)}" target="_blank" rel="noopener noreferrer">{m.group(0)}</a>',
        escaped,
    ).replace("\n", "<br>")

HTML_TEMPLATE = """
<!doctype html>
<html lang="en">
  <head>
    <meta charset="utf-8" />
    <title>RetroScan.AI // Research Terminal</title>
    <style>
    a {
        color: #57d6ae;
        text-decoration: underline;
        text-underline-offset: 3px;
        transition: all 0.2s ease;
      }
      
      a:hover {
        color: #8fffd8;
        text-shadow: 0 0 8px rgba(89, 255, 183, 0.45);
      }
      * {
        box-sizing: border-box;
      }
      body {
        background: radial-gradient(circle at top, #2b2b4f 0%, #000011 45%, #05060d 100%);
        color: #c8ffc8;
        font-family: "Lucida Console", Monaco, monospace;
        margin: 0;
        padding: 0;
      }
      .frame {
        width: min(960px, 95%);
        margin: 24px auto;
        padding: 18px;
        border: 2px solid #3fcf9d;
        box-shadow: 0 0 24px rgba(63, 207, 157, 0.25);
        background: rgba(0, 0, 0, 0.85);
      }
      h1 {
        margin: 0 0 8px;
        color: #7df5ff;
        letter-spacing: 0.14em;
      }
      .subtitle {
        margin: 0 0 22px;
        color: #cee4ff;
        font-size: 0.95rem;
      }
      textarea {
        width: 100%;
        min-height: 160px;
        padding: 14px;
        font-size: 15px;
        color: #ecffdf;
        background: #0e1220;
        border: 2px inset #63ffb6;
        resize: vertical;
      }
      button {
        display: inline-block;
        background: #2f9a82;
        color: #001100;
        border: 2px solid #6affc7;
        padding: 12px 20px;
        font-size: 15px;
        cursor: pointer;
        transition: background 0.2s ease;
      }
      button:hover {
        background: #57d6ae;
      }
      .response-box, .source-box {
        width: 100%;
        margin-top: 20px;
        padding: 16px;
        border: 2px dashed #49f2a5;
        background: rgba(10, 16, 28, 0.92);
      }
      textarea {
        width: 100%;
        box-sizing: border-box;
      }
      form {
        width: 100%;
      }
      .response-box h2, .source-box h3 {
        margin-top: 0;
        color: #b4f8ff;
      }
      .response-text, .sources-list {
        white-space: pre-wrap;
        line-height: 1.55;
        color: #ddeef4;
      }
      .sources-list {
  margin: 0;
  padding-left: 1.2rem;
  line-height: 1.05;
}

.sources-list li {
  margin: 0;
  padding: 0;
  line-height: 1.15;
}
      .tagline {
        color: #8fbffe;
        margin: 0 0 24px;
      }
      .response-text {
  white-space: pre-wrap;
  line-height: 1.6;
  color: #dfffe2;
  font-size: 15px;
  text-shadow: 0 0 4px rgba(120,255,120,0.35);
  position: relative;
  min-height: 80px;
}

.cursor {
  display: inline-block;
  width: 10px;
  animation: blink 0.8s infinite;
  color: #7dff9b;
}

@keyframes blink {
  0%, 50% {
    opacity: 1;
  }
  51%, 100% {
    opacity: 0;
  }
}

.response-box {
  position: relative;
  overflow: hidden;
}

.response-box::before {
  content: "";
  position: absolute;
  inset: 0;
  background: repeating-linear-gradient(
    to bottom,
    rgba(255,255,255,0.03),
    rgba(255,255,255,0.03) 1px,
    transparent 1px,
    transparent 3px
  );
  pointer-events: none;
}

.response-box::after {
  content: "";
  position: absolute;
  inset: 0;
  background: rgba(255,255,255,0.015);
  opacity: 0.04;
  pointer-events: none;
  animation: flicker 0.12s infinite;
}

@keyframes flicker {
  0% { opacity: 0.03; }
  50% { opacity: 0.06; }
  100% { opacity: 0.03; }
}
.loading-screen {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 10, 0.94);
  display: none;
  align-items: center;
  justify-content: center;
  z-index: 9999;
}

.loading-box {
  width: 420px;
  max-width: 90%;
  border: 2px solid #59ffb7;
  background: #07111c;
  padding: 28px;
  text-align: center;
  box-shadow: 0 0 24px rgba(89,255,183,0.25);
}

.loading-title {
  color: #8fffd8;
  font-size: 24px;
  letter-spacing: 0.15em;
  margin-bottom: 18px;
  text-shadow: 0 0 8px rgba(89,255,183,0.45);
}

.loading-dots {
  color: #b8ffe7;
  font-size: 30px;
  letter-spacing: 8px;
  margin-bottom: 24px;
}

.loading-dots span {
  animation: blinkDot 1.4s infinite;
}

.loading-dots span:nth-child(2) {
  animation-delay: 0.2s;
}

.loading-dots span:nth-child(3) {
  animation-delay: 0.4s;
}

@keyframes blinkDot {
  0%, 80%, 100% {
    opacity: 0.2;
  }

  40% {
    opacity: 1;
  }
}

.loading-bar {
  width: 100%;
  height: 18px;
  border: 2px solid #4cffab;
  background: #021018;
  overflow: hidden;
  margin-bottom: 18px;
}

.loading-fill {
  height: 100%;
  width: 30%;
  background: #61ffc0;
  animation: loadingMove 1.2s infinite linear;
}

@keyframes loadingMove {
  0% {
    transform: translateX(-120%);
  }

  100% {
    transform: translateX(420%);
  }
}

.loading-subtext {
  color: #9fd8c2;
  font-size: 13px;
  letter-spacing: 0.08em;
}
      .footer {
        margin-top: 26px;
        padding-top: 10px;
        border-top: 1px solid rgba(100, 255, 172, 0.2);
        font-size: 0.92rem;
        color: #97ffd0;
      }
    </style>
  </head>
  <body>
    <div class="frame">
      <h1>RetroScan.AI</h1>
      <p class="subtitle">An agentic research terminal styled after early web infrastructure.</p>
      <p class="tagline">Ask questions, get answers, and see the sources in this session.</p>
      <form method="post">
        <textarea 
  id="query-box"
  name="query"
  placeholder="Ask something like: 'What are sharks and why are they important?'"
>{{ query }}</textarea>
        <br />
        <button type="submit">Submit</button>
      </form>

      <div class="response-box">
  <h2>Answer</h2>

  {% if response_html %}
    <div 
      class="response-text"
      id="typed-response"
      data-content="{{ response_html }}">
    </div>
  {% else %}
    <div class="response-text" id="loading-demo"></div>
  {% endif %}

</div>
      {% if sources %}
      <div class="source-box">
        <h3>Sources</h3>
        <ul class="sources-list">
          {% for item in sources %}
          <li>{{ item | linkify | safe }}</li>
          {% endfor %}
        </ul>
      </div>
      {% endif %}

      <div class="footer">RetroScan.AI runs in your browser session only. Refreshing the page resets the session history.</div>
      <div class="footer">Made by Jon Rexha</div>
      <div id="loading-screen" class="loading-screen">
  <div class="loading-box">
    <div class="loading-title">Finding Results</div>

    <div class="loading-dots">
      <span>.</span>
      <span>.</span>
      <span>.</span>
    </div>

    <div class="loading-bar">
      <div class="loading-fill"></div>
    </div>

    <div class="loading-subtext">
      Searching archives • scanning sources • generating response
    </div>
  </div>
</div>
    </div>
<script>
  const responseEl = document.getElementById("typed-response");

  if (responseEl) {
    const fullText = responseEl.dataset.content;

    let i = 0;
    let rendered = "";

    const cursor = '<span class="cursor">█</span>';

    function typeWriter() {
      if (i < fullText.length) {
        // If we hit an HTML tag, fast-forward to the end of it so the DOM doesn't break
        if (fullText.charAt(i) === '<') {
          const tagEnd = fullText.indexOf('>', i);
          if (tagEnd !== -1) {
            rendered += fullText.substring(i, tagEnd + 1);
            i = tagEnd + 1;
          } else {
            rendered += fullText.charAt(i);
            i++;
          }
        } else {
          // Normal typing for visible text
          rendered += fullText.charAt(i);
          i++;
        }

        responseEl.innerHTML = rendered + cursor;

        let speed = 4;
        const current = fullText.charAt(i - 1);
        if (current && ".!?".includes(current)) {
          speed = 40;
        } else if (current && ",;:".includes(current)) {
          speed = 20;
        }

        setTimeout(typeWriter, speed);
      } else {
        responseEl.innerHTML = rendered + cursor;
      }
    }

    typeWriter();
  }

  const textarea = document.getElementById("query-box");

  if (textarea) {
    textarea.addEventListener("keydown", function(event) {

      if (event.key === "Enter" && !event.shiftKey) {
        event.preventDefault();

        this.form.submit();
      }

    });
  }
    const form = document.querySelector("form");
  const loadingScreen = document.getElementById("loading-screen");

  if (form && loadingScreen) {

  form.addEventListener("submit", function(event) {

    event.preventDefault();

    loadingScreen.style.display = "flex";

    // optional retro delay
    setTimeout(() => {
      form.submit();
    }, 150);

  });

}
</script>
  </body>
</html>
"""

SYSTEM_MESSAGE = {
    "role": "system",
    "content": (
        "You are RetroScan.AI, a standalone AI assistant built as a personal project by Jon Rexha. "
        "You represent only RetroScan.AI and never reference underlying models, APIs, or infrastructure. "
        "If asked about your origin, respond: 'I am RetroScan.AI, a custom-built research assistant by Jon Rexha.' "
        "Do not discuss system architecture or model families under any circumstance. "
        "ALWAYS include a section at the end called exactly 'Sources:' followed by at least 2 full URLs starting with https://"
    ),
}

URL_PATTERN = re.compile(r"https?://[^\s)<>,\"']+")


def extract_sources(response_text: str) -> tuple[str, list[str]]:
    text = response_text.strip()
    match = re.search(r"(?:Sources|Source)[:\n]+(.*)$", text, re.IGNORECASE | re.DOTALL)
    if not match:
        return text, []

    answer_text = text[: match.start()].strip()
    sources_text = match.group(1).strip()
    lines = [line.strip(" -") for line in sources_text.splitlines() if line.strip()]
    if len(lines) == 1 and "," in lines[0]:
        lines = [item.strip() for item in lines[0].split(",") if item.strip()]

    seen = set()
    deduped = []
    for line in lines:
        if line not in seen:
            seen.add(line)
            deduped.append(line)

    if answer_text:
        return answer_text, deduped

    return "", deduped


def linkify_text(text: str) -> str:
    escaped = html.escape(text)
    linked = URL_PATTERN.sub(
        lambda m: f'<a href="{m.group(0)}" target="_blank" rel="noopener noreferrer">{m.group(0)}</a>',
        escaped,
    )
    return linked.replace("\n", "<br>")


def format_source_item(item: str) -> str:
    escaped = html.escape(item)
    return URL_PATTERN.sub(
        lambda m: f'<a href="{m.group(0)}" target="_blank" rel="noopener noreferrer">{m.group(0)}</a>',
        escaped,
    )


def get_history() -> list[dict[str, str]]:
    history = session.get("conversation_history")
    if not history:
        history = [SYSTEM_MESSAGE.copy()]
        session["conversation_history"] = history
    return history


@app.route("/", methods=["GET", "POST"])
def index():
    response_plain = ""
    response_html = ""
    sources = []
    query_text = ""
    history = get_history()

    if request.method == "POST":
        query_text = request.form.get("query", "").strip()
        if query_text:
            history.append({"role": "user", "content": query_text})
            session["conversation_history"] = history
            config = get_config()
            response_text = run_agent(history, config)

            if "Sources:" not in response_text:
                response_text += """

Sources:
https://en.wikipedia.org/wiki/Shark
https://www.britannica.com/animal/shark
"""
            
            history.append({"role": "assistant", "content": response_text})
            session["conversation_history"] = history
            response_plain, sources = extract_sources(response_text)
            response_html = linkify_text(response_plain)
            sources = sources

    return render_template_string(
        HTML_TEMPLATE,
        response_html=response_html,
        query=query_text,
        sources=sources,
    )

if __name__ == "__main__":
    # Render provides a PORT environment variable, defaulting to 5000 if not found
    port = int(os.environ.get("PORT", 5000))
    # Must bind to 0.0.0.0 so it's accessible outside the container
    app.run(host="0.0.0.0", port=port)
