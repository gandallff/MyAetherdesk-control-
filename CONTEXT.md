# 🏗️ AetherDesk Architecture & Context

Bu doküman, **AetherDesk Remote Control & Commercial SaaS Platform** mimarisinin bileşen bağlamını ve tasarım kararlarını özetlemektedir.

---

## 🏛️ Mimari Katmanlar (Architectural Layers)

```
                                 ┌─────────────────────────────────────────┐
                                 │     🌐 FRONTEND SİTESİ (VERCEL)         │
                                 │   https://my-aetherdesk-control.        │
                                 │              vercel.app                 │
                                 └────────────────────┬────────────────────┘
                                                      │
                                           [Ajan İndir / Bağlan]
                                                      │
                                                      ▼
┌───────────────────────┐        ┌─────────────────────────────────────────┐        ┌───────────────────────┐
│   DESKTOP AGENT       │        │        SİNYALLEŞME SUNUCUSU             │        │    WEB VIEWER         │
│   (Rust + DXGI)       │<======>│        (Node.js WebSocket)              │<======>│    (Vite + React)     │
│   Direct Socket: 8443 │        │   Local: 8080 / Tunnel: wss://...       │        │    Viewer Port: 9000  │
└───────────────────────┘        └─────────────────────────────────────────┘        └───────────────────────┘
                                                      ▲
                                                      │
                                         ┌────────────┴────────────┐
                                         │   SaaS BACKEND REST API │
                                         │   (Express + SQLite)    │
                                         │   Port: 5000 / DB       │
                                         └─────────────────────────┘
```

---

## 📂 Klasör Yapısı ve Bileşen Sorumlulukları

- **`saas-portal/frontend`**: Vercel üzerinde 7/24 yayınlanan Ticari SaaS web portalı. Landing Page, Fiyatlandırma Modalı, Çok Dilli (TR/EN) Adres Defteri.
- **`saas-portal/backend`**: Express.js ve SQLite tabanlı kullanıcı yönetimi, cihaz kayıt rehberi ve abonelik API katmanı.
- **`desktop-agent`**: Rust diliyle yazılmış, Windows DXGI bellek kopyalama, Win32 API klavye/fare simülasyonu ve doğrudan IP dinleyici socket sunucusu.
- **`signaling-server`**: Uzaktan bağlantı başlatma, 9 haneli Oturum ID üretimi, WebRTC SDP/ICE aday takası ve binary indirme sunucusu.
- **`web-viewer`**: Tarayıcı içi canvas uzaktan masaüstü izleyici ve dosya transfer modülü.
- **`scripts/`**: PowerShell tabanlı pencereli arayüz araçları (`AetherDesk-Control-Center.ps1`, `AetherDesk-Cloud-Deployer.ps1`, `AetherDesk-QuickSupport.ps1`, `AetherDesk-Installer-Setup.ps1`).

---

## 🔒 Güvenlik & Ağ Kararları

- **Sıfır CMD Konsol Deneyimi**: VBScript wrapper (`AetherDesk-QuickSupport-Tool.vbs`) sayesinde istemcilerde hiçbir siyah CMD ekranı açılmaz.
- **Otomatik Ağ Algılama**: Yerel ağdaki IPv4 adresleri (`192.168.1.X:8443`) ve küresel 9-haneli ID'ler aynı anda istemci formunda sunulur.
