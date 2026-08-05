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
