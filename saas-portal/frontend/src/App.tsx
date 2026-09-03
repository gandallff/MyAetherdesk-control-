import React, { useState, useEffect } from 'react';
import { ApiService, User } from './services/api';
import { LanguageProvider } from './context/LanguageContext';
import { LandingPage } from './pages/LandingPage';
import { LoginPage } from './pages/LoginPage';
import { DashboardPage } from './pages/DashboardPage';

export const AppContent: React.FC = () => {
  // Synchronous URL inspection
  const checkIsAuthUrl = (): boolean => {
    const fullUrl = window.location.href.toLowerCase();
    const searchParams = new URLSearchParams(window.location.search);
    const hash = window.location.hash || '';
    const hashParams = new URLSearchParams(hash.includes('?') ? hash.substring(hash.indexOf('?')) : '');

    return (
      fullUrl.includes('login') ||
      fullUrl.includes('register') ||
      fullUrl.includes('google_login') ||
      searchParams.has('action') ||
      searchParams.has('device_id') ||
      searchParams.has('provider') ||
      searchParams.has('google_login') ||
      hashParams.has('action') ||
      hashParams.has('device_id') ||
      hashParams.has('provider')
    );
  };

  const isAuthInitially = checkIsAuthUrl();
  const [currentUser, setCurrentUser] = useState<User | null>(null);
  const [showAuthModal, setShowAuthModal] = useState<boolean>(isAuthInitially);
  const [loading, setLoading] = useState<boolean>(!isAuthInitially);

  useEffect(() => {
    // If not on an explicit auth page, check stored login state
    if (!isAuthInitially) {
      ApiService.getCurrentUser()
        .then((res) => {
          setCurrentUser(res.user);
        })
        .catch(() => {
          ApiService.clearToken();
        })
        .finally(() => {
          setLoading(false);
        });
    }
  }, [isAuthInitially]);

  const handleLogout = () => {
    ApiService.clearToken();
    setCurrentUser(null);
    setShowAuthModal(false);
    window.history.replaceState({}, document.title, window.location.pathname);
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-[#090d16] text-slate-400 flex items-center justify-center text-sm font-mono">
        Loading AetherDesk SaaS Platform...
      </div>
    );
  }

  // 1. If explicit auth URL requested -> ALWAYS render Image 1 (LoginPage / Register)
  if (showAuthModal && !currentUser) {
    return (
      <LoginPage
        onLoginSuccess={(user) => {
          setCurrentUser(user);
          setShowAuthModal(false);
        }}
        onBackToHome={() => {
          setShowAuthModal(false);
          window.history.replaceState({}, document.title, window.location.pathname);
        }}
      />
    );
  }

  // 2. Logged In View -> Dashboard Page
  if (currentUser) {
    return (
      <DashboardPage
        user={currentUser}
        onLogout={handleLogout}
        onUserUpdated={(updated) => setCurrentUser(updated)}
      />
    );
  }

  // 3. Commercial Marketing Landing Page
  return <LandingPage onOpenAuth={() => setShowAuthModal(true)} />;
};

export const App: React.FC = () => {
  return (
    <LanguageProvider>
      <AppContent />
    </LanguageProvider>
  );
};
