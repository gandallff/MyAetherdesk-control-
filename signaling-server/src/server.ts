import { WebSocketServer, WebSocket } from 'ws';
import http from 'http';
import fs from 'fs';
import path from 'path';
import { CONFIG } from './config';
import { SessionManager } from './session_manager';
import { WebSocketHandler } from './websocket_handler';

// In-Memory Cloud Frame, Event Buffer, and File Store
const screenBuffers = new Map<string, { buffer: Buffer; updatedAt: number }>();
const pendingEvents = new Map<string, Array<{ x: number; y: number; sw: number; sh: number; action: string; key?: string; text?: string }>>();
const activeSessions = new Map<string, { lastSeen: number; ip: string; mode: string }>();
const transferredFiles = new Map<string, { filename: string; buffer: Buffer; timestamp: number }>();

const server = http.createServer((req, res) => {
  // CORS Headers
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
      service: 'AetherDesk Cloud Relay & Remote File System',
      activeSessionsCount: activeSessions.size,
      uptime: process.uptime()
    }));
    return;
  }

  // 2. Screen Upload
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

  // 3. Screen Fetch
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
      res.end(JSON.stringify({ error: 'No screen frame available' }));
    }
    return;
  }

  // 4. Mouse Event
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

    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ ok: true, queueLength: pendingEvents.get(sessionId)!.length }));
    return;
  }

  // 5. Keyboard Event
  if (pathname.startsWith('/api/keyboard/')) {
    const sessionId = pathname.replace('/api/keyboard/', '').replace(/[\s\-]/g, '');
    const key = url.searchParams.get('key') || '';
    const text = url.searchParams.get('text') || '';

    if (!pendingEvents.has(sessionId)) {
      pendingEvents.set(sessionId, []);
    }
    pendingEvents.get(sessionId)!.push({ x: 0, y: 0, sw: 0, sh: 0, action: 'key', key, text });

    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ ok: true }));
    return;
  }

  // 6. Poll Events
  if (pathname.startsWith('/api/events/')) {
    const sessionId = pathname.replace('/api/events/', '').replace(/[\s\-]/g, '');
    const events = pendingEvents.get(sessionId) || [];
    pendingEvents.set(sessionId, []);

    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ events }));
    return;
  }

  // 7. File Upload (Send File to Remote PC)
  if (pathname.startsWith('/api/file/upload/')) {
    const sessionId = pathname.replace('/api/file/upload/', '').replace(/[\s\-]/g, '');
    const filename = url.searchParams.get('name') || 'Transferred_File.dat';

    const chunks: Buffer[] = [];
    req.on('data', (chunk) => chunks.push(chunk));
    req.on('end', () => {
      const fullBuffer = Buffer.concat(chunks);
      transferredFiles.set(sessionId, { filename, buffer: fullBuffer, timestamp: Date.now() });

      // Notify remote agent about incoming file
      if (!pendingEvents.has(sessionId)) pendingEvents.set(sessionId, []);
      pendingEvents.get(sessionId)!.push({ x: 0, y: 0, sw: 0, sh: 0, action: 'incoming_file', text: filename });

      res.writeHead(200, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({ ok: true, filename, size: fullBuffer.length }));
    });
    return;
  }

  // 8. File Download (Remote Agent downloads incoming file)
  if (pathname.startsWith('/api/file/download/')) {
    const sessionId = pathname.replace('/api/file/download/', '').replace(/[\s\-]/g, '');
    const file = transferredFiles.get(sessionId);

    if (file) {
      res.writeHead(200, {
        'Content-Type': 'application/octet-stream',
        'Content-Disposition': `attachment; filename="${file.filename}"`,
        'Content-Length': file.buffer.length
      });
      res.end(file.buffer);
      transferredFiles.delete(sessionId);
    } else {
      res.writeHead(404, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({ error: 'No pending file transfer for session' }));
    }
    return;
  }

  // 9. Active Sessions
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

  // 10. Agent Downloads
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
  console.log(`  🚀 AetherDesk Cloud Signaling, Stream & File Active`);
  console.log(`  Port: ${PORT}`);
  console.log(`=======================================================`);
});
