import React, { useState } from 'react';
import { ConnectionMode } from '../types/protocol';
import { Radio, Network, ArrowRight, ShieldAlert, Lock, Zap } from 'lucide-react';

interface ConnectionPanelProps {
  onConnect: (mode: ConnectionMode, target: string, password?: string) => void;
  isConnecting: boolean;
}

export const ConnectionPanel: React.FC<ConnectionPanelProps> = ({ onConnect, isConnecting }) => {
  const [mode, setMode] = useState<ConnectionMode>('SIGNALING_ID');
  const [targetId, setTargetId] = useState('');
  const [ipAddress, setIpAddress] = useState('192.168.1.100');
  const [port, setPort] = useState('8443');
  const [password, setPassword] = useState('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (mode === 'SIGNALING_ID') {
      if (!targetId.trim()) return;
      onConnect('SIGNALING_ID', targetId, password);
    } else {
      if (!ipAddress.trim()) return;
      onConnect('DIRECT_IP', `${ipAddress}:${port}`, password);
    }
  };

  return (
    <div className="glass-card rounded-2xl p-6 shadow-2xl border border-slate-800 h-full flex flex-col justify-between">
      <div className="flex items-center space-x-3 mb-6">
        <div className="p-2.5 bg-indigo-500/10 rounded-xl text-indigo-400 border border-indigo-500/20">
          <Zap className="w-6 h-6" />
        </div>
        <div>
          <h2 className="text-lg font-semibold text-slate-100">Connect to Remote Desk</h2>
          <p className="text-xs text-slate-400">Establish ultra-low latency P2P desktop session</p>
        </div>
      </div>

      {/* Mode Selector Tabs */}
      <div className="grid grid-cols-2 gap-2 bg-slate-900/90 p-1.5 rounded-xl border border-slate-800 mb-6">
        <button
          type="button"
          onClick={() => setMode('SIGNALING_ID')}
          className={`flex flex-col items-center justify-center py-2.5 rounded-lg text-xs font-medium transition-all ${
            mode === 'SIGNALING_ID'
              ? 'bg-blue-600 text-white shadow-lg shadow-blue-500/20'
              : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/50'
          }`}
        >
          <div className="flex items-center space-x-1.5">
            <Radio className="w-4 h-4" />
            <span>9-Digit Session ID</span>
          </div>
          <span className="text-[10px] opacity-75 mt-0.5">🌐 İnternet / Tüm Ağlar</span>
        </button>

        <button
          type="button"
          onClick={() => setMode('DIRECT_IP')}
          className={`flex flex-col items-center justify-center py-2.5 rounded-lg text-xs font-medium transition-all ${
            mode === 'DIRECT_IP'
              ? 'bg-blue-600 text-white shadow-lg shadow-blue-500/20'
              : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/50'
          }`}
        >
          <div className="flex items-center space-x-1.5">
            <Network className="w-4 h-4" />
            <span>Direct IP : Port</span>
          </div>
          <span className="text-[10px] opacity-75 mt-0.5">🏠 Aynı Ağ / Local LAN</span>
        </button>
      </div>

      <form onSubmit={handleSubmit} className="space-y-4">
        {mode === 'SIGNALING_ID' ? (
          <div>
            <label className="block text-xs font-medium text-slate-300 mb-1.5">Remote Partner ID</label>
            <input
              type="text"
              placeholder="e.g. 482 910 375"
              value={targetId}
              onChange={(e) => setTargetId(e.target.value)}
              className="w-full bg-slate-900 border border-slate-800 rounded-xl px-4 py-3 text-lg font-mono tracking-widest text-slate-100 placeholder-slate-600 focus:outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500 transition-all"
            />
          </div>
        ) : (
          <div className="grid grid-cols-3 gap-3">
            <div className="col-span-2">
              <label className="block text-xs font-medium text-slate-300 mb-1.5">Target IPv4 / IPv6</label>
              <input
                type="text"
                placeholder="192.168.1.100"
                value={ipAddress}
                onChange={(e) => setIpAddress(e.target.value)}
                className="w-full bg-slate-900 border border-slate-800 rounded-xl px-4 py-3 text-sm font-mono text-slate-100 focus:outline-none focus:border-blue-500"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-slate-300 mb-1.5">TCP/UDP Port</label>
              <input
                type="text"
                placeholder="8443"
                value={port}
                onChange={(e) => setPort(e.target.value)}
                className="w-full bg-slate-900 border border-slate-800 rounded-xl px-3 py-3 text-sm font-mono text-slate-100 focus:outline-none focus:border-blue-500"
              />
            </div>
          </div>
        )}

        <div>
          <label className="block text-xs font-medium text-slate-300 mb-1.5 flex items-center justify-between">
            <span>Remote Password (Optional)</span>
            <Lock className="w-3.5 h-3.5 text-slate-500" />
          </label>
          <input
            type="password"
            placeholder="Enter unattended access password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="w-full bg-slate-900 border border-slate-800 rounded-xl px-4 py-2.5 text-sm text-slate-100 focus:outline-none focus:border-blue-500"
          />
        </div>

        <button
          type="submit"
          disabled={isConnecting}
          className="w-full bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-500 hover:to-indigo-500 text-white font-medium py-3 rounded-xl shadow-lg shadow-blue-500/25 transition-all flex items-center justify-center space-x-2 text-sm disabled:opacity-50"
        >
          {isConnecting ? (
            <span>Connecting to Peer...</span>
          ) : (
            <>
              <span>Connect to Remote Desktop</span>
              <ArrowRight className="w-4 h-4" />
            </>
          )}
        </button>
      </form>
    </div>
  );
};
