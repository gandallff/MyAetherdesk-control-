import { WebSocket } from 'ws';
import { SessionIDGenerator } from './id_generator';

export interface Peer {
  id: string; // 9-digit ID (e.g., "982410735")
  formattedId: string; // Formatted 9-digit ID (e.g., "982-410-735")
  ws: WebSocket;
  isHost: boolean;
  connectedTargetId?: string;
  directIp?: string;
  directPort?: number;
  isAlive: boolean;
}

export class SessionManager {
  private peersByID: Map<string, Peer> = new Map();
  private wsToPeer: Map<WebSocket, Peer> = new Map();
  private idGenerator: SessionIDGenerator = new SessionIDGenerator();

  public registerPeer(ws: WebSocket, isHost: boolean = true): Peer {
    const id = this.idGenerator.generateID();
    const formattedId = this.idGenerator.formatID(id);
    const peer: Peer = {
      id,
      formattedId,
      ws,
      isHost,
      isAlive: true
    };

    this.peersByID.set(id, peer);
    this.wsToPeer.set(ws, peer);
    return peer;
  }

  public setDirectIPInfo(ws: WebSocket, ip: string, port: number): void {
    const peer = this.wsToPeer.get(ws);
    if (peer) {
      peer.directIp = ip;
      peer.directPort = port;
    }
  }

  public getPeerByWS(ws: WebSocket): Peer | undefined {
    return this.wsToPeer.get(ws);
  }

  public getPeerByID(id: string): Peer | undefined {
    // Sanitize ID string (remove spaces and dashes)
    const cleanID = id.replace(/[\s\-]/g, '');
    return this.peersByID.get(cleanID);
  }

  public removePeer(ws: WebSocket): Peer | undefined {
    const peer = this.wsToPeer.get(ws);
    if (!peer) return undefined;

    this.wsToPeer.delete(ws);
    this.peersByID.delete(peer.id);
    this.idGenerator.releaseID(peer.id);
    return peer;
  }

  public sendToPeer(targetId: string, message: object): boolean {
    const cleanID = targetId.replace(/[\s\-]/g, '');
    const target = this.peersByID.get(cleanID);

    if (target && target.ws.readyState === WebSocket.OPEN) {
      target.ws.send(JSON.stringify(message));
      return true;
    }
    return false;
  }
}

