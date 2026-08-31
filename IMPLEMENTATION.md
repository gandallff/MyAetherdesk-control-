# 🛠️ AetherDesk Technical Implementation & Operation Manual

Bu kılavuz, **AetherDesk** projesinin teknik kurulumunu, çalıştırma adımlarını, veritabanı şemalarını ve bulut yayınlama adımlarını içermektedir.

---

## ⚡ 1. Hızlı Çalıştırma (Single Master Launcher)

Proje kök dizinindeki **[AetherDesk-Control-Center.bat](file:///c:/Users/QALab/Desktop/App_DataControl/AetherDesk_RemoteControl/AetherDesk-Control-Center.bat)** dosyasına çift tıklayarak Yönetim Merkezini başlatın:

1. **`[1] TUM SERVISLERI BASLAT`**: 5 arka plan servisini (Sinyalleşme: 8080, SaaS API: 5000, Web Viewer: 9000, SaaS Frontend: 9090 ve Cloudflare Tüneli) çalıştırır.
2. **`[2] BULUTA YAYINLA (DEPLOY)`**: Önce GitHub (`gandallff/MyAetherdesk-control-`) reposuna yükleme yapar, ardından Vercel (`https://aetherdesk-control.vercel.app`) adresine yayınlar.

---

## 🌐 2. Bulut Yayınlama & Git Kuralları

- **`.gitignore` Yapılandırması**: `node_modules/`, `dist/`, `target/`, `.vercel/` ve `.db` dosyaları Git takibinden çıkarılmıştır.
- **GitHub Deposu**: **[https://github.com/gandallff/MyAetherdesk-control-](https://github.com/gandallff/MyAetherdesk-control-)**
- **Vercel Canlı Adresi**: **[https://aetherdesk-control.vercel.app](https://aetherdesk-control.vercel.app)**

---

## 🔌 3. Port ve Ağ Tablosu

| Port | Servis Adı | İşlevi |
| :--- | :--- | :--- |
| **8080** | Sinyalleşme & İndirme Sunucusu | WebSocket SDP takası & `.bat/.exe` indirme |
| **9090** | SaaS Portalı Frontend | Ticari pazarlama & üyelik paneli |
| **9000** | Uzaktan Masaüstü Web Viewer | Tarayıcı uzaktan kontrol ekranı |
| **5000** | SaaS Backend API | Express REST API & SQLite Veritabanı |
| **8443** | Direct Agent Socket | Doğrudan IP/LAN üzerinden ajan soket dinleyicisi |

---

## 💾 4. Veritabanı Şeması (`aetherdesk_saas.db`)

```sql
CREATE TABLE IF NOT EXISTS users (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  email TEXT UNIQUE NOT NULL,
  password_hash TEXT NOT NULL,
  company_name TEXT,
  plan TEXT DEFAULT 'FREE',
  subscription_status TEXT DEFAULT 'ACTIVE',
  created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS devices (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  user_id INTEGER,
  device_name TEXT NOT NULL,
  session_id TEXT NOT NULL,
  local_ip TEXT,
  status TEXT DEFAULT 'ONLINE',
  last_seen DATETIME DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY(user_id) REFERENCES users(id)
);
```
