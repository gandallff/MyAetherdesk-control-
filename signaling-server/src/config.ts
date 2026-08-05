export const CONFIG = {
  PORT: process.env.PORT ? parseInt(process.env.PORT, 10) : 8080,
  HEARTBEAT_INTERVAL_MS: 30000,
  ID_LENGTH: 9,
  STUN_SERVERS: [
    { urls: "stun:stun.l.google.com:19302" },
    { urls: "stun:stun1.l.google.com:19302" }
  ]
};
