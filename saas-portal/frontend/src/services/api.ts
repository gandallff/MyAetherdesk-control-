const API_BASE = (import.meta as any).env?.VITE_API_URL || 'http://localhost:5000/api';

export interface User {
  id: string;
  email: string;
  name: string;
  role: 'ADMIN' | 'USER' | 'OPERATOR';
  company: string;
  plan?: 'FREE' | 'PRO' | 'ENTERPRISE';
  subscription_status?: string;
}

export interface Plan {
  id: 'FREE' | 'PRO' | 'ENTERPRISE';
  name: string;
  price: string;
  period: string;
  popular?: boolean;
  features: string[];
  maxDevices: number;
}

export interface Device {
  id: string;
  user_id: string;
  name: string;
  session_id: string;
  is_online: number;
  direct_ip: string;
  direct_port: number;
  last_seen: string;
}

export interface SecurityAlert {
  id: string;
  device_id: string;
  device_name: string;
  alert_type: string;
  severity: 'LOW' | 'MEDIUM' | 'HIGH' | 'CRITICAL';
  details: string;
  status: 'ACTIVE' | 'RESOLVED' | 'QUARANTINED';
  created_at: string;
}

// Initial Mock Devices for Cloud Demo
const DEFAULT_DEVICES: Device[] = [
  {
    id: 'dev_01',
    user_id: 'usr_admin',
    name: 'Ana Sunucu (Office Server)',
    session_id: '482 910 375',
    is_online: 1,
    direct_ip: '192.168.1.100',
    direct_port: 8443,
    last_seen: 'Just now'
  },
  {
    id: 'dev_02',
    user_id: 'usr_admin',
    name: 'Muhasebe Is Istasyonu',
    session_id: '891 204 153',
    is_online: 1,
    direct_ip: '192.168.1.105',
    direct_port: 8443,
    last_seen: '2 mins ago'
  },
  {
    id: 'dev_03',
    user_id: 'usr_admin',
    name: 'Yonetici Laptop (Remote)',
    session_id: '739 184 920',
    is_online: 0,
    direct_ip: '192.168.1.110',
    direct_port: 8443,
    last_seen: '1 hour ago'
  }
];

export class ApiService {
  private static getToken(): string | null {
    return localStorage.getItem('aether_token');
  }

  public static setToken(token: string): void {
    localStorage.setItem('aether_token', token);
  }

  public static clearToken(): void {
    localStorage.removeItem('aether_token');
    localStorage.removeItem('aether_user');
  }

  private static getStoredUser(): User | null {
    const userStr = localStorage.getItem('aether_user');
    if (userStr) {
      try { return JSON.parse(userStr); } catch { return null; }
    }
    return null;
  }

  private static setStoredUser(user: User): void {
    localStorage.setItem('aether_user', JSON.stringify(user));
  }

  private static getStoredDevices(): Device[] {
    const devStr = localStorage.getItem('aether_devices');
    if (devStr) {
      try { return JSON.parse(devStr); } catch { return DEFAULT_DEVICES; }
    }
    localStorage.setItem('aether_devices', JSON.stringify(DEFAULT_DEVICES));
    return DEFAULT_DEVICES;
  }

  private static setStoredDevices(devices: Device[]): void {
    localStorage.setItem('aether_devices', JSON.stringify(devices));
  }

  private static async request(endpoint: string, options: RequestInit = {}): Promise<any> {
    const token = this.getToken();
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      ...(options.headers as Record<string, string>),
    };

    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    try {
      const res = await fetch(`${API_BASE}${endpoint}`, {
        ...options,
        headers,
        signal: AbortSignal.timeout(3000)
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data.message || 'API request failed');
      return data;
    } catch {
      // Fallback seamlessly to local simulation
      return null;
    }
  }

