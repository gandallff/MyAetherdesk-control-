import React, { useState, useRef } from 'react';
import { X, UploadCloud, File, CheckCircle2, ArrowDownCircle, ShieldCheck, HardDrive } from 'lucide-react';
import { FileTransferService } from '../services/fileTransfer';

interface FileExplorerModalProps {
  isOpen: boolean;
  onClose: () => void;
  fileTransferService: FileTransferService | null;
}

export const FileExplorerModal: React.FC<FileExplorerModalProps> = ({
  isOpen,
  onClose,
  fileTransferService
}) => {
  const [transferProgress, setTransferProgress] = useState<number>(0);
  const [activeFileName, setActiveFileName] = useState<string>('');
  const [isTransferring, setIsTransferring] = useState<boolean>(false);
  const [completedLogs, setCompletedLogs] = useState<Array<{ name: string; size: string; time: string }>>([]);
  const fileInputRef = useRef<HTMLInputElement>(null);

  if (!isOpen) return null;

  const handleFileSelect = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (!files || files.length === 0 || !fileTransferService) return;

    const file = files[0];
    setActiveFileName(file.name);
    setIsTransferring(true);
    setTransferProgress(0);

    try {
      await fileTransferService.sendFile(file, (progress) => {
        setTransferProgress(progress);
      });

      const sizeMB = (file.size / (1024 * 1024)).toFixed(2);
      setCompletedLogs((prev) => [
        { name: file.name, size: `${sizeMB} MB`, time: new Date().toLocaleTimeString() },
        ...prev
      ]);
    } catch (err) {
      console.error('File transfer error', err);
    } finally {
      setIsTransferring(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm p-4">
      <div className="glass-card w-full max-w-2xl rounded-2xl p-6 shadow-2xl border border-slate-700 relative overflow-hidden">
        {/* Header */}
        <div className="flex items-center justify-between pb-4 border-b border-slate-800">
          <div className="flex items-center space-x-3">
            <div className="p-2.5 bg-blue-500/10 rounded-xl text-blue-400 border border-blue-500/20">
              <HardDrive className="w-6 h-6" />
            </div>
            <div>
              <h2 className="text-lg font-semibold text-slate-100">Bi-directional File Transfer Engine</h2>
              <p className="text-xs text-slate-400">RTCDataChannel 64KB Binary Chunks (SCTP Protocol)</p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-2 rounded-xl text-slate-400 hover:text-white hover:bg-slate-800 transition-all"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Drag & Drop Upload Zone */}
        <div className="my-6">
          <input
            type="file"
            ref={fileInputRef}
            onChange={handleFileSelect}
            className="hidden"
          />
          <div
            onClick={() => fileInputRef.current?.click()}
            className="border-2 border-dashed border-slate-700 hover:border-blue-500/60 bg-slate-900/60 hover:bg-slate-900 rounded-2xl p-8 flex flex-col items-center justify-center cursor-pointer transition-all group"
          >
            <div className="p-4 bg-blue-500/10 rounded-full text-blue-400 group-hover:scale-110 transition-all mb-3 border border-blue-500/20">
              <UploadCloud className="w-8 h-8" />
            </div>
            <p className="text-sm font-medium text-slate-200 mb-1">Click or drag files here to send to Remote Host</p>
            <span className="text-xs text-slate-500">64KB binary chunking stream with SHA-256 integrity</span>
          </div>
        </div>

        {/* Active Transfer Progress */}
        {isTransferring && (
          <div className="bg-slate-900/90 rounded-xl p-4 mb-6 border border-slate-800">
            <div className="flex items-center justify-between text-xs mb-2">
              <span className="font-medium text-slate-200 flex items-center space-x-2">
                <File className="w-4 h-4 text-blue-400" />
                <span>{activeFileName}</span>
              </span>
              <span className="font-mono text-blue-400 font-bold">{transferProgress.toFixed(1)}%</span>
            </div>
            <div className="w-full bg-slate-800 h-2.5 rounded-full overflow-hidden">
              <div
                className="bg-gradient-to-r from-blue-500 to-indigo-500 h-full transition-all duration-150"
                style={{ width: `${transferProgress}%` }}
              ></div>
            </div>
          </div>
        )}

        {/* Completed Transfers Table */}
        <div>
          <h3 className="text-xs font-semibold text-slate-400 uppercase tracking-wider mb-3">Transfer Activity Log</h3>
          <div className="max-h-40 overflow-y-auto space-y-2 pr-1">
            {completedLogs.length === 0 ? (
              <p className="text-xs text-slate-600 italic py-4 text-center">No files transferred in this session yet.</p>
            ) : (
              completedLogs.map((item, idx) => (
                <div key={idx} className="flex items-center justify-between bg-slate-900/60 px-4 py-2.5 rounded-xl border border-slate-800 text-xs">
                  <div className="flex items-center space-x-3">
                    <CheckCircle2 className="w-4 h-4 text-emerald-400" />
                    <span className="font-medium text-slate-200">{item.name}</span>
                  </div>
                  <div className="flex items-center space-x-4 text-slate-400">
                    <span>{item.size}</span>
                    <span className="font-mono text-slate-500">{item.time}</span>
                    <span className="px-2 py-0.5 bg-emerald-500/10 text-emerald-400 rounded-md text-[10px] border border-emerald-500/20">SHA-256 OK</span>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      </div>
    </div>
  );
};
