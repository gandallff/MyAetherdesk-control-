import React, { createContext, useContext, useState, useEffect } from 'react';

export type Language = 'TR' | 'EN';

export interface Translations {
  // Navbar
  brandSub: string;
  navFeatures: string;
  navPricing: string;
  navSecurity: string;
  signIn: string;
  downloadAgent: string;

  // Hero Section
  badgeGpu: string;
  heroTitleLine1: string;
  heroTitleLine2: string;
  heroSubtitle: string;
  downloadFreeAgent: string;
  launchDashboard: string;
  liveRendererTitle: string;
  liveSessionActive: string;

  // Feature Cards
  featureSpeedTitle: string;
  featureSpeedSub: string;
  featDxgiTitle: string;
  featDxgiDesc: string;
  featChunkTitle: string;
  featChunkDesc: string;
  featSecurityTitle: string;
  featSecurityDesc: string;

  // Dashboard & Address Book
  addressBookTitle: string;
  addressBookSub: string;
  addDevice: string;
  tokenizedSetup: string;
  noDevicesTitle: string;
  noDevicesSub: string;
  addFirstDevice: string;
  directIp: string;
  statusOnline: string;
  statusOffline: string;
  oneClickConnect: string;

  // Modals & Auth
  workEmail: string;
  passwordLabel: string;
  createAccount: string;
  signInBtn: string;
  alreadyAccount: string;
  noAccount: string;
  upgradeLicenseTitle: string;
  upgradeLicenseSub: string;
  currentPlan: string;
  upgradeBtn: string;
}

