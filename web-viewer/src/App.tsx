import React, { useState, useEffect } from 'react';
import { ConnectionMode, RemoteInputEvent } from './types/protocol';
import { HostStatusCard } from './components/HostStatusCard';
import { ConnectionPanel } from './components/ConnectionPanel';
import { RemoteViewport } from './components/RemoteViewport';
import { RemoteToolbar } from './components/RemoteToolbar';
import { FileExplorerModal } from './components/FileExplorerModal';
import { MultiRemoteViewport } from './components/MultiRemoteViewport';
import { SignalingService } from './services/signaling';
import { WebRTCService } from './services/webrtc';
import { FileTransferService } from './services/fileTransfer';
import { Monitor, ShieldCheck, Zap, LayoutGrid, ArrowLeft } from 'lucide-react';

interface MonitorSession {
  targetId: string;
  name?: string;
  directIp?: string;
  directPort?: number;
  connectionMode: 'DIRECT_IP' | 'SIGNALING_ID';
  isConnected: boolean;
  isConnecting: boolean;
  stream: MediaStream | null;
  webrtcService: WebRTCService;
  isInteractive: boolean;
}

export const App: React.FC = () => {
  const [hostId, setHostId] = useState<string>('482 910 375');
  const [isSignalingConnected, setIsSignalingConnected] = useState<boolean>(false);
  const [isConnectedToRemote, setIsConnectedToRemote] = useState<boolean>(false);
  const [isConnecting, setIsConnecting] = useState<boolean>(false);
  const [remoteStream, setRemoteStream] = useState<MediaStream | null>(null);
  const [isFullscreen, setIsFullscreen] = useState<boolean>(false);
  const [isFileModalOpen, setIsFileModalOpen] = useState<boolean>(false);

  const [signalingService] = useState(() => new SignalingService((import.meta as any).env?.VITE_SIGNALING_URL || 'ws://localhost:8080'));
  const [webrtcService] = useState(() => new WebRTCService());
  const [fileTransferService, setFileTransferService] = useState<FileTransferService | null>(null);

  // ── Multi-Monitor states ──────────────────────────────────────────────────
  const [isMonitoring, setIsMonitoring] = useState<boolean>(false);
  const [sessions, setSessions] = useState<MonitorSession[]>([]);
  const [focusedSessionId, setFocusedSessionId] = useState<string | null>(null);

  useEffect(() => {
    // 1. Connect to WebSocket Signaling Server
    signalingService
      .connect()
      .then(() => {
        setIsSignalingConnected(true);
        signalingService.send({ type: 'REGISTER_HOST' });
      })
      .catch(() => {
        console.warn('Signaling server unavailable - operating in local preview mode');
        setIsSignalingConnected(false);
      });

    // 2. Register signaling listeners
    signalingService.on('HOST_REGISTERED', (msg) => {
      if (msg.payload?.formattedId) {
        setHostId(msg.payload.formattedId);
      }
    });

    signalingService.on('SDP_OFFER', async (msg) => {
      if (msg.payload) {
        webrtcService.initConnection();
        await webrtcService.handleAnswer(msg.payload);
      }
    });

    return () => {
      signalingService.disconnect();
    };
  }, []);

  // ── Helper to start simulated feed based on connection routing ────────────
  const startMockFeed = (session: { targetId: string; name?: string; directIp?: string; directPort?: number }, mode: 'DIRECT_IP' | 'SIGNALING_ID', delay: number) => {
    setTimeout(() => {
      setSessions(prev => prev.map(s => {
        if (s.targetId === session.targetId) {
          let mockStream: MediaStream | null = null;
          try {
            const canvas = document.createElement('canvas');
            canvas.width = 640;
            canvas.height = 480;
            const ctx = canvas.getContext('2d');
            if (ctx) {
              let frame = 0;
              
              const draw = () => {
                ctx.fillStyle = '#0b0f19';
                ctx.fillRect(0, 0, 640, 480);
                
                // Draw grid
                ctx.strokeStyle = 'rgba(59, 130, 246, 0.08)';
                ctx.lineWidth = 1;
                for (let x = 0; x < 640; x += 40) {
                  ctx.beginPath(); ctx.moveTo(x, 0); ctx.lineTo(x, 480); ctx.stroke();
                }
                for (let y = 0; y < 480; y += 40) {
                  ctx.beginPath(); ctx.moveTo(0, y); ctx.lineTo(640, y); ctx.stroke();
                }

                // Header
                ctx.fillStyle = '#60a5fa';
                ctx.font = 'bold 18px monospace';
                ctx.fillText(`AETHERDESK CONTROL CONSOLE`, 40, 60);
                
                // Connection specifications
                ctx.font = '12px monospace';
                ctx.fillStyle = '#94a3b8';
                ctx.fillText(`Device Name  : ${session.name}`, 40, 100);
                ctx.fillText(`Session ID   : ${session.targetId}`, 40, 125);
                ctx.fillText(`Connection   : ${mode === 'DIRECT_IP' ? '🏠 Local LAN (Direct IP)' : '☁️ Cloud WAN (Relayed)'}`, 40, 150);
                ctx.fillText(`IP:Port Link : ${mode === 'DIRECT_IP' ? `${session.directIp || '192.168.1.100'}:${session.directPort || '8443'}` : 'relayed_tunnel:8080'}`, 40, 175);
                ctx.fillText(`Latency Specs: ${mode === 'DIRECT_IP' ? '1ms ~ 2ms RTT (Eth 1Gbps)' : '32ms ~ 45ms RTT (Internet Link)'}`, 40, 200);
                ctx.fillText(`Frames Sync  : ${frame}`, 40, 225);

                // Dynamic sine wave
                ctx.strokeStyle = mode === 'DIRECT_IP' ? '#10b981' : '#f59e0b';
                ctx.lineWidth = 2;
                ctx.beginPath();
                for (let x = 40; x < 600; x++) {
                  const y = 320 + Math.sin((x + frame) * (mode === 'DIRECT_IP' ? 0.08 : 0.04)) * 35;
                  if (x === 40) ctx.moveTo(x, y);
                  else ctx.lineTo(x, y);
                }
                ctx.stroke();

                // Live box indicator
                ctx.fillStyle = '#3b82f6';
                ctx.fillRect(40 + (frame % 520), 395, 12, 12);

                frame++;
                requestAnimationFrame(draw);
              };
              draw();
              // @ts-ignore
              mockStream = canvas.captureStream(30);
            }
          } catch (e) {
            console.error(e);
          }

          return {
            ...s,
            isConnected: true,
            isConnecting: false,
            stream: s.stream || mockStream
          };
        }
        return s;
      }));
    }, delay);
  };

  // ── Multi-PC Monitoring connection loop ───────────────────────────────────
  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const monitoringParam = params.get('monitoring');
    const targetsParam = params.get('targets');

    if (monitoringParam === 'true' && targetsParam) {
      setIsMonitoring(true);
      const targetList = targetsParam.split(',').map(item => {
        const [targetId, name, directIp, directPort] = item.split(':');
        return {
          targetId,
          name: name ? decodeURIComponent(name) : `Remote PC (${targetId})`,
          directIp: directIp || undefined,
          directPort: directPort ? parseInt(directPort) : undefined
        };
      });

      const initialSessions = targetList.map(t => {
        const webrtc = new WebRTCService();
        // Default to Direct IP if LAN config is present, otherwise fallback to Signaling ID
        const mode: 'DIRECT_IP' | 'SIGNALING_ID' = t.directIp ? 'DIRECT_IP' : 'SIGNALING_ID';

        return {
          targetId: t.targetId,
          name: t.name,
          directIp: t.directIp,
          directPort: t.directPort,
          connectionMode: mode,
          isConnected: false,
          isConnecting: true,
          stream: null,
          webrtcService: webrtc,
          isInteractive: false
        };
      });

      setSessions(initialSessions);

      // Connect each monitoring target
      initialSessions.forEach(session => {
        if (session.connectionMode === 'SIGNALING_ID') {
          signalingService.send({
            type: 'CONNECT_REQUEST',
            targetId: session.targetId,
            payload: {}
          });
        }

        session.webrtcService.initConnection();
        session.webrtcService.onTrack((stream) => {
          setSessions(prev => prev.map(s => {
            if (s.targetId === session.targetId) {
              return { ...s, stream };
            }
            return s;
          }));
        });

        startMockFeed(session, session.connectionMode, 1200 + Math.random() * 800);
      });
    }
  }, [isSignalingConnected]);

  const handleConnect = async (mode: ConnectionMode, target: string, password?: string) => {
    setIsConnecting(true);

    if (mode === 'SIGNALING_ID') {
      signalingService.send({
        type: 'CONNECT_REQUEST',
        targetId: target,
        payload: { password }
      });
    }

    // Initialize WebRTC P2P Session
    webrtcService.initConnection();
    webrtcService.onTrack((stream) => {
      setRemoteStream(stream);
    });

    webrtcService.onDataChannel((dc) => {
      const ft = new FileTransferService(dc);
      setFileTransferService(ft);
    });

    // Simulate P2P Connection Handshake Completion
    setTimeout(() => {
      setIsConnecting(false);
      setIsConnectedToRemote(true);
    }, 1200);
  };

  const handleInputEvent = (event: RemoteInputEvent) => {
    if (focusedSessionId) {
      const session = sessions.find(s => s.targetId === focusedSessionId);
      if (session) session.webrtcService.sendInputEvent(event);
    } else {
      webrtcService.sendInputEvent(event);
    }
  };

  const handleGridInputEvent = (targetId: string, event: RemoteInputEvent) => {
    const session = sessions.find(s => s.targetId === targetId);
    if (session && session.isInteractive) {
      session.webrtcService.sendInputEvent(event);
    }
  };

  const handleDisconnect = () => {
    webrtcService.close();
    setIsConnectedToRemote(false);
    setRemoteStream(null);
  };

  // ── Multi session actions ─────────────────────────────────────────────────
  const handleDisconnectSession = (targetId: string) => {
    setSessions(prev => {
      const target = prev.find(s => s.targetId === targetId);
      if (target) target.webrtcService.close();
      return prev.filter(s => s.targetId !== targetId);
    });
    if (focusedSessionId === targetId) {
      setFocusedSessionId(null);
    }
  };

  const handleToggleInteractive = (targetId: string) => {
    setSessions(prev => prev.map(s => {
      if (s.targetId === targetId) {
        return { ...s, isInteractive: !s.isInteractive };
      }
      return { ...s, isInteractive: false }; // Disable others to avoid input chaos
    }));
  };

  // Switch connection route between local direct IP socket and Signaling server relay
  const handleChangeRoute = (targetId: string, mode: 'DIRECT_IP' | 'SIGNALING_ID') => {
    setSessions(prev => prev.map(s => {
      if (s.targetId === targetId) {
        // Disconnect previous session channel
        s.webrtcService.close();

        // Create new session channel
        const nextWebrtc = new WebRTCService();
        nextWebrtc.initConnection();

        if (mode === 'SIGNALING_ID') {
          signalingService.send({
            type: 'CONNECT_REQUEST',
            targetId: s.targetId,
            payload: {}
          });
        }

        nextWebrtc.onTrack((stream) => {
          setSessions(prevSessions => prevSessions.map(ps => 
            ps.targetId === targetId ? { ...ps, stream } : ps
          ));
        });

        // Trigger reconnecting view state
        startMockFeed(s, mode, 1000);

        return {
          ...s,
          connectionMode: mode,
          isConnected: false,
          isConnecting: true,
          stream: null,
          webrtcService: nextWebrtc
        };
      }
      return s;
    }));
  };

  const focusedSession = sessions.find(s => s.targetId === focusedSessionId);

  return (
    <div className="min-h-screen bg-[#0b0f19] text-slate-100 flex flex-col justify-between p-4 md:p-8">
      {/* Top Header Navigation */}
      <header className="flex items-center justify-between pb-6 border-b border-slate-800/80">
        <div className="flex items-center space-x-3">
          <div className="p-2.5 bg-blue-600 rounded-xl shadow-lg shadow-blue-500/30 text-white font-bold text-lg flex items-center justify-center">
            <Zap className="w-5 h-5" />
          </div>
          <div>
            <h1 className="text-xl font-bold tracking-tight text-white flex items-center space-x-2">
              <span>AetherDesk</span>
              <span className="text-[10px] uppercase tracking-widest bg-blue-500/20 text-blue-400 font-semibold px-2 py-0.5 rounded-full border border-blue-500/30">
                PRO 2026
              </span>
              {isMonitoring && (
                <span className="text-[10px] uppercase tracking-widest bg-emerald-500/20 text-emerald-400 font-bold px-2 py-0.5 rounded-full border border-emerald-500/30 flex items-center space-x-1">
                  <LayoutGrid className="w-3 h-3" />
                  <span>GRID MONITOR</span>
                </span>
              )}
            </h1>
            <p className="text-xs text-slate-400">Low-Latency DXGI Screen Sharing & Input Synthesis</p>
          </div>
        </div>

        <div className="flex items-center space-x-4">
          {focusedSessionId && (
            <button
              onClick={() => setFocusedSessionId(null)}
              className="px-3 py-1.5 rounded-xl bg-slate-900 hover:bg-slate-800 text-slate-300 font-bold text-xs border border-slate-800 flex items-center space-x-1.5 transition-all shadow-sm"
            >
              <ArrowLeft className="w-3.5 h-3.5" />
              <span>Back to Grid</span>
            </button>
          )}

          <div className="flex items-center space-x-2 text-xs text-slate-400 bg-slate-900 px-3 py-1.5 rounded-xl border border-slate-800">
            <ShieldCheck className="w-4 h-4 text-emerald-400" />
            <span>TLS 1.3 + DTLS-SRTP AES-256</span>
          </div>
        </div>
      </header>

      {/* Main Content Viewport */}
      <main className="my-6 flex-1 flex flex-col justify-center">
        {isMonitoring ? (
          focusedSessionId && focusedSession ? (
            <div className="relative w-full h-[78vh] flex items-center justify-center animate-in fade-in duration-300">
              <RemoteToolbar
                isFullscreen={isFullscreen}
                onToggleFullscreen={() => setIsFullscreen(!isFullscreen)}
                onOpenFileTransfer={() => setIsFileModalOpen(true)}
                onDisconnect={() => setFocusedSessionId(null)}
                onSendMacro={(macro) => console.log('Macro sent:', macro)}
              />
              <RemoteViewport
                stream={focusedSession.stream}
                onInputEvent={handleInputEvent}
                isFullscreen={isFullscreen}
              />
            </div>
          ) : (
            <div className="w-full min-h-[75vh] flex flex-col">
              <MultiRemoteViewport
                sessions={sessions}
                onDisconnect={handleDisconnectSession}
                onToggleInteractive={handleToggleInteractive}
                onInputEvent={handleGridInputEvent}
                onFocusSession={(id) => setFocusedSessionId(id)}
                onChangeRoute={handleChangeRoute}
              />
            </div>
          )
        ) : isConnectedToRemote ? (
          <div className="relative w-full h-[78vh] flex items-center justify-center">
            <RemoteToolbar
              isFullscreen={isFullscreen}
              onToggleFullscreen={() => setIsFullscreen(!isFullscreen)}
              onOpenFileTransfer={() => setIsFileModalOpen(true)}
              onDisconnect={handleDisconnect}
              onSendMacro={(macro) => console.log('Macro sent:', macro)}
            />
            <RemoteViewport
              stream={remoteStream}
              onInputEvent={handleInputEvent}
              isFullscreen={isFullscreen}
            />
          </div>
        ) : (
          <div className="max-w-4xl mx-auto w-full grid grid-cols-1 md:grid-cols-2 gap-6 items-stretch">
            <HostStatusCard hostId={hostId} isOnline={isSignalingConnected} />
            <ConnectionPanel onConnect={handleConnect} isConnecting={isConnecting} />
          </div>
        )}
      </main>

      {/* File Explorer Modal */}
      <FileExplorerModal
        isOpen={isFileModalOpen}
        onClose={() => setIsFileModalOpen(false)}
        fileTransferService={fileTransferService}
      />

      {/* Footer Status Bar */}
      <footer className="pt-4 border-t border-slate-800/60 flex items-center justify-between text-xs text-slate-500 font-mono">
        <div>AetherDesk Engine v1.0.0 | DXGI Desktop Duplication API</div>
        <div>WebSocket Signaling: {isSignalingConnected ? 'Online (ws://localhost:8080)' : 'Offline (Direct IP Mode)'}</div>
      </footer>
    </div>
  );
};
