# 🛠️ AetherDesk Technical Implementation Blueprint

This document details the exact technical implementation specifications for all four subsystems of the **AetherDesk** ecosystem.

---

## 1. Subsystem Specifications

### A. Signaling Server (`signaling-server/`)
- **Runtime**: Node.js + TypeScript (`ws` library).
- **Core Components**:
  - `id_generator.ts`: Non-colliding 9-digit ID allocation (e.g. `982 410 735`).
  - `session_manager.ts`: Active peer registry, socket lookup maps.
  - `websocket_handler.ts`: Relays `CONNECT_TO_ID`, `SDP_OFFER`, `SDP_ANSWER`, `ICE_CANDIDATE`, `REGISTER_DIRECT_IP`, and `GET_DIRECT_IP`.
  - `server.ts`: HTTP + WebSocket server listening on port `8080` with `/download/agent` endpoints.

### B. Desktop Host Agent (`desktop-agent/`)
- **Runtime**: Rust native binary (`tokio`, `windows-rs`, `scrap`).
- **Core Components**:
  - `dxgi_win.rs`: DXGI Desktop Duplication API GPU texture capture + Dirty Region metadata extraction (`DXGI_OUTDUPL_FRAME_INFO`).
  - `nvenc_encoder.rs`: Hardware NVENC H.264 video frame bitstream encoder.
  - `win_input.rs`: Win32 `SendInput` driver synthesizing mouse motion/clicks and Virtual Key codes.
  - `autostart.rs`: Windows Registry (`HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`) auto-boot background service setup.
  - `chunk_stream.rs`: 64KB binary chunking parser and SHA-256 integrity calculator.

### C. Remote Web Viewer (`web-viewer/`)
- **Runtime**: React 18 + Tailwind CSS + Vite (Port `9000`).
- **Core Components**:
  - `RemoteViewport.tsx`: Low-latency canvas/video viewport with crosshair pointer capture.
  - `RemoteToolbar.tsx`: Glassmorphism toolbar with resolution switcher, Ctrl+Alt+Del macro, and fullscreen toggle.
  - `FileExplorerModal.tsx`: Drag-and-drop file uploader with 64KB binary chunk progress bar.
  - `fileTransfer.ts`: WebRTC `RTCDataChannel` 64KB chunking stream manager with backpressure control (`bufferedAmount > 4MB`).

### D. Commercial SaaS Portal (`saas-portal/`)
- **Backend (`saas-portal/backend`)**: Express REST API + SQLite DB (Port `5000`) with JWT Auth, User & Device tables, and Subscription plans (`FREE`, `PRO`, `ENTERPRISE`).
- **Frontend (`saas-portal/frontend`)**: Ultra-modern React + Tailwind dashboard (Port `9090`) with Landing Page, Address Book live device cards, 1-Click Connect, and Pricing modal.

---

## 2. Port Allocation Table

| Service | Protocol | Default Port | Description |
| :--- | :--- | :--- | :--- |
| **SaaS Console** | HTTP | `9090` | Commercial Dashboard & Address Book |
| **Web Viewer** | HTTP | `9000` | Remote Desktop Control & Video Stream |
| **Signaling Server** | WS / HTTP | `8080` | 9-Digit ID Router & Direct IP Agent Downloader |
| **SaaS Backend API** | HTTP | `5000` | REST API & SQLite Database |
| **Direct Agent Socket** | TCP/UDP | `8443` | Native Host Agent Direct IP Listener |

---

## 3. Master Scripts & Execution

- **`start_all_services.bat`**: Master 1-click script that launches all 4 services in separate terminal windows.
- **`build_release_package.bat`**: Production build packager creating `AetherDesk-Distribution-Package`.
- **`Install-AetherDesk-Service.bat`**: 1-click Windows service installer for remote PCs.
- **`Run-AetherDesk-Portable.bat`**: Standalone portable runner without installation.