const dictionaries: Record<Language, Translations> = {
  TR: {
    // Navbar
    brandSub: "KURUMSAL",
    navFeatures: "Özellikler",
    navPricing: "Fiyatlandırma",
    navSecurity: "Güvenlik Mimarisi",
    signIn: "Giriş Yap",
    downloadAgent: "Ajanı İndir (.exe)",

    // Hero Section
    badgeGpu: "DXGI 60 FPS GPU Hızlandırma ve NVENC Donanım Mimarisi",
    heroTitleLine1: "Ultra Düşük Gecikmeli Uzak Masaüstü",
    heroTitleLine2: "Kesintisiz Kontrol İçin Tasarlandı",
    heroSubtitle: "AnyDesk ve TeamViewer alternatifi. Anında 9 haneli oturum bağlantısı, 64KB ikili dosya aktarımı, katılımsız başlatma ve sıfır konfigürasyonlu WebRTC P2P yayınları.",
    downloadFreeAgent: "Ücretsiz Ajan İndir (.exe / .bat)",
    launchDashboard: "Hızlı Web Bağlantısı (Web Viewer)",
    liveRendererTitle: "AetherDesk Düşük Gecikmeli Render — 1920x1080 @ 60 FPS",
    liveSessionActive: "Canlı WebRTC Oturumu Aktif",

    // Feature Cards
    featureSpeedTitle: "Eşsiz Hız ve Güvenlik İçin Tasarlandı",
    featureSpeedSub: "Yerel C++/Rust sürücüleri ve WebRTC altyapısı ile sıfırdan geliştirildi.",
    featDxgiTitle: "60 FPS DXGI Yakalama",
    featDxgiDesc: "DirectX 11 GPU Desktop Duplication API ile 5ms altında gecikme ve Dirty Region band genişliği optimizasyonu.",
    featChunkTitle: "64KB Binary Dosya Motoru",
    featChunkDesc: "Dinamik backpressure ve SHA-256 doğrulama ile yüksek hızlı SCTP DataChannel binary parça akışı.",
    featSecurityTitle: "TLS 1.3 ve DTLS-SRTP",
    featSecurityDesc: "Uçtan uca AES-256-GCM medya şifreleme, Windows başlangıç servisi ve Katılımsız parola koruması.",

    // Dashboard & Address Book
    addressBookTitle: "Kayıtlı Cihazlar ve Adres Defteri",
    addressBookSub: "Canlı bilgisayar durumları ve 1-Tıkla P2P Uzaktan Bağlantı",
    addDevice: "Cihaz Ekle",
    tokenizedSetup: "Hesaba Özel Kurulum İndir",
    noDevicesTitle: "Henüz Cihaz Eklenmedi",
    noDevicesSub: "Tek tıkla uzaktan bağlantı için bilgisayarlarınızı SaaS Adres Defterinize ekleyin.",
    addFirstDevice: "İlk Cihazınızı Ekleyin",
    directIp: "Doğrudan IP:",
    statusOnline: "ÇEVRİMİÇİ",
    statusOffline: "ÇEVRİMDIŞI",
    oneClickConnect: "1-Tıkla Bağlan",

    // Modals & Auth
    workEmail: "İş E-Posta Adresi",
    passwordLabel: "Parola",
    createAccount: "SaaS Hesabı Oluştur",
    signInBtn: "Portala Giriş Yap",
    alreadyAccount: "Zaten hesabınız var mı? Giriş Yapın",
    noAccount: "Hesabınız yok mu? Kaydolun",
    upgradeLicenseTitle: "AetherDesk SaaS Lisansınızı Yükseltin",
    upgradeLicenseSub: "Adres Defteri, 64KB Dosya Transferi ve Katılımsız Erişimi Açın",
    currentPlan: "Mevcut Paketiniz",
    upgradeBtn: "Pakete Yükselt",
  },
  EN: {
    // Navbar
    brandSub: "ENTERPRISE",
    navFeatures: "Features",
    navPricing: "Pricing Plans",
    navSecurity: "Security Architecture",
    signIn: "Sign In",
    downloadAgent: "Download Agent (.exe)",

    // Hero Section
    badgeGpu: "DXGI 60 FPS GPU Acceleration & Hardware NVENC Pipeline",
    heroTitleLine1: "Ultra-Low Latency Remote Desktop",
    heroTitleLine2: "Engineered For Seamless Control",
    heroSubtitle: "AnyDesk and TeamViewer alternative. Instant 9-digit session connection, 64KB binary chunk file transfer, unattended boot access, and zero-configuration WebRTC P2P streams.",
    downloadFreeAgent: "Download Free Agent (.exe / .bat)",
    launchDashboard: "Quick Web Connection (Web Viewer)",
    liveRendererTitle: "AetherDesk Low Latency Renderer — 1920x1080 @ 60 FPS",
    liveSessionActive: "Live WebRTC Session Active",

    // Feature Cards
    featureSpeedTitle: "Engineered For Unmatched Speed & Security",
    featureSpeedSub: "Built from the ground up with native C++/Rust drivers and WebRTC infrastructure.",
    featDxgiTitle: "60 FPS DXGI Capture",
    featDxgiDesc: "DirectX 11 GPU Desktop Duplication API capture under 5ms latency with Dirty Region bandwidth optimization.",
    featChunkTitle: "64KB Binary File Engine",
    featChunkDesc: "High-speed SCTP DataChannel binary chunk streaming with dynamic backpressure and SHA-256 integrity verification.",
    featSecurityTitle: "TLS 1.3 & DTLS-SRTP",
    featSecurityDesc: "End-to-end AES-256-GCM media encryption, Windows boot registry auto-start, and Unattended password protection.",

    // Dashboard & Address Book
    addressBookTitle: "Registered Devices & Address Book",
    addressBookSub: "Live workstation status and 1-Click P2P Remote Connect",
    addDevice: "Add Device",
    tokenizedSetup: "Tokenized Agent Setup",
    noDevicesTitle: "No Devices Added Yet",
    noDevicesSub: "Add your remote PCs to your SaaS Address Book for instant 1-Click Remote Desktop connection.",
    addFirstDevice: "Add Your First Device",
    directIp: "Direct IP:",
    statusOnline: "ONLINE",
    statusOffline: "OFFLINE",
    oneClickConnect: "1-Click Connect",

    // Modals & Auth
    workEmail: "Work Email",
    passwordLabel: "Password",
    createAccount: "Create SaaS Account",
    signInBtn: "Sign In to Portal",
    alreadyAccount: "Already have an account? Sign In",
    noAccount: "Don't have an account? Register",
    upgradeLicenseTitle: "Upgrade Your AetherDesk SaaS License",
    upgradeLicenseSub: "Unlock Address Book, 64KB File Transfer & Unattended Access",
    currentPlan: "Current Plan",
    upgradeBtn: "Upgrade to Plan",
  }
};

interface LanguageContextType {
  language: Language;
  setLanguage: (lang: Language) => void;
  t: Translations;
}

const LanguageContext = createContext<LanguageContextType | undefined>(undefined);

export const LanguageProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [language, setLanguageState] = useState<Language>(() => {
    const saved = localStorage.getItem('aether_lang');
    return (saved === 'TR' || saved === 'EN') ? saved : 'TR'; // Default to Turkish
  });

  const setLanguage = (lang: Language) => {
    setLanguageState(lang);
    localStorage.setItem('aether_lang', lang);
  };

  return (
    <LanguageContext.Provider value={{ language, setLanguage, t: dictionaries[language] }}>
      {children}
    </LanguageContext.Provider>
  );
};

export const useLanguage = () => {
  const ctx = useContext(LanguageContext);
  if (!ctx) throw new Error('useLanguage must be used within LanguageProvider');
  return ctx;
};
