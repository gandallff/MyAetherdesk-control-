import React, { useState, useEffect } from 'react';
import { ApiService, User } from './services/api';
import { LanguageProvider } from './context/LanguageContext';
import { LandingPage } from './pages/LandingPage';
import { LoginPage } from './pages/LoginPage';
import { DashboardPage } from './pages/DashboardPage';

export const AppContent: React.FC = () => {
  const [currentUser, setCurrentUser] = useState<User | null>(null);
  const [showAuthModal, setShowAuthModal] = useState<boolean>(false);
  const [loading, setLoading] = useState<boolean>(true);

  useEffect(() => {
    const fullUrl = window.location.href;
    const searchParams = new URLSearchParams(window.location.search);
    const hash = window.location.hash || '';
    const hashParams = new URLSearchParams(hash.includes('?') ? hash.substring(hash.indexOf('?')) : '');

    const isAuthUrl = 
      fullUrl.includes('login') || 
      fullUrl.includes('register') || 
      searchParams.has('action') || 
      searchParams.has('device_id') || 
      searchParams.has('provider') ||
      hashParams.has('action') ||
      hashParams.has('device_id') ||
      hashParams.has('provider');

    if (isAuthUrl) {
      setShowAuthModal(true);
    }

    const directConnectId = searchParams.get('connect') || searchParams.get('id') || hashParams.get('connect') || hashParams.get('id');

    ApiService.getCurrentUser()
      .then((res) => {
        setCurrentUser(res.user);
      })
      .catch(() => {
        if (directConnectId) {
          const guestUser: User = {
            id: 'guest_operator',
            email: 'operator@aetherdesk.com',
            name: 'Operator',
            role: 'ADMIN',
            company: 'AetherDesk Direct',
            plan: 'PRO'
          };
          setCurrentUser(guestUser);
        } else {
          ApiService.clearToken();
        }
      })
      .finally(() => {
        setLoading(false);
      });
  }, []);

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

  // 1. Logged In View -> Dashboard Page
  if (currentUser) {
    return (
      <DashboardPage
        user={currentUser}
        onLogout={handleLogout}
        onUserUpdated={(updated) => setCurrentUser(updated)}
      />
    );
  }

  // 2. Auth Modal Screen
  if (showAuthModal) {
    return (
      <LoginPage
        onLoginSuccess={(user) => {
          setCurrentUser(user);
          setShowAuthModal(false);
        }}
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
