import { SignalingMessage } from '../types/protocol';

export class SignalingService {
  private ws: WebSocket | null = null;
  private listeners: Map<string, Array<(msg: SignalingMessage) => void>> = new Map();

  constructor(private url: string = 'ws://localhost:8080') {}

  public connect(): Promise<void> {
    return new Promise((resolve, reject) => {
      this.ws = new WebSocket(this.url);

      this.ws.onopen = () => {
        console.log('[Signaling Service] WebSocket Connected');
        resolve();
      };

      this.ws.onerror = (err) => {
        console.error('[Signaling Service Error]', err);
        reject(err);
      };

      this.ws.onmessage = (event) => {
        try {
          const msg: SignalingMessage = JSON.parse(event.data);
          this.emit(msg.type, msg);
        } catch (e) {
          console.error('[Signaling Service] Malformed message', e);
        }
      };

      this.ws.onclose = () => {
        console.log('[Signaling Service] WebSocket Disconnected');
      };
    });
  }

  public send(msg: SignalingMessage): void {
    if (this.ws && this.ws.readyState === WebSocket.OPEN) {
      this.ws.send(JSON.stringify(msg));
    }
  }

  public on(type: string, callback: (msg: SignalingMessage) => void): () => void {
    if (!this.listeners.has(type)) {
      this.listeners.set(type, []);
    }
    this.listeners.get(type)!.push(callback);

    return () => {
      const arr = this.listeners.get(type) || [];
      this.listeners.set(type, arr.filter(cb => cb !== callback));
    };
  }

  private emit(type: string, msg: SignalingMessage): void {
    const callbacks = this.listeners.get(type) || [];
    callbacks.forEach(cb => cb(msg));
  }

  public disconnect(): void {
    if (this.ws) {
      this.ws.close();
      this.ws = null;
    }
  }
}
