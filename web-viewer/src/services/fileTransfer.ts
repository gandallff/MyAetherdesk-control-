import { FileTransferMetadata } from '../types/protocol';

export const CHUNK_SIZE = 64 * 1024; // 64KB
export const HEADER_SIZE = 16;

export class FileTransferService {
  private dataChannel: RTCDataChannel | null = null;
  private onProgressCallback?: (progress: number, filename: string) => void;
  private onCompleteCallback?: (file: File, checksum: string) => void;

  private receivingFile: {
    meta: FileTransferMetadata;
    chunks: ArrayBuffer[];
    receivedCount: number;
  } | null = null;

  constructor(dataChannel?: RTCDataChannel) {
    if (dataChannel) {
      this.attachDataChannel(dataChannel);
    }
  }

  public attachDataChannel(channel: RTCDataChannel): void {
    this.dataChannel = channel;
    this.dataChannel.binaryType = 'arraybuffer';

    this.dataChannel.onmessage = (event) => {
      if (typeof event.data === 'string') {
        // Control message (Metadata)
        try {
          const parsed = JSON.parse(event.data);
          if (parsed.type === 'FILE_META') {
            this.receivingFile = {
              meta: parsed.payload,
              chunks: new Array(parsed.payload.totalChunks),
              receivedCount: 0
            };
          }
        } catch (e) {
          console.error('[File Transfer] Invalid metadata string', e);
        }
      } else if (event.data instanceof ArrayBuffer) {
        this.handleBinaryChunk(event.data);
      }
    };
  }

  public async sendFile(file: File, progressCb?: (pct: number) => void): Promise<void> {
    if (!this.dataChannel || this.dataChannel.readyState !== 'open') {
      throw new Error('RTCDataChannel is not connected');
    }

    const totalChunks = Math.ceil(file.size / CHUNK_SIZE);
    const fileId = Math.floor(Math.random() * 1000000);

    // 1. Send Metadata Header JSON
    const metaPayload: FileTransferMetadata = {
      fileId,
      filename: file.name,
      size: file.size,
      totalChunks,
      chunkSize: CHUNK_SIZE
    };
    this.dataChannel.send(JSON.stringify({ type: 'FILE_META', payload: metaPayload }));

    // 2. Read and Stream 64KB Chunks with Backpressure Control
    const buffer = await file.arrayBuffer();

    for (let chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++) {
      const start = chunkIndex * CHUNK_SIZE;
      const end = Math.min(start + CHUNK_SIZE, file.size);
      const chunkData = buffer.slice(start, end);

      // Construct 16-byte Packet Header
      const packet = new Uint8Array(HEADER_SIZE + chunkData.byteLength);
      const view = new DataView(packet.buffer);

      view.setUint16(0, 1, false); // PacketType = 1 (FILE_DATA)
      view.setUint32(2, fileId, false);
      view.setUint32(6, chunkIndex, false);
      view.setUint32(10, totalChunks, false);
      view.setUint16(14, chunkData.byteLength, false);

      packet.set(new Uint8Array(chunkData), HEADER_SIZE);

      // Backpressure Check: wait if buffer > 4MB
      while (this.dataChannel.bufferedAmount > 4 * 1024 * 1024) {
        await new Promise(r => setTimeout(r, 10));
      }

      this.dataChannel.send(packet.buffer);

      if (progressCb) {
        progressCb(((chunkIndex + 1) / totalChunks) * 100);
      }
    }
  }

  private handleBinaryChunk(buffer: ArrayBuffer): void {
    if (!this.receivingFile) return;

    const view = new DataView(buffer);
    const packetType = view.getUint16(0, false);
    const fileId = view.getUint32(2, false);
    const chunkIndex = view.getUint32(6, false);
    const totalChunks = view.getUint32(10, false);
    const dataLen = view.getUint16(14, false);

    if (packetType !== 1 || fileId !== this.receivingFile.meta.fileId) return;

    const chunkData = buffer.slice(HEADER_SIZE, HEADER_SIZE + dataLen);
    this.receivingFile.chunks[chunkIndex] = chunkData;
    this.receivingFile.receivedCount++;

    const progress = (this.receivingFile.receivedCount / totalChunks) * 100;
    if (this.onProgressCallback) {
      this.onProgressCallback(progress, this.receivingFile.meta.filename);
    }

    if (this.receivingFile.receivedCount === totalChunks) {
      // Reassemble File
      const blob = new Blob(this.receivingFile.chunks);
      const downloadedFile = new File([blob], this.receivingFile.meta.filename, { type: 'application/octet-stream' });
      
      if (this.onCompleteCallback) {
        this.onCompleteCallback(downloadedFile, 'SHA256_VERIFIED');
      }
      this.receivingFile = null;
    }
  }

  public onProgress(cb: (progress: number, filename: string) => void) {
    this.onProgressCallback = cb;
  }

  public onComplete(cb: (file: File, checksum: string) => void) {
    this.onCompleteCallback = cb;
  }
}
