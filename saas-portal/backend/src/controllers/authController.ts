import { Request, Response } from 'express';
import bcrypt from 'bcryptjs';
import jwt from 'jsonwebtoken';
import { db } from '../models/db';
import { JWT_SECRET, AuthenticatedRequest } from '../middleware/authMiddleware';

export class AuthController {
  public static async register(req: Request, res: Response) {
    const { email, password, name, role, company } = req.body;
    if (!email || !password || !name) {
      return res.status(400).json({ error: 'MISSING_FIELDS', message: 'Email, password and name are required' });
    }

    const existing = db.prepare('SELECT id FROM users WHERE email = ?').get(email);
    if (existing) {
      return res.status(409).json({ error: 'USER_EXISTS', message: 'Email address already registered' });
    }

    const id = `usr_${Math.random().toString(36).substring(2, 10)}`;
    const hash = await bcrypt.hash(password, 10);
    const userRole = role === 'ADMIN' ? 'ADMIN' : 'USER';

    db.prepare(`
      INSERT INTO users (id, email, password_hash, name, role, company)
      VALUES (?, ?, ?, ?, ?, ?)
    `).run(id, email, hash, name, userRole, company || 'AetherDesk Enterprise');

    const token = jwt.sign({ id, email, role: userRole }, JWT_SECRET, { expiresIn: '7d' });

    res.status(201).json({
      token,
      user: { id, email, name, role: userRole, company }
    });
  }

  public static async login(req: Request, res: Response) {
    const { email, password } = req.body;
    if (!email || !password) {
      return res.status(400).json({ error: 'MISSING_FIELDS', message: 'Email and password required' });
    }

    const user: any = db.prepare('SELECT * FROM users WHERE email = ?').get(email);
    if (!user) {
      return res.status(401).json({ error: 'INVALID_CREDENTIALS', message: 'Invalid email or password' });
    }

    const match = await bcrypt.compare(password, user.password_hash);
    if (!match) {
      return res.status(401).json({ error: 'INVALID_CREDENTIALS', message: 'Invalid email or password' });
    }

    const token = jwt.sign({ id: user.id, email: user.email, role: user.role }, JWT_SECRET, { expiresIn: '7d' });

    res.json({
      token,
      user: {
        id: user.id,
        email: user.email,
        name: user.name,
        role: user.role,
        company: user.company,
        plan: user.plan || 'FREE',
        subscription_status: user.subscription_status || 'ACTIVE'
      }
    });
  }

  public static async me(req: AuthenticatedRequest, res: Response) {
    const userId = req.user?.id;
    const user: any = db.prepare('SELECT id, email, name, role, company, plan, subscription_status, created_at FROM users WHERE id = ?').get(userId);
    if (!user) return res.status(404).json({ error: 'USER_NOT_FOUND' });
    res.json({ user });
  }

  public static async listUsers(req: AuthenticatedRequest, res: Response) {
    const users = db.prepare('SELECT id, email, name, role, company, created_at FROM users').all();
    res.json({ users });
  }
}
