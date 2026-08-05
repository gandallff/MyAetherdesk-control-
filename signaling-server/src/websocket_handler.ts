import { WebSocket } from 'ws';
import { SessionManager, Peer } from './session_manager';
import { CONFIG } from './config';

export interface SignalingMessage {
  type: 
    | 'SESSION_ASSIGNED'
    | 'CONNECT_TO_ID'
    | 'CONNECT_REQUEST'
    | 'TARGET_STATUS'
    | 'SDP_OFFER'
    | 'SDP_ANSWER'
    | 'ICE_CANDIDATE'
    | 'REGISTER_DIRECT_IP'
    | 'GET_DIRECT_IP'
    | 'DIRECT_IP_RESPONSE'
    | 'ERROR'
    | 'PING'
    | 'PONG';
  targetId?: string;
  senderId?: string;
  payload?: any;
}

export class WebSocketHandler {
  constructor(private sessionManager: SessionManager) {}

  public handleConnection(ws: WebSocket): void {
    // 1. Assign 9-digit Session ID on new connection immediately
    const peer = this.sessionManager.registerPeer(ws, true);

    ws.send(JSON.stringify({
      type: 'SESSION_ASSIGNED',
      payload: {
        sessionId: peer.id,
        formattedId: peer.formattedId,
        iceServers: CONFIG.STUN_SERVERS
      }
    }));
    console.log(`[Peer Connected] Assigned 9-Digit ID: ${peer.formattedId} (${peer.id})`);

    ws.on('message', (data: Buffer) => {
      try {
        const message: SignalingMessage = JSON.parse(data.toString());
        this.processMessage(ws, message);
      } catch (err) {
        this.sendError(ws, 'INVALID_JSON', 'Malformed JSON payload received');
      }
    });

    ws.on('close', () => {
      const removedPeer = this.sessionManager.removePeer(ws);
      if (removedPeer) {
        console.log(`[Peer Disconnected] Released 9-Digit ID: ${removedPeer.formattedId}`);
        if (removedPeer.connectedTargetId) {
          this.sessionManager.sendToPeer(removedPeer.connectedTargetId, {
            type: 'PEER_DISCONNECTED',
            senderId: removedPeer.id
          });
        }
      }
    });

    ws.on('error', (error) => {
      console.error('[WebSocket Error]', error);
    });
  }

  private processMessage(ws: WebSocket, msg: SignalingMessage): void {
    const peer = this.sessionManager.getPeerByWS(ws);
    if (!peer) return;

    switch (msg.type) {
      // 2. Client target connection request
      case 'CONNECT_TO_ID':
      case 'CONNECT_REQUEST': {
        const rawTargetId = msg.targetId || msg.payload?.targetId;
        if (!rawTargetId) {
          return this.sendError(ws, 'MISSING_TARGET_ID', 'Target Session ID is required');
        }

        const targetHost = this.sessionManager.getPeerByID(rawTargetId);

        if (!targetHost) {
          ws.send(JSON.stringify({
            type: 'TARGET_STATUS',
            payload: {
              targetId: rawTargetId,
              isOnline: false,
              message: `Host ID ${rawTargetId} is offline or not found`
            }
          }));
          return;
        }

        // Link peers
        peer.connectedTargetId = targetHost.id;
        targetHost.connectedTargetId = peer.id;

        // Send confirmation back to requester
        ws.send(JSON.stringify({
          type: 'TARGET_STATUS',
          payload: {
            targetId: targetHost.id,
            formattedId: targetHost.formattedId,
            isOnline: true,
            directIp: targetHost.directIp,
            directPort: targetHost.directPort,
            message: 'Target host is online. Relaying SDP Offer...'
          }
        }));

        // Notify target host of connection attempt
        this.sessionManager.sendToPeer(targetHost.id, {
          type: 'CONNECT_REQUEST',
          senderId: peer.id,
          payload: {
            requesterId: peer.id,
            requesterFormattedId: peer.formattedId
          }
        });

        console.log(`[Connection Request] ${peer.formattedId} -> ${targetHost.formattedId}`);
        break;
      }

      // 3. WebRTC SDP & ICE Candidate Transparent Relay
      case 'SDP_OFFER':
      case 'SDP_ANSWER':
      case 'ICE_CANDIDATE': {
        const targetId = msg.targetId || peer.connectedTargetId;
        if (!targetId) {
          return this.sendError(ws, 'MISSING_TARGET_ID', `Target ID required to relay ${msg.type}`);
        }

        const relayed = this.sessionManager.sendToPeer(targetId, {
          type: msg.type,
          senderId: peer.id,
          payload: msg.payload
        });

        if (!relayed) {
          this.sendError(ws, 'RELAY_FAILED', `Could not reach target peer ${targetId}`);
        } else {
          console.log(`[Relay ${msg.type}] ${peer.formattedId} -> ${targetId}`);
        }
        break;
      }

      // 4. Direct IP:Port Listener Registration & Query
      case 'REGISTER_DIRECT_IP': {
        const ip = msg.payload?.ip || '127.0.0.1';
        const port = msg.payload?.port || 8443;
        this.sessionManager.setDirectIPInfo(ws, ip, port);
        console.log(`[Direct IP Registered] ${peer.formattedId} -> ${ip}:${port}`);
        break;
      }

      case 'GET_DIRECT_IP': {
        const targetId = msg.targetId;
        if (!targetId) {
          return this.sendError(ws, 'MISSING_TARGET_ID', 'Target ID required');
        }
        const targetHost = this.sessionManager.getPeerByID(targetId);
        ws.send(JSON.stringify({
          type: 'DIRECT_IP_RESPONSE',
          payload: {
            targetId,
            directIp: targetHost?.directIp || null,
            directPort: targetHost?.directPort || null,
            isAvailable: !!(targetHost?.directIp && targetHost?.directPort)
          }
        }));
        break;
      }

      case 'PING': {
        ws.send(JSON.stringify({ type: 'PONG' }));
        break;
      }

      default:
        this.sendError(ws, 'UNKNOWN_MESSAGE', `Unhandled message type: ${msg.type}`);
    }
  }

  private sendError(ws: WebSocket, code: string, message: string): void {
    if (ws.readyState === WebSocket.OPEN) {
      ws.send(JSON.stringify({
        type: 'ERROR',
        payload: { code, message }
      }));
    }
  }
}
