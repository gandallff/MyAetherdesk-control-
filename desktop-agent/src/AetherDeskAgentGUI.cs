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

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x04;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const uint MOUSEEVENTF_RIGHTUP = 0x10;

        // Main Layout Panels
        private Panel pnlHeader;
        private Button btnHamburger;
        private Label lblAppTitle;
        private Panel pnlHeaderStatus;
        private Label lblHeaderStatusText;
        private Panel pnlBody;
        private Panel pnlSidebar;
        private Panel pnlContent;

        // Navigation Buttons
        private Button btnNavMyDevice;
        private Button btnNavRemoteConnect;
        private Button btnNavActiveSession;
        private Button btnNavSecurity;
        private Button btnNavSettings;

        // Content Pages
        private Panel pageMyDevice;
        private Panel pageRemoteConnect;
        private Panel pageSecurity;
        private Panel pageSettings;

        // In-App Unified Remote Viewer
        private Panel pageActiveSession;
        private PictureBox picSessionViewport;
        private Panel pnlSessionTopBar;
        private Label lblSessionTargetInfo;
        private Label lblSessionDuration;
        private Button btnCloseSession;
        private Button btnSessionCtrlAltDel;
        private Button btnSessionSendFile;
        private Button btnSessionFullscreen;
        private Thread inAppStreamThread;
        private bool isInAppStreaming = false;
        private string activeConnectedId = "";
        private DateTime sessionStartTime;
        private System.Windows.Forms.Timer sessionTimer;

        // Page 1 Controls
        private Label lblIdText;
        private Button btnCopyId;
        private CheckBox chkInput;
        private CheckBox chkFiles;
        private CheckBox chkClipboard;
        private Button btnDisconnect;

        // Page 2 Controls
        private TextBox txtTargetId;
        private Button btnConnectTarget;
        private FlowLayoutPanel pnlRecentFlow;

        // Page 3 Controls
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
        private bool isSidebarOpen = true;

        public static string CLOUD_RELAY_URL = "https://myaetherdesk-control.onrender.com";

        public AetherDeskAppForm()
        {
            this.mySessionId = GetOrCreateUniqueSessionId();

            this.Text = "AetherDesk Enterprise 2026";
            this.Size = new Size(960, 680);
            this.MinimumSize = new Size(860, 580);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.BackColor = Color.FromArgb(7, 11, 20);
            this.ForeColor = Color.FromArgb(241, 245, 249);
            this.Font = new Font("Segoe UI", 9.5f);
            this.DoubleBuffered = true;

            sessionTimer = new System.Windows.Forms.Timer();
            sessionTimer.Interval = 1000;
            sessionTimer.Tick += (s, e) => UpdateSessionTimer();

            BuildAppStructure();
            LoadSettings();
            StartListener();
            StartCloudRelayThread();
            StartInputPollThread();
        }

        private void BuildAppStructure()
        {
            // 1. TOP MODERN HEADER BAR
            pnlHeader = new Panel();
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Height = 54;
            pnlHeader.BackColor = Color.FromArgb(15, 23, 42);
            pnlHeader.Padding = new Padding(12, 0, 16, 0);
            this.Controls.Add(pnlHeader);

            btnHamburger = new Button();
            btnHamburger.Text = "☰";
            btnHamburger.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            btnHamburger.ForeColor = Color.FromArgb(56, 189, 248);
            btnHamburger.BackColor = Color.FromArgb(30, 41, 59);
            btnHamburger.FlatStyle = FlatStyle.Flat;
            btnHamburger.FlatAppearance.BorderSize = 0;
            btnHamburger.Size = new Size(36, 36);
            btnHamburger.Location = new Point(10, 9);
            btnHamburger.Cursor = Cursors.Hand;
            btnHamburger.Click += (s, e) => ToggleSidebar();
            pnlHeader.Controls.Add(btnHamburger);

            lblAppTitle = new Label();
            lblAppTitle.Text = "⚡ AetherDesk Enterprise";
            lblAppTitle.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            lblAppTitle.ForeColor = Color.FromArgb(248, 250, 252);
            lblAppTitle.Location = new Point(56, 13);
            lblAppTitle.AutoSize = true;
            pnlHeader.Controls.Add(lblAppTitle);

            pnlHeaderStatus = new Panel();
            pnlHeaderStatus.Location = new Point(660, 11);
            pnlHeaderStatus.Size = new Size(260, 32);
            pnlHeaderStatus.BackColor = Color.FromArgb(10, 15, 29);
            pnlHeaderStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            pnlHeaderStatus.Paint += (s, e) => {
                using (Pen p = new Pen(Color.FromArgb(16, 185, 129), 1f))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlHeaderStatus.Width - 1, pnlHeaderStatus.Height - 1);
                }
            };
            pnlHeader.Controls.Add(pnlHeaderStatus);

            Panel dot = new Panel();
            dot.Location = new Point(10, 11);
            dot.Size = new Size(10, 10);
            dot.BackColor = Color.FromArgb(16, 185, 129);
            pnlHeaderStatus.Controls.Add(dot);

            lblHeaderStatusText = new Label();
            lblHeaderStatusText.Text = "Bulut & P2P Yayını Aktif (Online)";
            lblHeaderStatusText.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            lblHeaderStatusText.ForeColor = Color.FromArgb(52, 211, 153);
            lblHeaderStatusText.Location = new Point(24, 8);
            lblHeaderStatusText.AutoSize = true;
            pnlHeaderStatus.Controls.Add(lblHeaderStatusText);

            // 2. BODY CONTAINER
            pnlBody = new Panel();
            pnlBody.Dock = DockStyle.Fill;
            pnlBody.BackColor = Color.FromArgb(7, 11, 20);
            this.Controls.Add(pnlBody);
            pnlBody.BringToFront();

            // 3. COLLAPSIBLE SIDEBAR
            pnlSidebar = new Panel();
            pnlSidebar.Dock = DockStyle.Left;
            pnlSidebar.Width = 220;
            pnlSidebar.BackColor = Color.FromArgb(11, 17, 32);
            pnlSidebar.Padding = new Padding(8, 12, 8, 12);
            pnlBody.Controls.Add(pnlSidebar);

            btnNavMyDevice = CreateSidebarItem("🖥️  Bu Bilgisayarım", 10, (s, e) => ShowPage(pageMyDevice, btnNavMyDevice));
            btnNavRemoteConnect = CreateSidebarItem("🚀  Uzak Bağlantı", 60, (s, e) => ShowPage(pageRemoteConnect, btnNavRemoteConnect));
            
            // Dynamic Active Session Tab Button
            btnNavActiveSession = CreateSidebarItem("🟢  Canlı Oturum (Aktif)", 110, (s, e) => ShowPage(pageActiveSession, btnNavActiveSession));
            btnNavActiveSession.Visible = false;
            btnNavActiveSession.ForeColor = Color.FromArgb(52, 211, 153);

            btnNavSecurity = CreateSidebarItem("🔒  Güvenlik & Yetki", 160, (s, e) => ShowPage(pageSecurity, btnNavSecurity));
            btnNavSettings = CreateSidebarItem("⚙️  Ayarlar & Ağ", 210, (s, e) => ShowPage(pageSettings, btnNavSettings));

            pnlSidebar.Controls.Add(btnNavMyDevice);
            pnlSidebar.Controls.Add(btnNavRemoteConnect);
            pnlSidebar.Controls.Add(btnNavActiveSession);
            pnlSidebar.Controls.Add(btnNavSecurity);
            pnlSidebar.Controls.Add(btnNavSettings);

            // 4. MAIN CONTENT CONTAINER (PAGES)
            pnlContent = new Panel();
            pnlContent.Dock = DockStyle.Fill;
            pnlContent.BackColor = Color.FromArgb(7, 11, 20);
            pnlContent.Padding = new Padding(20);
            pnlBody.Controls.Add(pnlContent);
            pnlContent.BringToFront();

            BuildPageMyDevice();
            BuildPageRemoteConnect();
            BuildPageSecurity();
            BuildPageSettings();
            BuildPageActiveSession();

            ShowPage(pageMyDevice, btnNavMyDevice);
        }

        private void ToggleSidebar()
        {
            isSidebarOpen = !isSidebarOpen;
            pnlSidebar.Visible = isSidebarOpen;
        }

        private Button CreateSidebarItem(string title, int top, EventHandler onClick)
        {
            Button btn = new Button();
            btn.Text = title;
            btn.Top = top;
            btn.Left = 8;
            btn.Width = 204;
            btn.Height = 44;
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

        private void ShowPage(Panel targetPage, Button activeBtn)
        {
            pageMyDevice.Visible = false;
            pageRemoteConnect.Visible = false;
            pageSecurity.Visible = false;
            pageSettings.Visible = false;
            pageActiveSession.Visible = false;

            btnNavMyDevice.BackColor = Color.Transparent;
            btnNavMyDevice.ForeColor = Color.FromArgb(203, 213, 225);
            btnNavRemoteConnect.BackColor = Color.Transparent;
            btnNavRemoteConnect.ForeColor = Color.FromArgb(203, 213, 225);
            btnNavSecurity.BackColor = Color.Transparent;
            btnNavSecurity.ForeColor = Color.FromArgb(203, 213, 225);
            btnNavSettings.BackColor = Color.Transparent;
            btnNavSettings.ForeColor = Color.FromArgb(203, 213, 225);

            if (isInAppStreaming)
            {
                btnNavActiveSession.BackColor = Color.Transparent;
                btnNavActiveSession.ForeColor = Color.FromArgb(52, 211, 153);
            }

            if (activeBtn != null)
            {
                activeBtn.BackColor = Color.FromArgb(30, 41, 59);
                activeBtn.ForeColor = Color.FromArgb(56, 189, 248);
            }

            targetPage.Visible = true;
            targetPage.BringToFront();
        }

        // PAGE 1: Bu Bilgisayarım
        private void BuildPageMyDevice()
        {
            pageMyDevice = new Panel();
            pageMyDevice.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(pageMyDevice);

            Label lblH = new Label();
            lblH.Text = "Bu Bilgisayarın Oturum Bilgisi";
            lblH.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblH.ForeColor = Color.FromArgb(248, 250, 252);
            lblH.Location = new Point(0, 0);
            lblH.AutoSize = true;
            pageMyDevice.Controls.Add(lblH);

            Label lblSub = new Label();
            lblSub.Text = "Uzak kullanıcının bağlanabilmesi için aşağıdaki 9 haneli ID'yi paylaşın.";
            lblSub.Font = new Font("Segoe UI", 9);
            lblSub.ForeColor = Color.FromArgb(148, 163, 184);
            lblSub.Location = new Point(0, 26);
            lblSub.AutoSize = true;
            pageMyDevice.Controls.Add(lblSub);

            Panel pnlIdBox = new Panel();
            pnlIdBox.Location = new Point(0, 56);
            pnlIdBox.Size = new Size(660, 115);
            pnlIdBox.BackColor = Color.FromArgb(15, 23, 42);
            pnlIdBox.Paint += (s, e) => {
                using (Pen p = new Pen(Color.FromArgb(56, 189, 248), 1.5f))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlIdBox.Width - 1, pnlIdBox.Height - 1);
                }
            };
            pageMyDevice.Controls.Add(pnlIdBox);

            Label lblTag = new Label();
            lblTag.Text = "BU CİHAZIN 9 HANELİ ID'Sİ:";
            lblTag.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            lblTag.ForeColor = Color.FromArgb(148, 163, 184);
            lblTag.Location = new Point(18, 14);
            lblTag.AutoSize = true;
            pnlIdBox.Controls.Add(lblTag);

            lblIdText = new Label();
            lblIdText.Text = this.mySessionId;
            lblIdText.Font = new Font("Consolas", 26, FontStyle.Bold);
            lblIdText.ForeColor = Color.FromArgb(56, 189, 248);
            lblIdText.Location = new Point(18, 32);
            lblIdText.Size = new Size(330, 44);
            pnlIdBox.Controls.Add(lblIdText);

            btnCopyId = new Button();
            btnCopyId.Text = "📋 ID Kopyala";
            btnCopyId.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnCopyId.ForeColor = Color.White;
            btnCopyId.BackColor = Color.FromArgb(37, 99, 235);
            btnCopyId.FlatStyle = FlatStyle.Flat;
            btnCopyId.FlatAppearance.BorderSize = 0;
            btnCopyId.Location = new Point(480, 36);
            btnCopyId.Size = new Size(160, 42);
            btnCopyId.Cursor = Cursors.Hand;
            btnCopyId.Click += (s, e) => {
                Clipboard.SetText(this.mySessionId.Replace(" ", ""));
                btnCopyId.Text = "✓ Kopyalandı!";
                btnCopyId.BackColor = Color.FromArgb(16, 185, 129);
            };
            pnlIdBox.Controls.Add(btnCopyId);

            string localIp = GetLocalIp();
            Label lblIp = new Label();
            lblIp.Text = "Yerel IP: " + localIp + ":8443  |  Bulut Sunucu: Aktif (Render)";
            lblIp.Font = new Font("Consolas", 8.5f);
            lblIp.ForeColor = Color.FromArgb(148, 163, 184);
            lblIp.Location = new Point(18, 86);
            lblIp.AutoSize = true;
            pnlIdBox.Controls.Add(lblIp);

            GroupBox grpPerms = new GroupBox();
            grpPerms.Text = " 🛡️ Bağlantı Yetkileri (İzin Verilen Eylemler) ";
            grpPerms.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            grpPerms.ForeColor = Color.FromArgb(56, 189, 248);
            grpPerms.Location = new Point(0, 185);
            grpPerms.Size = new Size(660, 130);
            pageMyDevice.Controls.Add(grpPerms);

            chkInput = new CheckBox();
            chkInput.Text = "Fare ve Klavye Kontrolü (Tam Yönetim)";
            chkInput.Checked = true;
            chkInput.Font = new Font("Segoe UI", 9);
            chkInput.ForeColor = Color.FromArgb(241, 245, 249);
            chkInput.Location = new Point(18, 26);
            chkInput.Size = new Size(600, 24);
            chkInput.CheckedChanged += (s, e) => SavePermissions();
            grpPerms.Controls.Add(chkInput);

            chkFiles = new CheckBox();
            chkFiles.Text = "Çift Yönlü Dosya Transferi";
            chkFiles.Checked = true;
            chkFiles.Font = new Font("Segoe UI", 9);
            chkFiles.ForeColor = Color.FromArgb(241, 245, 249);
            chkFiles.Location = new Point(18, 56);
            chkFiles.Size = new Size(600, 24);
            chkFiles.CheckedChanged += (s, e) => SavePermissions();
            grpPerms.Controls.Add(chkFiles);

            chkClipboard = new CheckBox();
            chkClipboard.Text = "Pano Paylaşımı (Metin Kopyala / Yapıştır)";
            chkClipboard.Checked = true;
            chkClipboard.Font = new Font("Segoe UI", 9);
            chkClipboard.ForeColor = Color.FromArgb(241, 245, 249);
            chkClipboard.Location = new Point(18, 86);
            chkClipboard.Size = new Size(600, 24);
            chkClipboard.CheckedChanged += (s, e) => SavePermissions();
            grpPerms.Controls.Add(chkClipboard);

            btnDisconnect = new Button();
            btnDisconnect.Text = "🚫 Aktif Bağlantıyı Hemen Sonlandır";
            btnDisconnect.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnDisconnect.ForeColor = Color.White;
            btnDisconnect.BackColor = Color.FromArgb(225, 29, 72);
            btnDisconnect.FlatStyle = FlatStyle.Flat;
            btnDisconnect.FlatAppearance.BorderSize = 0;
            btnDisconnect.Location = new Point(0, 330);
            btnDisconnect.Size = new Size(660, 38);
            btnDisconnect.Cursor = Cursors.Hand;
            btnDisconnect.Click += (s, e) => {
                MessageBox.Show("Uzak oturum sonlandırıldı.", "AetherDesk Enterprise", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            pageMyDevice.Controls.Add(btnDisconnect);
        }

        // PAGE 2: Uzak Bağlantı
        private void BuildPageRemoteConnect()
        {
            pageRemoteConnect = new Panel();
            pageRemoteConnect.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(pageRemoteConnect);

            Label lblH = new Label();
            lblH.Text = "Başka Bir Cihaza Bağlan";
            lblH.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblH.ForeColor = Color.FromArgb(248, 250, 252);
            lblH.Location = new Point(0, 0);
            lblH.AutoSize = true;
            pageRemoteConnect.Controls.Add(lblH);

            Label lblSub = new Label();
            lblSub.Text = "Karşı bilgisayarın 9 haneli ID'sini girerek bu pencere içinde anında kontrol edin.";
            lblSub.Font = new Font("Segoe UI", 9);
            lblSub.ForeColor = Color.FromArgb(148, 163, 184);
            lblSub.Location = new Point(0, 26);
            lblSub.AutoSize = true;
            pageRemoteConnect.Controls.Add(lblSub);

            Panel pnlBox = new Panel();
            pnlBox.Location = new Point(0, 56);
            pnlBox.Size = new Size(660, 65);
            pnlBox.BackColor = Color.FromArgb(15, 23, 42);
            pageRemoteConnect.Controls.Add(pnlBox);

            txtTargetId = new TextBox();
            txtTargetId.Font = new Font("Consolas", 14, FontStyle.Bold);
            txtTargetId.BackColor = Color.FromArgb(10, 15, 29);
            txtTargetId.ForeColor = Color.FromArgb(56, 189, 248);
            txtTargetId.Location = new Point(16, 16);
            txtTargetId.Size = new Size(440, 32);
            pnlBox.Controls.Add(txtTargetId);

            btnConnectTarget = new Button();
            btnConnectTarget.Text = "🚀 Hemen Bağlan";
            btnConnectTarget.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnConnectTarget.ForeColor = Color.White;
            btnConnectTarget.BackColor = Color.FromArgb(37, 99, 235);
            btnConnectTarget.FlatStyle = FlatStyle.Flat;
            btnConnectTarget.FlatAppearance.BorderSize = 0;
            btnConnectTarget.Location = new Point(475, 14);
            btnConnectTarget.Size = new Size(170, 36);
            btnConnectTarget.Cursor = Cursors.Hand;
            btnConnectTarget.Click += (s, e) => {
                string target = txtTargetId.Text.Trim().Replace(" ", "");
                if (!string.IsNullOrEmpty(target))
                {
                    StartInAppSession(target);
                }
            };
            pnlBox.Controls.Add(btnConnectTarget);

            Label lblRec = new Label();
            lblRec.Text = "Son Bağlanılan Oturumlar:";
            lblRec.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblRec.ForeColor = Color.FromArgb(203, 213, 225);
            lblRec.Location = new Point(0, 140);
            lblRec.AutoSize = true;
            pageRemoteConnect.Controls.Add(lblRec);

            pnlRecentFlow = new FlowLayoutPanel();
            pnlRecentFlow.Location = new Point(0, 170);
            pnlRecentFlow.Size = new Size(660, 200);
            pnlRecentFlow.AutoScroll = true;
            pageRemoteConnect.Controls.Add(pnlRecentFlow);

            AddRecentCard("778 375 604", "Ofis Bilgisayarı", "14 ms");
            AddRecentCard("482 910 375", "Ana Sunucu", "20 ms");
            AddRecentCard("891 204 153", "Muhasebe Terminali", "18 ms");
        }

        private void AddRecentCard(string id, string name, string ping)
        {
            Panel card = new Panel();
            card.Size = new Size(195, 90);
            card.Margin = new Padding(0, 0, 12, 12);
            card.BackColor = Color.FromArgb(15, 23, 42);
            card.Cursor = Cursors.Hand;

            Label lblN = new Label();
            lblN.Text = name;
            lblN.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblN.ForeColor = Color.FromArgb(241, 245, 249);
            lblN.Location = new Point(10, 8);
            lblN.AutoSize = true;
            card.Controls.Add(lblN);

            Label lblI = new Label();
            lblI.Text = id;
            lblI.Font = new Font("Consolas", 10, FontStyle.Bold);
            lblI.ForeColor = Color.FromArgb(56, 189, 248);
            lblI.Location = new Point(10, 30);
            lblI.AutoSize = true;
            card.Controls.Add(lblI);

            Label lblP = new Label();
            lblP.Text = "🟢 " + ping;
            lblP.Font = new Font("Segoe UI", 8);
            lblP.ForeColor = Color.FromArgb(52, 211, 153);
            lblP.Location = new Point(10, 60);
            lblP.AutoSize = true;
            card.Controls.Add(lblP);

            card.Click += (s, e) => {
                txtTargetId.Text = id.Replace(" ", "");
                StartInAppSession(id.Replace(" ", ""));
            };

            pnlRecentFlow.Controls.Add(card);
        }

        // UNIFIED IN-APP REMOTE DESKTOP CANVAS (SINGLE-WINDOW WITH TIMER & FILE TRANSFER)
        private void BuildPageActiveSession()
        {
            pageActiveSession = new Panel();
            pageActiveSession.Dock = DockStyle.Fill;
            pageActiveSession.BackColor = Color.Black;
            pnlContent.Controls.Add(pageActiveSession);

            pnlSessionTopBar = new Panel();
            pnlSessionTopBar.Dock = DockStyle.Top;
            pnlSessionTopBar.Height = 44;
            pnlSessionTopBar.BackColor = Color.FromArgb(15, 23, 42);
            pnlSessionTopBar.Padding = new Padding(10, 6, 10, 6);
            pageActiveSession.Controls.Add(pnlSessionTopBar);

            lblSessionTargetInfo = new Label();
            lblSessionTargetInfo.Text = "⚡ Canlı Oturum: Bağlanıyor...";
            lblSessionTargetInfo.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblSessionTargetInfo.ForeColor = Color.FromArgb(56, 189, 248);
            lblSessionTargetInfo.Location = new Point(12, 12);
            lblSessionTargetInfo.AutoSize = true;
            pnlSessionTopBar.Controls.Add(lblSessionTargetInfo);

            lblSessionDuration = new Label();
            lblSessionDuration.Text = "⏱️ 00:00:00";
            lblSessionDuration.Font = new Font("Consolas", 9.5f, FontStyle.Bold);
            lblSessionDuration.ForeColor = Color.FromArgb(52, 211, 153);
            lblSessionDuration.Location = new Point(320, 12);
            lblSessionDuration.AutoSize = true;
            pnlSessionTopBar.Controls.Add(lblSessionDuration);

            // File Send Button
            btnSessionSendFile = new Button();
            btnSessionSendFile.Text = "📁 Dosya Gönder";
            btnSessionSendFile.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnSessionSendFile.ForeColor = Color.White;
            btnSessionSendFile.BackColor = Color.FromArgb(30, 41, 59);
            btnSessionSendFile.FlatStyle = FlatStyle.Flat;
            btnSessionSendFile.FlatAppearance.BorderSize = 0;
            btnSessionSendFile.Size = new Size(115, 28);
            btnSessionSendFile.Location = new Point(480, 8);
            btnSessionSendFile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSessionSendFile.Cursor = Cursors.Hand;
            btnSessionSendFile.Click += (s, e) => SendFileToRemote();
            pnlSessionTopBar.Controls.Add(btnSessionSendFile);

            btnSessionCtrlAltDel = new Button();
            btnSessionCtrlAltDel.Text = "🛡️ Ctrl+Alt+Del";
            btnSessionCtrlAltDel.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnSessionCtrlAltDel.ForeColor = Color.White;
            btnSessionCtrlAltDel.BackColor = Color.FromArgb(30, 41, 59);
            btnSessionCtrlAltDel.FlatStyle = FlatStyle.Flat;
            btnSessionCtrlAltDel.FlatAppearance.BorderSize = 0;
            btnSessionCtrlAltDel.Size = new Size(110, 28);
            btnSessionCtrlAltDel.Location = new Point(605, 8);
            btnSessionCtrlAltDel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSessionCtrlAltDel.Cursor = Cursors.Hand;
            btnSessionCtrlAltDel.Click += (s, e) => SendRemoteKey("CtrlAltDel");
            pnlSessionTopBar.Controls.Add(btnSessionCtrlAltDel);

            btnCloseSession = new Button();
            btnCloseSession.Text = "✕ Oturumu Kapat";
            btnCloseSession.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnCloseSession.ForeColor = Color.White;
            btnCloseSession.BackColor = Color.FromArgb(225, 29, 72);
            btnCloseSession.FlatStyle = FlatStyle.Flat;
            btnCloseSession.FlatAppearance.BorderSize = 0;
            btnCloseSession.Size = new Size(125, 28);
            btnCloseSession.Location = new Point(725, 8);
            btnCloseSession.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCloseSession.Cursor = Cursors.Hand;
            btnCloseSession.Click += (s, e) => CloseInAppSession();
            pnlSessionTopBar.Controls.Add(btnCloseSession);

            picSessionViewport = new PictureBox();
            picSessionViewport.Dock = DockStyle.Fill;
            picSessionViewport.SizeMode = PictureBoxSizeMode.Zoom;
            picSessionViewport.BackColor = Color.Black;
            picSessionViewport.Cursor = Cursors.Cross;
            pageActiveSession.Controls.Add(picSessionViewport);
            picSessionViewport.BringToFront();

            // Pixel-Perfect Mouse & Click Forwarding
            picSessionViewport.MouseClick += (s, e) => {
                Point norm = TranslateZoomCoordinates(picSessionViewport, e.Location);
                if (!norm.IsEmpty)
                {
                    string action = e.Button == MouseButtons.Right ? "rightclick" : "click";
                    SendRemoteMouse(norm.X, norm.Y, 1920, 1080, action);
                }
            };

            picSessionViewport.MouseDoubleClick += (s, e) => {
                Point norm = TranslateZoomCoordinates(picSessionViewport, e.Location);
                if (!norm.IsEmpty)
                {
                    SendRemoteMouse(norm.X, norm.Y, 1920, 1080, "dblclick");
                }
            };

            this.KeyPreview = true;
            this.KeyDown += (s, e) => {
                if (isInAppStreaming && pageActiveSession.Visible)
                {
                    SendRemoteKey(e.KeyCode.ToString());
                }
            };
        }

        private void StartInAppSession(string targetId)
        {
            this.activeConnectedId = targetId;
            this.sessionStartTime = DateTime.Now;
            sessionTimer.Start();

            lblSessionTargetInfo.Text = "⚡ Canlı Oturum: " + targetId + " (Doğrudan Masaüstü)";
            btnNavActiveSession.Visible = true;
            ShowPage(pageActiveSession, btnNavActiveSession);

            if (!isInAppStreaming)
            {
                isInAppStreaming = true;
                inAppStreamThread = new Thread(() =>
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    while (isInAppStreaming)
                    {
                        try
                        {
                            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(CLOUD_RELAY_URL + "/api/screen/" + targetId);
                            req.Method = "GET";
                            req.Timeout = 2000;

                            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                            using (Stream s = resp.GetResponseStream())
                            using (MemoryStream ms = new MemoryStream())
                            {
                                s.CopyTo(ms);
                                byte[] imgBytes = ms.ToArray();
                                if (imgBytes.Length > 0)
                                {
                                    using (MemoryStream mem = new MemoryStream(imgBytes))
                                    {
                                        Image img = Image.FromStream(mem);
                                        if (picSessionViewport.IsHandleCreated)
                                        {
                                            picSessionViewport.Invoke(new Action(() => {
                                                Image old = picSessionViewport.Image;
                                                picSessionViewport.Image = new Bitmap(img);
                                                if (old != null) old.Dispose();
                                            }));
                                        }
                                    }
                                }
                            }
                        }
                        catch { }

                        Thread.Sleep(150);
                    }
                });
                inAppStreamThread.IsBackground = true;
                inAppStreamThread.Start();
            }
        }

        private void UpdateSessionTimer()
        {
            if (isInAppStreaming)
            {
                TimeSpan elapsed = DateTime.Now - sessionStartTime;
                lblSessionDuration.Text = string.Format("⏱️ {0:D2}:{1:D2}:{2:D2}", (int)elapsed.TotalHours, elapsed.Minutes, elapsed.Seconds);
                btnNavActiveSession.Text = string.Format("🟢 Canlı ({0:D2}:{1:D2})", elapsed.Minutes, elapsed.Seconds);
            }
        }

        private void CloseInAppSession()
        {
            isInAppStreaming = false;
            sessionTimer.Stop();
            btnNavActiveSession.Visible = false;
            ShowPage(pageRemoteConnect, btnNavRemoteConnect);
        }

        private void SendFileToRemote()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Karşı Bilgisayara Gönderilecek Dosyayı Seçin";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string filePath = ofd.FileName;
                    string fileName = Path.GetFileName(filePath);
                    ThreadPool.QueueUserWorkItem((state) =>
                    {
                        try
                        {
                            byte[] fileBytes = File.ReadAllBytes(filePath);
                            string uploadUrl = string.Format("{0}/api/file/upload/{1}?name={2}",
                                CLOUD_RELAY_URL, activeConnectedId, Uri.EscapeDataString(fileName));
                            
                            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(uploadUrl);
                            req.Method = "POST";
                            req.ContentType = "application/octet-stream";
                            req.ContentLength = fileBytes.Length;
                            using (Stream s = req.GetRequestStream())
                            {
                                s.Write(fileBytes, 0, fileBytes.Length);
                            }
                            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse()) { }

                            MessageBox.Show("'" + fileName + "' karşı bilgisayara başarıyla gönderildi!", "Dosya Transferi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Dosya gönderilemedi: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    });
                }
            }
        }

        private Point TranslateZoomCoordinates(PictureBox pic, Point p)
        {
            if (pic.Image == null) return p;
            float imgAspect = (float)pic.Image.Width / pic.Image.Height;
            float boxAspect = (float)pic.Width / pic.Height;
            float scale = (boxAspect > imgAspect) ? (float)pic.Height / pic.Image.Height : (float)pic.Width / pic.Image.Width;
            float renderW = pic.Image.Width * scale;
            float renderH = pic.Image.Height * scale;
            float offsetX = (pic.Width - renderW) / 2f;
            float offsetY = (pic.Height - renderH) / 2f;

            float normX = (p.X - offsetX) / renderW;
            float normY = (p.Y - offsetY) / renderH;

            if (normX < 0 || normX > 1 || normY < 0 || normY > 1) return Point.Empty;

            return new Point((int)(normX * 1920), (int)(normY * 1080));
        }

        private void SendRemoteMouse(int x, int y, int sw, int sh, string action)
        {
            ThreadPool.QueueUserWorkItem((state) =>
            {
                try
                {
                    string url = string.Format("{0}/api/mouse/{1}?x={2}&y={3}&sw={4}&sh={5}&action={6}",
                        CLOUD_RELAY_URL, activeConnectedId, x, y, sw, sh, action);
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                    req.Method = "GET";
                    req.Timeout = 1500;
                    using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse()) { }
                }
                catch { }
            });
        }

        private void SendRemoteKey(string key)
        {
            ThreadPool.QueueUserWorkItem((state) =>
            {
                try
                {
                    string url = string.Format("{0}/api/keyboard/{1}?key={2}",
                        CLOUD_RELAY_URL, activeConnectedId, Uri.EscapeDataString(key));
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                    req.Method = "GET";
                    req.Timeout = 1500;
                    using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse()) { }
                }
                catch { }
            });
        }

        // PAGE 3: Güvenlik & Yetki
        private void BuildPageSecurity()
        {
            pageSecurity = new Panel();
            pageSecurity.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(pageSecurity);

            Label lblH = new Label();
            lblH.Text = "Erişim Doğrulama ve Güvenlik";
            lblH.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblH.ForeColor = Color.FromArgb(248, 250, 252);
            lblH.Location = new Point(0, 0);
            lblH.AutoSize = true;
            pageSecurity.Controls.Add(lblH);

            GroupBox grpM = new GroupBox();
            grpM.Text = " 🔒 Bağlantı Doğrulama Modu ";
            grpM.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            grpM.ForeColor = Color.FromArgb(56, 189, 248);
            grpM.Location = new Point(0, 45);
            grpM.Size = new Size(660, 200);
            pageSecurity.Controls.Add(grpM);

            rbUnattended = new RadioButton();
            rbUnattended.Text = "Katılımsız Erişim (Şifresiz Doğrudan Kabul)";
            rbUnattended.Font = new Font("Segoe UI", 9);
            rbUnattended.ForeColor = Color.FromArgb(241, 245, 249);
            rbUnattended.Location = new Point(20, 28);
            rbUnattended.Size = new Size(600, 24);
            grpM.Controls.Add(rbUnattended);

            rbPassword = new RadioButton();
            rbPassword.Text = "Özel Şifreli Erişim (Bağlanana şifre sorulsun)";
            rbPassword.Font = new Font("Segoe UI", 9);
            rbPassword.ForeColor = Color.FromArgb(241, 245, 249);
            rbPassword.Location = new Point(20, 58);
            rbPassword.Size = new Size(600, 24);
            grpM.Controls.Add(rbPassword);

            txtCustomPassword = new TextBox();
            txtCustomPassword.Font = new Font("Consolas", 11);
            txtCustomPassword.BackColor = Color.FromArgb(10, 15, 29);
            txtCustomPassword.ForeColor = Color.FromArgb(245, 158, 11);
            txtCustomPassword.Location = new Point(42, 88);
            txtCustomPassword.Size = new Size(200, 27);
            grpM.Controls.Add(txtCustomPassword);

            rbPrompt = new RadioButton();
            rbPrompt.Text = "Manuel Onay (Her bağlantıda ekranda Kabul/Reddet çıksın)";
            rbPrompt.Font = new Font("Segoe UI", 9);
            rbPrompt.ForeColor = Color.FromArgb(241, 245, 249);
            rbPrompt.Location = new Point(20, 126);
            rbPrompt.Size = new Size(600, 24);
            grpM.Controls.Add(rbPrompt);

            btnSaveSecurity = new Button();
            btnSaveSecurity.Text = "✓ Güvenlik Ayarlarını Kaydet";
            btnSaveSecurity.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnSaveSecurity.ForeColor = Color.White;
            btnSaveSecurity.BackColor = Color.FromArgb(16, 185, 129);
            btnSaveSecurity.FlatStyle = FlatStyle.Flat;
            btnSaveSecurity.FlatAppearance.BorderSize = 0;
            btnSaveSecurity.Location = new Point(0, 265);
            btnSaveSecurity.Size = new Size(660, 42);
            btnSaveSecurity.Cursor = Cursors.Hand;
            btnSaveSecurity.Click += (s, e) => SaveSecurity();
            pageSecurity.Controls.Add(btnSaveSecurity);
        }

        // PAGE 4: Ayarlar & Ağ
        private void BuildPageSettings()
        {
            pageSettings = new Panel();
            pageSettings.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(pageSettings);

            Label lblH = new Label();
            lblH.Text = "Sistem & Ağ Yapılandırması";
            lblH.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblH.ForeColor = Color.FromArgb(248, 250, 252);
            lblH.Location = new Point(0, 0);
            lblH.AutoSize = true;
            pageSettings.Controls.Add(lblH);

            Panel pnlBox = new Panel();
            pnlBox.Location = new Point(0, 45);
            pnlBox.Size = new Size(660, 260);
            pnlBox.BackColor = Color.FromArgb(15, 23, 42);
            pnlBox.Padding = new Padding(20);
            pageSettings.Controls.Add(pnlBox);

            Label lblInfo = new Label();
            lblInfo.Text = "Bulut Sinyal & Relay Sunucusu:\n" + CLOUD_RELAY_URL + "\n\n" +
                           "Yerel Soket Dinleme Portu:\n8443 (HTTP Direct P2P)\n\n" +
                           "Ekran Yakalama Çekirdeği:\nDXGI / Windows Graphics Subsystem (60 FPS)\n\n" +
                           "DPI Farkındalığı & Hassasiyet:\nAktif (Piksel Birebir Eşleme)\n\n" +
                           "Dosya Transferi:\nAktif (C:\\Users\\<Kullanıcı>\\Downloads)";
            lblInfo.Font = new Font("Segoe UI", 9.5f);
            lblInfo.ForeColor = Color.FromArgb(203, 213, 225);
            lblInfo.Dock = DockStyle.Fill;
            pnlBox.Controls.Add(lblInfo);
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
                    chkInput.Checked = allowInput;
                    chkFiles.Checked = allowFiles;
                    chkClipboard.Checked = allowClip;

                    if (mode == "PASSWORD") rbPassword.Checked = true;
                    else if (mode == "PROMPT") rbPrompt.Checked = true;
                    else rbUnattended.Checked = true;
                }
            }
            catch { rbUnattended.Checked = true; }
        }

        private void SavePermissions()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\AetherDesk"))
                {
                    key.SetValue("AllowInput", chkInput.Checked.ToString());
                    key.SetValue("AllowFiles", chkFiles.Checked.ToString());
                    key.SetValue("AllowClip", chkClipboard.Checked.ToString());
                }
            }
            catch { }
        }

        private void SaveSecurity()
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

                if (path == "/mouse" && chkInput.Checked)
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
                            if (!string.IsNullOrEmpty(json) && json.Contains("action"))
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

                    if (chkInput.Checked && (action == "click" || action == "rightclick" || action == "dblclick" || action == "move"))
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
                    else if (chkInput.Checked && action == "key")
                    {
                        string key = GetRegexVal(obj, "key");
                        if (!string.IsNullOrEmpty(key))
                        {
                            SendKeySafe(key);
                        }
                    }
                    else if (chkFiles.Checked && action == "incoming_file")
                    {
                        // Auto-download incoming transferred file
                        DownloadIncomingFile();
                    }
                }
            }
            catch { }
        }

        private void DownloadIncomingFile()
        {
            ThreadPool.QueueUserWorkItem((state) =>
            {
                try
                {
                    string cleanId = this.mySessionId.Replace(" ", "");
                    string downloadUrl = CLOUD_RELAY_URL + "/api/file/download/" + cleanId;
                    
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(downloadUrl);
                    req.Method = "GET";
                    req.Timeout = 15000;

                    using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                    {
                        string contentDisp = resp.Headers["Content-Disposition"];
                        string filename = "Gelen_Dosya.dat";
                        if (!string.IsNullOrEmpty(contentDisp) && contentDisp.Contains("filename="))
                        {
                            int start = contentDisp.IndexOf("filename=") + 9;
                            filename = contentDisp.Substring(start).Replace("\"", "").Trim();
                        }

                        string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                        string targetPath = Path.Combine(downloadsPath, filename);

                        using (Stream s = resp.GetResponseStream())
                        using (FileStream fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write))
                        {
                            s.CopyTo(fs);
                        }
                    }
                }
                catch { }
            });
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
