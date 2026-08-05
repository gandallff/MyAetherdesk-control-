import React, { useState } from 'react';
import { Maximize2, Minimize2, FolderSync, ShieldAlert, Sliders, Power, Activity, Lock } from 'lucide-react';

interface RemoteToolbarProps {
  onToggleFullscreen: () => void;
  isFullscreen: boolean;
  onOpenFileTransfer: () => void;
  onDisconnect: () => void;
  onSendMacro: (macro: string) => void;
}

export const RemoteToolbar: React.FC<RemoteToolbarProps> = ({
  onToggleFullscreen,
  isFullscreen,
  onOpenFileTransfer,
  onDisconnect,
  onSendMacro
}) => {
  const [fps, setFps] = useState(60);
  const [latencyMs, setLatencyMs] = useState(8);

  return (
    <div className="absolute top-4 left-1/2 -translate-x-1/2 z-40 glass-panel px-4 py-2 rounded-2xl shadow-2xl flex items-center space-x-3 border border-slate-700/60">
      {/* Network Latency Badge */}
      <div className="flex items-center space-x-2 px-2.5 py-1 bg-slate-900/80 rounded-xl text-xs font-mono text-emerald-400 border border-emerald-500/20">
        <Activity className="w-3.5 h-3.5 animate-pulse" />
        <span>{latencyMs}ms</span>
        <span className="text-slate-600">|</span>
        <span>{fps} FPS</span>
      </div>

      <div className="h-4 w-px bg-slate-700"></div>

      {/* File Transfer Button */}
      <button
        onClick={onOpenFileTransfer}
        className="p-2 rounded-xl bg-slate-800/80 hover:bg-slate-700 text-slate-200 hover:text-white transition-all flex items-center space-x-1 text-xs font-medium"
        title="Open File Explorer & Transfer"
      >
        <FolderSync className="w-4 h-4 text-blue-400" />
        <span>Files</span>
      </button>

      {/* Ctrl+Alt+Del Macro */}
      <button
        onClick={() => onSendMacro('CTRL_ALT_DEL')}
        className="p-2 rounded-xl bg-slate-800/80 hover:bg-slate-700 text-slate-200 hover:text-white transition-all flex items-center space-x-1 text-xs font-medium"
        title="Send Ctrl+Alt+Del"
      >
        <Lock className="w-4 h-4 text-amber-400" />
        <span>Ctrl+Alt+Del</span>
      </button>

      {/* Fullscreen Toggle */}
      <button
        onClick={onToggleFullscreen}
        className="p-2 rounded-xl bg-slate-800/80 hover:bg-slate-700 text-slate-200 hover:text-white transition-all"
        title="Toggle Fullscreen"
      >
        {isFullscreen ? <Minimize2 className="w-4 h-4" /> : <Maximize2 className="w-4 h-4" />}
      </button>

      <div className="h-4 w-px bg-slate-700"></div>

      {/* Disconnect Button */}
      <button
        onClick={onDisconnect}
        className="p-2 rounded-xl bg-rose-500/20 hover:bg-rose-500/30 text-rose-400 hover:text-rose-300 transition-all flex items-center space-x-1 text-xs font-medium border border-rose-500/30"
        title="Disconnect Session"
      >
        <Power className="w-4 h-4" />
        <span>Disconnect</span>
      </button>
    </div>
  );
};
