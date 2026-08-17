import { Request, Response } from 'express';
import { db } from '../models/db';

export class SecurityController {
  // GET /api/admin/security/alerts - Fetch all security alerts for System Admin
  public static getAlerts(req: Request, res: Response): void {
    try {
      const alerts = db.prepare(`
        SELECT * FROM security_alerts 
        ORDER BY created_at DESC 
        LIMIT 100
      `).all();

      const stats = db.prepare(`
        SELECT 
          COUNT(*) as total_alerts,
          SUM(CASE WHEN severity = 'CRITICAL' THEN 1 ELSE 0 END) as critical_count,
          SUM(CASE WHEN status = 'ACTIVE' THEN 1 ELSE 0 END) as active_count
        FROM security_alerts
      `).get();

      res.json({ success: true, alerts, stats });
    } catch (err: any) {
      console.error('[SecurityController] Error fetching alerts:', err);
      res.status(500).json({ error: 'Failed to fetch security telemetry' });
    }
  }

  // POST /api/security/telemetry - Receive threat/integrity payload from Rust agent
  public static receiveTelemetry(req: Request, res: Response): void {
    try {
      const { device_id, device_name, alert_type, severity, details } = req.body;
      const alertId = 'sec_' + Date.now() + '_' + Math.random().toString(36).substr(2, 4);

      db.prepare(`
        INSERT INTO security_alerts (id, device_id, device_name, alert_type, severity, details, status)
        VALUES (?, ?, ?, ?, ?, ?, 'ACTIVE')
      `).run(alertId, device_id || 'unknown_device', device_name || 'Agent Endpoint', alert_type || 'SUSPICIOUS_BEHAVIOR', severity || 'HIGH', details || 'Threat detected on endpoint');

      console.log(`[SECURITY ALERT LOGGED] ${severity} - ${alert_type} on ${device_name}: ${details}`);
      res.json({ success: true, alert_id: alertId });
    } catch (err: any) {
      console.error('[SecurityController] Error logging telemetry:', err);
      res.status(500).json({ error: 'Failed to record security alert' });
    }
  }

  // POST /api/admin/security/resolve - Mark threat alert as RESOLVED or QUARANTINED
  public static resolveAlert(req: Request, res: Response): void {
    try {
      const { alert_id, action } = req.body; // action: 'RESOLVE' | 'QUARANTINE'
      const newStatus = action === 'QUARANTINE' ? 'QUARANTINED' : 'RESOLVED';

      db.prepare(`
        UPDATE security_alerts 
        SET status = ? 
        WHERE id = ?
      `).run(newStatus, alert_id);

      res.json({ success: true, message: `Security alert ${alert_id} set to ${newStatus}` });
    } catch (err: any) {
      console.error('[SecurityController] Error resolving alert:', err);
      res.status(500).json({ error: 'Failed to update alert status' });
    }
  }
}
