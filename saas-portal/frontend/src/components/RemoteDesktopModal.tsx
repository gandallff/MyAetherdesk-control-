import React, { useState, useEffect, useRef } from 'react';
import { Device } from '../services/api';
import { Monitor, X, Maximize2, Minimize2, Shield, HardDrive, RefreshCw, Lock, Terminal, Activity, ArrowRightLeft, MousePointer, AppWindow, Folder, Play, Check } from 'lucide-react';

interface RemoteDesktopModalProps {
  device: Device | null;
  isOpen: boolean;
  onClose: () => void;
}

export const RemoteDesktopModal: React.FC<RemoteDesktopModalProps> = ({ device, isOpen, onClose }) => {
  const [isFullScreen, setIsFullScreen] = useState(false);
  const [connectionStatus, setConnectionStatus] = useState<'CONNECTING' | 'CONNECTED'>('CONNECTING');
  const [latency, setLatency] = useState(12);
  const [fps, setFps] = useState(60);
  const [toastMessage, setToastMessage] = useState<string | null>(null);
  const [showFileTransfer, setShowFileTransfer] = useState(false);
  
  // Interactive Remote Desktop Simulation State
  const [mousePos, setMousePos] = useState({ x: 450, y: 280 });
  const [activeWindow, setActiveWindow] = useState<'NONE' | 'EXPLORER' | 'TERMINAL'>('TERMINAL');
  const [terminalLogs, setTerminalLogs] = useState<string[]>([
    'AetherDesk DXGI 60FPS Video Engine Connected.',
    'Direct P2P Stream Active: 1920x1080 @ 60 FPS (NVENC H.264)',
    'Remote Input Controller: Keyboard & Mouse Hook Hooked.'
  ]);
  const [commandInput, setCommandInput] = useState('');

  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (isOpen && device) {
      setConnectionStatus('CONNECTING');
      const timer = setTimeout(() => {
        setConnectionStatus('CONNECTED');
        showToast('✓ Doğrudan P2P WebRTC Bağlantısı Kuruldu (12ms)');
      }, 700);
      return () => clearTimeout(timer);
    }
  }, [isOpen, device]);

  const showToast = (msg: string) => {
    setToastMessage(msg);
    setTimeout(() => setToastMessage(null), 3000);
  };

  const handleMouseMove = (e: React.MouseEvent<HTMLDivElement>) => {
    if (!containerRef.current) return;
    const rect = containerRef.current.getBoundingClientRect();
    const x = Math.round(e.clientX - rect.left);
    const y = Math.round(e.clientY - rect.top);
    setMousePos({ x, y });
  };

  const handleRunCommand = (e: React.FormEvent) => {
    e.preventDefault();
    if (!commandInput.trim()) return;
    setTerminalLogs(prev => [...prev, `> ${commandInput}`, `[AetherDesk Remote Output]: Command '${commandInput}' executed successfully.`]);
    setCommandInput('');
  };

  if (!isOpen || !device) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/90 backdrop-blur-md p-1 md:p-4 animate-in fade-in duration-150">
      <div className={`glass-card w-full flex flex-col rounded-2xl border border-slate-700 shadow-2xl overflow-hidden bg-[#050811] transition-all ${isFullScreen ? 'h-full max-w-full rounded-none' : 'max-w-6xl h-[88vh]'}`}>
        
        {/* Top Header Bar */}
        <div className="bg-slate-900/95 border-b border-slate-800 px-4 py-2.5 flex items-center justify-between z-20">
          <div className="flex items-center space-x-3">
            <div className="p-2 bg-blue-600/20 text-blue-400 rounded-xl border border-blue-500/30">
              <Monitor className="w-5 h-5" />
            </div>
            <div>
              <div className="flex items-center space-x-2">
                <h3 className="text-sm font-bold text-slate-100">{device.name}</h3>
                <span className="text-[11px] font-mono bg-emerald-500/10 text-emerald-400 border border-emerald-500/30 px-2 py-0.5 rounded-full font-bold">
                  ID: {device.session_id}
                </span>
                <span className="text-[10px] font-mono bg-blue-500/10 text-blue-400 border border-blue-500/20 px-2 py-0.5 rounded-full font-semibold flex items-center space-x-1">
                  <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse"></span>
                  <span>{connectionStatus}</span>
                </span>
              </div>
            </div>
          </div>

          {/* Center Stats */}
          <div className="hidden md:flex items-center space-x-4 bg-slate-950 px-3.5 py-1.5 rounded-xl border border-slate-800 text-xs font-mono text-slate-300">
            <div className="flex items-center space-x-1.5 text-emerald-400 font-bold">
              <Activity className="w-3.5 h-3.5 animate-pulse" />
              <span>{latency} ms</span>
            </div>
            <span className="text-slate-700">|</span>
            <div className="text-blue-400 font-semibold">{fps} FPS</div>
            <span className="text-slate-700">|</span>
            <div className="text-slate-400">1920x1080 • NVENC H.264</div>
          </div>

          {/* Action Tools */}
          <div className="flex items-center space-x-2">
            <button
              onClick={() => showToast('⚡ Ctrl+Alt+Del komutu uzaktaki bilgisayara iletildi.')}
              className="px-3 py-1.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-semibold border border-slate-700 transition-all flex items-center space-x-1.5 cursor-pointer"
            >
              <Shield className="w-3.5 h-3.5 text-amber-400" />
              <span className="hidden sm:inline">Ctrl+Alt+Del</span>
            </button>

            <button
              onClick={() => setShowFileTransfer(!showFileTransfer)}
              className="px-3 py-1.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-semibold border border-slate-700 transition-all flex items-center space-x-1.5 cursor-pointer"
            >
              <HardDrive className="w-3.5 h-3.5 text-blue-400" />
              <span className="hidden sm:inline">Dosya Transferi</span>
            </button>

            <button
              onClick={() => setIsFullScreen(!isFullScreen)}
              className="p-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 transition-all border border-slate-700 cursor-pointer"
            >
              {isFullScreen ? <Minimize2 className="w-4 h-4" /> : <Maximize2 className="w-4 h-4" />}
            </button>

            <button
              onClick={onClose}
              className="p-2 rounded-xl bg-rose-500/20 hover:bg-rose-500/30 text-rose-400 transition-all border border-rose-500/30 cursor-pointer"
              title="Oturumu Kapat"
            >
              <X className="w-4 h-4" />
            </button>
          </div>
        </div>

        {/* Remote Live Screen Viewport Canvas */}
        <div
          ref={containerRef}
          onMouseMove={handleMouseMove}
          className="flex-1 bg-gradient-to-br from-slate-950 via-[#0a1226] to-[#040817] relative flex flex-col justify-between overflow-hidden select-none cursor-crosshair group"
        >
          {toastMessage && (
            <div className="absolute top-4 left-1/2 -translate-x-1/2 z-40 bg-slate-900/95 border border-emerald-500/40 text-emerald-300 px-4 py-2 rounded-xl text-xs font-semibold shadow-2xl animate-in slide-in-from-top duration-200">
              {toastMessage}
            </div>
          )}

          {connectionStatus === 'CONNECTING' ? (
            <div className="m-auto text-center space-y-3">
              <RefreshCw className="w-10 h-10 text-blue-500 animate-spin mx-auto" />
              <p className="text-sm font-semibold text-slate-200">Uzaktaki Ekrana Bağlanılıyor...</p>
              <p className="text-xs font-mono text-slate-500">Host ID: {device.session_id} | WebRTC P2P Akışı Başlatılıyor</p>
            </div>
          ) : (
            <>
              {/* Remote Desktop Workspace Background with Desktop Icons */}
              <div className="p-6 grid grid-cols-1 gap-4 w-fit z-10">
                <div
                  onClick={() => setActiveWindow('EXPLORER')}
                  className="flex flex-col items-center p-3 rounded-xl hover:bg-white/10 text-slate-200 cursor-pointer transition-all w-24 text-center group/icon"
                >
                  <Folder className="w-10 h-10 text-amber-400 group-hover/icon:scale-110 transition-transform" />
                  <span className="text-[11px] font-medium mt-1 drop-shadow-md">Bu Bilgisayar</span>
                </div>

                <div
                  onClick={() => setActiveWindow('TERMINAL')}
                  className="flex flex-col items-center p-3 rounded-xl hover:bg-white/10 text-slate-200 cursor-pointer transition-all w-24 text-center group/icon"
                >
                  <Terminal className="w-10 h-10 text-blue-400 group-hover/icon:scale-110 transition-transform" />
                  <span className="text-[11px] font-medium mt-1 drop-shadow-md">PowerShell</span>
                </div>
              </div>

              {/* Active Windows on Remote Desktop */}
              {activeWindow === 'TERMINAL' && (
                <div className="absolute left-32 top-16 w-[520px] max-w-[80%] bg-slate-950/95 border border-slate-700 rounded-xl shadow-2xl overflow-hidden z-20 backdrop-blur-xl">
                  <div className="bg-slate-900 px-3 py-2 flex items-center justify-between border-b border-slate-800">
                    <div className="flex items-center space-x-2 text-xs font-mono text-slate-300">
                      <Terminal className="w-3.5 h-3.5 text-blue-400" />
                      <span>Administrator: Windows PowerShell (Uzak Oturum)</span>
                    </div>
                    <button onClick={() => setActiveWindow('NONE')} className="text-slate-400 hover:text-white text-xs px-1">✕</button>
                  </div>
                  <div className="p-3 font-mono text-xs text-emerald-400 space-y-1 h-52 overflow-y-auto bg-black/60">
                    {terminalLogs.map((log, idx) => (
                      <p key={idx}>{log}</p>
                    ))}
                  </div>
                  <form onSubmit={handleRunCommand} className="p-2 border-t border-slate-800 flex items-center space-x-2 bg-slate-900/80">
                    <span className="text-xs font-mono text-blue-400 pl-1">PS C:\&gt;</span>
                    <input
                      type="text"
                      value={commandInput}
                      onChange={(e) => setCommandInput(e.target.value)}
                      placeholder="Komut yazın ve Enter'a basın..."
                      className="flex-1 bg-transparent text-xs font-mono text-slate-100 focus:outline-none"
                    />
                  </form>
                </div>
              )}

              {activeWindow === 'EXPLORER' && (
                <div className="absolute left-48 top-20 w-[480px] max-w-[80%] bg-slate-950/95 border border-slate-700 rounded-xl shadow-2xl overflow-hidden z-20 backdrop-blur-xl">
                  <div className="bg-slate-900 px-3 py-2 flex items-center justify-between border-b border-slate-800">
                    <div className="flex items-center space-x-2 text-xs font-mono text-slate-300">
                      <Folder className="w-3.5 h-3.5 text-amber-400" />
                      <span>Dosya Gezgini - Yerel Disk (C:)</span>
                    </div>
                    <button onClick={() => setActiveWindow('NONE')} className="text-slate-400 hover:text-white text-xs px-1">✕</button>
                  </div>
                  <div className="p-3 text-xs font-mono text-slate-300 space-y-2 bg-black/60 h-44 overflow-y-auto">
                    <div className="flex items-center space-x-2 p-1.5 hover:bg-slate-800 rounded">
                      <Folder className="w-4 h-4 text-amber-400" />
                      <span>Program Files</span>
                    </div>
                    <div className="flex items-center space-x-2 p-1.5 hover:bg-slate-800 rounded">
                      <Folder className="w-4 h-4 text-amber-400" />
                      <span>Users\Administrator\Desktop</span>
                    </div>
                    <div className="flex items-center space-x-2 p-1.5 hover:bg-slate-800 rounded">
                      <Folder className="w-4 h-4 text-blue-400" />
                      <span>AetherDesk_Runtime</span>
                    </div>
                  </div>
                </div>
              )}

              {/* Bottom Windows Taskbar */}
              <div className="bg-slate-900/90 border-t border-slate-800/80 px-4 py-2 flex items-center justify-between z-20 backdrop-blur-md">
                <div className="flex items-center space-x-3">
                  <button
                    onClick={() => setActiveWindow(activeWindow === 'TERMINAL' ? 'NONE' : 'TERMINAL')}
                    className="px-3 py-1 bg-blue-600 hover:bg-blue-500 rounded-lg text-white font-bold text-xs flex items-center space-x-1.5 cursor-pointer shadow-md"
                  >
                    <span>⊞ Başlat</span>
                  </button>
                  <button
                    onClick={() => setActiveWindow('EXPLORER')}
                    className="p-1.5 hover:bg-slate-800 rounded-lg text-slate-300 transition-colors"
                    title="Dosya Gezgini"
                  >
                    <Folder className="w-4 h-4 text-amber-400" />
                  </button>
                  <button
                    onClick={() => setActiveWindow('TERMINAL')}
                    className="p-1.5 hover:bg-slate-800 rounded-lg text-slate-300 transition-colors"
                    title="PowerShell"
                  >
                    <Terminal className="w-4 h-4 text-blue-400" />
                  </button>
                </div>

                <div className="flex items-center space-x-4 text-xs font-mono text-slate-400">
                  <span>TR • Q Klavye</span>
                  <span>{new Date().toLocaleTimeString('tr-TR')}</span>
                </div>
              </div>
            </>
          )}

          {/* File Transfer Drawer Modal */}
          {showFileTransfer && (
            <div className="absolute right-4 top-4 bottom-14 w-80 bg-slate-900/95 border border-slate-700 rounded-2xl p-4 shadow-2xl backdrop-blur-xl z-40 flex flex-col justify-between animate-in slide-in-from-right duration-200">
              <div>
                <div className="flex items-center justify-between pb-3 border-b border-slate-800 mb-3">
                  <h4 className="text-xs font-bold text-slate-200 flex items-center space-x-2">
                    <HardDrive className="w-4 h-4 text-blue-400" />
                    <span>Çift Yönlü Dosya Transferi</span>
                  </h4>
                  <button onClick={() => setShowFileTransfer(false)} className="text-slate-400 hover:text-white">
                    <X className="w-4 h-4" />
                  </button>
                </div>
                <div className="space-y-2 text-xs font-mono">
                  <div className="p-2.5 rounded-xl bg-slate-950 border border-slate-800 text-slate-300">
                    <p className="font-bold text-blue-400 mb-1">C:\Users\Desktop</p>
                    <p className="text-[11px] text-slate-500">📁 Raporlar_2026.xlsx (2.4 MB)</p>
                    <p className="text-[11px] text-slate-500">📁 AetherDesk_Config.json (12 KB)</p>
                  </div>
                  <div className="p-3 border border-dashed border-slate-700 rounded-xl text-center text-slate-400 hover:border-blue-500 cursor-pointer transition-all">
                    <ArrowRightLeft className="w-6 h-6 mx-auto mb-1 text-slate-500" />
                    <p className="text-[11px]">Dosyayı buraya sürükleyin</p>
                    <p className="text-[9px] text-slate-500">64KB Parçalı P2P Transfer</p>
                  </div>
                </div>
              </div>
              <button
                onClick={() => {
                  showToast('✓ Dosya karşı bilgisayara aktarıldı.');
                  setShowFileTransfer(false);
                }}
                className="w-full py-2 bg-blue-600 hover:bg-blue-500 text-white rounded-xl text-xs font-semibold transition-all shadow-md shadow-blue-500/20"
              >
                Dosya Gönder
              </button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
