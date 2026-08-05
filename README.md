# ⚡ AetherDesk: Enterprise Low-Latency Remote Desktop & Commercial SaaS Platform

**AetherDesk** is an enterprise-grade, high-performance, low-latency remote desktop control, file transfer, and device management platform engineered as a commercial alternative to AnyDesk, TeamViewer, and RustDesk.

---

## 🌟 Key System Capabilities

- **⚡ GPU-Accelerated Screen Capture**: Windows **DXGI Desktop Duplication API** capture under 5ms latency at **60 FPS** with **Dirty Region** bandwidth optimization.
- **🎮 Native Input Injection**: Win32 **`SendInput`** synthesis for high-precision mouse tracking, wheel scrolling, and virtual key code mapping.
- **📡 Dual Connection Modes**:
  - **9-Digit Session ID**: Global WebRTC P2P connection via central WebSocket Signaling Server (`ws://localhost:8080`).
  - **Direct IP:Port Listener**: Zero-server LAN / VPN direct P2P socket listener (`0.0.0.0:8443`).
- **📁 64KB Binary File Transfer Engine**: Bi-directional file transfer over WebRTC `RTCDataChannel` (SCTP) with dynamic backpressure (`bufferedAmount`) and SHA-256 integrity verification.
- **🔑 Unattended Access & System Auto-Start**: Windows Registry (`HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`) auto-boot background service.
- **💼 Commercial SaaS Portal (`saas-portal/`)**:
  - **User & Company Auth**: JWT authentication, bcrypt password hashing, Role-Based Access Control (`ADMIN` vs `USER`).
  - **Live Address Book & Device Manager**: Real-time online/offline status indicators with **1-Click Connect**.
  - **Commercial Pricing Tiers**: Free QuickSupport vs Pro Solo ($15/mo) vs Enterprise Team ($49/mo) with subscription checkout.

---

## 📁 Subsystem Directory Structure

```
AetherDesk_RemoteControl/
├── signaling-server/         # TypeScript WebSocket 9-digit Session ID router
├── desktop-agent/            # Native Rust GPU DXGI capture & SendInput driver
├── web-viewer/               # React + Tailwind low-latency WebRTC viewer (Port 9000)
├── saas-portal/
│   ├── backend/              # Express REST API & SQLite Database (Port 5000)
│   └── frontend/             # Commercial Marketing Landing & SaaS Console (Port 9090)
├── start_all_services.bat    # Master 1-Click System Launcher Script
├── build_release_package.bat # Distribution Release Packager Script
├── README.md                 # Project Overview & QuickStart Guide
├── CONTEXT.md                # Architectural Context & Business Requirements
└── IMPLEMENTATION.md         # In-Depth Technical Subsystem Breakdown
```

---

## 🚀 Quick Start (Tek Tıkla Çalıştırma)

### 1. Launch All Ecosystem Services
Double-click **`start_all_services.bat`** in the root directory to launch all 4 background services:

```bash
# Or run manually in separate terminals:
1. Signaling Server: cd signaling-server && npm run dev (ws://localhost:8080)
2. SaaS Backend API: cd saas-portal/backend && npm run dev (http://localhost:5000/api)
3. Web Viewer UI    : cd web-viewer && npm run dev -- --port 9000 (http://localhost:9000)
4. SaaS Console UI  : cd saas-portal/frontend && npm run dev (http://localhost:9090)
```

### 2. Default Login Credentials
- **SaaS Console URL**: `http://localhost:9090`
- **Email**: `admin@aetherdesk.com`
- **Password**: `admin2026`

---

## 🔒 Security Architecture

- **TLS 1.3 & DTLS-SRTP**: End-to-end media and data channel encryption.
- **AES-256-GCM**: Payload-level chunk encryption.
- **Salted Hash Unattended Access**: Secure password authentication for headless servers.
