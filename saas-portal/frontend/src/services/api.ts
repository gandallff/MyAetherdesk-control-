const API_BASE = 'http://localhost:5000/api';

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

export class ApiService {
  private static getToken(): string | null {
    return localStorage.getItem('aether_token');
  }

  public static setToken(token: string): void {
    localStorage.setItem('aether_token', token);
  }

  public static clearToken(): void {
    localStorage.removeItem('aether_token');
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

    const res = await fetch(`${API_BASE}${endpoint}`, { ...options, headers });
    const data = await res.json();

    if (!res.ok) {
      throw new Error(data.message || 'API request failed');
    }

    return data;
  }

  public static login(email: string, password: string): Promise<{ token: string; user: User }> {
    return this.request('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    });
  }

  public static register(email: string, password: string, name: string, role: string = 'USER'): Promise<{ token: string; user: User }> {
    return this.request('/auth/register', {
      method: 'POST',
      body: JSON.stringify({ email, password, name, role }),
    });
  }

  public static getCurrentUser(): Promise<{ user: User }> {
    return this.request('/auth/me');
  }

  public static getDevices(): Promise<{ devices: Device[] }> {
    return this.request('/devices');
  }

  public static addDevice(name: string, session_id: string, direct_ip?: string, direct_port?: number): Promise<{ device: Device }> {
    return this.request('/devices', {
      method: 'POST',
      body: JSON.stringify({ name, session_id, direct_ip, direct_port }),
    });
  }

  public static removeDevice(id: string): Promise<{ success: boolean }> {
    return this.request(`/devices/${id}`, {
      method: 'DELETE',
    });
  }

  public static getAdminUsers(): Promise<{ users: User[] }> {
    return this.request('/admin/users');
  }

  public static getPlans(): Promise<{ plans: Plan[] }> {
    return this.request('/subscription/plans');
  }

  public static upgradePlan(plan: string): Promise<{ success: boolean; user: User }> {
    return this.request('/subscription/upgrade', {
      method: 'POST',
      body: JSON.stringify({ plan }),
    });
  }

  public static getSecurityAlerts(): Promise<{ alerts: SecurityAlert[]; stats: { total_alerts: number; critical_count: number; active_count: number } }> {
    return this.request('/admin/security/alerts');
  }

  public static resolveSecurityAlert(alert_id: string, action: 'RESOLVE' | 'QUARANTINE'): Promise<{ success: boolean }> {
    return this.request('/admin/security/resolve', {
      method: 'POST',
      body: JSON.stringify({ alert_id, action }),
    });
  }
}

