# ⚡ AetherDesk Remote Control & Commercial SaaS Platform

AetherDesk, yüksek performanslı **Rust tabanlı DXGI GPU ekran yakalama**, ultra düşük gecikmeli **WebRTC sinyalleşmesi**, ticari **SaaS üyelik modelleri** ve **Vercel / GitHub entegreli bulut yayın mimarisi** sunan kurumsal uzaktan masaüstü ve cihaz yönetim platformudur.

---

## 🌐 Canlı Sistem ve Bulut Bağlantıları (Live Production Links)

| Bileşen | Canlı Adres / Bağlantı | Durum |
| :--- | :--- | :--- |
| 🚀 **Vercel Canlı SaaS Portalı** | **[https://aetherdesk-control.vercel.app](https://aetherdesk-control.vercel.app)** | 🟢 7/24 YAYINDA |
| 🐙 **GitHub Resmi Deposu** | **[https://github.com/gandalff/AetherDesk](https://github.com/gandalff/AetherDesk)** | 🟢 CANLI |
| 🎛️ **Master Yönetim Merkezi** | **[AetherDesk-Control-Center.bat](file:///c:/Users/QALab/Desktop/App_DataControl/AetherDesk_RemoteControl/AetherDesk-Control-Center.bat)** | 🟢 MASAÜSTÜ GUI |

---

## 🎛️ Tek Merkezden Yönetim (`AetherDesk-Control-Center.bat`)

Proje kök dizininde bulunan **[AetherDesk-Control-Center.bat](file:///c:/Users/QALab/Desktop/App_DataControl/AetherDesk_RemoteControl/AetherDesk-Control-Center.bat)** dosyası, tüm ekosistemi pencereli arayüz ile yönetir:

- **`[1] TÜM SERVİSLERİ BAŞLAT`**: 4 Yerel Servis + Cloudflare Tünelini aynı anda çalıştırır.
- **`[2] BULUTA YAYINLA (DEPLOY)`**: Önce GitHub reposunu günceller, ardından Vercel canlı bulut sitesini yayınlar. Görsel **Progress Bar (%10-%100)** ve işlem sonu **Popup Bildirimi** sunar.
- **`[3] MÜŞTERİ DESTEK ARACI`**: İstemci bilgisayarlarda 9 Haneli ID, LAN IP tespiti ve tek tıkla kopyalama sunan GUI aracını açar.
- **`[4] KURULUM SİHİRBAZI`**: 1-Tıkla Windows Kurulum Sihirbazı formunu açar.

---

## 🚀 Öne Çıkan Kurumsal Özellikler

1. **Ultra Düşük Gecikme (60 FPS DXGI GPU Capture)**:
   - Rust dilinde yazılmış GPU bellek kopyalama (`DXGI Desktop Duplication API`) ve NVENC donanım ivmeli H.264 video kodlama.
2. **Ticari SaaS ve Abonelik Modelleri**:
   - Ücretsiz QuickSupport, Pro Solo ($15/ay) ve Enterprise Team ($49/ay) üyelik katmanları, 1-tıkla ödeme simülasyonu modalı.
3. **Çok Dilli (TR / EN) Ön Yüz**:
   - Türkçe Varsayılan dil seçeneği ve tek tıkla İngilizce (`🇹🇷 TR / 🇬🇧 EN`) geçiş imkanı.
4. **Yerel Ağ (LAN IP) Tespiti ve İkili Kopyalama**:
   - Ajan çalıştırıldığında yerel IPv4 adresini (Örn: `192.168.1.105:8443`) otomatik algılar ve tek tıkla kopyalama imkanı sunar.
5. **Windows Defender Güvenlik Duvarı Yöneticisi**:
   - 8443, 8080, 9000, 9090 ve WebRTC portları için otomatik kural ekleme.
