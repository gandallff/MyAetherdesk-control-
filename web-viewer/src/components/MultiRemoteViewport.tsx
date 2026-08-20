import React, { useRef, useEffect } from 'react';
import { RemoteInputEvent } from '../types/protocol';
import { WebRTCService } from '../services/webrtc';
import { Monitor, Zap, Power, ShieldAlert, Maximize2, MonitorPlay, Network, Globe } from 'lucide-react';

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

interface MultiRemoteViewportProps {
  sessions: MonitorSession[];
  onDisconnect: (targetId: string) => void;
  onToggleInteractive: (targetId: string) => void;
  onInputEvent: (targetId: string, event: RemoteInputEvent) => void;
  onFocusSession: (targetId: string) => void;
  onChangeRoute: (targetId: string, mode: 'DIRECT_IP' | 'SIGNALING_ID') => void;
}

// ── Single Grid Cell Component ──────────────────────────────────────────────
const GridCellViewport: React.FC<{
  session: MonitorSession;
  onDisconnect: (targetId: string) => void;
  onToggleInteractive: (targetId: string) => void;
  onInputEvent: (targetId: string, event: RemoteInputEvent) => void;
  onFocusSession: (targetId: string) => void;
  onChangeRoute: (targetId: string, mode: 'DIRECT_IP' | 'SIGNALING_ID') => void;
}> = ({ session, onDisconnect, onToggleInteractive, onInputEvent, onFocusSession, onChangeRoute }) => {
  const videoRef = useRef<HTMLVideoElement>(null);
  const cellRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (videoRef.current && session.stream) {
      videoRef.current.srcObject = session.stream;
    }
  }, [session.stream]);

  const handleMouseMove = (e: React.MouseEvent<HTMLDivElement>) => {
    if (!session.isInteractive || !cellRef.current) return;
    const rect = cellRef.current.getBoundingClientRect();
    const x = (e.clientX - rect.left) / rect.width;
    const y = (e.clientY - rect.top) / rect.height;

    onInputEvent(session.targetId, {
      type: 'MouseMove',
      payload: { x: Math.max(0, Math.min(1, x)), y: Math.max(0, Math.min(1, y)) }
    });
  };

  const handleMouseDown = (e: React.MouseEvent<HTMLDivElement>) => {
    if (!session.isInteractive) return;
    e.preventDefault();
    onInputEvent(session.targetId, {
      type: 'MouseDown',
      payload: { button: e.button }
    });
  };

  const handleMouseUp = (e: React.MouseEvent<HTMLDivElement>) => {
    if (!session.isInteractive) return;
    e.preventDefault();
    onInputEvent(session.targetId, {
      type: 'MouseUp',
      payload: { button: e.button }
    });
  };

  const handleWheel = (e: React.WheelEvent<HTMLDivElement>) => {
    if (!session.isInteractive) return;
    onInputEvent(session.targetId, {
      type: 'MouseWheel',
      payload: { delta_y: Math.sign(e.deltaY) }
    });
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLDivElement>) => {
    if (!session.isInteractive) return;
    onInputEvent(session.targetId, {
      type: 'KeyDown',
      payload: { vk_code: e.keyCode, key: e.key }
    });
  };

  const handleKeyUp = (e: React.KeyboardEvent<HTMLDivElement>) => {
    if (!session.isInteractive) return;
    onInputEvent(session.targetId, {
      type: 'KeyUp',
      payload: { vk_code: e.keyCode, key: e.key }
    });
  };

  return (
    <div className={`glass-card rounded-2xl border ${session.isInteractive ? 'border-blue-500 ring-2 ring-blue-500/20' : 'border-slate-800'} overflow-hidden flex flex-col justify-between bg-slate-950/40 hover:border-slate-700/80 transition-all`}>
      {/* Viewport Header info */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between p-3 gap-2 border-b border-slate-800/80 bg-slate-900/60">
        <div className="flex items-center space-x-2">
          <div className={`p-1.5 rounded-lg border ${session.isInteractive ? 'bg-blue-500/10 text-blue-400 border-blue-500/20' : 'bg-slate-800 text-slate-400 border-slate-700/50'}`}>
            <Monitor className="w-4 h-4" />
          </div>
          <div>
            <h3 className="text-xs font-semibold text-slate-200">{session.name || 'Remote PC'}</h3>
            <span className="text-[9px] text-slate-500 font-mono">ID: {session.targetId}</span>
          </div>
        </div>

        {/* Dual Connection Selector (LAN / Cloud) */}
        <div className="flex items-center space-x-2">
          <div className="flex bg-slate-950/80 p-0.5 rounded-lg border border-slate-800 text-[9px] font-semibold">
            <button
              onClick={() => onChangeRoute(session.targetId, 'DIRECT_IP')}
              className={`px-2 py-1 rounded flex items-center space-x-1.5 transition-all ${
                session.connectionMode === 'DIRECT_IP'
                  ? 'bg-blue-600 text-white shadow'
                  : 'text-slate-500 hover:text-slate-300'
              }`}
              title="Connect directly over local network (High Speed, zero WAN load)"
            >
              <Network className="w-3 h-3" />
              <span>LAN</span>
            </button>
            <button
              onClick={() => onChangeRoute(session.targetId, 'SIGNALING_ID')}
              className={`px-2 py-1 rounded flex items-center space-x-1.5 transition-all ${
                session.connectionMode === 'SIGNALING_ID'
                  ? 'bg-blue-600 text-white shadow'
                  : 'text-slate-500 hover:text-slate-300'
              }`}
              title="Connect via signaling server (Relayed over WAN/Internet)"
            >
              <Globe className="w-3 h-3" />
              <span>Cloud</span>
            </button>
          </div>

          <span className={`w-2 h-2 rounded-full ${session.isConnected ? 'bg-emerald-400' : 'bg-amber-400 animate-pulse'}`}></span>
        </div>
      </div>

      {/* Main video area */}
      <div
        ref={cellRef}
        tabIndex={0}
        onMouseMove={handleMouseMove}
        onMouseDown={handleMouseDown}
        onMouseUp={handleMouseUp}
        onWheel={handleWheel}
        onKeyDown={handleKeyDown}
        onKeyUp={handleKeyUp}
        className={`relative flex-1 bg-black flex items-center justify-center overflow-hidden focus:outline-none min-h-[220px] ${
          session.isInteractive ? 'cursor-crosshair' : 'cursor-default'
        }`}
      >
        {session.stream ? (
          <video
            ref={videoRef}
            autoPlay
            playsInline
            muted
            className="w-full h-full object-contain pointer-events-none"
          />
        ) : (
          <div className="flex flex-col items-center justify-center space-y-3 text-slate-600">
            <div className="w-12 h-12 rounded-xl bg-blue-500/10 border border-blue-500/20 flex items-center justify-center text-blue-400 animate-pulse">
              <MonitorPlay className="w-6 h-6" />
            </div>
            <div className="text-center">
              <p className="text-xs font-medium text-slate-400">
                {session.isConnecting ? 'Connecting...' : 'Initializing Stream'}
              </p>
              <span className="text-[10px] text-slate-500 font-mono">
                {session.connectionMode === 'DIRECT_IP' ? 'Direct LAN Socket Link' : 'Relayed WAN Sdp Exchange'}
              </span>
            </div>
          </div>
        )}

        {/* Floating action overlay on cell hover */}
        <div className="absolute inset-0 bg-slate-950/80 opacity-0 hover:opacity-100 transition-opacity flex items-center justify-center space-x-2.5 duration-200">
          <button
            onClick={() => onFocusSession(session.targetId)}
            className="px-3.5 py-1.5 bg-slate-900 hover:bg-slate-800 border border-slate-700/80 text-white rounded-xl text-xs font-semibold flex items-center space-x-1.5 transition-all shadow-lg"
          >
            <Maximize2 className="w-3.5 h-3.5" />
            <span>Full Control</span>
          </button>
          
          <button
            onClick={() => onToggleInteractive(session.targetId)}
            className={`px-3.5 py-1.5 border rounded-xl text-xs font-semibold flex items-center space-x-1.5 transition-all shadow-lg ${
              session.isInteractive
                ? 'bg-blue-600 border-blue-500 hover:bg-blue-500 text-white'
                : 'bg-slate-900 border-slate-700 hover:bg-slate-800 text-slate-300'
            }`}
          >
            <Zap className="w-3.5 h-3.5" />
            <span>{session.isInteractive ? 'Disable Input' : 'Enable Input'}</span>
          </button>

          <button
            onClick={() => onDisconnect(session.targetId)}
            className="px-3.5 py-1.5 bg-rose-600/10 hover:bg-rose-600/20 border border-rose-500/30 text-rose-400 rounded-xl text-xs font-semibold flex items-center space-x-1.5 transition-all shadow-lg"
          >
            <Power className="w-3.5 h-3.5" />
            <span>Disconnect</span>
          </button>
        </div>
      </div>

      {/* Viewport footer specs */}
      <div className="flex items-center justify-between p-2.5 border-t border-slate-900 bg-slate-950/80 text-[10px] text-slate-500 font-mono">
        <div>{session.connectionMode === 'DIRECT_IP' ? '🏠 Local LAN (Direct)' : '☁️ Cloud WAN (Relayed)'}</div>
        <div>
          RTT: {session.isConnected ? (session.connectionMode === 'DIRECT_IP' ? '1ms (Eth)' : '32ms (Cloud)') : 'Connecting...'}
        </div>
      </div>
    </div>
  );
};

