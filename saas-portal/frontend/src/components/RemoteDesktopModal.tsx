import React, { useState, useEffect } from 'react';
import { Device } from '../services/api';
import { Monitor, X, Maximize2, Minimize2, Shield, HardDrive, RefreshCw, Power, Lock, Terminal, Activity, ArrowRightLeft } from 'lucide-react';

interface RemoteDesktopModalProps {
  device: Device | null;
  isOpen: boolean;
  onClose: () => void;
}

export const RemoteDesktopModal: React.FC<RemoteDesktopModalProps> = ({ device, isOpen, onClose }) => {
  const [isFullScreen, setIsFullScreen] = useState(false);
  const [connectionStatus, setConnectionStatus] = useState<'CONNECTING' | 'CONNECTED' | 'DISCONNECTED'>('CONNECTING');
  const [latency, setLatency] = useState(8);
  const [fps, setFps] = useState(60);
  const [toastMessage, setToastMessage] = useState<string | null>(null);
  const [showFileTransfer, setShowFileTransfer] = useState(false);

  useEffect(() => {
    if (isOpen) {
      setConnectionStatus('CONNECTING');
      const timer = setTimeout(() => {
        setConnectionStatus('CONNECTED');
        showToast('✓ WebRTC P2P Doğrudan Bağlantı Kuruldu (8ms)');
      }, 1000);
      return () => clearTimeout(timer);
    }
  }, [isOpen]);

  const showToast = (msg: string) => {
    setToastMessage(msg);
    setTimeout(() => setToastMessage(null), 3000);
  };

  if (!isOpen || !device) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/85 backdrop-blur-md p-2 md:p-6 animate-in fade-in duration-200">
      <div className={`glass-card w-full flex flex-col rounded-2xl border border-slate-700 shadow-2xl overflow-hidden bg-[#070b14] transition-all ${isFullScreen ? 'h-full max-w-full' : 'max-w-5xl h-[85vh]'}`}>
        
        {/* Top Remote Control Header Toolbar */}
        <div className="bg-slate-900/90 border-b border-slate-800 px-4 py-3 flex items-center justify-between z-20">
          <div className="flex items-center space-x-3">
            <div className="p-2 bg-blue-600/20 text-blue-400 rounded-xl border border-blue-500/30">
              <Monitor className="w-5 h-5" />
            </div>
            <div>
              <div className="flex items-center space-x-2">
                <h3 className="text-sm font-bold text-slate-100">{device.name}</h3>
                <span className="text-[10px] font-mono bg-blue-500/10 text-blue-400 border border-blue-500/20 px-2 py-0.5 rounded-full font-semibold">
                  ID: {device.session_id}
                </span>
                <span className="text-[10px] font-mono bg-emerald-500/10 text-emerald-400 border border-emerald-500/20 px-2 py-0.5 rounded-full font-semibold flex items-center space-x-1">
                  <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse"></span>
                  <span>{connectionStatus}</span>
                </span>
              </div>
              <p className="text-[11px] text-slate-400 font-mono">Direct IP: {device.direct_ip}:{device.direct_port || 8443}</p>
            </div>
          </div>

          {/* Center Stats */}
          <div className="hidden md:flex items-center space-x-4 bg-slate-950/80 px-3 py-1.5 rounded-xl border border-slate-800 text-xs font-mono text-slate-300">
            <div className="flex items-center space-x-1.5 text-emerald-400">
              <Activity className="w-3.5 h-3.5 animate-pulse" />
              <span>{latency} ms</span>
            </div>
            <span className="text-slate-700">|</span>
            <div className="text-blue-400 font-semibold">{fps} FPS</div>
            <span className="text-slate-700">|</span>
            <div className="text-slate-400">DXGI GPU NVENC</div>
          </div>

          {/* Action Tools */}
          <div className="flex items-center space-x-2">
            <button
              onClick={() => showToast('⚡ Ctrl+Alt+Del komutu uzaktaki bilgisayara iletildi.')}
              className="px-3 py-1.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-semibold border border-slate-700 transition-all flex items-center space-x-1.5"
              title="Send Ctrl+Alt+Del"
            >
              <Shield className="w-3.5 h-3.5 text-amber-400" />
              <span className="hidden sm:inline">Ctrl+Alt+Del</span>
            </button>

            <button
              onClick={() => setShowFileTransfer(!showFileTransfer)}
              className="px-3 py-1.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-semibold border border-slate-700 transition-all flex items-center space-x-1.5"
              title="File Transfer"
            >
              <HardDrive className="w-3.5 h-3.5 text-blue-400" />
              <span className="hidden sm:inline">Dosya Transferi</span>
            </button>

            <button
              onClick={() => setIsFullScreen(!isFullScreen)}
              className="p-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 transition-all border border-slate-700"
              title={isFullScreen ? "Exit Fullscreen" : "Fullscreen"}
            >
              {isFullScreen ? <Minimize2 className="w-4 h-4" /> : <Maximize2 className="w-4 h-4" />}
            </button>

            <button
              onClick={onClose}
              className="p-2 rounded-xl bg-rose-500/20 hover:bg-rose-500/30 text-rose-400 transition-all border border-rose-500/30"
              title="Oturumu Sonlandır"
            >
              <X className="w-4 h-4" />
            </button>
          </div>
        </div>

        {/* Remote Screen Viewport Canvas */}
        <div className="flex-1 bg-black relative flex items-center justify-center overflow-hidden group">
          {toastMessage && (
            <div className="absolute top-4 z-30 bg-slate-900/95 border border-emerald-500/40 text-emerald-300 px-4 py-2 rounded-xl text-xs font-semibold shadow-2xl animate-in slide-in-from-top duration-200">
              {toastMessage}
            </div>
          )}

          {connectionStatus === 'CONNECTING' ? (
            <div className="text-center space-y-3 z-10">
              <RefreshCw className="w-10 h-10 text-blue-500 animate-spin mx-auto" />
              <p className="text-sm font-semibold text-slate-200">Uzaktaki Cihaza Bağlanılıyor...</p>
              <p className="text-xs font-mono text-slate-500">Host ID: {device.session_id} | WebRTC ICE Takası Başlatıldı</p>
            </div>
          ) : (
            <div className="w-full h-full relative flex flex-col justify-between p-4 bg-gradient-to-tr from-slate-950 via-slate-900 to-blue-950/40 select-none">
              {/* Simulated Live Remote Workstation Desktop */}
              <div className="flex items-center justify-between text-xs text-slate-400 font-mono bg-slate-900/60 p-2.5 rounded-xl border border-slate-800/80 backdrop-blur-sm">
                <div className="flex items-center space-x-2">
                  <div className="w-2.5 h-2.5 rounded-full bg-emerald-400 animate-pulse"></div>
                  <span>Uzak Masaüstü: {device.name} (Windows 11 Pro 64-bit)</span>
                </div>
                <div className="text-slate-400">1920x1080 @ 60 FPS • NVENC H.264</div>
              </div>

              {/* Central Active Remote Workspace Display */}
              <div className="my-auto flex flex-col items-center justify-center text-center space-y-4">
                <div className="p-6 bg-slate-900/80 border border-slate-800 rounded-2xl shadow-2xl backdrop-blur-xl max-w-lg w-full">
                  <div className="flex items-center space-x-3 mb-4 pb-3 border-b border-slate-800">
                    <div className="p-2 bg-blue-600 rounded-xl text-white">
                      <Terminal className="w-5 h-5" />
                    </div>
                    <div className="text-left">
                      <h4 className="text-sm font-bold text-slate-100">Aktif Uzak Oturum</h4>
                      <p className="text-xs text-slate-400">Fare ve Klavye Girdileri Eşzamanlı İletiliyor</p>
                    </div>
                  </div>

                  <div className="bg-slate-950 rounded-xl p-3 text-left font-mono text-xs text-emerald-400 space-y-1.5 border border-slate-800/60">
                    <p className="text-slate-500">// AetherDesk DXGI Ultra-Low-Latency Link</p>
                    <p>✓ GPU Capture: Active (DXGI Desktop Duplication)</p>
                    <p>✓ Audio Stream: Stereo 48kHz (Low Latency)</p>
                    <p>✓ Input Hook: Keyboard & Mouse Direct Synced</p>
                  </div>
                </div>
              </div>

              {/* Bottom Windows Taskbar Mock */}
              <div className="bg-slate-900/90 border border-slate-800 rounded-xl p-2 flex items-center justify-between text-xs text-slate-300 backdrop-blur-md">
                <div className="flex items-center space-x-3">
                  <button className="p-1.5 bg-blue-600 rounded-lg text-white font-bold text-xs hover:bg-blue-500">
                    ⊞ Başlat
                  </button>
                  <span className="text-xs text-slate-400 font-mono">Dosya Gezgini</span>
                  <span className="text-xs text-slate-400 font-mono">Görev Yöneticisi</span>
                </div>
                <div className="font-mono text-xs text-slate-400">
                  {new Date().toLocaleTimeString('tr-TR')}
                </div>
              </div>
            </div>
          )}

          {/* File Transfer Drawer Modal */}
          {showFileTransfer && (
            <div className="absolute right-4 top-4 bottom-4 w-80 bg-slate-900/95 border border-slate-700 rounded-2xl p-4 shadow-2xl backdrop-blur-xl z-40 flex flex-col justify-between animate-in slide-in-from-right duration-200">
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
                    <p className="font-bold text-blue-400 mb-1">C:\Users\RemoteUser\Desktop</p>
                    <p className="text-[11px] text-slate-500">📁 Raporlar_2026.xlsx (2.4 MB)</p>
                    <p className="text-[11px] text-slate-500">📁 AetherDesk_Config.json (12 KB)</p>
                  </div>
                  <div className="p-3 border border-dashed border-slate-700 rounded-xl text-center text-slate-400 hover:border-blue-500 cursor-pointer transition-all">
                    <ArrowRightLeft className="w-6 h-6 mx-auto mb-1 text-slate-500" />
                    <p className="text-[11px]">Dosyayı buraya sürükleyip bırakın</p>
                    <p className="text-[9px] text-slate-500">64KB Parçalı P2P Hızlı Transfer</p>
                  </div>
                </div>
              </div>
              <button
                onClick={() => {
                  showToast('✓ Dosya karşı bilgisayara başarıyla gönderildi.');
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
