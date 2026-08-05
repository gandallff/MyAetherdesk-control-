import { Request, Response, NextFunction } from 'express';
import jwt from 'jsonwebtoken';

export const JWT_SECRET = process.env.JWT_SECRET || 'aetherdesk_super_secret_saas_key_2026';

export interface AuthenticatedRequest extends Request {
  user?: {
    id: string;
    email: string;
    role: 'ADMIN' | 'USER' | 'OPERATOR';
  };
}

export function authMiddleware(req: AuthenticatedRequest, res: Response, next: NextFunction) {
  const authHeader = req.headers.authorization;
  if (!authHeader || !authHeader.startsWith('Bearer ')) {
    return res.status(401).json({ error: 'UNAUTHORIZED', message: 'Authentication token required' });
  }

  const token = authHeader.split(' ')[1];
  try {
    const decoded = jwt.verify(token, JWT_SECRET) as any;
    req.user = decoded;
    next();
  } catch (err) {
    return res.status(401).json({ error: 'INVALID_TOKEN', message: 'Session token expired or invalid' });
  }
}

export function requireRole(role: 'ADMIN' | 'USER') {
  return (req: AuthenticatedRequest, res: Response, next: NextFunction) => {
    if (!req.user || (req.user.role !== role && req.user.role !== 'ADMIN')) {
      return res.status(403).json({ error: 'FORBIDDEN', message: 'Insufficient privileges' });
    }
    next();
  };
}
