import React, { useState, useEffect } from 'react';
import { ApiService, Device, User } from '../services/api';
import { PricingModal } from '../components/PricingModal';
import { SecurityDashboardModal } from '../components/SecurityDashboardModal';
import { RemoteDesktopModal } from '../components/RemoteDesktopModal';
import { LanguageSelector } from '../components/LanguageSelector';
import { Monitor, Plus, Download, Trash2, Zap, RefreshCw, Crown, ExternalLink, ShieldCheck, Edit3, Globe, Wifi, Search, ArrowRight, Radio } from 'lucide-react';

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
  const [activeRemoteDevice, setActiveRemoteDevice] = useState<Device | null>(null);
  
  // Quick ID Connect Bar State
  const [quickTargetId, setQuickTargetId] = useState('');

  // Device Form States
  const [editingDeviceId, setEditingDeviceId] = useState<string | null>(null);
  const [deviceName, setDeviceName] = useState('');
  const [sessionId, setSessionId] = useState('');
  const [directIp, setDirectIp] = useState('');
  const [connectionMode, setConnectionMode] = useState<'AUTO_P2P' | 'DIRECT_LAN'>('AUTO_P2P');
  const [selectedDevices, setSelectedDevices] = useState<Set<string>>(new Set());

  const handleToggleSelect = (id: string) => {
    setSelectedDevices(prev => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const fetchDevices = async () => {
    setLoading(true);
    try {
      const res = await ApiService.getDevices();
      setDevices(res.devices);
      setSelectedDevices(new Set());
    } catch (err) {
      console.error('Failed to load devices', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchDevices();

    // Check for direct connection query param ?connect=778375604 or ?id=778375604
    const params = new URLSearchParams(window.location.search);
    const directId = params.get('connect') || params.get('id');
    if (directId) {
      const clean = directId.replace(/[\s\-]/g, '');
      const autoDevice: Device = {
        id: `auto_${clean}`,
        user_id: user.id,
        name: `Uzak Cihaz (${clean})`,
        session_id: clean,
        is_online: 1,
        direct_ip: 'WebRTC Cloud Relay',
        direct_port: 8443,
        last_seen: new Date().toISOString()
      };
      setActiveRemoteDevice(autoDevice);
    }
  }, []);

  const getDeviceLimit = (): number => {
    const plan = user.plan || 'FREE';
    if (plan === 'PRO') return 15;
    if (plan === 'ENTERPRISE') return 9999;
    return 3;
  };

  const openAddModal = () => {
    const maxLimit = getDeviceLimit();
    if (devices.length >= maxLimit) {
      alert(`⚠️ Paket Limitine Ulaşıldı!\n\nMevcut (${user.plan || 'FREE'}) paketinizde en fazla ${maxLimit} kayıtlı bilgisayar ekleyebilirsiniz.\n\nDaha fazla cihaz eklemek için lütfen paketinizi yükseltin.`);
      setIsPricingOpen(true);
      return;
    }
    setEditingDeviceId(null);
    setDeviceName('');
    setSessionId('');
    setDirectIp('');
    setConnectionMode('AUTO_P2P');
    setIsModalOpen(true);
  };

  const openEditModal = (dev: Device) => {
    setEditingDeviceId(dev.id);
    setDeviceName(dev.name);
    setSessionId(dev.session_id);
    setDirectIp(dev.direct_ip || '');
    setConnectionMode(dev.direct_ip?.includes('.') && !dev.direct_ip.includes('Cloud') ? 'DIRECT_LAN' : 'AUTO_P2P');
    setIsModalOpen(true);
  };

  const handleSaveDevice = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const finalIp = connectionMode === 'DIRECT_LAN' ? directIp : (directIp || 'WebRTC Cloud Relay');
      if (editingDeviceId) {
        const updated = devices.map(d => d.id === editingDeviceId ? {
          ...d,
          name: deviceName,
          session_id: sessionId,
          direct_ip: finalIp
        } : d);
        localStorage.setItem('aether_devices', JSON.stringify(updated));
      } else {
        await ApiService.addDevice(deviceName, sessionId, finalIp);
      }
      setIsModalOpen(false);
      fetchDevices();
    } catch (err) {
      console.error('Error saving device', err);
    }
  };

  const handleDeleteDevice = async (id: string) => {
    if (!confirm('Bu cihazı Adres Defterinizden silmek istediğinize emin misiniz?')) return;
    try {
      await ApiService.removeDevice(id);
      fetchDevices();
    } catch (err) {
      console.error('Failed to delete device', err);
    }
  };

  const handleConnectDevice = (device: Device) => {
    setActiveRemoteDevice(device);
  };

  const handleQuickConnect = (e: React.FormEvent) => {
    e.preventDefault();
    if (!quickTargetId.trim()) return;
    const cleanId = quickTargetId.trim();
    
    // Check if device already exists in address book
    const existing = devices.find(d => d.session_id.replace(/\s+/g, '') === cleanId.replace(/\s+/g, ''));
    if (existing) {
      setActiveRemoteDevice(existing);
    } else {
      const tempDevice: Device = {
        id: `quick_${Date.now()}`,
        user_id: user.id,
        name: `Uzak Masaüstü (${cleanId})`,
        session_id: cleanId,
        is_online: 1,
        direct_ip: 'WebRTC P2P Auto-Match',
        direct_port: 8443,
        last_seen: 'Now'
      };
      setActiveRemoteDevice(tempDevice);
    }
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
            <div className="flex items-center space-x-2">
              <h1 className="text-lg font-bold tracking-tight text-white">AetherDesk SaaS Portal</h1>
              <span className="text-[10px] font-bold px-2 py-0.5 bg-blue-500/20 text-blue-400 border border-blue-500/30 rounded-full">
                {user.role}
              </span>
              <span className="text-[10px] font-bold px-2 py-0.5 bg-amber-500/20 text-amber-400 border border-amber-500/30 rounded-full flex items-center space-x-1">
                <Crown className="w-3 h-3" />
                <span>{user.plan || 'FREE'} PLAN</span>
              </span>
            </div>
            <p className="text-xs text-slate-400 font-medium">{user.company} — Address Book & Devices</p>
          </div>
        </div>

        {/* Navbar Right Actions */}
        <div className="flex items-center space-x-3">
          <LanguageSelector />

          {user.role === 'ADMIN' && (
            <button
              onClick={() => setIsSecurityOpen(true)}
              className="px-3.5 py-2 rounded-xl bg-slate-900 hover:bg-slate-800 text-emerald-400 border border-emerald-500/30 text-xs font-semibold transition-all flex items-center space-x-1.5 shadow-md shadow-emerald-500/10"
            >
              <ShieldCheck className="w-4 h-4" />
              <span>Security Guard</span>
            </button>
          )}

          <button
            onClick={() => setIsPricingOpen(true)}
            className="px-3.5 py-2 rounded-xl bg-gradient-to-r from-amber-500 to-orange-500 hover:from-amber-400 hover:to-orange-400 text-slate-950 text-xs font-bold transition-all shadow-lg shadow-amber-500/20 flex items-center space-x-1.5"
          >
            <Crown className="w-4 h-4" />
            <span>Upgrade Plan</span>
          </button>

          <div className="text-right hidden sm:block">
            <div className="text-xs font-bold text-slate-200">{user.name}</div>
            <div className="text-[10px] text-slate-500">{user.email}</div>
          </div>

          <button
            onClick={onLogout}
            className="p-2 rounded-xl bg-slate-900 hover:bg-rose-500/20 hover:text-rose-400 text-slate-400 border border-slate-800 transition-all cursor-pointer"
            title="Logout"
          >
            <ExternalLink className="w-4 h-4" />
          </button>
        </div>
      </header>

      {/* Main Content Area */}
      <main className="flex-1 py-8">
        <div className="max-w-7xl mx-auto space-y-8">
          
          {/* Quick Connect by 9-Digit ID Banner */}
          <div className="glass-card rounded-2xl p-6 border border-blue-500/30 bg-gradient-to-r from-blue-950/40 via-slate-900 to-indigo-950/40 shadow-xl relative overflow-hidden">
            <div className="flex flex-col md:flex-row items-start md:items-center justify-between gap-6">
              <div className="space-y-1 max-w-lg">
                <div className="flex items-center space-x-2">
                  <span className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse"></span>
                  <h2 className="text-base font-bold text-white flex items-center space-x-2">
                    <Radio className="w-4 h-4 text-blue-400 animate-pulse" />
                    <span>Hızlı Oturum Bağlantısı (Otomatik ID Eşleştirme)</span>
                  </h2>
                </div>
                <p className="text-xs text-slate-400 leading-relaxed">
                  Karşı bilgisayarın ekranında yazan 9 haneli ID'yi girin. Sistem otomatik olarak IP ve ağ taraması yaparak WebRTC P2P ile anında bağlanacaktır.
                </p>
              </div>

              <form onSubmit={handleQuickConnect} className="flex items-center space-x-2 w-full md:w-auto">
                <div className="relative flex-1 md:w-72">
                  <Search className="w-4 h-4 absolute left-3.5 top-3 text-slate-500" />
                  <input
                    type="text"
                    placeholder="9 Haneli ID (Örn: 128 575 981)"
                    value={quickTargetId}
                    onChange={(e) => setQuickTargetId(e.target.value)}
                    className="w-full pl-10 pr-4 py-2.5 bg-slate-950 border border-slate-700 rounded-xl text-sm font-mono tracking-wider text-emerald-400 font-bold placeholder:text-slate-600 focus:outline-none focus:border-blue-500"
                    required
                  />
                </div>
                <button
                  type="submit"
                  className="px-5 py-2.5 bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-500 hover:to-indigo-500 text-white rounded-xl text-xs font-bold shadow-lg shadow-blue-500/25 transition-all flex items-center space-x-2 cursor-pointer shrink-0"
                >
                  <span>Hemen Bağlan</span>
                  <ArrowRight className="w-4 h-4" />
                </button>
              </form>
            </div>
          </div>

          {/* Section: Address Book Title Bar */}
          <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
            <div>
              <div className="flex items-center space-x-2">
                <h2 className="text-xl font-bold text-slate-100">Kayıtlı Cihazlar & Adres Defteri</h2>
                <span className="text-[11px] font-semibold px-2.5 py-0.5 rounded-full bg-slate-800 text-cyan-400 border border-slate-700">
                  {devices.length} / {getDeviceLimit() === 9999 ? 'Sınırsız' : `${getDeviceLimit()} Cihaz`}
                </span>
              </div>
              <p className="text-xs text-slate-400 mt-0.5">Sık bağlandığınız bilgisayarlar ve 1-Click uzaktan kontrol listesi</p>
            </div>

            <div className="flex items-center space-x-3 w-full sm:w-auto">
              <button
                onClick={fetchDevices}
                className="p-2.5 rounded-xl bg-slate-900 hover:bg-slate-800 text-slate-400 border border-slate-800 transition-all cursor-pointer"
                title="Refresh Devices"
              >
                <RefreshCw className={`w-4 h-4 ${loading ? 'animate-spin' : ''}`} />
              </button>

              <a
                href="/AetherDesk-QuickSupport.zip"
                download="AetherDesk-QuickSupport.zip"
                className="px-4 py-2.5 rounded-xl bg-slate-900 hover:bg-slate-800 text-blue-400 border border-blue-500/30 text-xs font-medium transition-all flex items-center space-x-2"
              >
                <Download className="w-4 h-4" />
                <span>Ajan İndir (.zip)</span>
              </a>

              <button
                onClick={openAddModal}
                className="px-4 py-2.5 rounded-xl bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-500 hover:to-indigo-500 text-white text-xs font-medium shadow-lg shadow-blue-500/25 transition-all flex items-center space-x-2 cursor-pointer"
              >
                <Plus className="w-4 h-4" />
                <span>Yeni Cihaz Kaydet</span>
              </button>
            </div>
          </div>

          {/* Devices Grid Cards */}
          {loading ? (
            <div className="py-20 text-center text-slate-500 text-sm">
              <RefreshCw className="w-8 h-8 animate-spin mx-auto text-blue-500 mb-2" />
              Cihazlar yükleniyor...
            </div>
          ) : devices.length === 0 ? (
            <div className="py-16 text-center glass-card rounded-2xl border border-slate-800 p-8 space-y-4">
              <Monitor className="w-12 h-12 text-slate-600 mx-auto" />
              <div>
                <h3 className="text-base font-bold text-slate-300">Henüz Kayıtlı Cihaz Yok</h3>
                <p className="text-xs text-slate-500 mt-1 max-w-md mx-auto">
                  Karşı bilgisayarda Ajanı açın ve ekranda görünen 9 haneli ID'yi yukarıdaki hızlı kutuya girerek hemen bağlanın veya "+ Yeni Cihaz Kaydet" ile listenize ekleyin.
                </p>
              </div>
              <button
                onClick={openAddModal}
                className="px-5 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-500 text-white text-xs font-semibold shadow-lg shadow-blue-500/20 cursor-pointer"
              >
                + İlk Cihazınızı Ekleyin
              </button>
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
              {devices.map((device) => {
                const isSelected = selectedDevices.has(device.session_id);
                return (
                  <div
                    key={device.id}
                    className={`glass-card rounded-2xl p-5 border transition-all relative overflow-hidden group flex flex-col justify-between space-y-4 ${
                      isSelected ? 'border-blue-500/80 bg-blue-950/20 shadow-lg shadow-blue-500/10' : 'border-slate-800 hover:border-slate-700 bg-slate-900/40'
                    }`}
                  >
                    <div>
                      {/* Top Header Card */}
                      <div className="flex items-start justify-between">
                        <div className="flex items-center space-x-3">
                          <input
                            type="checkbox"
                            checked={isSelected}
                            onChange={() => handleToggleSelect(device.session_id)}
                            className="w-4 h-4 rounded bg-slate-900 border-slate-700 text-blue-600 focus:ring-0 cursor-pointer"
                          />
                          <div className="p-2.5 rounded-xl bg-slate-800/80 text-blue-400 border border-slate-700/60">
                            <Monitor className="w-5 h-5" />
                          </div>
                          <div>
                            <h3 className="text-sm font-bold text-slate-100 group-hover:text-blue-400 transition-colors">
                              {device.name}
                            </h3>
                            <div className="text-[11px] font-mono text-emerald-400 font-bold tracking-wider">
                              ID: {device.session_id}
                            </div>
                          </div>
                        </div>

                        {/* Online Indicator */}
                        <div className="flex items-center space-x-1.5 bg-slate-950 px-2.5 py-1 rounded-full border border-slate-800">
                          <span className={`w-2 h-2 rounded-full ${device.is_online ? 'bg-emerald-400 animate-pulse' : 'bg-slate-600'}`}></span>
                          <span className="text-[10px] font-mono text-slate-400">
                            {device.is_online ? 'ONLINE' : 'OFFLINE'}
                          </span>
                        </div>
                      </div>

                      {/* Network Routing Info */}
                      <div className="mt-4 p-3 rounded-xl bg-slate-950/70 border border-slate-800/80 space-y-1 text-[11px] font-mono">
                        <div className="flex items-center justify-between text-slate-400">
                          <span className="flex items-center space-x-1">
                            {device.direct_ip?.includes('.') && !device.direct_ip.includes('Cloud') && !device.direct_ip.includes('Auto') ? (
                              <Wifi className="w-3 h-3 text-emerald-400" />
                            ) : (
                              <Globe className="w-3 h-3 text-blue-400" />
                            )}
                            <span>Bağlantı Modu:</span>
                          </span>
                          <span className="text-slate-200 font-bold">
                            {device.direct_ip?.includes('.') && !device.direct_ip.includes('Cloud') && !device.direct_ip.includes('Auto') ? 'Yerel Ağ (LAN)' : 'WebRTC P2P (Farklı Ağ)'}
                          </span>
                        </div>
                        <div className="flex items-center justify-between text-slate-500">
                          <span>Hedef IP / Port:</span>
                          <span className="text-blue-400 font-semibold">{device.direct_ip || 'Otomatik Eşleştirme'}</span>
                        </div>
                      </div>
                    </div>

                    {/* Action Buttons */}
                    <div className="flex items-center space-x-2 pt-2 border-t border-slate-800/80">
                      <button
                        onClick={() => handleConnectDevice(device)}
                        className="flex-1 py-2.5 bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-500 hover:to-indigo-500 text-white rounded-xl text-xs font-bold shadow-md shadow-blue-500/20 transition-all flex items-center justify-center space-x-1.5 cursor-pointer"
                      >
                        <span>1-Click Connect</span>
                        <ExternalLink className="w-3.5 h-3.5" />
                      </button>

                      <button
                        onClick={() => openEditModal(device)}
                        className="p-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 hover:text-white border border-slate-700 transition-all cursor-pointer"
                        title="Cihaz Bilgilerini Düzenle"
                      >
                        <Edit3 className="w-4 h-4" />
                      </button>

                      <button
                        onClick={() => handleDeleteDevice(device.id)}
                        className="p-2.5 rounded-xl bg-slate-800/50 hover:bg-rose-500/20 text-slate-400 hover:text-rose-400 border border-slate-700/50 transition-all cursor-pointer"
                        title="Adres Defterinden Sil"
                      >
                        <Trash2 className="w-4 h-4" />
                      </button>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </main>

      {/* Add / Edit Device Modal */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/75 backdrop-blur-sm p-4 animate-in fade-in duration-150">
          <div className="glass-card w-full max-w-md rounded-2xl p-6 border border-slate-700 shadow-2xl bg-slate-900/95 space-y-4">
            <div className="flex items-center justify-between pb-3 border-b border-slate-800">
              <h3 className="text-base font-bold text-slate-100 flex items-center space-x-2">
                <Monitor className="w-5 h-5 text-blue-400" />
                <span>{editingDeviceId ? 'Cihaz Bilgilerini Düzenle' : 'Yeni Cihaz Ekle'}</span>
              </h3>
              <button onClick={() => setIsModalOpen(false)} className="text-slate-400 hover:text-white">✕</button>
            </div>

            <form onSubmit={handleSaveDevice} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold text-slate-300 mb-1">Cihaz Adı (Açıklama)</label>
                <input
                  type="text"
                  placeholder="Örn: Ofis Bilgisayarı / Muhasebe"
                  value={deviceName}
                  onChange={(e) => setDeviceName(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2.5 text-sm text-slate-100 focus:outline-none focus:border-blue-500"
                  required
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-300 mb-1">9 Haneli Oturum ID'si</label>
                <input
                  type="text"
                  placeholder="Örn: 128 575 981"
                  value={sessionId}
                  onChange={(e) => setSessionId(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2.5 text-sm font-mono tracking-widest text-emerald-400 font-bold focus:outline-none focus:border-blue-500"
                  required
                />
                <p className="text-[10px] text-slate-500 mt-1">Karşı bilgisayarın ajan ekranında yazan 9 haneli numarayı girin.</p>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-300 mb-1">Bağlantı Türü</label>
                <div className="grid grid-cols-2 gap-2 mb-2">
                  <button
                    type="button"
                    onClick={() => setConnectionMode('AUTO_P2P')}
                    className={`py-2 px-3 rounded-xl text-xs font-semibold border flex items-center justify-center space-x-1.5 transition-all ${
                      connectionMode === 'AUTO_P2P' ? 'bg-blue-600 border-blue-500 text-white' : 'bg-slate-950 border-slate-800 text-slate-400'
                    }`}
                  >
                    <Globe className="w-3.5 h-3.5" />
                    <span>Farklı Ağ (WebRTC)</span>
                  </button>

                  <button
                    type="button"
                    onClick={() => setConnectionMode('DIRECT_LAN')}
                    className={`py-2 px-3 rounded-xl text-xs font-semibold border flex items-center justify-center space-x-1.5 transition-all ${
                      connectionMode === 'DIRECT_LAN' ? 'bg-blue-600 border-blue-500 text-white' : 'bg-slate-950 border-slate-800 text-slate-400'
                    }`}
                  >
                    <Wifi className="w-3.5 h-3.5" />
                    <span>Aynı Ağ (Yerel IP)</span>
                  </button>
                </div>

                {connectionMode === 'DIRECT_LAN' ? (
                  <div>
                    <input
                      type="text"
                      placeholder="Örn: 192.168.1.34:8443 (Boş bırakırsanız otomatik taranır)"
                      value={directIp}
                      onChange={(e) => setDirectIp(e.target.value)}
                      className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2.5 text-sm font-mono text-slate-100 focus:outline-none focus:border-blue-500"
                    />
                    <p className="text-[10px] text-slate-500 mt-1">İsteğe bağlıdır. Karşı bilgisayarın ajanında yazan IP adresidir.</p>
                  </div>
                ) : (
                  <div className="p-2.5 rounded-xl bg-slate-950 border border-slate-800/80 text-[11px] text-slate-400 font-mono">
                    ✓ Otomatik P2P: Dünyanın her yerinden doğrudan 9 haneli ID ile bağlanılır (IP girmeniz gerekmez).
                  </div>
                )}
              </div>

              <div className="flex justify-end space-x-3 pt-2">
                <button
                  type="button"
                  onClick={() => setIsModalOpen(false)}
                  className="px-4 py-2.5 rounded-xl bg-slate-800 text-slate-300 text-xs font-semibold hover:bg-slate-700"
                >
                  İptal
                </button>
                <button
                  type="submit"
                  className="px-5 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-500 text-white text-xs font-bold shadow-md shadow-blue-500/25"
                >
                  {editingDeviceId ? 'Değişiklikleri Kaydet' : 'Cihazı Kaydet'}
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

      {/* Remote Desktop Live Stream Modal */}
      <RemoteDesktopModal
        device={activeRemoteDevice}
        isOpen={!!activeRemoteDevice}
        onClose={() => setActiveRemoteDevice(null)}
      />

      {/* Footer Status */}
      <footer className="pt-4 border-t border-slate-800/60 flex items-center justify-between text-xs text-slate-500 font-mono">
        <div>AetherDesk SaaS Portal v1.0.0</div>
        <div>WebRTC P2P & Direct LAN Ready</div>
      </footer>
    </div>
  );
};
