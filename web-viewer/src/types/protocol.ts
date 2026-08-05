export type ConnectionMode = 'SIGNALING_ID' | 'DIRECT_IP';

export interface SignalingMessage {
  type:
    | 'REGISTER_HOST'
    | 'HOST_REGISTERED'
    | 'CONNECT_REQUEST'
    | 'CONNECT_RESPONSE'
    | 'SDP_OFFER'
    | 'SDP_ANSWER'
    | 'ICE_CANDIDATE'
    | 'DIRECT_IP_INFO'
    | 'ERROR'
    | 'PING'
    | 'PONG';
  targetId?: string;
  senderId?: string;
  payload?: any;
}

export interface RemoteInputEvent {
  type: 'MouseMove' | 'MouseDown' | 'MouseUp' | 'MouseWheel' | 'KeyDown' | 'KeyUp';
  payload: {
    x?: number;
    y?: number;
    button?: number;
    delta_y?: number;
    vk_code?: number;
    key?: string;
  };
}

export interface FileTransferMetadata {
  fileId: number;
  filename: string;
  size: number;
  totalChunks: number;
  chunkSize: number;
}
