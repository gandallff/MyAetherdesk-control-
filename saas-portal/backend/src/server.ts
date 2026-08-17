import express from 'express';
import cors from 'cors';
import bcrypt from 'bcryptjs';
import { db } from './models/db';
import { AuthController } from './controllers/authController';
import { DeviceController } from './controllers/deviceController';
import { authMiddleware, requireRole } from './middleware/authMiddleware';

const app = express();
const PORT = process.env.PORT ? parseInt(process.env.PORT, 10) : 5000;

app.use(cors());
app.use(express.json());

// Seed Initial Admin User if database is empty
async function seedDefaultAdmin() {
  const count: any = db.prepare('SELECT count(*) as count FROM users').get();
  if (count.count === 0) {
    const adminId = 'usr_admin_01';
    const hash = await bcrypt.hash('admin2026', 10);
    db.prepare(`
      INSERT INTO users (id, email, password_hash, name, role, company)
      VALUES (?, ?, ?, ?, ?, ?)
    `).run(adminId, 'admin@aetherdesk.com', hash, 'System Administrator', 'ADMIN', 'AetherDesk HQ');

    // Seed Demo Devices
    db.prepare(`
      INSERT INTO devices (id, user_id, name, session_id, is_online, direct_ip, direct_port)
      VALUES 
        ('dev_01', 'usr_admin_01', 'HQ Server Room 01', '482910375', 1, '192.168.1.100', 8443),
        ('dev_02', 'usr_admin_01', 'Finance Workstation 04', '982410735', 1, '192.168.1.104', 8443),
        ('dev_03', 'usr_admin_01', 'Development Laptop - Mac', '123456789', 0, '192.168.1.120', 8443)
    `).run();

    console.log('[Seed Data] Default Admin Created: admin@aetherdesk.com / admin2026');
  }
}
seedDefaultAdmin();

import { SubscriptionController } from './controllers/subscriptionController';
import { SecurityController } from './controllers/securityController';

// Seed initial demo security alert if empty
function seedDemoSecurityAlerts() {
  const alertCount: any = db.prepare('SELECT count(*) as count FROM security_alerts').get();
  if (alertCount.count === 0) {
    db.prepare(`
      INSERT INTO security_alerts (id, device_id, device_name, alert_type, severity, details, status)
      VALUES 
        ('sec_01', 'dev_02', 'Finance Workstation 04', 'TROJAN_PREVENTION', 'HIGH', 'Blocked suspicious process injection attempt (unauthorized DLL hook)', 'ACTIVE'),
        ('sec_02', 'dev_01', 'HQ Server Room 01', 'INTEGRITY_TAMPER', 'MEDIUM', 'SHA-256 binary fingerprint verified clean on boot', 'RESOLVED')
    `).run();
  }
}
seedDemoSecurityAlerts();

// --- Auth Routes ---
app.post('/api/auth/register', AuthController.register);
app.post('/api/auth/login', AuthController.login);
app.get('/api/auth/me', authMiddleware, AuthController.me);
app.get('/api/admin/users', authMiddleware, requireRole('ADMIN'), AuthController.listUsers);

// --- Subscription Routes ---
app.get('/api/subscription/plans', SubscriptionController.getPlans);
app.post('/api/subscription/upgrade', authMiddleware, SubscriptionController.upgradePlan);

// --- Device & Address Book Routes ---
app.get('/api/devices', authMiddleware, DeviceController.listDevices);
app.post('/api/devices', authMiddleware, DeviceController.addDevice);
app.delete('/api/devices/:id', authMiddleware, DeviceController.removeDevice);
app.get('/api/devices/installer-token', authMiddleware, DeviceController.generateInstallerToken);
app.get('/api/admin/session-logs', authMiddleware, requireRole('ADMIN'), DeviceController.listSessionLogs);

// --- System Admin Security & Threat Telemetry Routes ---
app.get('/api/admin/security/alerts', authMiddleware, requireRole('ADMIN'), SecurityController.getAlerts);
app.post('/api/admin/security/resolve', authMiddleware, requireRole('ADMIN'), SecurityController.resolveAlert);
app.post('/api/security/telemetry', SecurityController.receiveTelemetry);


// --- Health Route ---
app.get('/api/health', (req, res) => {
  res.json({ status: 'ok', service: 'AetherDesk SaaS API', uptime: process.uptime() });
});

app.listen(PORT, () => {
  console.log(`=======================================================`);
  console.log(`  🚀 AetherDesk SaaS REST API Server Listening`);
  console.log(`  URL: http://localhost:${PORT}`);
  console.log(`  Default Admin Credentials: admin@aetherdesk.com / admin2026`);
  console.log(`=======================================================`);
});
