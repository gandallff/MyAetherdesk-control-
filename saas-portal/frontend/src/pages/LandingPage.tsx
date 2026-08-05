import React, { useState } from 'react';
import { useLanguage } from '../context/LanguageContext';
import { LanguageSelector } from '../components/LanguageSelector';
import { Download, Zap, ShieldCheck, Monitor, ArrowRight, Check, Lock, Globe, HardDrive, Cpu, Activity, Play, Sparkles, ExternalLink } from 'lucide-react';

interface LandingPageProps {
  onOpenAuth: () => void;
}

export const LandingPage: React.FC<LandingPageProps> = ({ onOpenAuth }) => {
  const { t } = useLanguage();
  const [activeTab, setActiveTab] = useState<'features' | 'pricing' | 'security'>('features');

  return (
    <div className="min-h-screen bg-[#080c14] text-slate-100 flex flex-col justify-between overflow-hidden relative">
      {/* Ambient Glows */}
      <div className="absolute top-[-10%] left-[20%] w-[600px] h-[600px] bg-blue-600/10 rounded-full blur-[140px] pointer-events-none"></div>
      <div className="absolute top-[40%] right-[10%] w-[500px] h-[500px] bg-indigo-600/10 rounded-full blur-[140px] pointer-events-none"></div>

      {/* Top Navbar */}
      <nav className="max-w-7xl mx-auto w-full px-6 py-6 flex items-center justify-between z-20">
        <div className="flex items-center space-x-3">
          <div className="p-2.5 bg-gradient-to-tr from-blue-600 to-indigo-600 rounded-xl shadow-lg shadow-blue-500/25 text-white font-bold">
            <Zap className="w-5 h-5" />
          </div>
          <div className="flex items-center space-x-2">
            <span className="text-xl font-extrabold tracking-tight text-white">AetherDesk</span>
            <span className="text-[10px] uppercase font-bold text-blue-400 bg-blue-500/10 border border-blue-500/20 px-2 py-0.5 rounded-full">
              {t.brandSub}
            </span>
          </div>
        </div>

        <div className="flex items-center space-x-4">
          <div className="hidden md:flex items-center space-x-6 text-xs text-slate-400 font-medium">
            <button onClick={() => setActiveTab('features')} className={`hover:text-white transition-all ${activeTab === 'features' ? 'text-blue-400 font-semibold' : ''}`}>{t.navFeatures}</button>
            <button onClick={() => setActiveTab('pricing')} className={`hover:text-white transition-all ${activeTab === 'pricing' ? 'text-blue-400 font-semibold' : ''}`}>{t.navPricing}</button>
            <button onClick={() => setActiveTab('security')} className={`hover:text-white transition-all ${activeTab === 'security' ? 'text-blue-400 font-semibold' : ''}`}>{t.navSecurity}</button>
          </div>

          <LanguageSelector />

          <button
            onClick={onOpenAuth}
            className="text-xs text-slate-300 hover:text-white font-semibold px-4 py-2 rounded-xl hover:bg-slate-900 transition-all border border-transparent hover:border-slate-800"
          >
            {t.signIn}
          </button>

          <a
            href="http://localhost:8080/download/agent"
            download="AetherDesk-QuickSupport.bat"
            className="bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-500 hover:to-indigo-500 text-white px-5 py-2.5 rounded-xl text-xs font-semibold transition-all shadow-lg shadow-blue-500/25 flex items-center space-x-2 border border-blue-400/20"
          >
            <Download className="w-4 h-4" />
            <span>{t.downloadAgent}</span>
          </a>
        </div>
      </nav>

      {/* Hero Section */}
      <section className="max-w-5xl mx-auto px-6 pt-12 pb-16 text-center z-10">
        <div className="inline-flex items-center space-x-2 px-3.5 py-1.5 bg-slate-900/90 border border-slate-800 text-slate-300 rounded-full text-xs font-medium mb-8 shadow-xl">
          <Sparkles className="w-4 h-4 text-amber-400" />
          <span>{t.badgeGpu}</span>
        </div>

        <h1 className="text-4xl md:text-6xl font-extrabold tracking-tight text-white mb-6 leading-tight">
          {t.heroTitleLine1} <br />
          <span className="bg-clip-text text-transparent bg-gradient-to-r from-blue-400 via-indigo-400 to-purple-400">
            {t.heroTitleLine2}
          </span>
        </h1>

        <p className="text-slate-400 text-sm md:text-base max-w-2xl mx-auto mb-10 leading-relaxed">
          {t.heroSubtitle}
        </p>

        <div className="flex flex-col sm:flex-row items-center justify-center gap-4 mb-16">
          <a
            href="http://localhost:8080/download/agent"
            download="AetherDesk-QuickSupport.bat"
            className="w-full sm:w-auto px-8 py-4 bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-500 hover:to-indigo-500 text-white font-semibold rounded-2xl shadow-xl shadow-blue-500/30 transition-all flex items-center justify-center space-x-2 text-sm border border-blue-400/20"
          >
            <Download className="w-5 h-5" />
            <span>{t.downloadFreeAgent}</span>
          </a>

          <a
            href="http://localhost:9000"
            target="_blank"
            rel="noopener noreferrer"
            className="w-full sm:w-auto px-8 py-4 bg-slate-900/80 hover:bg-slate-800/90 border border-slate-700/80 text-slate-200 font-semibold rounded-2xl transition-all flex items-center justify-center space-x-2 text-sm backdrop-blur-xl"
          >
            <span>{t.launchDashboard}</span>
            <ExternalLink className="w-4 h-4 text-blue-400" />
          </a>
        </div>

        {/* Live Mockup Viewport Banner */}
        <div className="glass-card rounded-2xl p-4 border border-slate-800 shadow-2xl max-w-4xl mx-auto overflow-hidden relative group">
          <div className="flex items-center justify-between px-4 py-2 border-b border-slate-800/80 text-xs text-slate-500 font-mono">
            <div className="flex items-center space-x-2">
              <span className="w-3 h-3 rounded-full bg-rose-500/80 inline-block"></span>
              <span className="w-3 h-3 rounded-full bg-amber-500/80 inline-block"></span>
              <span className="w-3 h-3 rounded-full bg-emerald-500/80 inline-block"></span>
              <span className="ml-2 text-slate-400">{t.liveRendererTitle}</span>
            </div>
            <div className="flex items-center space-x-2 text-emerald-400">
              <Activity className="w-3.5 h-3.5 animate-pulse" />
              <span>P2P Direct Link (8ms)</span>
            </div>
          </div>
          <div className="bg-slate-950/90 h-64 md:h-80 rounded-xl flex items-center justify-center relative overflow-hidden">
            <div className="absolute inset-0 bg-gradient-to-tr from-blue-900/20 to-purple-900/20 pointer-events-none"></div>
            <div className="text-center z-10 space-y-3">
              <div className="w-16 h-16 rounded-2xl bg-blue-600/20 text-blue-400 border border-blue-500/30 flex items-center justify-center mx-auto shadow-lg shadow-blue-500/10">
                <Play className="w-8 h-8 ml-1" />
              </div>
              <p className="text-sm font-semibold text-slate-200">{t.liveSessionActive}</p>
              <p className="text-xs font-mono text-slate-500">Host ID: 482 910 375 | NVENC H.264 Accelerated Stream</p>
            </div>
          </div>
        </div>
      </section>

      {/* Feature Showcase Grid */}
      <section className="max-w-6xl mx-auto px-6 py-12 z-10">
        <div className="text-center mb-12">
          <h2 className="text-2xl font-bold text-white mb-2">{t.featureSpeedTitle}</h2>
          <p className="text-xs text-slate-400 max-w-xl mx-auto">{t.featureSpeedSub}</p>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <div className="glass-card glass-card-hover p-6 rounded-2xl border border-slate-800">
            <div className="p-3 bg-blue-500/10 rounded-xl text-blue-400 w-fit mb-4 border border-blue-500/20">
              <Cpu className="w-6 h-6" />
            </div>
            <h3 className="text-base font-bold text-white mb-2">{t.featDxgiTitle}</h3>
            <p className="text-xs text-slate-400 leading-relaxed">{t.featDxgiDesc}</p>
          </div>

          <div className="glass-card glass-card-hover p-6 rounded-2xl border border-slate-800">
            <div className="p-3 bg-indigo-500/10 rounded-xl text-indigo-400 w-fit mb-4 border border-indigo-500/20">
              <HardDrive className="w-6 h-6" />
            </div>
            <h3 className="text-base font-bold text-white mb-2">{t.featChunkTitle}</h3>
            <p className="text-xs text-slate-400 leading-relaxed">{t.featChunkDesc}</p>
          </div>

          <div className="glass-card glass-card-hover p-6 rounded-2xl border border-slate-800">
            <div className="p-3 bg-purple-500/10 rounded-xl text-purple-400 w-fit mb-4 border border-purple-500/20">
              <Lock className="w-6 h-6" />
            </div>
            <h3 className="text-base font-bold text-white mb-2">{t.featSecurityTitle}</h3>
            <p className="text-xs text-slate-400 leading-relaxed">{t.featSecurityDesc}</p>
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="max-w-7xl mx-auto w-full px-6 py-8 border-t border-slate-800/60 flex flex-col md:flex-row items-center justify-between text-xs text-slate-500 z-10 gap-4">
        <div>© 2026 AetherDesk Remote Desktop. All rights reserved.</div>
        <div className="flex space-x-6 text-slate-400">
          <span className="hover:text-white cursor-pointer transition-all">{t.navFeatures}</span>
          <span className="hover:text-white cursor-pointer transition-all">{t.navPricing}</span>
          <span className="hover:text-white cursor-pointer transition-all">{t.navSecurity}</span>
        </div>
      </footer>
    </div>
  );
};
