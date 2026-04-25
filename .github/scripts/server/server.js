// .github/scripts/server/server.js
// Zero external dependencies — Node.js built-ins only.
// Config comes from environment variables set by the launcher script:
//   SERVER_PORT      — starting port to try (default 3737)
//   SCREEN_DIR       — absolute path to watch for HTML fragment files
//   STATE_DIR        — absolute path to write events file
//   UI_HTML_PATH     — absolute path to ui.html (loaded once at startup)

'use strict';

const http = require('http');
const fs   = require('fs');
const path = require('path');
const url  = require('url');

// ---------------------------------------------------------------------------
// Config
// ---------------------------------------------------------------------------
const SCREEN_DIR   = process.env.SCREEN_DIR;
const STATE_DIR    = process.env.STATE_DIR;
const UI_HTML_PATH = process.env.UI_HTML_PATH;
const START_PORT   = parseInt(process.env.SERVER_PORT || '3737', 10);

if (!SCREEN_DIR || !STATE_DIR || !UI_HTML_PATH) {
  console.error('Missing required env vars: SCREEN_DIR, STATE_DIR, UI_HTML_PATH');
  process.exit(1);
}

// ---------------------------------------------------------------------------
// Load ui.html once at startup (embed as string — no per-request disk reads)
// ---------------------------------------------------------------------------
let UI_HTML;
try {
  UI_HTML = fs.readFileSync(UI_HTML_PATH, 'utf8');
} catch (e) {
  console.error('Cannot read ui.html:', e.message);
  process.exit(1);
}

// ---------------------------------------------------------------------------
// Track the latest screen file
// ---------------------------------------------------------------------------
let latestScreen = null; // { filename, html }

function scanForLatestScreen() {
  try {
    const files = fs.readdirSync(SCREEN_DIR)
      .filter(f => f.endsWith('.html'))
      .map(f => ({
        name: f,
        mtime: fs.statSync(path.join(SCREEN_DIR, f)).mtimeMs,
      }))
      .sort((a, b) => b.mtime - a.mtime);

    if (files.length === 0) {
      latestScreen = null;
      return null;
    }

    const newest = files[0];
    const html = fs.readFileSync(path.join(SCREEN_DIR, newest.name), 'utf8');
    latestScreen = { filename: newest.name, html };
    return latestScreen;
  } catch {
    return null;
  }
}

// Initial scan
scanForLatestScreen();

// ---------------------------------------------------------------------------
// SSE: connected clients
// ---------------------------------------------------------------------------
const sseClients = new Set();

function broadcastSSE(data) {
  const payload = `data: ${JSON.stringify(data)}\n\n`;
  for (const res of sseClients) {
    try { res.write(payload); } catch { sseClients.delete(res); }
  }
}

// ---------------------------------------------------------------------------
// Watch screen_dir for new files
// ---------------------------------------------------------------------------
let watcher;
try {
  watcher = fs.watch(SCREEN_DIR, (eventType, filename) => {
    if (!filename || !filename.endsWith('.html')) return;
    // Small delay so the file is fully written before we read it
    setTimeout(() => {
      const prev = latestScreen ? latestScreen.filename : null;
      const screen = scanForLatestScreen();
      if (screen && screen.filename !== prev) {
        broadcastSSE({ type: 'screen', filename: screen.filename });
      }
    }, 100);
  });
} catch (e) {
  console.error('fs.watch failed:', e.message);
}

// ---------------------------------------------------------------------------
// CORS helper
// ---------------------------------------------------------------------------
function setCORSHeaders(res) {
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');
}

// ---------------------------------------------------------------------------
// Request handler
// ---------------------------------------------------------------------------
function handleRequest(req, res) {
  setCORSHeaders(res);

  if (req.method === 'OPTIONS') {
    res.writeHead(204);
    res.end();
    return;
  }

  const parsed  = url.parse(req.url, true);
  const pathname = parsed.pathname;

  // GET /
  if (req.method === 'GET' && pathname === '/') {
    res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
    res.end(UI_HTML);
    return;
  }

  // GET /health
  if (req.method === 'GET' && pathname === '/health') {
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ status: 'ok' }));
    return;
  }

  // GET /screen — return latest screen or {"html":null}
  if (req.method === 'GET' && pathname === '/screen') {
    res.writeHead(200, { 'Content-Type': 'application/json' });
    if (latestScreen) {
      res.end(JSON.stringify({ html: latestScreen.html, filename: latestScreen.filename }));
    } else {
      res.end(JSON.stringify({ html: null }));
    }
    return;
  }

  // GET /events — SSE stream
  if (req.method === 'GET' && pathname === '/events') {
    res.writeHead(200, {
      'Content-Type':  'text/event-stream',
      'Cache-Control': 'no-cache',
      'Connection':    'keep-alive',
    });
    res.write(': connected\n\n');

    // If there's already a screen loaded, send it immediately so the browser
    // renders on reconnect without waiting for the next file write.
    if (latestScreen) {
      res.write(`data: ${JSON.stringify({ type: 'screen', filename: latestScreen.filename })}\n\n`);
    }

    sseClients.add(res);

    req.on('close', () => sseClients.delete(res));
    return;
  }

  // 404
  res.writeHead(404, { 'Content-Type': 'application/json' });
  res.end(JSON.stringify({ error: 'not found' }));
}

// ---------------------------------------------------------------------------
// Find a free port starting at START_PORT
// ---------------------------------------------------------------------------
function tryListen(server, port, cb) {
  server.once('error', (err) => {
    if (err.code === 'EADDRINUSE') {
      tryListen(server, port + 1, cb);
    } else {
      cb(err);
    }
  });
  server.listen(port, '127.0.0.1', () => cb(null, port));
}

// ---------------------------------------------------------------------------
// Start
// ---------------------------------------------------------------------------
const server = http.createServer(handleRequest);

tryListen(server, START_PORT, (err, port) => {
  if (err) {
    console.error('Server failed to start:', err.message);
    process.exit(1);
  }

  // Output startup JSON — the launcher script parses this line
  const startupInfo = {
    url:        `http://localhost:${port}`,
    screen_dir: SCREEN_DIR,
    state_dir:  STATE_DIR,
  };
  process.stdout.write(JSON.stringify(startupInfo) + '\n');
});

// ---------------------------------------------------------------------------
// Graceful shutdown
// ---------------------------------------------------------------------------
function shutdown() {
  if (watcher) try { watcher.close(); } catch {}
  server.close(() => process.exit(0));
  setTimeout(() => process.exit(0), 3000);
}
process.on('SIGTERM', shutdown);
process.on('SIGINT',  shutdown);