// ── MultiRemoteViewport Component ────────────────────────────────────────────
export const MultiRemoteViewport: React.FC<MultiRemoteViewportProps> = ({
  sessions,
  onDisconnect,
  onToggleInteractive,
  onInputEvent,
  onFocusSession,
  onChangeRoute
}) => {
  const getGridLayoutClass = () => {
    const count = sessions.length;
    if (count <= 1) return 'grid-cols-1';
    if (count === 2) return 'grid-cols-1 md:grid-cols-2';
    if (count <= 4) return 'grid-cols-1 md:grid-cols-2 lg:grid-cols-2';
    return 'grid-cols-1 md:grid-cols-2 lg:grid-cols-3';
  };

  return (
    <div className="flex-1 flex flex-col justify-between">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 mb-4">
        <div>
          <h2 className="text-base font-bold text-slate-100 flex items-center space-x-2">
            <span className="w-2.5 h-2.5 rounded-full bg-blue-500 animate-pulse"></span>
            <span>Multi-Desk Grid Monitoring Dashboard</span>
          </h2>
          <p className="text-xs text-slate-400">Dual-network routing enabled. Switch route dynamically per PC card.</p>
        </div>
        <div className="flex items-center space-x-2 text-[11px] text-slate-500 bg-slate-900/60 border border-slate-800 px-3 py-1.5 rounded-xl font-mono">
          <span>Active Connections: {sessions.filter(s => s.isConnected).length} / {sessions.length}</span>
        </div>
      </div>

      {sessions.length === 0 ? (
        <div className="glass-card rounded-2xl p-16 text-center border border-slate-800 flex-1 flex flex-col justify-center items-center">
          <ShieldAlert className="w-12 h-12 text-slate-600 mb-3" />
          <h3 className="text-sm font-semibold text-slate-300">No Active Sessions</h3>
          <p className="text-xs text-slate-500 max-w-sm mx-auto mt-1">
            All remote PCs have been disconnected. Close this tab or check your dashboard.
          </p>
        </div>
      ) : (
        <div className={`grid ${getGridLayoutClass()} gap-4 items-stretch flex-1`}>
          {sessions.map((session) => (
            <GridCellViewport
              key={session.targetId}
              session={session}
              onDisconnect={onDisconnect}
              onToggleInteractive={onToggleInteractive}
              onInputEvent={onInputEvent}
              onFocusSession={onFocusSession}
              onChangeRoute={onChangeRoute}
            />
          ))}
        </div>
      )}
    </div>
  );
};
