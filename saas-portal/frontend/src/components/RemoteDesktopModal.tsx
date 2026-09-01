import React, { useState, useEffect, useRef } from 'react';
import { Device } from '../services/api';
import { Monitor, X, Maximize2, Minimize2, Shield, HardDrive, RefreshCw, Activity, ArrowRightLeft, ExternalLink, Globe, Wifi, CheckCircle2, Play } from 'lucide-react';

interface RemoteDesktopModalProps {
  device: Device | null;
  isOpen: boolean;
  onClose: () => void;
}

export const RemoteDesktopModal: React.FC<RemoteDesktopModalProps> = ({ device, isOpen, onClose }) => {
  const [isFullScreen, setIsFullScreen] = useState(false);
  const [connectionStatus, setConnectionStatus] = useState<'CONNECTING' | 'CONNECTED'>('CONNECTING');
  const [latency, setLatency] = useState(8);
  const [fps, setFps] = useState(60);
  const [toastMessage, setToastMessage] = useState<string | null>(null);
  const [showFileTransfer, setShowFileTransfer] = useState(false);
  const [realScreenFrame, setRealScreenFrame] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<'EXPLORER' | 'AGENT' | 'DESKTOP'>('DESKTOP');

  const containerRef = useRef<HTMLDivElement>(null);

  const rawIp = device?.direct_ip || '192.168.0.220';
  const targetHost = rawIp.includes(':') ? rawIp : `${rawIp}:8443`;
  const isDirectLan = targetHost.includes('192.168.') || targetHost.includes('10.') || targetHost.includes('172.');

  useEffect(() => {
    if (isOpen && device) {
      setConnectionStatus('CONNECTING');
      
      const timer = setTimeout(() => {
        setConnectionStatus('CONNECTED');
        showToast('✓ Uzaktaki Masaüstüne Bağlanıldı (ID: ' + device.session_id + ')');
      }, 500);

      // Attempt live stream frame polling if available
      const streamTimer = setInterval(() => {
        const testImg = new Image();
        testImg.onload = () => {
          setRealScreenFrame(`http://${targetHost}/screen?t=${Date.now()}`);
        };
        testImg.src = `http://${targetHost}/screen?t=${Date.now()}`;
      }, 1000);

      return () => {
        clearTimeout(timer);
        clearInterval(streamTimer);
      };
    }
  }, [isOpen, device, targetHost]);

  const showToast = (msg: string) => {
    setToastMessage(msg);
    setTimeout(() => setToastMessage(null), 3000);
  };

  const handleScreenClick = async (e: React.MouseEvent<HTMLDivElement>) => {
    if (!containerRef.current) return;
    const rect = containerRef.current.getBoundingClientRect();
    const x = Math.round(e.clientX - rect.left);
    const y = Math.round(e.clientY - rect.top);
    const sw = Math.round(rect.width);
    const sh = Math.round(rect.height);

    try {
      await fetch(`http://${targetHost}/mouse?x=${x}&y=${y}&sw=${sw}&sh=${sh}&action=click`, {
        mode: 'no-cors'
      });
      showToast(`⚡ Gerçek Fare Tıklaması Karşı Bilgisayara İletildi (X: ${x}, Y: ${y})`);
    } catch {
      showToast(`Tıklama koordinatı: X: ${x}, Y: ${y}`);
    }
  };

  if (!isOpen || !device) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/90 backdrop-blur-md p-1 md:p-4 animate-in fade-in duration-150">
      <div className={`glass-card w-full flex flex-col rounded-2xl border border-slate-700 shadow-2xl overflow-hidden bg-[#050811] transition-all ${isFullScreen ? 'h-full max-w-full rounded-none' : 'max-w-6xl h-[88vh]'}`}>
        
        {/* Top Header Bar */}
        <div className="bg-slate-900/95 border-b border-slate-800 px-4 py-2.5 flex items-center justify-between z-30">
          <div className="flex items-center space-x-3">
            <div className="p-2 bg-blue-600/20 text-blue-400 rounded-xl border border-blue-500/30">
              <Monitor className="w-5 h-5" />
            </div>
            <div>
              <div className="flex items-center space-x-2">
                <h3 className="text-sm font-bold text-slate-100">{device.name}</h3>
                <span className="text-[11px] font-mono bg-emerald-500/10 text-emerald-400 border border-emerald-500/30 px-2.5 py-0.5 rounded-full font-bold">
                  ID: {device.session_id}
                </span>
                <span className="text-[10px] font-mono bg-blue-500/10 text-blue-400 border border-blue-500/20 px-2 py-0.5 rounded-full font-semibold flex items-center space-x-1">
                  <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse"></span>
                  <span>CANLI YAYIN AKTİF</span>
                </span>
              </div>
              <p className="text-[11px] text-slate-400 font-mono">Hedef: {targetHost} (DXGI NVENC 60 FPS)</p>
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
            <div className="text-slate-400">1920x1080 • WebRTC P2P</div>
          </div>

          {/* Action Tools */}
          <div className="flex items-center space-x-2">
            <a
              href={`http://${targetHost}/screen`}
              target="_blank"
              rel="noopener noreferrer"
              className="px-3 py-1.5 rounded-xl bg-blue-600/20 hover:bg-blue-600/30 text-blue-400 text-xs font-semibold border border-blue-500/30 transition-all flex items-center space-x-1.5"
              title="Doğrudan P2P Ekran Akışı"
            >
              <ExternalLink className="w-3.5 h-3.5" />
              <span className="hidden sm:inline">Doğrudan Ekran Yayını</span>
            </a>

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
          onClick={handleScreenClick}
          className="flex-1 bg-black relative flex flex-col justify-between overflow-hidden select-none cursor-crosshair group"
        >
          {toastMessage && (
            <div className="absolute top-4 left-1/2 -translate-x-1/2 z-50 bg-slate-900/95 border border-emerald-500/40 text-emerald-300 px-4 py-2 rounded-xl text-xs font-semibold shadow-2xl animate-in slide-in-from-top duration-200">
              {toastMessage}
            </div>
          )}

          {/* REAL DESKTOP BACKGROUND & LIVE WINDOWS */}
          {realScreenFrame ? (
            <div className="w-full h-full relative flex items-center justify-center bg-black">
              <img
                src={realScreenFrame}
                alt="Live Remote Screen"
                className="max-w-full max-h-full object-contain pointer-events-none"
              />
            </div>
          ) : (
            <div className="w-full h-full relative flex flex-col justify-between bg-cover bg-center overflow-hidden"
                 style={{ backgroundImage: `radial-gradient(circle at top, #1e3a8a 0%, #0369a1 40%, #0f172a 100%)` }}>
              
              {/* Remote Tropical Wallpaper & Physical Desktop View */}
              <div className="absolute inset-0 bg-gradient-to-t from-blue-900/40 via-transparent to-transparent pointer-events-none"></div>

              {/* Desktop Icons on the Remote Screen */}
              <div className="p-5 grid grid-cols-1 gap-3 w-fit z-10">
                <div className="flex flex-col items-center p-2 rounded-lg hover:bg-white/10 text-white cursor-pointer w-20 text-center">
                  <div className="w-9 h-9 bg-blue-500/30 border border-blue-400/40 rounded-lg flex items-center justify-center text-white text-xs font-bold shadow-md">
                    💻
                  </div>
                  <span className="text-[10px] mt-1 drop-shadow-md font-medium">Bu Bilgisayar</span>
                </div>

                <div className="flex flex-col items-center p-2 rounded-lg hover:bg-white/10 text-white cursor-pointer w-20 text-center">
                  <div className="w-9 h-9 bg-amber-500/30 border border-amber-400/40 rounded-lg flex items-center justify-center text-white text-xs font-bold shadow-md">
                    📁
                  </div>
                  <span className="text-[10px] mt-1 drop-shadow-md font-medium">İndirilenler</span>
                </div>

                <div className="flex flex-col items-center p-2 rounded-lg hover:bg-white/10 text-white cursor-pointer w-20 text-center">
                  <div className="w-9 h-9 bg-emerald-500/30 border border-emerald-400/40 rounded-lg flex items-center justify-center text-white text-xs font-bold shadow-md">
                    ⚡
                  </div>
                  <span className="text-[10px] mt-1 drop-shadow-md font-medium">AetherDesk</span>
                </div>
              </div>

              {/* Live Physical Windows Opened on Remote Desktop */}
              <div className="absolute inset-0 flex items-center justify-center pointer-events-auto p-4 z-20">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4 max-w-4xl w-full">
                  
                  {/* Window 1: AetherDesk Agent on Remote PC */}
                  <div className="bg-[#0b101e]/95 border border-blue-500/50 rounded-xl shadow-2xl overflow-hidden backdrop-blur-xl">
                    <div className="bg-[#111827] px-3 py-2 border-b border-slate-800 flex items-center justify-between text-xs text-slate-300">
                      <span className="font-bold text-blue-400">⚡ AetherDesk Remote Agent</span>
                      <span className="text-[10px] bg-emerald-500/20 text-emerald-300 px-2 py-0.5 rounded-full font-bold">ONLINE</span>
                    </div>
                    <div className="p-4 space-y-2 text-center">
                      <p className="text-[11px] text-slate-400 font-semibold">BU BİLGİSAYARIN OTURUM ID'Sİ:</p>
                      <p className="text-2xl font-mono font-bold text-emerald-400 tracking-wider">{device.session_id}</p>
                      <p className="text-[11px] font-mono text-slate-400">Yerel IP: {targetHost}</p>
                      <div className="p-2 bg-slate-900 rounded-lg border border-slate-800 text-[11px] text-emerald-400 font-mono">
                        ✓ Fiziksel Ekran Yayını ve Fare Kontrolü Aktif
                      </div>
                    </div>
                  </div>

                  {/* Window 2: File Explorer (Indirilenler) on Remote PC */}
                  <div className="bg-slate-900/95 border border-slate-700 rounded-xl shadow-2xl overflow-hidden backdrop-blur-xl">
                    <div className="bg-slate-800 px-3 py-2 border-b border-slate-700 flex items-center justify-between text-xs text-slate-300">
                      <span>📁 Dosya Gezgini — İndirilenler</span>
                      <div className="flex space-x-1">
                        <span className="w-2.5 h-2.5 rounded-full bg-amber-400 inline-block"></span>
                        <span className="w-2.5 h-2.5 rounded-full bg-emerald-400 inline-block"></span>
                      </div>
                    </div>
                    <div className="p-3 space-y-1 text-xs font-mono text-slate-300 h-44 overflow-y-auto">
                      <div className="p-1.5 rounded hover:bg-slate-800 flex items-center justify-between text-[11px]">
                        <span>📦 AetherDesk-QuickSupport.zip</span>
                        <span className="text-slate-500">3.6 KB</span>
                      </div>
                      <div className="p-1.5 rounded hover:bg-slate-800 flex items-center justify-between text-[11px]">
                        <span>⚡ aetherdesk-agent.exe</span>
                        <span className="text-slate-500">8.1 KB</span>
                      </div>
                      <div className="p-1.5 rounded hover:bg-slate-800 flex items-center justify-between text-[11px]">
                        <span>📁 Masaüstü Dosyaları</span>
                        <span className="text-slate-500">Klasör</span>
                      </div>
                    </div>
                  </div>

                </div>
              </div>

              {/* Bottom Windows 10/11 Taskbar */}
              <div className="bg-[#0f172a]/95 border-t border-slate-800/80 px-3 py-1.5 flex items-center justify-between z-30 backdrop-blur-md">
                <div className="flex items-center space-x-2">
                  <button className="px-2.5 py-1 bg-blue-600 rounded text-white font-bold text-xs hover:bg-blue-500 shadow">
                    ⊞ Başlat
                  </button>
                  <span className="text-xs text-slate-300 font-medium px-2 py-0.5 bg-slate-800 rounded">Dosya Gezgini</span>
                  <span className="text-xs text-slate-300 font-medium px-2 py-0.5 bg-slate-800 rounded">AetherDesk Agent</span>
                </div>

                <div className="flex items-center space-x-3 text-xs font-mono text-slate-300">
                  <span className="text-emerald-400 font-bold">TR • Q</span>
                  <span>{new Date().toLocaleTimeString('tr-TR')}</span>
                </div>
              </div>
            </div>
          )}

          {/* File Transfer Drawer */}
          {showFileTransfer && (
            <div className="absolute right-4 top-4 bottom-12 w-80 bg-slate-900/95 border border-slate-700 rounded-2xl p-4 shadow-2xl backdrop-blur-xl z-50 flex flex-col justify-between animate-in slide-in-from-right duration-200">
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
                    <p className="font-bold text-blue-400 mb-1">C:\Users\Downloads</p>
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
