const output = document.getElementById('output');
const form = document.getElementById('prompt');
const input = document.getElementById('input');
const sessionList = document.getElementById('session-list');
const newSessionButton = document.getElementById('new-session');
const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

const HELP = [
  'COMMANDS',
  '  /new      start a new research session',
  '  /delete   delete the open session',
  '  /clear    clear the screen',
  '  /help     show this list',
  'Anything else is a question. Answers cite Wikipedia articles as [1], [2], ...',
  'Arrow up and down recall earlier input.',
];

let sessions = [];
let current = null;
const inputHistory = [];
let historyIndex = 0;

class ApiError extends Error {
  constructor(status, problem) {
    const details = problem?.errors ? Object.values(problem.errors).flat() : [problem?.detail].filter(Boolean);
    super([`ERROR ${status}: ${problem?.title ?? 'request failed'}`, ...details].join('\n'));
  }
}

async function api(path, { method = 'GET', body } = {}) {
  const response = await fetch(`/api${path}`, {
    method,
    headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  if (response.status === 204) {
    return null;
  }
  const payload = await response.json().catch(() => null);
  if (!response.ok) {
    throw new ApiError(response.status, payload);
  }
  return payload;
}

function scrollDown() {
  output.scrollTop = output.scrollHeight;
}

function line(text, className = '') {
  const element = document.createElement('p');
  element.className = `line ${className}`.trim();
  element.textContent = text;
  output.append(element);
  scrollDown();
  return element;
}

const nextFrame = () => new Promise((resolve) => requestAnimationFrame(resolve));

function safeUrl(url) {
  return /^https:\/\//.test(url) ? url : '#';
}

function sourceLink(source, text) {
  const link = document.createElement('a');
  link.href = safeUrl(source.url);
  link.target = '_blank';
  link.rel = 'noopener noreferrer';
  link.textContent = text;
  return link;
}

function citationSegments(answer, sources) {
  const byNumber = new Map(sources.map((source) => [source.number, source]));
  return answer.split(/(\[\d+\])/).filter(Boolean).map((part) => {
    const source = byNumber.get(Number(part.slice(1, -1)));
    return /^\[\d+\]$/.test(part) && source ? { text: part, source } : { text: part };
  });
}

async function typeOut(container, segments, animate) {
  for (const segment of segments) {
    if (segment.source) {
      const cite = sourceLink(segment.source, segment.text);
      cite.className = 'cite';
      cite.title = segment.source.title;
      container.append(cite);
      continue;
    }
    const text = document.createTextNode('');
    container.append(text);
    if (!animate) {
      text.data = segment.text;
      continue;
    }
    for (let shown = 0; shown < segment.text.length; shown += 6) {
      text.data = segment.text.slice(0, shown + 6);
      scrollDown();
      await nextFrame();
    }
  }
  scrollDown();
}

async function renderAnswer(question, animate) {
  line(`SEARCHED WIKIPEDIA FOR "${question.searchQuery}"`, 'dim');
  const answer = document.createElement('p');
  answer.className = 'answer';
  output.append(answer);
  await typeOut(answer, citationSegments(question.answer, question.sources), animate);

  if (question.sources.length > 0) {
    const list = document.createElement('ol');
    list.className = 'sources';
    for (const source of question.sources) {
      const item = document.createElement('li');
      item.append(`[${source.number}] `, sourceLink(source, source.title));
      list.append(item);
    }
    output.append(list);
    scrollDown();
  }
}

function formatDate(iso) {
  return new Date(iso).toLocaleString(undefined, { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' });
}

function renderSessionList() {
  sessionList.replaceChildren(...sessions.map((session) => {
    const button = document.createElement('button');
    button.type = 'button';
    button.setAttribute('aria-current', String(session.id === current?.id));
    button.addEventListener('click', () => run(() => openSession(session.id)));

    const title = document.createElement('span');
    title.className = 'session-title';
    title.textContent = session.title || 'UNTITLED';
    const meta = document.createElement('span');
    meta.className = 'session-meta';
    meta.textContent = `${session.questionCount} Q  ${formatDate(session.updatedAt)}`;
    button.append(title, meta);

    const item = document.createElement('li');
    item.append(button);
    return item;
  }));
}

async function loadSessions() {
  sessions = await api('/sessions');
  renderSessionList();
}

async function openSession(id) {
  current = await api(`/sessions/${id}`);
  output.replaceChildren();
  line(`SESSION "${current.title || 'UNTITLED'}" LOADED, ${current.questions.length} QUESTION(S) ON FILE.`, 'dim');
  for (const question of current.questions) {
    line(`> ${question.text}`, 'user');
    await renderAnswer(question, false);
  }
  renderSessionList();
}

async function newSession() {
  current = await api('/sessions', { method: 'POST', body: {} });
  output.replaceChildren();
  line('NEW SESSION OPENED. ASK A QUESTION.', 'dim');
  await loadSessions();
}

async function deleteSession() {
  if (!current) {
    line('NO SESSION IS OPEN.', 'error');
    return;
  }
  await api(`/sessions/${current.id}`, { method: 'DELETE' });
  current = null;
  output.replaceChildren();
  line('SESSION DELETED.', 'dim');
  await loadSessions();
}

async function ask(text) {
  const startedSession = !current;
  if (startedSession) {
    current = await api('/sessions', { method: 'POST', body: {} });
  }
  line(`> ${text}`, 'user');
  const scanning = line('SCANNING WIKIPEDIA', 'dim blink');
  try {
    const question = await api(`/sessions/${current.id}/questions`, { method: 'POST', body: { text } })
      .finally(() => scanning.remove());
    await renderAnswer(question, !reducedMotion);
  } catch (error) {
    if (startedSession) {
      await api(`/sessions/${current.id}`, { method: 'DELETE' }).catch(() => {});
      current = null;
    }
    throw error;
  } finally {
    await loadSessions();
  }
}

async function handle(text) {
  switch (text.toLowerCase()) {
    case '/help':
      HELP.forEach((entry) => line(entry, 'dim'));
      return;
    case '/new':
      return newSession();
    case '/delete':
      return deleteSession();
    case '/clear':
      output.replaceChildren();
      return;
    default:
      if (text.startsWith('/')) {
        line(`UNKNOWN COMMAND ${text}. TYPE /help.`, 'error');
        return;
      }
      return ask(text);
  }
}

async function run(action) {
  input.disabled = true;
  try {
    await action();
  } catch (error) {
    line(error.message, 'error');
  } finally {
    input.disabled = false;
    input.focus();
  }
}

form.addEventListener('submit', (event) => {
  event.preventDefault();
  const text = input.value.trim();
  if (!text) {
    return;
  }
  inputHistory.push(text);
  historyIndex = inputHistory.length;
  input.value = '';
  run(() => handle(text));
});

input.addEventListener('keydown', (event) => {
  if (event.key !== 'ArrowUp' && event.key !== 'ArrowDown') {
    return;
  }
  event.preventDefault();
  historyIndex = Math.max(0, Math.min(inputHistory.length, historyIndex + (event.key === 'ArrowUp' ? -1 : 1)));
  input.value = inputHistory[historyIndex] ?? '';
});

newSessionButton.addEventListener('click', () => run(newSession));

async function boot() {
  line('RETROSCAN.AI RESEARCH TERMINAL v2.0', 'dim');
  const health = await fetch('/health').then((response) => response.text()).catch(() => 'Unreachable');
  line(`DATABASE ........... ${health === 'Healthy' ? 'OK' : health.toUpperCase()}`, health === 'Healthy' ? 'dim' : 'error');
  await loadSessions();
  line(`SESSIONS ON FILE ... ${sessions.length}`, 'dim');
  line('TYPE A QUESTION TO START A SESSION, OR /help FOR COMMANDS.', 'dim');
}

run(boot);
