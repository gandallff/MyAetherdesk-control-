import React, { useState } from 'react';
import { useLanguage, Language } from '../context/LanguageContext';
import { Globe, ChevronDown } from 'lucide-react';

export const LanguageSelector: React.FC = () => {
  const { language, setLanguage } = useLanguage();
  const [isOpen, setIsOpen] = useState(false);

  const handleSelect = (lang: Language) => {
    setLanguage(lang);
    setIsOpen(false);
  };

  return (
    <div className="relative z-50">
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="flex items-center space-x-1.5 px-3 py-1.5 rounded-xl bg-slate-900/90 hover:bg-slate-800 text-slate-200 text-xs font-semibold border border-slate-700/80 transition-all shadow-md"
      >
        <Globe className="w-3.5 h-3.5 text-blue-400" />
        <span>{language === 'TR' ? '🇹🇷 TR' : '🇬🇧 EN'}</span>
        <ChevronDown className="w-3 h-3 text-slate-400" />
      </button>

      {isOpen && (
        <div className="absolute right-0 mt-2 w-28 glass-card rounded-xl border border-slate-700 shadow-2xl py-1 z-50">
          <button
            onClick={() => handleSelect('TR')}
            className={`w-full text-left px-3 py-2 text-xs font-medium flex items-center space-x-2 transition-all ${
              language === 'TR' ? 'bg-blue-600/20 text-blue-400 font-bold' : 'text-slate-300 hover:bg-slate-800'
            }`}
          >
            <span>🇹🇷</span>
            <span>Türkçe</span>
          </button>
          <button
            onClick={() => handleSelect('EN')}
            className={`w-full text-left px-3 py-2 text-xs font-medium flex items-center space-x-2 transition-all ${
              language === 'EN' ? 'bg-blue-600/20 text-blue-400 font-bold' : 'text-slate-300 hover:bg-slate-800'
            }`}
          >
            <span>🇬🇧</span>
            <span>English</span>
          </button>
        </div>
      )}
    </div>
  );
};
