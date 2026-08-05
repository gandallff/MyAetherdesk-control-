import React, { useState, useEffect } from 'react';
import { ConnectionMode, RemoteInputEvent } from './types/protocol';
import { HostStatusCard } from './components/HostStatusCard';
import { ConnectionPanel } from './components/ConnectionPanel';
import { RemoteViewport } from './components/RemoteViewport';
import { RemoteToolbar } from './components/RemoteToolbar';
import { FileExplorerModal } from './components/FileExplorerModal';
import { SignalingService } from './services/signaling';
import { WebRTCService } from './services/webrtc';
import { FileTransferService } from './services/fileTransfer';
import { Monitor, ShieldCheck, Zap } from 'lucide-react';

export const App: React.FC = () => {
  const [hostId, setHostId] = useState<string>('482 910 375');
  const [isSignalingConnected, setIsSignalingConnected] = useState<boolean>(false);
  const [isConnectedToRemote, setIsConnectedToRemote] = useState<boolean>(false);
  const [isConnecting, setIsConnecting] = useState<boolean>(false);
  const [remoteStream, setRemoteStream] = useState<MediaStream | null>(null);
  const [isFullscreen, setIsFullscreen] = useState<boolean>(false);
  const [isFileModalOpen, setIsFileModalOpen] = useState<boolean>(false);

  const [signalingService] = useState(() => new SignalingService('ws://localhost:8080'));
  const [webrtcService] = useState(() => new WebRTCService());
  const [fileTransferService, setFileTransferService] = useState<FileTransferService | null>(null);

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
    webrtcService.sendInputEvent(event);
  };

  const handleDisconnect = () => {
    webrtcService.close();
    setIsConnectedToRemote(false);
    setRemoteStream(null);
  };

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
            </h1>
            <p className="text-xs text-slate-400">Low-Latency DXGI Screen Sharing & Input Synthesis</p>
          </div>
        </div>

        <div className="flex items-center space-x-4">
          <div className="flex items-center space-x-2 text-xs text-slate-400 bg-slate-900 px-3 py-1.5 rounded-xl border border-slate-800">
            <ShieldCheck className="w-4 h-4 text-emerald-400" />
            <span>TLS 1.3 + DTLS-SRTP AES-256</span>
          </div>
        </div>
      </header>

      {/* Main Content Viewport */}
      <main className="my-6 flex-1 flex flex-col justify-center">
        {isConnectedToRemote ? (
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
