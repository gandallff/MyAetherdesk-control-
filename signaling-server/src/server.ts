import { WebSocketServer } from 'ws';
import http from 'http';
import fs from 'fs';
import path from 'path';
import { CONFIG } from './config';
import { SessionManager } from './session_manager';
import { WebSocketHandler } from './websocket_handler';

const server = http.createServer((req, res) => {
  // 1. Health check endpoint
  if (req.url === '/health') {
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ status: 'ok', uptime: process.uptime() }));
    return;
  }

  // 2. Direct Web Download Endpoints for Remote PCs
  if (req.url === '/download/agent' || req.url === '/download/agent.exe') {
    const filePath = path.join(__dirname, '../../Install-AetherDesk-Service.bat');
    res.writeHead(200, {
      'Content-Type': 'application/octet-stream',
      'Content-Disposition': 'attachment; filename="AetherDesk-Installer.bat"'
    });
    if (fs.existsSync(filePath)) {
      fs.createReadStream(filePath).pipe(res);
    } else {
      res.end('@echo off\necho Starting AetherDesk Agent...\n');
    }
    return;
  }

  if (req.url === '/download/portable') {
    const filePath = path.join(__dirname, '../../Run-AetherDesk-Portable.bat');
    res.writeHead(200, {
      'Content-Type': 'application/octet-stream',
      'Content-Disposition': 'attachment; filename="AetherDesk-Portable.bat"'
    });
    if (fs.existsSync(filePath)) {
      fs.createReadStream(filePath).pipe(res);
    } else {
      res.end('@echo off\necho Launching AetherDesk Portable...\n');
    }
    return;
  }

  res.writeHead(404);
  res.end('Not Found');
});

const wss = new WebSocketServer({ server });
const sessionManager = new SessionManager();
const handler = new WebSocketHandler(sessionManager);

wss.on('connection', (ws, req) => {
  const ip = req.socket.remoteAddress;
  console.log(`[Connection Established] Remote IP: ${ip}`);
  handler.handleConnection(ws);
});

server.listen(CONFIG.PORT, () => {
  console.log(`=======================================================`);
  console.log(`  🚀 AetherDesk Signaling & Download Server Active`);
  console.log(`  Port: ${CONFIG.PORT}`);
  console.log(`  WebSocket URL: ws://localhost:${CONFIG.PORT}`);
  console.log(`  Agent Download: http://localhost:${CONFIG.PORT}/download/agent`);
  console.log(`=======================================================`);
});
