import React, { useRef, useEffect } from 'react';
import { RemoteInputEvent } from '../types/protocol';

interface RemoteViewportProps {
  stream: MediaStream | null;
  onInputEvent: (event: RemoteInputEvent) => void;
  isFullscreen: boolean;
}

export const RemoteViewport: React.FC<RemoteViewportProps> = ({ stream, onInputEvent, isFullscreen }) => {
  const videoRef = useRef<HTMLVideoElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (videoRef.current && stream) {
      videoRef.current.srcObject = stream;
    }
  }, [stream]);

  const handleMouseMove = (e: React.MouseEvent<HTMLDivElement>) => {
    if (!containerRef.current) return;
    const rect = containerRef.current.getBoundingClientRect();
    const x = (e.clientX - rect.left) / rect.width;
    const y = (e.clientY - rect.top) / rect.height;

    onInputEvent({
      type: 'MouseMove',
      payload: { x: Math.max(0, Math.min(1, x)), y: Math.max(0, Math.min(1, y)) }
    });
  };

  const handleMouseDown = (e: React.MouseEvent<HTMLDivElement>) => {
    e.preventDefault();
    onInputEvent({
      type: 'MouseDown',
      payload: { button: e.button }
    });
  };

  const handleMouseUp = (e: React.MouseEvent<HTMLDivElement>) => {
    e.preventDefault();
    onInputEvent({
      type: 'MouseUp',
      payload: { button: e.button }
    });
  };

  const handleWheel = (e: React.WheelEvent<HTMLDivElement>) => {
    onInputEvent({
      type: 'MouseWheel',
      payload: { delta_y: Math.sign(e.deltaY) }
    });
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLDivElement>) => {
    onInputEvent({
      type: 'KeyDown',
      payload: { vk_code: e.keyCode, key: e.key }
    });
  };

  const handleKeyUp = (e: React.KeyboardEvent<HTMLDivElement>) => {
    onInputEvent({
      type: 'KeyUp',
      payload: { vk_code: e.keyCode, key: e.key }
    });
  };

  return (
    <div
      ref={containerRef}
      tabIndex={0}
      onMouseMove={handleMouseMove}
      onMouseDown={handleMouseDown}
      onMouseUp={handleMouseUp}
      onWheel={handleWheel}
      onKeyDown={handleKeyDown}
      onKeyUp={handleKeyUp}
      className={`relative w-full h-full bg-black flex items-center justify-center overflow-hidden focus:outline-none cursor-crosshair ${
        isFullscreen ? 'fixed inset-0 z-50' : 'rounded-2xl border border-slate-800 shadow-2xl'
      }`}
    >
      {stream ? (
        <video
          ref={videoRef}
          autoPlay
          playsInline
          muted
          className="w-full h-full object-contain pointer-events-none"
        />
      ) : (
        /* Fallback Simulation Render Viewport when active stream is starting */
        <div className="flex flex-col items-center justify-center space-y-4 text-slate-500">
          <div className="w-16 h-16 rounded-2xl bg-blue-500/10 border border-blue-500/20 flex items-center justify-center text-blue-400 animate-pulse">
            <svg className="w-8 h-8" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
            </svg>
          </div>
          <p className="text-sm font-medium text-slate-400">Stream Initializing — Low Latency DirectX Capture active</p>
          <span className="text-xs font-mono text-slate-600 bg-slate-900 px-3 py-1 rounded-md">DXGI BGRA 60FPS / WebRTC P2P DTLS-SRTP</span>
        </div>
      )}
    </div>
  );
};
