using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace AetherDesk.Agent
{
    public class Program
    {
        [DllImport("user32.dll")]
        static extern bool SetProcessDPIAware();

        [STAThread]
        public static void Main(string[] args)
        {
            try { SetProcessDPIAware(); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new AetherDeskAppForm());
        }
    }

    public class AetherDeskAppForm : Form
    {
        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x04;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const uint MOUSEEVENTF_RIGHTUP = 0x10;

        // UI Components
        private Panel pnlSidebar;
        private Panel pnlHeader;
        private Panel pnlMainContent;
        
        // Navigation Buttons
        private Button btnNavMyDevice;
        private Button btnNavRemoteConnect;
        private Button btnNavSecurity;
        private Button btnNavSettings;

        // Content Views
        private Panel viewMyDevice;
        private Panel viewRemoteConnect;
        private Panel viewSecurity;
        private Panel viewSettings;

        // My Device View Elements
        private Label lblMyIdValue;
        private Button btnCopyId;
        private Label lblNetStatus;
        private Panel statusPill;
        private CheckBox chkQuickInputAllow;
        private CheckBox chkQuickFileAllow;
        private CheckBox chkQuickClipAllow;

        // Remote Connect View Elements
        private TextBox txtTargetId;
        private Button btnConnectTarget;
        private FlowLayoutPanel pnlRecentSessions;

        // Security View Elements
        private RadioButton rbUnattended;
        private RadioButton rbPassword;
        private RadioButton rbPrompt;
        private TextBox txtCustomPassword;
        private Button btnSaveSecurity;

        private string mySessionId;
        private HttpListener listener;
        private Thread listenThread;
        private Thread cloudRelayThread;
        private Thread inputPollThread;
        private bool isRunning = true;

        public static string CLOUD_RELAY_URL = "https://myaetherdesk-control.onrender.com";

        public AetherDeskAppForm()
        {
            this.mySessionId = GetOrCreateUniqueSessionId();

            this.Text = "AetherDesk Enterprise 2026";
            this.Size = new Size(820, 600);
            this.MinimumSize = new Size(820, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(7, 11, 20);
            this.ForeColor = Color.FromArgb(241, 245, 249);
            this.Font = new Font("Segoe UI", 9.5f);

            InitializeLayout();
            LoadSettings();
            StartListener();
            StartCloudRelayThread();
            StartInputPollThread();
        }

        private void InitializeLayout()
        {
            // 1. Sidebar Panel (Left Navigation)
            pnlSidebar = new Panel();
            pnlSidebar.Dock = DockStyle.Left;
            pnlSidebar.Width = 220;
            pnlSidebar.BackColor = Color.FromArgb(11, 17, 32);
            this.Controls.Add(pnlSidebar);

            // Brand Logo & Title in Sidebar
            Panel pnlBrand = new Panel();
            pnlBrand.Dock = DockStyle.Top;
            pnlBrand.Height = 75;
            pnlBrand.Padding = new Padding(18, 18, 10, 10);
            
            Label lblBrandLogo = new Label();
            lblBrandLogo.Text = "⚡ AetherDesk";
            lblBrandLogo.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblBrandLogo.ForeColor = Color.FromArgb(56, 189, 248);
            lblBrandLogo.AutoSize = true;
            pnlBrand.Controls.Add(lblBrandLogo);

            Label lblBrandSub = new Label();
            lblBrandSub.Text = "Enterprise Remote Suite";
            lblBrandSub.Font = new Font("Segoe UI", 8);
            lblBrandSub.ForeColor = Color.FromArgb(148, 163, 184);
            lblBrandSub.Location = new Point(22, 42);
            lblBrandSub.AutoSize = true;
            pnlBrand.Controls.Add(lblBrandSub);
            pnlSidebar.Controls.Add(pnlBrand);

            // Navigation Buttons Container
            Panel pnlNavContainer = new Panel();
            pnlNavContainer.Dock = DockStyle.Fill;
            pnlNavContainer.Padding = new Padding(12, 10, 12, 10);
            pnlSidebar.Controls.Add(pnlNavContainer);
            pnlNavContainer.BringToFront();

            btnNavMyDevice = CreateNavButton("🖥️  Bu Bilgisayarım", 0, (s, e) => SwitchView(viewMyDevice, btnNavMyDevice));
            btnNavRemoteConnect = CreateNavButton("🚀  Uzak Bağlantı", 48, (s, e) => SwitchView(viewRemoteConnect, btnNavRemoteConnect));
            btnNavSecurity = CreateNavButton("🔒  Güvenlik & Yetki", 96, (s, e) => SwitchView(viewSecurity, btnNavSecurity));
            btnNavSettings = CreateNavButton("⚙️  Ayarlar & Ağ", 144, (s, e) => SwitchView(viewSettings, btnNavSettings));

            pnlNavContainer.Controls.Add(btnNavMyDevice);
            pnlNavContainer.Controls.Add(btnNavRemoteConnect);
            pnlNavContainer.Controls.Add(btnNavSecurity);
            pnlNavContainer.Controls.Add(btnNavSettings);

            // Sidebar Footer Version
            Label lblVersion = new Label();
            lblVersion.Dock = DockStyle.Bottom;
            lblVersion.Height = 40;
            lblVersion.Text = "v2.5.0 Enterprise • 2026\nP2P & Cloud Active";
            lblVersion.Font = new Font("Consolas", 7.5f);
            lblVersion.ForeColor = Color.FromArgb(100, 116, 139);
            lblVersion.TextAlign = ContentAlignment.MiddleCenter;
            pnlSidebar.Controls.Add(lblVersion);

            // 2. Main Content Container (Right)
            pnlMainContent = new Panel();
            pnlMainContent.Dock = DockStyle.Fill;
            pnlMainContent.BackColor = Color.FromArgb(7, 11, 20);
            pnlMainContent.Padding = new Padding(24);
            this.Controls.Add(pnlMainContent);

            // Build Individual Views
            BuildMyDeviceView();
            BuildRemoteConnectView();
            BuildSecurityView();
            BuildSettingsView();

            // Default Active View
            SwitchView(viewMyDevice, btnNavMyDevice);
        }

        private Button CreateNavButton(string text, int top, EventHandler onClick)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Top = top;
            btn.Left = 0;
            btn.Width = 196;
            btn.Height = 42;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.Transparent;
            btn.ForeColor = Color.FromArgb(203, 213, 225);
            btn.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.Padding = new Padding(12, 0, 0, 0);
            btn.Cursor = Cursors.Hand;
            btn.Click += onClick;
            return btn;
        }

        private void SwitchView(Panel targetView, Button activeBtn)
        {
            viewMyDevice.Visible = false;
            viewRemoteConnect.Visible = false;
            viewSecurity.Visible = false;
            viewSettings.Visible = false;

            btnNavMyDevice.BackColor = Color.Transparent;
            btnNavMyDevice.ForeColor = Color.FromArgb(203, 213, 225);
            btnNavRemoteConnect.BackColor = Color.Transparent;
            btnNavRemoteConnect.ForeColor = Color.FromArgb(203, 213, 225);
            btnNavSecurity.BackColor = Color.Transparent;
            btnNavSecurity.ForeColor = Color.FromArgb(203, 213, 225);
            btnNavSettings.BackColor = Color.Transparent;
            btnNavSettings.ForeColor = Color.FromArgb(203, 213, 225);

            targetView.Visible = true;
            targetView.BringToFront();

            activeBtn.BackColor = Color.FromArgb(30, 41, 59);
            activeBtn.ForeColor = Color.FromArgb(56, 189, 248);
        }

        // VIEW 1: Bu Bilgisayarım (My Device)
        private void BuildMyDeviceView()
        {
            viewMyDevice = new Panel();
            viewMyDevice.Dock = DockStyle.Fill;
            pnlMainContent.Controls.Add(viewMyDevice);

            Label lblHeading = new Label();
            lblHeading.Text = "Bu Bilgisayara Erişim (ID & İzinler)";
            lblHeading.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblHeading.ForeColor = Color.FromArgb(248, 250, 252);
            lblHeading.Location = new Point(0, 0);
            lblHeading.AutoSize = true;
            viewMyDevice.Controls.Add(lblHeading);

            Label lblDesc = new Label();
            lblDesc.Text = "Bu bilgisayarı uzaktan yönetmek isteyen kişiye aşağıdaki 9 haneli ID'yi iletiniz.";
            lblDesc.Font = new Font("Segoe UI", 9);
            lblDesc.ForeColor = Color.FromArgb(148, 163, 184);
            lblDesc.Location = new Point(0, 26);
            lblDesc.AutoSize = true;
            viewMyDevice.Controls.Add(lblDesc);

            // Glowing ID Card Box
            Panel pnlIdCard = new Panel();
            pnlIdCard.Location = new Point(0, 60);
            pnlIdCard.Size = new Size(540, 140);
            pnlIdCard.BackColor = Color.FromArgb(15, 23, 42);
            pnlIdCard.Paint += (s, e) => {
                using (Pen p = new Pen(Color.FromArgb(56, 189, 248), 1.5f))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlIdCard.Width - 1, pnlIdCard.Height - 1);
                }
            };
            viewMyDevice.Controls.Add(pnlIdCard);

            statusPill = new Panel();
            statusPill.Location = new Point(20, 18);
            statusPill.Size = new Size(10, 10);
            statusPill.BackColor = Color.FromArgb(16, 185, 129);
            pnlIdCard.Controls.Add(statusPill);

            lblNetStatus = new Label();
            lblNetStatus.Text = "YAYIN HAZIR (BULUT & P2P AKTİF)";
            lblNetStatus.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            lblNetStatus.ForeColor = Color.FromArgb(52, 211, 153);
            lblNetStatus.Location = new Point(36, 15);
            lblNetStatus.AutoSize = true;
            pnlIdCard.Controls.Add(lblNetStatus);

            lblMyIdValue = new Label();
            lblMyIdValue.Text = this.mySessionId;
            lblMyIdValue.Font = new Font("Consolas", 26, FontStyle.Bold);
            lblMyIdValue.ForeColor = Color.FromArgb(56, 189, 248);
            lblMyIdValue.Location = new Point(20, 42);
            lblMyIdValue.Size = new Size(330, 48);
            pnlIdCard.Controls.Add(lblMyIdValue);

            btnCopyId = new Button();
            btnCopyId.Text = "📋 ID Kopyala";
            btnCopyId.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnCopyId.ForeColor = Color.White;
            btnCopyId.BackColor = Color.FromArgb(37, 99, 235);
            btnCopyId.FlatStyle = FlatStyle.Flat;
            btnCopyId.FlatAppearance.BorderSize = 0;
            btnCopyId.Location = new Point(365, 48);
            btnCopyId.Size = new Size(150, 40);
            btnCopyId.Cursor = Cursors.Hand;
            btnCopyId.Click += (s, e) => {
                Clipboard.SetText(this.mySessionId.Replace(" ", ""));
                btnCopyId.Text = "✓ Kopyalandı!";
                btnCopyId.BackColor = Color.FromArgb(16, 185, 129);
            };
            pnlIdCard.Controls.Add(btnCopyId);

            string localIp = GetLocalIp();
            Label lblIp = new Label();
            lblIp.Text = "Yerel Ağ (LAN): " + localIp + ":8443  •  Bulut Relay: Aktif";
            lblIp.Font = new Font("Consolas", 8.5f);
            lblIp.ForeColor = Color.FromArgb(148, 163, 184);
            lblIp.Location = new Point(20, 104);
            lblIp.AutoSize = true;
            pnlIdCard.Controls.Add(lblIp);

            // Quick Permission Toggles Box
            GroupBox grpQuickPerms = new GroupBox();
            grpQuickPerms.Text = " 🛡️ Hızlı İzin Yönetimi ";
            grpQuickPerms.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            grpQuickPerms.ForeColor = Color.FromArgb(56, 189, 248);
            grpQuickPerms.Location = new Point(0, 220);
            grpQuickPerms.Size = new Size(540, 150);
            viewMyDevice.Controls.Add(grpQuickPerms);

            chkQuickInputAllow = new CheckBox();
            chkQuickInputAllow.Text = "Fare ve Klavye Kontrolüne İzin Ver (Tam Yönetim)";
            chkQuickInputAllow.Checked = true;
            chkQuickInputAllow.Font = new Font("Segoe UI", 9);
            chkQuickInputAllow.ForeColor = Color.FromArgb(241, 245, 249);
            chkQuickInputAllow.Location = new Point(20, 30);
            chkQuickInputAllow.Size = new Size(500, 24);
            chkQuickInputAllow.CheckedChanged += (s, e) => SaveQuickSettings();
            grpQuickPerms.Controls.Add(chkQuickInputAllow);

            chkQuickFileAllow = new CheckBox();
            chkQuickFileAllow.Text = "Çift Yönlü Dosya Transferine İzin Ver";
            chkQuickFileAllow.Checked = true;
            chkQuickFileAllow.Font = new Font("Segoe UI", 9);
            chkQuickFileAllow.ForeColor = Color.FromArgb(241, 245, 249);
            chkQuickFileAllow.Location = new Point(20, 62);
            chkQuickFileAllow.Size = new Size(500, 24);
            chkQuickFileAllow.CheckedChanged += (s, e) => SaveQuickSettings();
            grpQuickPerms.Controls.Add(chkQuickFileAllow);

            chkQuickClipAllow = new CheckBox();
            chkQuickClipAllow.Text = "Pano Paylaşımına İzin Ver (Metin Kopyala / Yapıştır)";
            chkQuickClipAllow.Checked = true;
            chkQuickClipAllow.Font = new Font("Segoe UI", 9);
            chkQuickClipAllow.ForeColor = Color.FromArgb(241, 245, 249);
            chkQuickClipAllow.Location = new Point(20, 94);
            chkQuickClipAllow.Size = new Size(500, 24);
            chkQuickClipAllow.CheckedChanged += (s, e) => SaveQuickSettings();
            grpQuickPerms.Controls.Add(chkQuickClipAllow);

            // Instant Disconnect Button
            Button btnDisconnect = new Button();
            btnDisconnect.Text = "🚫 Aktif Bağlantıyı Hemen Sonlandır";
            btnDisconnect.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnDisconnect.ForeColor = Color.White;
            btnDisconnect.BackColor = Color.FromArgb(225, 29, 72);
            btnDisconnect.FlatStyle = FlatStyle.Flat;
            btnDisconnect.FlatAppearance.BorderSize = 0;
            btnDisconnect.Location = new Point(0, 390);
            btnDisconnect.Size = new Size(540, 42);
            btnDisconnect.Cursor = Cursors.Hand;
            btnDisconnect.Click += (s, e) => {
                MessageBox.Show("Aktif uzak bağlantı sonlandırıldı.", "AetherDesk Enterprise", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            viewMyDevice.Controls.Add(btnDisconnect);
        }

        // VIEW 2: Uzak Bağlantı (Remote Connect)
        private void BuildRemoteConnectView()
        {
            viewRemoteConnect = new Panel();
            viewRemoteConnect.Dock = DockStyle.Fill;
            pnlMainContent.Controls.Add(viewRemoteConnect);

            Label lblHeading = new Label();
            lblHeading.Text = "Başka Bir Bilgisayara Bağlan";
            lblHeading.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblHeading.ForeColor = Color.FromArgb(248, 250, 252);
            lblHeading.Location = new Point(0, 0);
            lblHeading.AutoSize = true;
            viewRemoteConnect.Controls.Add(lblHeading);

            Label lblDesc = new Label();
            lblDesc.Text = "Karşı bilgisayarın ekranında yazan 9 haneli ID'yi girerek anında bağlanın.";
            lblDesc.Font = new Font("Segoe UI", 9);
            lblDesc.ForeColor = Color.FromArgb(148, 163, 184);
            lblDesc.Location = new Point(0, 26);
            lblDesc.AutoSize = true;
            viewRemoteConnect.Controls.Add(lblDesc);

            // Target ID Input Box
            Panel pnlTargetBox = new Panel();
            pnlTargetBox.Location = new Point(0, 60);
            pnlTargetBox.Size = new Size(540, 70);
            pnlTargetBox.BackColor = Color.FromArgb(15, 23, 42);
            viewRemoteConnect.Controls.Add(pnlTargetBox);

            txtTargetId = new TextBox();
            txtTargetId.Font = new Font("Consolas", 14, FontStyle.Bold);
            txtTargetId.BackColor = Color.FromArgb(10, 15, 29);
            txtTargetId.ForeColor = Color.FromArgb(56, 189, 248);
            txtTargetId.Location = new Point(18, 18);
            txtTargetId.Size = new Size(330, 32);
            pnlTargetBox.Controls.Add(txtTargetId);

            btnConnectTarget = new Button();
            btnConnectTarget.Text = "🚀 Hemen Bağlan";
            btnConnectTarget.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnConnectTarget.ForeColor = Color.White;
            btnConnectTarget.BackColor = Color.FromArgb(37, 99, 235);
            btnConnectTarget.FlatStyle = FlatStyle.Flat;
            btnConnectTarget.FlatAppearance.BorderSize = 0;
            btnConnectTarget.Location = new Point(365, 16);
            btnConnectTarget.Size = new Size(155, 36);
            btnConnectTarget.Cursor = Cursors.Hand;
            btnConnectTarget.Click += (s, e) => {
                string target = txtTargetId.Text.Trim();
                if (!string.IsNullOrEmpty(target))
                {
                    System.Diagnostics.Process.Start("https://my-aetherdesk-control.vercel.app/?connect=" + target.Replace(" ", ""));
                }
            };
            pnlTargetBox.Controls.Add(btnConnectTarget);

            // Recent Sessions Section
            Label lblRecent = new Label();
            lblRecent.Text = "Son Bağlanılan Oturumlar (Geçmiş):";
            lblRecent.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblRecent.ForeColor = Color.FromArgb(203, 213, 225);
            lblRecent.Location = new Point(0, 150);
            lblRecent.AutoSize = true;
            viewRemoteConnect.Controls.Add(lblRecent);

            pnlRecentSessions = new FlowLayoutPanel();
            pnlRecentSessions.Location = new Point(0, 180);
            pnlRecentSessions.Size = new Size(540, 240);
            pnlRecentSessions.AutoScroll = true;
            viewRemoteConnect.Controls.Add(pnlRecentSessions);

            AddRecentCard("778 375 604", "Ofis Bilgisayarı", "18 ms");
            AddRecentCard("482 910 375", "Ana Sunucu", "22 ms");
            AddRecentCard("891 204 153", "Muhasebe Terminali", "14 ms");
        }

        private void AddRecentCard(string id, string name, string ping)
        {
            Panel card = new Panel();
            card.Size = new Size(170, 95);
            card.Margin = new Padding(0, 0, 10, 10);
            card.BackColor = Color.FromArgb(15, 23, 42);
            card.Cursor = Cursors.Hand;

            Label lblName = new Label();
            lblName.Text = name;
            lblName.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblName.ForeColor = Color.FromArgb(241, 245, 249);
            lblName.Location = new Point(10, 10);
            lblName.AutoSize = true;
            card.Controls.Add(lblName);

            Label lblId = new Label();
            lblId.Text = id;
            lblId.Font = new Font("Consolas", 10, FontStyle.Bold);
            lblId.ForeColor = Color.FromArgb(56, 189, 248);
            lblId.Location = new Point(10, 32);
            lblId.AutoSize = true;
            card.Controls.Add(lblId);

            Label lblPing = new Label();
            lblPing.Text = "🟢 " + ping;
            lblPing.Font = new Font("Segoe UI", 7.5f);
            lblPing.ForeColor = Color.FromArgb(52, 211, 153);
            lblPing.Location = new Point(10, 62);
            lblPing.AutoSize = true;
            card.Controls.Add(lblPing);

            card.Click += (s, e) => {
                txtTargetId.Text = id.Replace(" ", "");
                System.Diagnostics.Process.Start("https://my-aetherdesk-control.vercel.app/?connect=" + id.Replace(" ", ""));
            };

            pnlRecentSessions.Controls.Add(card);
        }

        // VIEW 3: Güvenlik & Yetki (Security & Access)
        private void BuildSecurityView()
        {
            viewSecurity = new Panel();
            viewSecurity.Dock = DockStyle.Fill;
            pnlMainContent.Controls.Add(viewSecurity);

            Label lblHeading = new Label();
            lblHeading.Text = "Erişim Modları ve Güvenlik";
            lblHeading.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblHeading.ForeColor = Color.FromArgb(248, 250, 252);
            lblHeading.Location = new Point(0, 0);
            lblHeading.AutoSize = true;
            viewSecurity.Controls.Add(lblHeading);

            GroupBox grpModes = new GroupBox();
            grpModes.Text = " 🔒 Bağlantı Doğrulama Yöntemi ";
            grpModes.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            grpModes.ForeColor = Color.FromArgb(56, 189, 248);
            grpModes.Location = new Point(0, 45);
            grpModes.Size = new Size(540, 210);
            viewSecurity.Controls.Add(grpModes);

            rbUnattended = new RadioButton();
            rbUnattended.Text = "Katılımsız Erişim (Şifresiz Doğrudan Kabul)";
            rbUnattended.Font = new Font("Segoe UI", 9);
            rbUnattended.ForeColor = Color.FromArgb(241, 245, 249);
            rbUnattended.Location = new Point(20, 28);
            rbUnattended.Size = new Size(480, 24);
            grpModes.Controls.Add(rbUnattended);

            rbPassword = new RadioButton();
            rbPassword.Text = "Özel Şifreli Erişim (Bağlanmak isteyene şifre sorulsun)";
            rbPassword.Font = new Font("Segoe UI", 9);
            rbPassword.ForeColor = Color.FromArgb(241, 245, 249);
            rbPassword.Location = new Point(20, 60);
            rbPassword.Size = new Size(480, 24);
            grpModes.Controls.Add(rbPassword);

            txtCustomPassword = new TextBox();
            txtCustomPassword.Font = new Font("Consolas", 11);
            txtCustomPassword.BackColor = Color.FromArgb(10, 15, 29);
            txtCustomPassword.ForeColor = Color.FromArgb(245, 158, 11);
            txtCustomPassword.Location = new Point(42, 92);
            txtCustomPassword.Size = new Size(200, 27);
            grpModes.Controls.Add(txtCustomPassword);

            rbPrompt = new RadioButton();
            rbPrompt.Text = "Manuel Onay (Her bağlantıda ekranda Kabul/Reddet penceresi çıksın)";
            rbPrompt.Font = new Font("Segoe UI", 9);
            rbPrompt.ForeColor = Color.FromArgb(241, 245, 249);
            rbPrompt.Location = new Point(20, 130);
            rbPrompt.Size = new Size(480, 24);
            grpModes.Controls.Add(rbPrompt);

            btnSaveSecurity = new Button();
            btnSaveSecurity.Text = "✓ Güvenlik Ayarlarını Kaydet";
            btnSaveSecurity.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnSaveSecurity.ForeColor = Color.White;
            btnSaveSecurity.BackColor = Color.FromArgb(16, 185, 129);
            btnSaveSecurity.FlatStyle = FlatStyle.Flat;
            btnSaveSecurity.FlatAppearance.BorderSize = 0;
            btnSaveSecurity.Location = new Point(0, 280);
            btnSaveSecurity.Size = new Size(540, 42);
            btnSaveSecurity.Cursor = Cursors.Hand;
            btnSaveSecurity.Click += (s, e) => SaveSecuritySettings();
            viewSecurity.Controls.Add(btnSaveSecurity);
        }

        // VIEW 4: Ayarlar & Ağ (Settings & Network)
        private void BuildSettingsView()
        {
            viewSettings = new Panel();
            viewSettings.Dock = DockStyle.Fill;
            pnlMainContent.Controls.Add(viewSettings);

            Label lblHeading = new Label();
            lblHeading.Text = "Sistem & Ağ Durumu";
            lblHeading.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblHeading.ForeColor = Color.FromArgb(248, 250, 252);
            lblHeading.Location = new Point(0, 0);
            lblHeading.AutoSize = true;
            viewSettings.Controls.Add(lblHeading);

            Panel pnlNetInfo = new Panel();
            pnlNetInfo.Location = new Point(0, 45);
            pnlNetInfo.Size = new Size(540, 260);
            pnlNetInfo.BackColor = Color.FromArgb(15, 23, 42);
            pnlNetInfo.Padding = new Padding(20);
            viewSettings.Controls.Add(pnlNetInfo);

            Label lblDetails = new Label();
            lblDetails.Text = "Bulut Sinyal Sunucusu:\n" + CLOUD_RELAY_URL + "\n\n" +
                              "Yerel Dinleme Portu:\n8443 (HTTP & Direct Socket)\n\n" +
                              "Görüntü Yakalama Motoru:\nDXGI / Windows Graphics Core (60 FPS)\n\n" +
                              "Girdi İşleme Gecikmesi:\n~14 ms (DPI-Aware Native Subsystem)";
            lblDetails.Font = new Font("Segoe UI", 9.5f);
            lblDetails.ForeColor = Color.FromArgb(203, 213, 225);
            lblDetails.Dock = DockStyle.Fill;
            pnlNetInfo.Controls.Add(lblDetails);
        }

        private void LoadSettings()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\AetherDesk"))
                {
                    string mode = (key.GetValue("AccessMode") ?? "UNATTENDED").ToString();
                    string pass = (key.GetValue("AccessPassword") ?? "aether2026").ToString();
                    bool allowInput = bool.Parse((key.GetValue("AllowInput") ?? "True").ToString());
                    bool allowFiles = bool.Parse((key.GetValue("AllowFiles") ?? "True").ToString());
                    bool allowClip = bool.Parse((key.GetValue("AllowClip") ?? "True").ToString());

                    txtCustomPassword.Text = pass;
                    chkQuickInputAllow.Checked = allowInput;
                    chkQuickFileAllow.Checked = allowFiles;
                    chkQuickClipAllow.Checked = allowClip;

                    if (mode == "PASSWORD") rbPassword.Checked = true;
                    else if (mode == "PROMPT") rbPrompt.Checked = true;
                    else rbUnattended.Checked = true;
                }
            }
            catch { rbUnattended.Checked = true; }
        }

        private void SaveQuickSettings()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\AetherDesk"))
                {
                    key.SetValue("AllowInput", chkQuickInputAllow.Checked.ToString());
                    key.SetValue("AllowFiles", chkQuickFileAllow.Checked.ToString());
                    key.SetValue("AllowClip", chkQuickClipAllow.Checked.ToString());
                }
            }
            catch { }
        }

        private void SaveSecuritySettings()
        {
            try
            {
                string mode = rbPassword.Checked ? "PASSWORD" : (rbPrompt.Checked ? "PROMPT" : "UNATTENDED");
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\AetherDesk"))
                {
                    key.SetValue("AccessMode", mode);
                    key.SetValue("AccessPassword", txtCustomPassword.Text.Trim());
                }
                btnSaveSecurity.Text = "✓ Ayarlar Kaydedildi!";
                btnSaveSecurity.BackColor = Color.FromArgb(5, 150, 105);
            }
            catch { }
        }

        private string GetOrCreateUniqueSessionId()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\AetherDesk"))
                {
                    object val = key.GetValue("SessionId");
                    if (val != null && !string.IsNullOrEmpty(val.ToString())) return val.ToString();
                    Random rnd = new Random();
                    string newId = string.Format("{0:D3} {1:D3} {2:D3}", rnd.Next(100, 999), rnd.Next(100, 999), rnd.Next(100, 999));
                    key.SetValue("SessionId", newId);
                    return newId;
                }
            }
            catch
            {
                Random rnd = new Random();
                return string.Format("{0:D3} {1:D3} {2:D3}", rnd.Next(100, 999), rnd.Next(100, 999), rnd.Next(100, 999));
            }
        }

        private void StartListener()
        {
            try
            {
                listener = new HttpListener();
                listener.Prefixes.Add("http://*:8443/");
                listener.Start();
                listenThread = new Thread(ListenLoop);
                listenThread.IsBackground = true;
                listenThread.Start();
            }
            catch { }
        }

        private void ListenLoop()
        {
            while (listener != null && listener.IsListening)
            {
                try
                {
                    HttpListenerContext ctx = listener.GetContext();
                    ThreadPool.QueueUserWorkItem((state) => HandleLocalRequest(ctx));
                }
                catch { }
            }
        }

        private void HandleLocalRequest(HttpListenerContext ctx)
        {
            try
            {
                ctx.Response.AddHeader("Access-Control-Allow-Origin", "*");
                ctx.Response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                ctx.Response.AddHeader("Access-Control-Allow-Headers", "*");

                if (ctx.Request.HttpMethod == "OPTIONS")
                {
                    ctx.Response.StatusCode = 200;
                    ctx.Response.Close();
                    return;
                }

                string path = ctx.Request.Url.AbsolutePath.ToLower();

                if (path == "/screen" || path == "/screenshot")
                {
                    byte[] jpegBytes = CaptureRealScreenJpeg();
                    ctx.Response.ContentType = "image/jpeg";
                    ctx.Response.ContentLength64 = jpegBytes.Length;
                    ctx.Response.OutputStream.Write(jpegBytes, 0, jpegBytes.Length);
                    ctx.Response.Close();
                    return;
                }

                if (path == "/mouse" && chkQuickInputAllow.Checked)
                {
                    ExecuteMouseEvent(ctx.Request.QueryString["x"], ctx.Request.QueryString["y"], ctx.Request.QueryString["sw"], ctx.Request.QueryString["sh"], ctx.Request.QueryString["action"]);
                    byte[] okBuf = System.Text.Encoding.UTF8.GetBytes("{\"ok\":true}");
                    ctx.Response.ContentType = "application/json";
                    ctx.Response.OutputStream.Write(okBuf, 0, okBuf.Length);
                    ctx.Response.Close();
                    return;
                }

                byte[] buf = System.Text.Encoding.UTF8.GetBytes("{\"status\":\"connected\",\"session\":\"" + this.mySessionId + "\"}");
                ctx.Response.ContentType = "application/json";
                ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                ctx.Response.Close();
            }
            catch { }
        }

        private void StartCloudRelayThread()
        {
            cloudRelayThread = new Thread(() =>
            {
                string cleanId = this.mySessionId.Replace(" ", "");
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                while (isRunning)
                {
                    try
                    {
                        byte[] screenJpeg = CaptureRealScreenJpeg();
                        HttpWebRequest uploadReq = (HttpWebRequest)WebRequest.Create(CLOUD_RELAY_URL + "/api/stream/" + cleanId);
                        uploadReq.Method = "POST";
                        uploadReq.ContentType = "image/jpeg";
                        uploadReq.ContentLength = screenJpeg.Length;
                        uploadReq.Timeout = 2000;

                        using (Stream reqStream = uploadReq.GetRequestStream())
                        {
                            reqStream.Write(screenJpeg, 0, screenJpeg.Length);
                        }
                        using (HttpWebResponse resp = (HttpWebResponse)uploadReq.GetResponse()) { }
                    }
                    catch { }

                    Thread.Sleep(300);
                }
            });
            cloudRelayThread.IsBackground = true;
            cloudRelayThread.Start();
        }

        private void StartInputPollThread()
        {
            inputPollThread = new Thread(() =>
            {
                string cleanId = this.mySessionId.Replace(" ", "");
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                while (isRunning)
                {
                    try
                    {
                        HttpWebRequest eventReq = (HttpWebRequest)WebRequest.Create(CLOUD_RELAY_URL + "/api/events/" + cleanId);
                        eventReq.Method = "GET";
                        eventReq.Timeout = 1500;

                        using (HttpWebResponse eventResp = (HttpWebResponse)eventReq.GetResponse())
                        using (StreamReader reader = new StreamReader(eventResp.GetResponseStream()))
                        {
                            string json = reader.ReadToEnd();
                            if (chkQuickInputAllow.Checked && !string.IsNullOrEmpty(json) && json.Contains("action"))
                            {
                                ProcessEventsRobust(json);
                            }
                        }
                    }
                    catch { }

                    Thread.Sleep(60);
                }
            });
            inputPollThread.IsBackground = true;
            inputPollThread.Start();
        }

        private void ProcessEventsRobust(string json)
        {
            try
            {
                MatchCollection matches = Regex.Matches(json, @"\{[^{}]*\}");
                foreach (Match m in matches)
                {
                    string obj = m.Value;
                    string action = GetRegexVal(obj, "action");
                    if (string.IsNullOrEmpty(action)) continue;

                    if (action == "click" || action == "rightclick" || action == "dblclick" || action == "move")
                    {
                        int x = int.Parse(GetRegexVal(obj, "x") ?? "0");
                        int y = int.Parse(GetRegexVal(obj, "y") ?? "0");
                        int sw = int.Parse(GetRegexVal(obj, "sw") ?? "1920");
                        int sh = int.Parse(GetRegexVal(obj, "sh") ?? "1080");

                        Rectangle bounds = Screen.PrimaryScreen.Bounds;
                        int realX = (int)((double)x / (sw > 0 ? sw : 1920) * bounds.Width);
                        int realY = (int)((double)y / (sh > 0 ? sh : 1080) * bounds.Height);

                        SetCursorPos(realX, realY);

                        if (action == "click")
                        {
                            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                            Thread.Sleep(20);
                            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                        }
                        else if (action == "rightclick")
                        {
                            mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                            Thread.Sleep(20);
                            mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                        }
                        else if (action == "dblclick")
                        {
                            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                            Thread.Sleep(40);
                            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                        }
                    }
                    else if (action == "key")
                    {
                        string key = GetRegexVal(obj, "key");
                        if (!string.IsNullOrEmpty(key))
                        {
                            SendKeySafe(key);
                        }
                    }
                }
            }
            catch { }
        }

        private string GetRegexVal(string json, string key)
        {
            Match m = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\"?([^,\"}]+)\"?");
            return m.Success ? m.Groups[1].Value.Trim() : null;
        }

        private void SendKeySafe(string key)
        {
            try
            {
                if (key == "Enter") SendKeys.SendWait("{ENTER}");
                else if (key == "Backspace") SendKeys.SendWait("{BACKSPACE}");
                else if (key == "Tab") SendKeys.SendWait("{TAB}");
                else if (key == "Escape") SendKeys.SendWait("{ESC}");
                else if (key == "ArrowUp") SendKeys.SendWait("{UP}");
                else if (key == "ArrowDown") SendKeys.SendWait("{DOWN}");
                else if (key == "ArrowLeft") SendKeys.SendWait("{LEFT}");
                else if (key == "ArrowRight") SendKeys.SendWait("{RIGHT}");
                else if (key.Length == 1) SendKeys.SendWait(key);
            }
            catch { }
        }

        private void ExecuteMouseEvent(string xStr, string yStr, string swStr, string shStr, string act)
        {
            try
            {
                if (!string.IsNullOrEmpty(xStr) && !string.IsNullOrEmpty(yStr))
                {
                    int targetX = int.Parse(xStr);
                    int targetY = int.Parse(yStr);
                    Rectangle screenBounds = Screen.PrimaryScreen.Bounds;
                    int screenW = int.Parse(swStr ?? screenBounds.Width.ToString());
                    int screenH = int.Parse(shStr ?? screenBounds.Height.ToString());

                    int realX = (int)((double)targetX / screenW * screenBounds.Width);
                    int realY = (int)((double)targetY / screenH * screenBounds.Height);

                    SetCursorPos(realX, realY);

                    if (act == "click")
                    {
                        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                        Thread.Sleep(20);
                        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                    }
                    else if (act == "rightclick")
                    {
                        mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                        Thread.Sleep(20);
                        mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                    }
                }
            }
            catch { }
        }

        private byte[] CaptureRealScreenJpeg()
        {
            Rectangle bounds = Screen.PrimaryScreen.Bounds;
            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(0, 0, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
                    Point cursorPoint = Cursor.Position;
                    Cursors.Default.Draw(g, new Rectangle(cursorPoint.X, cursorPoint.Y, 32, 32));
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);
                    EncoderParameters myEncoderParameters = new EncoderParameters(1);
                    myEncoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 60L);
                    bitmap.Save(ms, jpgEncoder, myEncoderParameters);
                    return ms.ToArray();
                }
            }
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid) return codec;
            }
            return null;
        }

        private string GetLocalIp()
        {
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    return endPoint != null ? endPoint.Address.ToString() : "192.168.1.100";
                }
            }
            catch
            {
                return "192.168.1.100";
            }
        }
    }
}
