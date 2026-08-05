import { Response } from 'express';
import { db } from '../models/db';
import { AuthenticatedRequest } from '../middleware/authMiddleware';

export class DeviceController {
  public static listDevices(req: AuthenticatedRequest, res: Response) {
    const userId = req.user?.id;
    const isOwnerOrAdmin = req.user?.role === 'ADMIN';

    let devices;
    if (isOwnerOrAdmin) {
      devices = db.prepare('SELECT * FROM devices ORDER BY last_seen DESC').all();
    } else {
      devices = db.prepare('SELECT * FROM devices WHERE user_id = ? ORDER BY last_seen DESC').all(userId);
    }

    res.json({ devices });
  }

  public static addDevice(req: AuthenticatedRequest, res: Response) {
    const userId = req.user?.id;
    const { name, session_id, direct_ip, direct_port } = req.body;

    if (!name || !session_id) {
      return res.status(400).json({ error: 'MISSING_FIELDS', message: 'Name and session_id required' });
    }

    const deviceId = `dev_${Math.random().toString(36).substring(2, 10)}`;

    db.prepare(`
      INSERT INTO devices (id, user_id, name, session_id, is_online, direct_ip, direct_port)
      VALUES (?, ?, ?, ?, 1, ?, ?)
    `).run(deviceId, userId, name, session_id.replace(/\s+/g, ''), direct_ip || '192.168.1.100', direct_port || 8443);

    const newDevice = db.prepare('SELECT * FROM devices WHERE id = ?').get(deviceId);
    res.status(201).json({ device: newDevice });
  }

  public static removeDevice(req: AuthenticatedRequest, res: Response) {
    const { id } = req.params;
    const userId = req.user?.id;

    if (req.user?.role === 'ADMIN') {
      db.prepare('DELETE FROM devices WHERE id = ?').run(id);
    } else {
      db.prepare('DELETE FROM devices WHERE id = ? AND user_id = ?').run(id, userId);
    }

    res.json({ success: true, message: 'Device deleted successfully' });
  }

  public static generateInstallerToken(req: AuthenticatedRequest, res: Response) {
    const userId = req.user?.id;
    const downloadUrl = `http://localhost:5000/api/download/custom-agent?userToken=${userId}`;

    res.json({
      installerUrl: downloadUrl,
      userToken: userId,
      instructions: 'Run installer on target PC to automatically add device to your Address Book'
    });
  }

  public static listSessionLogs(req: AuthenticatedRequest, res: Response) {
    const logs = db.prepare('SELECT * FROM session_logs ORDER BY started_at DESC LIMIT 50').all();
    res.json({ logs });
  }
}
