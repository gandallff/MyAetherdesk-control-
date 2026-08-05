import { Response } from 'express';
import { db } from '../models/db';
import { AuthenticatedRequest } from '../middleware/authMiddleware';

export class SubscriptionController {
  public static getPlans(req: AuthenticatedRequest, res: Response) {
    res.json({
      plans: [
        {
          id: 'FREE',
          name: 'Free QuickSupport',
          price: '$0',
          period: 'forever',
          features: [
            'Basic 9-Digit Session ID Remote Access',
            '720p / 1080p Screen Streaming',
            'Standard Keyboard & Mouse Control',
            'Community Support'
          ],
          maxDevices: 0,
          allowFileTransfer: false,
          allowUnattended: false
        },
        {
          id: 'PRO',
          name: 'Pro Solo Specialist',
          price: '$15',
          period: 'per month',
          popular: true,
          features: [
            'Everything in Free',
            'Up to 25 Registered Devices in Address Book',
            '64KB Binary DataChannel File Transfer Engine',
            'Unattended Access with Password Protection',
            'Direct IP : Port LAN Accelerator'
          ],
          maxDevices: 25,
          allowFileTransfer: true,
          allowUnattended: true
        },
        {
          id: 'ENTERPRISE',
          name: 'Enterprise Team',
          price: '$49',
          period: 'per month',
          features: [
            'Everything in Pro',
            'Unlimited Registered Devices in Address Book',
            'Multi-Member Organization RBAC Control',
            'Custom Branded Host Agent Installer',
            'Audit & Connection Duration Logs',
            '24/7 Priority Support'
          ],
          maxDevices: 9999,
          allowFileTransfer: true,
          allowUnattended: true
        }
      ]
    });
  }

  public static upgradePlan(req: AuthenticatedRequest, res: Response) {
    const userId = req.user?.id;
    const { plan } = req.body;

    if (!plan || !['FREE', 'PRO', 'ENTERPRISE'].includes(plan)) {
      return res.status(400).json({ error: 'INVALID_PLAN', message: 'Valid plan required (FREE, PRO, ENTERPRISE)' });
    }

    db.prepare('UPDATE users SET plan = ?, subscription_status = "ACTIVE" WHERE id = ?').run(plan, userId);
    const updatedUser = db.prepare('SELECT id, email, name, role, company, plan, subscription_status FROM users WHERE id = ?').get(userId);

    res.json({
      success: true,
      message: `Account successfully upgraded to ${plan} Plan!`,
      user: updatedUser
    });
  }
}
