import React, { useState } from 'react';
import { ShieldCheck, Copy, Check, Monitor, KeyRound, Wifi } from 'lucide-react';

interface HostStatusCardProps {
  hostId: string;
  isOnline: boolean;
}

export const HostStatusCard: React.FC<HostStatusCardProps> = ({ hostId, isOnline }) => {
  const [copied, setCopied] = useState(false);
  const [password, setPassword] = useState('aether2026');

  const copyToClipboard = () => {
    navigator.clipboard.writeText(hostId.replace(/\s+/g, ''));
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div className="glass-card rounded-2xl p-6 shadow-2xl relative overflow-hidden border border-slate-800 h-full flex flex-col justify-between">
      <div className="absolute top-0 right-0 w-32 h-32 bg-blue-600/10 rounded-full blur-3xl pointer-events-none"></div>

      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center space-x-3">
          <div className="p-2.5 bg-blue-500/10 rounded-xl text-blue-400 border border-blue-500/20">
            <Monitor className="w-6 h-6" />
          </div>
          <div>
            <h2 className="text-lg font-semibold text-slate-100">Your Workstation</h2>
            <p className="text-xs text-slate-400">Allow remote access to this PC</p>
          </div>
        </div>
        <div className="flex items-center space-x-2">
          <span className={`w-2.5 h-2.5 rounded-full ${isOnline ? 'bg-emerald-400 animate-pulse' : 'bg-rose-500'}`}></span>
          <span className="text-xs font-medium text-slate-300">{isOnline ? 'Ready for Connect' : 'Offline'}</span>
        </div>
      </div>

      {/* 9-Digit ID Display */}
      <div className="bg-slate-900/80 rounded-xl p-4 mb-4 border border-slate-800/80 flex items-center justify-between">
        <div>
          <span className="text-xs font-medium text-slate-400 uppercase tracking-wider block mb-1">Your Session ID</span>
          <span className="text-2xl font-mono font-bold tracking-widest text-blue-400">{hostId || '482 910 375'}</span>
        </div>
        <button
          onClick={copyToClipboard}
          className="p-2.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 hover:text-white transition-all flex items-center space-x-1.5 text-xs"
        >
          {copied ? <Check className="w-4 h-4 text-emerald-400" /> : <Copy className="w-4 h-4" />}
          <span>{copied ? 'Copied!' : 'Copy ID'}</span>
        </button>
      </div>

      {/* Web Download Button for Other PCs */}
      <div className="mb-4">
        <a
          href="/AetherDesk-QuickSupport.zip"
          download="AetherDesk-QuickSupport.zip"
          className="w-full bg-slate-900/90 hover:bg-blue-600/20 text-blue-400 hover:text-blue-300 border border-blue-500/30 rounded-xl py-2.5 px-4 text-xs font-medium transition-all flex items-center justify-center space-x-2"
        >
          <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
          </svg>
          <span>Download Host Agent for Remote PC (.exe / .bat)</span>
        </a>
      </div>

      {/* Unattended Access Password */}
      <div className="flex items-center justify-between pt-3 border-t border-slate-800/60 text-xs text-slate-400">
        <div className="flex items-center space-x-2">
          <KeyRound className="w-4 h-4 text-amber-400" />
          <div className="flex flex-col">
            <span className="font-medium text-slate-300">Unattended Access Password</span>
            <span className="text-[10px] text-slate-500">Bilgisayar başında kimse yokken otomatik erişim şifresi</span>
          </div>
        </div>
        <input
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          className="bg-slate-900 border border-slate-700/80 rounded-md px-2.5 py-1 text-slate-200 font-mono focus:outline-none focus:border-blue-500 w-32 text-right"
        />
      </div>
    </div>
  );
};
