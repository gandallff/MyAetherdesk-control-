import { WebSocketServer, WebSocket } from 'ws';
import http from 'http';
import fs from 'fs';
import path from 'path';
import { CONFIG } from './config';
import { SessionManager } from './session_manager';
import { WebSocketHandler } from './websocket_handler';

// In-Memory Cloud Frame & Event Buffer per Session
const screenBuffers = new Map<string, { buffer: Buffer; updatedAt: number }>();
const pendingEvents = new Map<string, Array<{ x: number; y: number; sw: number; sh: number; action: string; key?: string; text?: string }>>();
const activeSessions = new Map<string, { lastSeen: number; ip: string; mode: string }>();

const server = http.createServer((req, res) => {
  // CORS Headers for Vercel & Web Clients
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, PUT, DELETE, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', '*');

  if (req.method === 'OPTIONS') {
    res.writeHead(200);
    res.end();
    return;
  }

  const url = new URL(req.url || '/', `http://${req.headers.host}`);
  const pathname = url.pathname.toLowerCase();

  // 1. Health Check
  if (pathname === '/health' || pathname === '/') {
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({
      status: 'ok',
      service: 'AetherDesk Cloud Signaling & Screen Relay Server',
      activeSessionsCount: activeSessions.size,
      uptime: process.uptime()
    }));
    return;
  }

  // 2. Host Agent uploads live screen frame (POST /api/stream/:sessionId)
  if (pathname.startsWith('/api/stream/')) {
    const sessionId = pathname.replace('/api/stream/', '').replace(/[\s\-]/g, '');
    const clientIp = req.socket.remoteAddress || 'unknown';

    const chunks: Buffer[] = [];
    req.on('data', (chunk) => chunks.push(chunk));
    req.on('end', () => {
      const fullBuffer = Buffer.concat(chunks);
      if (fullBuffer.length > 0) {
        screenBuffers.set(sessionId, { buffer: fullBuffer, updatedAt: Date.now() });
        activeSessions.set(sessionId, { lastSeen: Date.now(), ip: clientIp, mode: 'STREAMING' });
      }
      res.writeHead(200, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({ ok: true, size: fullBuffer.length }));
    });
    return;
  }

  // 3. Web Viewer fetches live screen frame (GET /api/screen/:sessionId)
  if (pathname.startsWith('/api/screen/')) {
    const sessionId = pathname.replace('/api/screen/', '').replace(/[\s\-]/g, '');
    const screenData = screenBuffers.get(sessionId);

    if (screenData && screenData.buffer) {
      res.writeHead(200, {
        'Content-Type': 'image/jpeg',
        'Cache-Control': 'no-cache, no-store, must-revalidate',
        'Content-Length': screenData.buffer.length
      });
      res.end(screenData.buffer);
    } else {
      res.writeHead(404, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({ error: 'No screen frame available for session: ' + sessionId }));
    }
    return;
  }

  // 4. Web Viewer sends mouse action (GET or POST /api/mouse/:sessionId)
  if (pathname.startsWith('/api/mouse/')) {
    const sessionId = pathname.replace('/api/mouse/', '').replace(/[\s\-]/g, '');
    const x = parseInt(url.searchParams.get('x') || '0', 10);
    const y = parseInt(url.searchParams.get('y') || '0', 10);
    const sw = parseInt(url.searchParams.get('sw') || '1920', 10);
    const sh = parseInt(url.searchParams.get('sh') || '1080', 10);
    const action = url.searchParams.get('action') || 'click';

    if (!pendingEvents.has(sessionId)) {
      pendingEvents.set(sessionId, []);
    }
    pendingEvents.get(sessionId)!.push({ x, y, sw, sh, action });
    console.log(`[Mouse Event Queued] Session: ${sessionId}, Action: ${action}, (${x}, ${y}) / (${sw}, ${sh})`);

    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ ok: true, queueLength: pendingEvents.get(sessionId)!.length }));
    return;
  }

  // 5. Web Viewer sends keyboard key (GET or POST /api/keyboard/:sessionId)
  if (pathname.startsWith('/api/keyboard/')) {
    const sessionId = pathname.replace('/api/keyboard/', '').replace(/[\s\-]/g, '');
    const key = url.searchParams.get('key') || '';
    const text = url.searchParams.get('text') || '';

    if (!pendingEvents.has(sessionId)) {
      pendingEvents.set(sessionId, []);
    }
    pendingEvents.get(sessionId)!.push({ x: 0, y: 0, sw: 0, sh: 0, action: 'key', key, text });
    console.log(`[Keyboard Event Queued] Session: ${sessionId}, Key: ${key}`);

    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ ok: true }));
    return;
  }

  // 6. Host Agent polls pending input events (GET /api/events/:sessionId)
  if (pathname.startsWith('/api/events/')) {
    const sessionId = pathname.replace('/api/events/', '').replace(/[\s\-]/g, '');
    const events = pendingEvents.get(sessionId) || [];
    pendingEvents.set(sessionId, []); // clear queue

    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ events }));
    return;
  }

  // 7. Active Sessions Discovery (GET /api/sessions)
  if (pathname === '/api/sessions') {
    const now = Date.now();
    const list: any[] = [];
    activeSessions.forEach((info, sid) => {
      if (now - info.lastSeen < 30000) {
        list.push({ sessionId: sid, ...info });
      }
    });
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ sessions: list }));
    return;
  }

  // 8. Direct Agent Downloads
  if (pathname === '/download/agent' || pathname === '/download/agent.exe') {
    const pathsToTry = [
      path.join(__dirname, '../../saas-portal/frontend/public/aetherdesk-agent.exe'),
      path.join(__dirname, '../../web-viewer/public/aetherdesk-agent.exe')
    ];

    for (const p of pathsToTry) {
      if (fs.existsSync(p)) {
        res.writeHead(200, {
          'Content-Type': 'application/octet-stream',
          'Content-Disposition': 'attachment; filename="aetherdesk-agent.exe"'
        });
        fs.createReadStream(p).pipe(res);
        return;
      }
    }
  }

  res.writeHead(404, { 'Content-Type': 'application/json' });
  res.end(JSON.stringify({ error: 'Not Found' }));
});

const wss = new WebSocketServer({ server });
const sessionManager = new SessionManager();
const handler = new WebSocketHandler(sessionManager);

wss.on('connection', (ws, req) => {
  const ip = req.socket.remoteAddress;
  console.log(`[WebSocket Connected] IP: ${ip}`);
  handler.handleConnection(ws);
});

const PORT = process.env.PORT || CONFIG.PORT || 8080;

server.listen(PORT, () => {
  console.log(`=======================================================`);
  console.log(`  🚀 AetherDesk Cloud Signaling & Relay Server Active`);
  console.log(`  Port: ${PORT}`);
  console.log(`=======================================================`);
});
