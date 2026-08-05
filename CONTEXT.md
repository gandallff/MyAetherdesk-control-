# 🧠 AetherDesk: Architectural Context & Domain Overview

## 1. Project Background & Objective

AetherDesk was designed to solve high latency, bandwidth inefficiency, and vendor lock-in inherent in traditional remote desktop solutions. The primary goal is to provide an open, enterprise-ready, ultra-low-latency remote control and file transfer platform with a commercial SaaS management tier.

---

## 2. Core Functional Requirements

1. **Low-Latency Frame Pipeline**:
   - Frame capture overhead must stay under **5ms**.
   - Capture pipeline must target **60 FPS** at 1080p / 4K resolutions.
   - Bandwidth optimization through **Dirty Rectangles (Dirty Regions)** detection—only sending changed screen sub-rectangles unless a full refresh is required.

2. **Native Input Injection**:
   - Zero-delay input synthesis for physical mouse events (movement, clicks, wheel scrolls) and physical Virtual Key (VK) codes.
   - Coordinate normalization from viewer normalized ratio `[0.0, 1.0]` to OS absolute hardware space `[0, 65535]`.

3. **Multi-Mode Connection Protocol**:
   - **Mode A (9-Digit Session ID)**: WebSocket signaling server allocates non-colliding IDs (e.g. `482 910 375`) and relays WebRTC SDP Offer/Answer and ICE candidates across different networks/NATs.
   - **Mode B (Direct IP:Port Listener)**: Host agent binds a direct TCP/UDP listener on `0.0.0.0:8443` for zero-signaling LAN / VPN P2P connection.

4. **Bi-directional 64KB Chunked File Sharing**:
   - Files are sliced into fixed `64 * 1024` byte payloads with 16-byte binary headers.
   - Flow control pauses reading if `dataChannel.bufferedAmount > 4MB` to prevent memory blowup.
   - SHA-256 integrity hash verification upon reassembly.

5. **Commercial SaaS & Freemium Model**:
   - **Free Tier**: QuickSupport agent download with basic 9-digit session connection.
   - **Pro Tier ($15/mo)**: Address book (up to 25 devices), 64KB file transfer, Unattended access.
   - **Enterprise Tier ($49/mo)**: Unlimited devices, multi-member company RBAC, audit logging, custom branded installers.

---

## 3. Non-Functional & Security Requirements

- **Security**: TLS 1.3 for signaling WebSocket, DTLS-SRTP for WebRTC media, AES-256-GCM for DataChannel payloads, Argon2id/bcrypt for password hashing.
- **Portability**: Auto-start registry service support for Windows (`HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`), standalone portable execution scripts.
