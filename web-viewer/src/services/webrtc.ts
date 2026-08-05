import { RemoteInputEvent } from '../types/protocol';

export class WebRTCService {
  private peerConnection: RTCPeerConnection | null = null;
  private dataChannel: RTCDataChannel | null = null;
  private onTrackCallback?: (stream: MediaStream) => void;
  private onDataChannelCallback?: (channel: RTCDataChannel) => void;
  private onIceCandidateCallback?: (candidate: RTCIceCandidate) => void;

  constructor(private iceServers: RTCIceServer[] = [{ urls: 'stun:stun.l.google.com:19302' }]) {}

  public initConnection(): void {
    this.peerConnection = new RTCPeerConnection({ iceServers: this.iceServers });

    this.peerConnection.onicecandidate = (event) => {
      if (event.candidate && this.onIceCandidateCallback) {
        this.onIceCandidateCallback(event.candidate);
      }
    };

    this.peerConnection.ontrack = (event) => {
      if (event.streams && event.streams[0] && this.onTrackCallback) {
        this.onTrackCallback(event.streams[0]);
      }
    };

    // Create Data Channel for control & file transfer
    this.dataChannel = this.peerConnection.createDataChannel('aetherdesk-data', {
      ordered: true,
    });

    if (this.onDataChannelCallback) {
      this.onDataChannelCallback(this.dataChannel);
    }
  }

  public async createOffer(): Promise<RTCSessionDescriptionInit> {
    if (!this.peerConnection) throw new Error('PeerConnection not initialized');
    const offer = await this.peerConnection.createOffer({
      offerToReceiveVideo: true,
      offerToReceiveAudio: true,
    });
    await this.peerConnection.setLocalDescription(offer);
    return offer;
  }

  public async handleAnswer(answer: RTCSessionDescriptionInit): Promise<void> {
    if (!this.peerConnection) return;
    await this.peerConnection.setRemoteDescription(new RTCSessionDescription(answer));
  }

  public async addIceCandidate(candidate: RTCIceCandidateInit): Promise<void> {
    if (this.peerConnection) {
      await this.peerConnection.addIceCandidate(new RTCIceCandidate(candidate));
    }
  }

  public sendInputEvent(event: RemoteInputEvent): void {
    if (this.dataChannel && this.dataChannel.readyState === 'open') {
      this.dataChannel.send(JSON.stringify(event));
    }
  }

  public getDataChannel(): RTCDataChannel | null {
    return this.dataChannel;
  }

  public onTrack(cb: (stream: MediaStream) => void) {
    this.onTrackCallback = cb;
  }

  public onIceCandidate(cb: (candidate: RTCIceCandidate) => void) {
    this.onIceCandidateCallback = cb;
  }

  public onDataChannel(cb: (channel: RTCDataChannel) => void) {
    this.onDataChannelCallback = cb;
  }

  public close(): void {
    if (this.dataChannel) this.dataChannel.close();
    if (this.peerConnection) this.peerConnection.close();
  }
}
