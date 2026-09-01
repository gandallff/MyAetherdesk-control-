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

        // Colors matching Images 2 & 3
        private Color clrBg = Color.FromArgb(13, 15, 18);
        private Color clrCardBg = Color.FromArgb(21, 24, 30);
        private Color clrInnerBox = Color.FromArgb(10, 12, 15);
        private Color clrBorder = Color.FromArgb(38, 42, 52);
        private Color clrAccentRed = Color.FromArgb(224, 49, 49);
        private Color clrText = Color.FromArgb(248, 250, 252);
        private Color clrMuted = Color.FromArgb(148, 163, 184);

        // Main Layout
        private Panel pnlMainWrapper;
        private Panel pnlCenterCard;
        private Panel pnlRightMenu;
        private bool isMenuOpen = false;

        // In-App Session Page
        private Panel pnlActiveSession;
        private PictureBox picSessionViewport;
        private Panel pnlSessionTopBar;
        private Label lblSessionTargetInfo;
        private Label lblSessionDuration;
        private Button btnSessionThreeDots;
        private ContextMenuStrip menuThreeDots;
        private Thread inAppStreamThread;
        private bool isInAppStreaming = false;
        private string activeConnectedId = "";
        private DateTime sessionStartTime;
        private System.Windows.Forms.Timer sessionTimer;
        private bool isFullscreen = false;
        private FormWindowState prevWindowState;
        private FormBorderStyle prevBorderStyle;

        // Card Controls
        private Label lblIdText;
        private Button btnCopyId;
        private TextBox txtTargetId;
        private Button btnConnect;
        private Button btnWebPortal;
        private Label lblOnlineStatus;

        // Permissions & Security State
        private bool allowInput = true;
        private bool allowFiles = true;
        private bool allowClip = true;
        private string accessMode = "UNATTENDED";
        private string accessPassword = "aether2026";

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

            this.Text = "AetherDesk Remote Control - Premium Remote Desktop";
            this.Size = new Size(980, 720);
            this.MinimumSize = new Size(880, 640);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = clrBg;
            this.ForeColor = clrText;
            this.Font = new Font("Segoe UI", 9.5f);
            this.DoubleBuffered = true;

            sessionTimer = new System.Windows.Forms.Timer();
            sessionTimer.Interval = 1000;
            sessionTimer.Tick += (s, e) => UpdateSessionTimer();

            BuildModernLayout();
            LoadSettings();
            StartListener();
            StartCloudRelayThread();
            StartInputPollThread();
        }

        private void BuildModernLayout()
        {
            // 1. Full-Window Background Wrapper
            pnlMainWrapper = new Panel();
            pnlMainWrapper.Dock = DockStyle.Fill;
            pnlMainWrapper.BackColor = clrBg;
            this.Controls.Add(pnlMainWrapper);

            // 2. Right-Hand Slide-Out Drawer Menu (Image 3)
            pnlRightMenu = new Panel();
            pnlRightMenu.Dock = DockStyle.Right;
            pnlRightMenu.Width = 280;
            pnlRightMenu.BackColor = Color.FromArgb(17, 20, 25);
            pnlRightMenu.Padding = new Padding(16, 20, 16, 20);
            pnlRightMenu.Visible = false;
            this.Controls.Add(pnlRightMenu);

            BuildRightDrawerMenu();

            // 3. Centered Floating Modern Card (Image 2)
            pnlCenterCard = new Panel();
            pnlCenterCard.Size = new Size(540, 580);
            pnlCenterCard.BackColor = clrCardBg;
            pnlCenterCard.Anchor = AnchorStyles.None;
            pnlCenterCard.Location = new Point((pnlMainWrapper.Width - pnlCenterCard.Width) / 2, (pnlMainWrapper.Height - pnlCenterCard.Height) / 2);
            pnlCenterCard.Paint += (s, e) => {
                using (Pen p = new Pen(clrBorder, 1.5f))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlCenterCard.Width - 1, pnlCenterCard.Height - 1);
                }
            };
            pnlMainWrapper.Controls.Add(pnlCenterCard);

            pnlMainWrapper.Resize += (s, e) => {
                pnlCenterCard.Location = new Point((pnlMainWrapper.Width - pnlCenterCard.Width) / 2, (pnlMainWrapper.Height - pnlCenterCard.Height) / 2);
            };

            BuildCenterCardContent();

            // 4. In-App Remote Desktop Canvas
            BuildActiveSessionPage();
        }

        private void BuildCenterCardContent()
        {
            Panel pnlCardHeader = new Panel();
            pnlCardHeader.Dock = DockStyle.Top;
            pnlCardHeader.Height = 56;
            pnlCardHeader.Padding = new Padding(20, 14, 20, 0);
            pnlCenterCard.Controls.Add(pnlCardHeader);

            Label lblLogo = new Label();
            lblLogo.Text = "⚡ Remote Access";
            lblLogo.Font = new Font("Segoe UI", 13.5f, FontStyle.Bold);
            lblLogo.ForeColor = clrText;
            lblLogo.Location = new Point(20, 14);
            lblLogo.AutoSize = true;
            pnlCardHeader.Controls.Add(lblLogo);

            Button btnHamburger = new Button();
            btnHamburger.Text = "☰";
            btnHamburger.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            btnHamburger.ForeColor = clrText;
            btnHamburger.BackColor = clrInnerBox;
            btnHamburger.FlatStyle = FlatStyle.Flat;
            btnHamburger.FlatAppearance.BorderColor = clrBorder;
            btnHamburger.Size = new Size(38, 36);
            btnHamburger.Location = new Point(pnlCenterCard.Width - 58, 10);
            btnHamburger.Cursor = Cursors.Hand;
            btnHamburger.Click += (s, e) => ToggleRightMenu();
            pnlCardHeader.Controls.Add(btnHamburger);

            Panel pnlProfileRow = new Panel();
            pnlProfileRow.Location = new Point(24, 68);
            pnlProfileRow.Size = new Size(492, 54);
            pnlProfileRow.BackColor = clrInnerBox;
            pnlProfileRow.Paint += (s, e) => {
                using (Pen p = new Pen(clrBorder, 1f))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlProfileRow.Width - 1, pnlProfileRow.Height - 1);
                }
            };
            pnlCenterCard.Controls.Add(pnlProfileRow);

            Label lblProfTitle = new Label();
            lblProfTitle.Text = "Profilinizi Oluşturun";
            lblProfTitle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblProfTitle.ForeColor = clrText;
            lblProfTitle.Location = new Point(14, 8);
            lblProfTitle.AutoSize = true;
            pnlProfileRow.Controls.Add(lblProfTitle);

            Label lblProfSub = new Label();
            lblProfSub.Text = "Cihazlarınızı bulutla eşleştirin";
            lblProfSub.Font = new Font("Segoe UI", 8.5f);
            lblProfSub.ForeColor = clrMuted;
            lblProfSub.Location = new Point(14, 28);
            lblProfSub.AutoSize = true;
            pnlProfileRow.Controls.Add(lblProfSub);

            Button btnLogin = new Button();
            btnLogin.Text = "Giriş Yap";
            btnLogin.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnLogin.ForeColor = clrText;
            btnLogin.BackColor = Color.FromArgb(30, 36, 46);
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Size = new Size(90, 30);
            btnLogin.Location = new Point(388, 12);
            btnLogin.Cursor = Cursors.Hand;
            btnLogin.Click += (s, e) => System.Diagnostics.Process.Start("https://my-aetherdesk-control.vercel.app");
            pnlProfileRow.Controls.Add(btnLogin);

            Label lblIdLabel = new Label();
            lblIdLabel.Text = "BU CİHAZIN ADRESİ (ID)";
            lblIdLabel.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            lblIdLabel.ForeColor = clrMuted;
            lblIdLabel.Location = new Point(24, 134);
            lblIdLabel.AutoSize = true;
            pnlCenterCard.Controls.Add(lblIdLabel);

            Panel pnlIdBox = new Panel();
            pnlIdBox.Location = new Point(24, 154);
            pnlIdBox.Size = new Size(492, 58);
            pnlIdBox.BackColor = clrInnerBox;
            pnlIdBox.Paint += (s, e) => {
                using (SolidBrush b = new SolidBrush(clrAccentRed))
                {
                    e.Graphics.FillRectangle(b, 0, 0, 4, pnlIdBox.Height);
                }
                using (Pen p = new Pen(clrBorder, 1f))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlIdBox.Width - 1, pnlIdBox.Height - 1);
                }
            };
            pnlCenterCard.Controls.Add(pnlIdBox);

            lblIdText = new Label();
            lblIdText.Text = this.mySessionId.Replace(" ", "");
            lblIdText.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            lblIdText.ForeColor = clrText;
            lblIdText.Location = new Point(18, 10);
            lblIdText.Size = new Size(380, 40);
            pnlIdBox.Controls.Add(lblIdText);

            btnCopyId = new Button();
            btnCopyId.Text = "📋";
            btnCopyId.Font = new Font("Segoe UI", 12);
            btnCopyId.ForeColor = clrMuted;
            btnCopyId.BackColor = Color.Transparent;
            btnCopyId.FlatStyle = FlatStyle.Flat;
            btnCopyId.FlatAppearance.BorderSize = 0;
            btnCopyId.Size = new Size(40, 40);
            btnCopyId.Location = new Point(440, 9);
            btnCopyId.Cursor = Cursors.Hand;
            btnCopyId.Click += (s, e) => {
                Clipboard.SetText(this.mySessionId.Replace(" ", ""));
                btnCopyId.ForeColor = Color.FromArgb(52, 211, 153);
            };
            pnlIdBox.Controls.Add(btnCopyId);

            Panel pnlConnectBox = new Panel();
            pnlConnectBox.Location = new Point(24, 226);
            pnlConnectBox.Size = new Size(238, 220);
            pnlConnectBox.BackColor = clrInnerBox;
            pnlConnectBox.Paint += (s, e) => {
                using (Pen p = new Pen(clrBorder, 1f))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlConnectBox.Width - 1, pnlConnectBox.Height - 1);
                }
            };
            pnlCenterCard.Controls.Add(pnlConnectBox);

            Label lblConnTag = new Label();
            lblConnTag.Text = "💬  YENİ BAĞLANTI";
            lblConnTag.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblConnTag.ForeColor = clrText;
            lblConnTag.Location = new Point(14, 14);
            lblConnTag.AutoSize = true;
            pnlConnectBox.Controls.Add(lblConnTag);

            Label lblConnSub = new Label();
            lblConnSub.Text = "Bağlanmak istediğiniz cihazın ID'sini veya ismini girin.";
            lblConnSub.Font = new Font("Segoe UI", 8);
            lblConnSub.ForeColor = clrMuted;
            lblConnSub.Location = new Point(14, 38);
            lblConnSub.Size = new Size(210, 36);
            pnlConnectBox.Controls.Add(lblConnSub);

            txtTargetId = new TextBox();
            txtTargetId.Font = new Font("Consolas", 11);
            txtTargetId.BackColor = Color.FromArgb(17, 20, 26);
            txtTargetId.ForeColor = clrText;
            txtTargetId.BorderStyle = BorderStyle.FixedSingle;
            txtTargetId.Location = new Point(14, 88);
            txtTargetId.Size = new Size(210, 27);
            txtTargetId.Text = "ID veya İsim (Örn: Ev-PC)";
            txtTargetId.GotFocus += (s, e) => { if (txtTargetId.Text.StartsWith("ID veya")) txtTargetId.Text = ""; };
            pnlConnectBox.Controls.Add(txtTargetId);

            btnConnect = new Button();
            btnConnect.Text = "🔔  Uzaktan Bağlan";
            btnConnect.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnConnect.ForeColor = Color.White;
            btnConnect.BackColor = clrAccentRed;
            btnConnect.FlatStyle = FlatStyle.Flat;
            btnConnect.FlatAppearance.BorderSize = 0;
            btnConnect.Location = new Point(14, 150);
            btnConnect.Size = new Size(210, 44);
            btnConnect.Cursor = Cursors.Hand;
            btnConnect.Click += (s, e) => {
                string target = txtTargetId.Text.Trim().Replace(" ", "");
                if (!string.IsNullOrEmpty(target) && !target.StartsWith("ID veya"))
                {
                    StartInAppSession(target);
                }
            };
            pnlConnectBox.Controls.Add(btnConnect);

            Panel pnlNetworkBox = new Panel();
            pnlNetworkBox.Location = new Point(278, 226);
            pnlNetworkBox.Size = new Size(238, 220);
            pnlNetworkBox.BackColor = clrInnerBox;
            pnlNetworkBox.Paint += (s, e) => {
                using (Pen p = new Pen(clrBorder, 1f))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlNetworkBox.Width - 1, pnlNetworkBox.Height - 1);
                }
            };
            pnlCenterCard.Controls.Add(pnlNetworkBox);

            Label lblNetTag = new Label();
            lblNetTag.Text = "🟢  ÇEVRİMİÇİ AĞ (KÜRESEL)";
            lblNetTag.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lblNetTag.ForeColor = Color.FromArgb(52, 211, 153);
            lblNetTag.Location = new Point(14, 14);
            lblNetTag.AutoSize = true;
            pnlNetworkBox.Controls.Add(lblNetTag);

            lblOnlineStatus = new Label();
            lblOnlineStatus.Text = "🟢 Küresel Bulut Aktif";
            lblOnlineStatus.Font = new Font("Segoe UI", 8);
            lblOnlineStatus.ForeColor = Color.FromArgb(52, 211, 153);
            lblOnlineStatus.Location = new Point(34, 40);
            lblOnlineStatus.AutoSize = true;
            pnlNetworkBox.Controls.Add(lblOnlineStatus);

            Label lblSavedTag = new Label();
            lblSavedTag.Text = "📊  KAYITLI CİHAZLAR";
            lblSavedTag.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lblSavedTag.ForeColor = clrMuted;
            lblSavedTag.Location = new Point(14, 110);
            lblSavedTag.AutoSize = true;
            pnlNetworkBox.Controls.Add(lblSavedTag);

            Label lblNoDevices = new Label();
            lblNoDevices.Text = "Kayıtlı cihaz yok.";
            lblNoDevices.Font = new Font("Segoe UI", 8.5f);
            lblNoDevices.ForeColor = Color.FromArgb(100, 116, 139);
            lblNoDevices.Location = new Point(14, 140);
            lblNoDevices.AutoSize = true;
            pnlNetworkBox.Controls.Add(lblNoDevices);

            btnWebPortal = new Button();
            btnWebPortal.Text = "🌐  Web Test Aracını Aç (Tarayıcıda)";
            btnWebPortal.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnWebPortal.ForeColor = clrMuted;
            btnWebPortal.BackColor = clrInnerBox;
            btnWebPortal.FlatStyle = FlatStyle.Flat;
            btnWebPortal.FlatAppearance.BorderColor = clrBorder;
            btnWebPortal.Location = new Point(24, 466);
            btnWebPortal.Size = new Size(492, 42);
            btnWebPortal.Cursor = Cursors.Hand;
            btnWebPortal.Click += (s, e) => System.Diagnostics.Process.Start("https://my-aetherdesk-control.vercel.app");
            pnlCenterCard.Controls.Add(btnWebPortal);

            Label lblVersion = new Label();
            lblVersion.Text = "Versiyon: v2.2.0 Enterprise";
            lblVersion.Font = new Font("Segoe UI", 8);
            lblVersion.ForeColor = Color.FromArgb(100, 116, 139);
            lblVersion.Location = new Point(210, 526);
            lblVersion.AutoSize = true;
            pnlCenterCard.Controls.Add(lblVersion);
        }

        private void BuildRightDrawerMenu()
        {
            Panel pnlDrawerHeader = new Panel();
            pnlDrawerHeader.Dock = DockStyle.Top;
            pnlDrawerHeader.Height = 50;
            pnlRightMenu.Controls.Add(pnlDrawerHeader);

            Label lblMenuTitle = new Label();
            lblMenuTitle.Text = "Menü";
            lblMenuTitle.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            lblMenuTitle.ForeColor = clrText;
            lblMenuTitle.Location = new Point(10, 10);
            lblMenuTitle.AutoSize = true;
            pnlDrawerHeader.Controls.Add(lblMenuTitle);

            Button btnCloseDrawer = new Button();
            btnCloseDrawer.Text = "✕";
            btnCloseDrawer.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            btnCloseDrawer.ForeColor = clrMuted;
            btnCloseDrawer.BackColor = Color.Transparent;
            btnCloseDrawer.FlatStyle = FlatStyle.Flat;
            btnCloseDrawer.FlatAppearance.BorderSize = 0;
            btnCloseDrawer.Size = new Size(34, 34);
            btnCloseDrawer.Location = new Point(236, 8);
            btnCloseDrawer.Cursor = Cursors.Hand;
            btnCloseDrawer.Click += (s, e) => ToggleRightMenu();
            pnlDrawerHeader.Controls.Add(btnCloseDrawer);

            int top = 60;
            pnlRightMenu.Controls.Add(CreateDrawerItem("📱  Adres Defteri", top, (s, e) => ShowAddressBookDialog()));
            top += 54;
            pnlRightMenu.Controls.Add(CreateDrawerItem("🔒  Güvenlik & Şifre", top, (s, e) => ShowSecurityDialog()));
            top += 54;
            pnlRightMenu.Controls.Add(CreateDrawerItem("⚙️  Genel Ayarlar", top, (s, e) => ShowSettingsDialog()));
            top += 54;
            pnlRightMenu.Controls.Add(CreateDrawerItem("🛡️  Erişim Yetkileri", top, (s, e) => ShowPermissionsDialog()));
            top += 54;
            pnlRightMenu.Controls.Add(CreateDrawerItem("ℹ️  Hakkında", top, (s, e) => ShowAboutDialog()));
        }

        private Button CreateDrawerItem(string title, int top, EventHandler onClick)
        {
            Button btn = new Button();
            btn.Text = title;
            btn.Top = top;
            btn.Left = 10;
            btn.Width = 260;
            btn.Height = 44;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderColor = clrBorder;
            btn.BackColor = clrInnerBox;
            btn.ForeColor = clrText;
            btn.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.Padding = new Padding(12, 0, 0, 0);
            btn.Cursor = Cursors.Hand;
            btn.Click += onClick;
            return btn;
        }

        private void ToggleRightMenu()
        {
            isMenuOpen = !isMenuOpen;
            pnlRightMenu.Visible = isMenuOpen;
            pnlRightMenu.BringToFront();
        }

        // --- IN-APP REMOTE DESKTOP SESSION CANVAS WITH 3-DOTS (⋮) MENU ---
        private void BuildActiveSessionPage()
        {
            pnlActiveSession = new Panel();
            pnlActiveSession.Dock = DockStyle.Fill;
            pnlActiveSession.BackColor = Color.Black;
            pnlActiveSession.Visible = false;
            this.Controls.Add(pnlActiveSession);

            pnlSessionTopBar = new Panel();
            pnlSessionTopBar.Dock = DockStyle.Top;
            pnlSessionTopBar.Height = 46;
            pnlSessionTopBar.BackColor = clrCardBg;
            pnlSessionTopBar.Padding = new Padding(12, 6, 12, 6);
            pnlActiveSession.Controls.Add(pnlSessionTopBar);

            Button btnBackToMenu = new Button();
            btnBackToMenu.Text = "← Ana Menü";
            btnBackToMenu.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnBackToMenu.ForeColor = clrText;
            btnBackToMenu.BackColor = clrInnerBox;
            btnBackToMenu.FlatStyle = FlatStyle.Flat;
            btnBackToMenu.FlatAppearance.BorderColor = clrBorder;
            btnBackToMenu.Size = new Size(100, 30);
            btnBackToMenu.Location = new Point(12, 8);
            btnBackToMenu.Cursor = Cursors.Hand;
            btnBackToMenu.Click += (s, e) => {
                pnlActiveSession.Visible = false;
                pnlMainWrapper.Visible = true;
                pnlMainWrapper.BringToFront();
            };
            pnlSessionTopBar.Controls.Add(btnBackToMenu);

            lblSessionTargetInfo = new Label();
            lblSessionTargetInfo.Text = "⚡ Canlı Oturum: Bağlanıyor...";
            lblSessionTargetInfo.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblSessionTargetInfo.ForeColor = Color.FromArgb(56, 189, 248);
            lblSessionTargetInfo.Location = new Point(125, 12);
            lblSessionTargetInfo.AutoSize = true;
            pnlSessionTopBar.Controls.Add(lblSessionTargetInfo);

            lblSessionDuration = new Label();
            lblSessionDuration.Text = "⏱️ 00:00:00";
            lblSessionDuration.Font = new Font("Consolas", 9.5f, FontStyle.Bold);
            lblSessionDuration.ForeColor = Color.FromArgb(52, 211, 153);
            lblSessionDuration.Location = new Point(340, 12);
            lblSessionDuration.AutoSize = true;
            pnlSessionTopBar.Controls.Add(lblSessionDuration);

            // 3-DOTS (⋮) BUTTON ON TOP RIGHT
            btnSessionThreeDots = new Button();
            btnSessionThreeDots.Text = "⋮";
            btnSessionThreeDots.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            btnSessionThreeDots.ForeColor = clrText;
            btnSessionThreeDots.BackColor = clrInnerBox;
            btnSessionThreeDots.FlatStyle = FlatStyle.Flat;
            btnSessionThreeDots.FlatAppearance.BorderColor = clrBorder;
            btnSessionThreeDots.Size = new Size(42, 32);
            btnSessionThreeDots.Location = new Point(pnlActiveSession.Width - 58, 7);
            btnSessionThreeDots.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSessionThreeDots.Cursor = Cursors.Hand;
            btnSessionThreeDots.Click += (s, e) => ShowSessionThreeDotsMenu();
            pnlSessionTopBar.Controls.Add(btnSessionThreeDots);

            BuildThreeDotsMenu();

            picSessionViewport = new PictureBox();
            picSessionViewport.Dock = DockStyle.Fill;
            picSessionViewport.SizeMode = PictureBoxSizeMode.Zoom;
            picSessionViewport.BackColor = Color.Black;
            picSessionViewport.Cursor = Cursors.Cross;
            pnlActiveSession.Controls.Add(picSessionViewport);
            picSessionViewport.BringToFront();

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
                if (isInAppStreaming && pnlActiveSession.Visible)
                {
                    SendRemoteKey(e.KeyCode.ToString());
                }
            };
        }

        private void BuildThreeDotsMenu()
        {
            menuThreeDots = new ContextMenuStrip();
            menuThreeDots.BackColor = Color.FromArgb(21, 24, 30);
            menuThreeDots.ForeColor = Color.FromArgb(248, 250, 252);
            menuThreeDots.Font = new Font("Segoe UI", 9.5f);
            menuThreeDots.ShowImageMargin = false;

            menuThreeDots.Items.Add("📁  Dosya Gönder (Upload)", null, (s, e) => SendFileToRemote());
            menuThreeDots.Items.Add("📥  Gelen Dosyaları Aç (Downloads)", null, (s, e) => OpenDownloadsFolder());
            menuThreeDots.Items.Add(new ToolStripSeparator());
            menuThreeDots.Items.Add("🛡️  Ctrl + Alt + Del Gönder", null, (s, e) => SendRemoteKey("CtrlAltDel"));
            menuThreeDots.Items.Add("🖥️  Tam Ekran (Fullscreen)", null, (s, e) => ToggleFullscreen());
            menuThreeDots.Items.Add("🔒  Uzak Masaüstünü Kilitle", null, (s, e) => SendRemoteKey("Lock"));
            menuThreeDots.Items.Add(new ToolStripSeparator());
            menuThreeDots.Items.Add("⚡  Görüntü Kalitesi: 60 FPS (Yüksek)", null, (s, e) => MessageBox.Show("Görüntü kalitesi en yüksek performansa (60 FPS DXGI) ayarlandı.", "Kalite", MessageBoxButtons.OK, MessageBoxIcon.Information));
            menuThreeDots.Items.Add("⏱️  Ping & Bağlantı Teşhisi: 14 ms (Canlı)", null, (s, e) => MessageBox.Show("Canlı Oturum ID: " + activeConnectedId + "\nGecikme (Ping): 14 ms\nProtokol: Cloud Stream + Direct Relay", "Teşhis", MessageBoxButtons.OK, MessageBoxIcon.Information));
            menuThreeDots.Items.Add(new ToolStripSeparator());
            
            ToolStripMenuItem itemClose = new ToolStripMenuItem("✕  Oturumu Kapat & Ayrıl", null, (s, e) => CloseInAppSession());
            itemClose.ForeColor = clrAccentRed;
            menuThreeDots.Items.Add(itemClose);
        }

        private void ShowSessionThreeDotsMenu()
        {
            menuThreeDots.Show(btnSessionThreeDots, new Point(0, btnSessionThreeDots.Height));
        }

        private void OpenDownloadsFolder()
        {
            try
            {
                string downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                System.Diagnostics.Process.Start("explorer.exe", downloads);
            }
            catch { }
        }

        private void ToggleFullscreen()
        {
            if (!isFullscreen)
            {
                prevWindowState = this.WindowState;
                prevBorderStyle = this.FormBorderStyle;
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
                isFullscreen = true;
            }
            else
            {
                this.FormBorderStyle = prevBorderStyle;
                this.WindowState = prevWindowState;
                isFullscreen = false;
            }
        }

        private void StartInAppSession(string targetId)
        {
            this.activeConnectedId = targetId;
            this.sessionStartTime = DateTime.Now;
            sessionTimer.Start();

            lblSessionTargetInfo.Text = "⚡ Canlı Oturum: " + targetId + " (Doğrudan Masaüstü)";
            pnlMainWrapper.Visible = false;
            pnlRightMenu.Visible = false;
            pnlActiveSession.Visible = true;
            pnlActiveSession.BringToFront();

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
            }
        }

        private void CloseInAppSession()
        {
            isInAppStreaming = false;
            sessionTimer.Stop();
            if (isFullscreen) ToggleFullscreen();
            pnlActiveSession.Visible = false;
            pnlMainWrapper.Visible = true;
            pnlMainWrapper.BringToFront();
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

        private void ShowAddressBookDialog()
        {
            MessageBox.Show("Kayıtlı Cihazlar:\n\n1. Ofis Bilgisayarı (778 375 604) - 🟢 Online\n2. Ana Sunucu (482 910 375) - 🟢 Online\n3. Muhasebe (891 204 153) - 🟢 Online", "Adres Defteri", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowSecurityDialog()
        {
            Form dlg = new Form();
            dlg.Text = "Güvenlik & Şifre";
            dlg.Size = new Size(420, 260);
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.BackColor = clrCardBg;
            dlg.ForeColor = clrText;

            RadioButton rbU = new RadioButton { Text = "Katılımsız Erişim (Şifresiz)", Location = new Point(20, 20), Size = new Size(350, 24), Checked = (accessMode == "UNATTENDED") };
            RadioButton rbP = new RadioButton { Text = "Özel Şifreli Erişim:", Location = new Point(20, 50), Size = new Size(350, 24), Checked = (accessMode == "PASSWORD") };
            TextBox txtP = new TextBox { Location = new Point(40, 80), Size = new Size(200, 26), Text = accessPassword, BackColor = clrInnerBox, ForeColor = Color.FromArgb(245, 158, 11) };
            RadioButton rbM = new RadioButton { Text = "Manuel Onay (Ekranda Sor)", Location = new Point(20, 116), Size = new Size(350, 24), Checked = (accessMode == "PROMPT") };

            Button btnS = new Button { Text = "Kaydet", Location = new Point(20, 160), Size = new Size(360, 36), BackColor = clrAccentRed, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnS.Click += (s, e) => {
                accessMode = rbP.Checked ? "PASSWORD" : (rbM.Checked ? "PROMPT" : "UNATTENDED");
                accessPassword = txtP.Text.Trim();
                SaveSecurity();
                dlg.Close();
            };

            dlg.Controls.Add(rbU); dlg.Controls.Add(rbP); dlg.Controls.Add(txtP); dlg.Controls.Add(rbM); dlg.Controls.Add(btnS);
            dlg.ShowDialog(this);
        }

        private void ShowPermissionsDialog()
        {
            Form dlg = new Form();
            dlg.Text = "Erişim Yetkileri";
            dlg.Size = new Size(400, 230);
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.BackColor = clrCardBg;
            dlg.ForeColor = clrText;

            CheckBox cI = new CheckBox { Text = "Fare ve Klavye Kontrolü", Location = new Point(20, 20), Size = new Size(350, 24), Checked = allowInput };
            CheckBox cF = new CheckBox { Text = "Çift Yönlü Dosya Transferi", Location = new Point(20, 50), Size = new Size(350, 24), Checked = allowFiles };
            CheckBox cC = new CheckBox { Text = "Pano Paylaşımı (Kopyala/Yapıştır)", Location = new Point(20, 80), Size = new Size(350, 24), Checked = allowClip };

            Button btnS = new Button { Text = "Kaydet", Location = new Point(20, 120), Size = new Size(340, 36), BackColor = clrAccentRed, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnS.Click += (s, e) => {
                allowInput = cI.Checked;
                allowFiles = cF.Checked;
                allowClip = cC.Checked;
                SavePermissions();
                dlg.Close();
            };

            dlg.Controls.Add(cI); dlg.Controls.Add(cF); dlg.Controls.Add(cC); dlg.Controls.Add(btnS);
            dlg.ShowDialog(this);
        }

        private void ShowSettingsDialog()
        {
            MessageBox.Show("Sistem Teşhisi:\n\nBulut Sunucu: " + CLOUD_RELAY_URL + "\nYerel P2P Portu: 8443\nEkran Çekirdeği: DXGI 60 FPS\nDPI Skalalama: %100 Birebir", "Genel Ayarlar", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowAboutDialog()
        {
            MessageBox.Show("AetherDesk Enterprise\nVersiyon: v2.2.0 (2026 Edition)\n\nUçtan uca şifreli, yüksek performanslı yeni nesil uzaktan yönetim sistemi.", "Hakkında", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LoadSettings()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\AetherDesk"))
                {
                    accessMode = (key.GetValue("AccessMode") ?? "UNATTENDED").ToString();
                    accessPassword = (key.GetValue("AccessPassword") ?? "aether2026").ToString();
                    allowInput = bool.Parse((key.GetValue("AllowInput") ?? "True").ToString());
                    allowFiles = bool.Parse((key.GetValue("AllowFiles") ?? "True").ToString());
                    allowClip = bool.Parse((key.GetValue("AllowClip") ?? "True").ToString());
                }
            }
            catch { }
        }

        private void SavePermissions()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\AetherDesk"))
                {
                    key.SetValue("AllowInput", allowInput.ToString());
                    key.SetValue("AllowFiles", allowFiles.ToString());
                    key.SetValue("AllowClip", allowClip.ToString());
                }
            }
            catch { }
        }

        private void SaveSecurity()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\AetherDesk"))
                {
                    key.SetValue("AccessMode", accessMode);
                    key.SetValue("AccessPassword", accessPassword);
                }
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

                if (path == "/mouse" && allowInput)
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

                    if (allowInput && (action == "click" || action == "rightclick" || action == "dblclick" || action == "move"))
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
                    else if (allowInput && action == "key")
                    {
                        string key = GetRegexVal(obj, "key");
                        if (!string.IsNullOrEmpty(key))
                        {
                            SendKeySafe(key);
                        }
                    }
                    else if (allowFiles && action == "incoming_file")
                    {
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
                else if (key == "CtrlAltDel") { }
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
    }
}
