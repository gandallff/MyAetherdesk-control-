import React, { useState, useEffect } from 'react';
import { ApiService, Device, User } from '../services/api';
import { PricingModal } from '../components/PricingModal';
import { SecurityDashboardModal } from '../components/SecurityDashboardModal';
import { LanguageSelector } from '../components/LanguageSelector';
import { Monitor, Plus, Download, Trash2, Power, Zap, RefreshCw, Crown, ExternalLink, Network, ShieldCheck } from 'lucide-react';

interface DashboardPageProps {
  user: User;
  onLogout: () => void;
  onUserUpdated: (user: User) => void;
}

export const DashboardPage: React.FC<DashboardPageProps> = ({ user, onLogout, onUserUpdated }) => {
  const [devices, setDevices] = useState<Device[]>([]);
  const [loading, setLoading] = useState(true);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isPricingOpen, setIsPricingOpen] = useState(false);
  const [isSecurityOpen, setIsSecurityOpen] = useState(false);
  const [deviceName, setDeviceName] = useState('');
  const [sessionId, setSessionId] = useState('');
  const [directIp, setDirectIp] = useState('192.168.1.100');


  const fetchDevices = async () => {
    setLoading(true);
    try {
      const res = await ApiService.getDevices();
      setDevices(res.devices);
    } catch (err) {
      console.error('Failed to load devices', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchDevices();
  }, []);

  const handleAddDevice = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await ApiService.addDevice(deviceName, sessionId, directIp);
      setIsModalOpen(false);
      setDeviceName('');
      setSessionId('');
      fetchDevices();
    } catch (err) {
      console.error('Error adding device', err);
    }
  };

  const handleDeleteDevice = async (id: string) => {
    if (!confirm('Are you sure you want to remove this device from your Address Book?')) return;
    try {
      await ApiService.removeDevice(id);
      fetchDevices();
    } catch (err) {
      console.error('Failed to delete device', err);
    }
  };

  const handleConnectDevice = (device: Device) => {
    window.open(`http://localhost:9000?targetId=${device.session_id}`, '_blank');
  };

  return (
    <div className="min-h-screen bg-[#090d16] text-slate-100 flex flex-col justify-between p-4 md:p-8">
      {/* Top Navbar */}
      <header className="flex items-center justify-between pb-6 border-b border-slate-800/80">
        <div className="flex items-center space-x-3">
          <div className="p-2.5 bg-blue-600 rounded-xl shadow-lg shadow-blue-500/30 text-white">
            <Zap className="w-5 h-5" />
          </div>
          <div>
            <h1 className="text-xl font-bold tracking-tight text-white flex items-center space-x-2">
              <span>AetherDesk SaaS Portal</span>
              <span className="text-[10px] uppercase tracking-widest bg-blue-500/20 text-blue-400 font-semibold px-2 py-0.5 rounded-full border border-blue-500/30">
                {user.role}
              </span>
              <span className="text-[10px] uppercase tracking-widest bg-amber-500/20 text-amber-300 font-bold px-2 py-0.5 rounded-full border border-amber-500/30 flex items-center space-x-1">
                <Crown className="w-3 h-3 text-amber-400" />
                <span>{user.plan || 'FREE'} PLAN</span>
              </span>
            </h1>
            <p className="text-xs text-slate-400">{user.company} — Address Book & Devices</p>
          </div>
        </div>

        <div className="flex items-center space-x-3">
          <LanguageSelector />

          {user.role === 'ADMIN' && (
            <button
              onClick={() => setIsSecurityOpen(true)}
              className="px-3 py-1.5 rounded-xl bg-emerald-500/10 hover:bg-emerald-500/20 text-emerald-400 font-bold text-xs border border-emerald-500/30 flex items-center space-x-1.5 transition-all shadow-sm"
              title="System Admin Security Guard"
            >
              <ShieldCheck className="w-3.5 h-3.5" />
              <span>Security Guard</span>
            </button>
          )}

          <button
            onClick={() => setIsPricingOpen(true)}
            className="px-3 py-1.5 rounded-xl bg-gradient-to-r from-amber-500 to-orange-500 hover:from-amber-400 hover:to-orange-400 text-slate-950 font-bold text-xs shadow-md shadow-amber-500/20 flex items-center space-x-1.5 transition-all"
          >
            <Crown className="w-3.5 h-3.5" />
            <span>Upgrade Plan</span>
          </button>


          <div className="text-right hidden sm:block">
            <div className="text-xs font-semibold text-slate-200">{user.name}</div>
            <div className="text-[10px] text-slate-500 font-mono">{user.email}</div>
          </div>

          <button
            onClick={onLogout}
            className="p-2 rounded-xl bg-slate-900 hover:bg-slate-800 text-slate-400 hover:text-rose-400 transition-all border border-slate-800"
            title="Sign Out"
          >
            <Power className="w-4 h-4" />
          </button>
        </div>
      </header>

      {/* Main Content Area */}
      <main className="my-6 flex-1 max-w-6xl mx-auto w-full">
        {/* Action Controls Bar */}
        <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 mb-6">
          <div>
            <h2 className="text-lg font-bold text-slate-100">Registered Devices & Address Book</h2>
            <p className="text-xs text-slate-400">Live workstation status and 1-Click P2P Remote Connect</p>
          </div>

          <div className="flex items-center space-x-3">
            <button
              onClick={fetchDevices}
              className="p-2.5 rounded-xl bg-slate-900 hover:bg-slate-800 text-slate-300 transition-all border border-slate-800"
              title="Refresh Devices"
            >
              <RefreshCw className="w-4 h-4" />
            </button>

            <a
              href="http://localhost:8080/download/agent"
              download="AetherDesk-Installer.bat"
              className="px-4 py-2.5 rounded-xl bg-slate-900 hover:bg-slate-800 text-blue-400 border border-blue-500/30 text-xs font-medium transition-all flex items-center space-x-2"
            >
              <Download className="w-4 h-4" />
              <span>Tokenized Agent Setup</span>
            </a>

            <button
              onClick={() => setIsModalOpen(true)}
              className="px-4 py-2.5 rounded-xl bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-500 hover:to-indigo-500 text-white text-xs font-medium shadow-lg shadow-blue-500/25 transition-all flex items-center space-x-2"
            >
              <Plus className="w-4 h-4" />
              <span>Add Device</span>
            </button>
          </div>
        </div>

        {/* Devices Grid */}
        {loading ? (
          <div className="text-center py-16 text-slate-500 text-sm">Loading registered devices...</div>
        ) : devices.length === 0 ? (
          <div className="glass-card rounded-2xl p-12 text-center border border-slate-800 my-8">
            <Monitor className="w-12 h-12 text-slate-600 mx-auto mb-3" />
            <h3 className="text-base font-semibold text-slate-300">No Devices Added Yet</h3>
            <p className="text-xs text-slate-500 max-w-sm mx-auto mt-1 mb-4">
              Add your remote PCs to your SaaS Address Book for instant 1-Click Remote Desktop connection.
            </p>
            <button
              onClick={() => setIsModalOpen(true)}
              className="px-4 py-2 rounded-xl bg-blue-600 hover:bg-blue-500 text-white text-xs font-medium"
            >
              Add Your First Device
            </button>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {devices.map((device) => (
              <div
                key={device.id}
                className="glass-card rounded-2xl p-5 border border-slate-800 hover:border-slate-700 transition-all flex flex-col justify-between relative overflow-hidden"
              >
                <div>
                  <div className="flex items-center justify-between mb-3">
                    <div className="flex items-center space-x-2.5">
                      <div className="p-2 bg-blue-500/10 rounded-xl text-blue-400 border border-blue-500/20">
                        <Monitor className="w-5 h-5" />
                      </div>
                      <div>
                        <h3 className="text-sm font-semibold text-slate-100">{device.name}</h3>
                        <span className="text-[10px] text-slate-400 font-mono">ID: {device.session_id}</span>
                      </div>
                    </div>
                    <span className={`w-2.5 h-2.5 rounded-full ${device.is_online ? 'bg-emerald-400 animate-pulse' : 'bg-rose-500'}`}></span>
                  </div>

                  <div className="bg-slate-900/80 rounded-xl p-3 mb-4 border border-slate-800 text-xs font-mono text-slate-400 space-y-1">
                    <div className="flex justify-between">
                      <span className="text-slate-500">Direct IP:</span>
                      <span className="text-slate-300">{device.direct_ip}:{device.direct_port}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-slate-500">Status:</span>
                      <span className={device.is_online ? 'text-emerald-400 font-semibold' : 'text-slate-500'}>
                        {device.is_online ? 'ONLINE' : 'OFFLINE'}
                      </span>
                    </div>
                  </div>
                </div>

                <div className="flex items-center space-x-2 pt-2 border-t border-slate-800">
                  <button
                    onClick={() => handleConnectDevice(device)}
                    className="flex-1 bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-500 hover:to-indigo-500 text-white font-medium py-2 rounded-xl text-xs flex items-center justify-center space-x-1.5 shadow-md shadow-blue-500/20 transition-all"
                  >
                    <span>1-Click Connect</span>
                    <ExternalLink className="w-3.5 h-3.5" />
                  </button>
                  <button
                    onClick={() => handleDeleteDevice(device.id)}
                    className="p-2 rounded-xl bg-slate-900 hover:bg-rose-500/20 text-slate-400 hover:text-rose-400 border border-slate-800 transition-all"
                    title="Remove Device"
                  >
                    <Trash2 className="w-4 h-4" />
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </main>

      {/* Add Device Modal */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm p-4">
          <div className="glass-card w-full max-w-md rounded-2xl p-6 border border-slate-700 shadow-2xl">
            <h3 className="text-base font-bold text-slate-100 mb-4">Add Device to Address Book</h3>
            <form onSubmit={handleAddDevice} className="space-y-4">
              <div>
                <label className="block text-xs font-medium text-slate-300 mb-1">Device Name</label>
                <input
                  type="text"
                  placeholder="e.g. Office Server 01"
                  value={deviceName}
                  onChange={(e) => setDeviceName(e.target.value)}
                  className="w-full bg-slate-900 border border-slate-800 rounded-xl px-3 py-2.5 text-sm text-slate-100 focus:outline-none focus:border-blue-500"
                  required
                />
              </div>

              <div>
                <label className="block text-xs font-medium text-slate-300 mb-1">9-Digit Session ID</label>
                <input
                  type="text"
                  placeholder="482 910 375"
                  value={sessionId}
                  onChange={(e) => setSessionId(e.target.value)}
                  className="w-full bg-slate-900 border border-slate-800 rounded-xl px-3 py-2.5 text-sm font-mono tracking-widest text-slate-100 focus:outline-none focus:border-blue-500"
                  required
                />
              </div>

              <div>
                <label className="block text-xs font-medium text-slate-300 mb-1">Direct LAN IP (Optional)</label>
                <input
                  type="text"
                  placeholder="192.168.1.100"
                  value={directIp}
                  onChange={(e) => setDirectIp(e.target.value)}
                  className="w-full bg-slate-900 border border-slate-800 rounded-xl px-3 py-2.5 text-sm font-mono text-slate-100 focus:outline-none focus:border-blue-500"
                />
              </div>

              <div className="flex justify-end space-x-3 pt-2">
                <button
                  type="button"
                  onClick={() => setIsModalOpen(false)}
                  className="px-4 py-2 rounded-xl bg-slate-800 text-slate-300 text-xs font-medium hover:bg-slate-700"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="px-4 py-2 rounded-xl bg-blue-600 text-white text-xs font-medium hover:bg-blue-500"
                >
                  Save Device
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Pricing Modal */}
      <PricingModal
        isOpen={isPricingOpen}
        onClose={() => setIsPricingOpen(false)}
        currentUser={user}
        onUpgradeSuccess={(updatedUser) => {
          onUserUpdated(updatedUser);
        }}
      />

      {/* Security Dashboard Modal */}
      <SecurityDashboardModal
        isOpen={isSecurityOpen}
        onClose={() => setIsSecurityOpen(false)}
      />

      {/* Footer Status */}

      <footer className="pt-4 border-t border-slate-800/60 flex items-center justify-between text-xs text-slate-500 font-mono">
        <div>AetherDesk SaaS Portal v1.0.0</div>
        <div>REST API: http://localhost:5000/api</div>
      </footer>
    </div>
  );
};
