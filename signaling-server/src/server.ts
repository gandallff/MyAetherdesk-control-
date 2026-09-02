import { WebSocketServer, WebSocket } from 'ws';
import http from 'http';
import fs from 'fs';
import path from 'path';
import { CONFIG } from './config';
import { SessionManager } from './session_manager';
import { WebSocketHandler } from './websocket_handler';

// In-Memory Cloud Frame, Event Buffer, File Store, and User Database
const screenBuffers = new Map<string, { buffer: Buffer; updatedAt: number }>();
const pendingEvents = new Map<string, Array<{ x: number; y: number; sw: number; sh: number; action: string; key?: string; text?: string }>>();
const activeSessions = new Map<string, { lastSeen: number; ip: string; mode: string }>();
const transferredFiles = new Map<string, { filename: string; buffer: Buffer; timestamp: number }>();

interface RegisteredUser {
  id: string;
  name: string;
  email: string;
  password?: string;
  provider?: string;
  devices: string[];
  createdAt: number;
}
const registeredUsers = new Map<string, RegisteredUser>();

// Pre-populate default admin user
registeredUsers.set('admin@aetherdesk.com', {
  id: 'usr_admin',
  name: 'AetherDesk Yöneticisi',
  email: 'admin@aetherdesk.com',
  password: 'admin',
  devices: ['212614962', '482910375'],
  createdAt: Date.now()
});

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
      service: 'AetherDesk Cloud Relay & Auth Hub',
      activeSessionsCount: activeSessions.size,
      registeredUsersCount: registeredUsers.size,
      uptime: process.uptime()
    }));
    return;
  }

  // 1.1 Cloud Auth: Register User & Link Device
  if (pathname === '/api/auth/register' && req.method === 'POST') {
    const chunks: Buffer[] = [];
    req.on('data', (c) => chunks.push(c));
    req.on('end', () => {
      try {
        const body = JSON.parse(Buffer.concat(chunks).toString());
        const email = (body.email || '').trim().toLowerCase();
        const name = body.name || email.split('@')[0];
        const password = body.password || 'default123';
        const deviceId = (body.deviceId || '').replace(/[\s\-]/g, '');

        if (!email || !email.includes('@')) {
          res.writeHead(400, { 'Content-Type': 'application/json' });
          res.end(JSON.stringify({ error: 'Geçerli bir e-posta adresi gerekli' }));
          return;
        }

        let user = registeredUsers.get(email);
        if (!user) {
          user = {
            id: 'usr_' + Math.random().toString(36).substr(2, 9),
            name,
            email,
            password,
            devices: deviceId ? [deviceId] : [],
            createdAt: Date.now()
          };
          registeredUsers.set(email, user);
        } else {
          if (deviceId && !user.devices.includes(deviceId)) {
            user.devices.push(deviceId);
          }
        }

        res.writeHead(200, { 'Content-Type': 'application/json' });
        res.end(JSON.stringify({
          ok: true,
          message: 'Kayıt başarılı',
          user: {
            id: user.id,
            name: user.name,
            email: user.email,
            devices: user.devices
          }
        }));
      } catch (e: any) {
        res.writeHead(400, { 'Content-Type': 'application/json' });
        res.end(JSON.stringify({ error: 'Geçersiz veri: ' + e.message }));
      }
    });
    return;
  }

  // 1.2 Cloud Auth: Login & Link Device
  if (pathname === '/api/auth/login' && req.method === 'POST') {
    const chunks: Buffer[] = [];
    req.on('data', (c) => chunks.push(c));
    req.on('end', () => {
      try {
        const body = JSON.parse(Buffer.concat(chunks).toString());
        const email = (body.email || '').trim().toLowerCase();
        const password = body.password || '';
        const deviceId = (body.deviceId || '').replace(/[\s\-]/g, '');

        let user = registeredUsers.get(email);
        if (!user) {
          // Auto-provision if valid email provided
          user = {
            id: 'usr_' + Math.random().toString(36).substr(2, 9),
            name: email.split('@')[0],
            email,
            password,
            devices: deviceId ? [deviceId] : [],
            createdAt: Date.now()
          };
          registeredUsers.set(email, user);
        } else {
          if (deviceId && !user.devices.includes(deviceId)) {
            user.devices.push(deviceId);
          }
        }

        res.writeHead(200, { 'Content-Type': 'application/json' });
        res.end(JSON.stringify({
          ok: true,
          user: {
            id: user.id,
            name: user.name,
            email: user.email,
            devices: user.devices
          }
        }));
      } catch (e: any) {
        res.writeHead(400, { 'Content-Type': 'application/json' });
        res.end(JSON.stringify({ error: e.message }));
      }
    });
    return;
  }

  // 1.3 Cloud Auth: SSO (Google / Microsoft / Apple)
  if (pathname === '/api/auth/sso' && req.method === 'POST') {
    const chunks: Buffer[] = [];
    req.on('data', (c) => chunks.push(c));
    req.on('end', () => {
      try {
        const body = JSON.parse(Buffer.concat(chunks).toString());
        const provider = body.provider || 'Google';
        const email = (body.email || `${provider.toLowerCase()}.user@aetherdesk.com`).trim().toLowerCase();
        const name = body.name || `${provider} Kullanıcısı`;
        const deviceId = (body.deviceId || '').replace(/[\s\-]/g, '');

        let user = registeredUsers.get(email);
        if (!user) {
          user = {
            id: 'usr_' + Math.random().toString(36).substr(2, 9),
            name,
            email,
            provider,
            devices: deviceId ? [deviceId] : [],
            createdAt: Date.now()
          };
          registeredUsers.set(email, user);
        } else {
          if (deviceId && !user.devices.includes(deviceId)) {
            user.devices.push(deviceId);
          }
        }

        res.writeHead(200, { 'Content-Type': 'application/json' });
        res.end(JSON.stringify({
          ok: true,
          provider,
          user: {
            id: user.id,
            name: user.name,
            email: user.email,
            devices: user.devices
          }
        }));
      } catch (e: any) {
        res.writeHead(400, { 'Content-Type': 'application/json' });
        res.end(JSON.stringify({ error: e.message }));
      }
    });
    return;
  }

  // 1.4 Get User Registered Devices
  if (pathname.startsWith('/api/user/devices/')) {
    const email = decodeURIComponent(pathname.replace('/api/user/devices/', '')).trim().toLowerCase();
    const user = registeredUsers.get(email);
    if (user) {
      res.writeHead(200, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({ ok: true, email: user.email, devices: user.devices }));
    } else {
      res.writeHead(404, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({ error: 'Kullanıcı bulunamadı', devices: [] }));
    }
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