  public static async login(email: string, password: string): Promise<{ token: string; user: User }> {
    // 1. Try real backend first if accessible
    const backendRes = await this.request('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    });

    if (backendRes?.token && backendRes?.user) {
      this.setStoredUser(backendRes.user);
      return backendRes;
    }

    // 2. Client-Side Authentication Fallback (for Vercel Cloud)
    let user: User;
    if (email.toLowerCase().includes('admin') || password === 'admin2026') {
      user = {
        id: 'usr_admin_01',
        email: email || 'admin@aetherdesk.com',
        name: 'System Administrator',
        role: 'ADMIN',
        company: 'AetherDesk Enterprise HQ',
        plan: 'PRO',
        subscription_status: 'ACTIVE'
      };
    } else {
      user = {
        id: `usr_${Math.random().toString(36).substring(2, 9)}`,
        email: email,
        name: email.split('@')[0] || 'AetherDesk User',
        role: 'USER',
        company: 'AetherDesk Client',
        plan: 'FREE',
        subscription_status: 'ACTIVE'
      };
    }

    const token = `jwt_mock_${Math.random().toString(36).substring(2, 15)}`;
    this.setToken(token);
    this.setStoredUser(user);
    return { token, user };
  }

  public static async register(email: string, password: string, name: string, role: string = 'USER'): Promise<{ token: string; user: User }> {
    const backendRes = await this.request('/auth/register', {
      method: 'POST',
      body: JSON.stringify({ email, password, name, role }),
    });

    if (backendRes?.token && backendRes?.user) {
      this.setStoredUser(backendRes.user);
      return backendRes;
    }

    const user: User = {
      id: `usr_${Math.random().toString(36).substring(2, 9)}`,
      email,
      name,
      role: role === 'ADMIN' ? 'ADMIN' : 'USER',
      company: 'AetherDesk Enterprise HQ',
      plan: role === 'ADMIN' ? 'PRO' : 'FREE',
      subscription_status: 'ACTIVE'
    };

    const token = `jwt_mock_${Math.random().toString(36).substring(2, 15)}`;
    this.setToken(token);
    this.setStoredUser(user);
    return { token, user };
  }

  public static async getCurrentUser(): Promise<{ user: User }> {
    const backendRes = await this.request('/auth/me');
    if (backendRes?.user) {
      this.setStoredUser(backendRes.user);
      return backendRes;
    }

    const stored = this.getStoredUser();
    if (stored) {
      return { user: stored };
    }

    const defaultAdmin: User = {
      id: 'usr_admin_01',
      email: 'admin@aetherdesk.com',
      name: 'System Administrator',
      role: 'ADMIN',
      company: 'AetherDesk Enterprise HQ',
      plan: 'PRO',
      subscription_status: 'ACTIVE'
    };
    return { user: defaultAdmin };
  }

  public static async getDevices(): Promise<{ devices: Device[] }> {
    const backendRes = await this.request('/devices');
    if (backendRes?.devices) {
      this.setStoredDevices(backendRes.devices);
      return backendRes;
    }
    return { devices: this.getStoredDevices() };
  }

  public static async addDevice(name: string, session_id: string, direct_ip?: string, direct_port?: number): Promise<{ device: Device }> {
    const backendRes = await this.request('/devices', {
      method: 'POST',
      body: JSON.stringify({ name, session_id, direct_ip, direct_port }),
    });

    if (backendRes?.device) {
      return backendRes;
    }

    const newDevice: Device = {
      id: `dev_${Math.random().toString(36).substring(2, 9)}`,
      user_id: 'usr_admin',
      name: name || 'Remote Workstation',
      session_id: session_id || '482 910 375',
      is_online: 1,
      direct_ip: direct_ip || '192.168.1.100',
      direct_port: direct_port || 8443,
      last_seen: 'Just now'
    };

    const devices = this.getStoredDevices();
    devices.unshift(newDevice);
    this.setStoredDevices(devices);
    return { device: newDevice };
  }

  public static async removeDevice(id: string): Promise<{ success: boolean }> {
    await this.request(`/devices/${id}`, { method: 'DELETE' });
    const devices = this.getStoredDevices().filter(d => d.id !== id);
    this.setStoredDevices(devices);
    return { success: true };
  }

  public static async getAdminUsers(): Promise<{ users: User[] }> {
    const backendRes = await this.request('/admin/users');
    if (backendRes?.users) return backendRes;
    return { users: [this.getStoredUser() || { id: 'usr_1', email: 'admin@aetherdesk.com', name: 'Admin', role: 'ADMIN', company: 'HQ' }] };
  }

  public static async getPlans(): Promise<{ plans: Plan[] }> {
    return {
      plans: [
        {
          id: 'FREE',
          name: 'QuickSupport Free',
          price: '$0',
          period: 'Lifetime',
          maxDevices: 1,
          features: ['1 Eşzamanlı Oturum', '60 FPS DXGI GPU Akışı', 'Temel Dosya Transferi', 'Topluluk Desteği']
        },
        {
          id: 'PRO',
          name: 'Pro Solo Specialist',
          price: '$15',
          period: '/ ay',
          popular: true,
          maxDevices: 5,
          features: ['5 Eşzamanlı Cihaz', 'NVENC H.264 Donanım Hızlandırma', 'Katılımsız Erişim (Unattended)', 'Çoklu Monitör Desteği', 'Öncelikli Destek']
        },
        {
          id: 'ENTERPRISE',
          name: 'Enterprise Grid Master',
          price: '$49',
          period: '/ ay',
          maxDevices: 50,
          features: ['Sınırsız Cihaz & Adres Defteri', 'Çoklu Grid İzleme Modu', 'Security Guard Tehdit Raporlama', 'Özel Domain & Şirket Logosu', '7/24 SLA Garantisi']
        }
      ]
    };
  }

  public static async upgradePlan(plan: string): Promise<{ success: boolean; user: User }> {
    const user = this.getStoredUser() || {
      id: 'usr_admin_01',
      email: 'admin@aetherdesk.com',
      name: 'System Administrator',
      role: 'ADMIN',
      company: 'AetherDesk Enterprise HQ',
      plan: 'FREE'
    };
    user.plan = plan as any;
    this.setStoredUser(user);
    return { success: true, user };
  }

  public static async getSecurityAlerts(): Promise<{ alerts: SecurityAlert[]; stats: { total_alerts: number; critical_count: number; active_count: number } }> {
    return {
      alerts: [
        {
          id: 'alt_01',
          device_id: 'dev_01',
          device_name: 'Ana Sunucu (Office Server)',
          alert_type: 'PORT_SCAN_ATTEMPT',
          severity: 'HIGH',
          details: '8443 Direct Portu uzerinde 14 basarisiz handshake girisimi engellendi.',
          status: 'ACTIVE',
          created_at: '10 mins ago'
        },
        {
          id: 'alt_02',
          device_id: 'dev_02',
          device_name: 'Muhasebe Is Istasyonu',
          alert_type: 'UNAUTHORIZED_CLIPBOARD',
          severity: 'LOW',
          details: 'Pano senkronizasyonunda 64KB boyut asimi engellendi.',
          status: 'RESOLVED',
          created_at: '2 hours ago'
        }
      ],
      stats: {
        total_alerts: 2,
        critical_count: 0,
        active_count: 1
      }
    };
  }

  public static async resolveSecurityAlert(alert_id: string, action: 'RESOLVE' | 'QUARANTINE'): Promise<{ success: boolean }> {
    return { success: true };
  }
}
