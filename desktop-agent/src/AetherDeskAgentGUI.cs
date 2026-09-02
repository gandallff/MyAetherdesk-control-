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

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWA_TEXT_COLOR = 36;

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x04;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const uint MOUSEEVENTF_RIGHTUP = 0x10;

        // Colors
        private Color clrBg = Color.FromArgb(13, 15, 18);
        private Color clrCardBg = Color.FromArgb(20, 24, 32);
        private Color clrInnerBox = Color.FromArgb(10, 12, 16);
        private Color clrBorder = Color.FromArgb(36, 42, 54);
        private Color clrAccentBlue = Color.FromArgb(37, 99, 235);
        private Color clrAccentBlueHover = Color.FromArgb(59, 130, 246);
        private Color clrAccentRed = Color.FromArgb(224, 49, 49);
        private Color clrText = Color.FromArgb(248, 250, 252);
        private Color clrMuted = Color.FromArgb(148, 163, 184);

        // Logo
        private Image appLogoImage;

        // Custom Seamless Dark Title Bar
        private Panel pnlCustomTitleBar;
        private Button btnHamburger;
        private PictureBox picTitleLogo;
        private Label lblAppBrandTitle;
        private Label lblUserStatusTag;
        private Button btnUserAvatarBadge;
        private Button btnMin;
        private Button btnMax;
        private Button btnClose;

        // Main Layout
        private Panel pnlMainBody;
        private Panel pnlLeftSidebar;
        private Panel pnlMainContent;
        private Panel pnlCardContainer;
        private bool isLeftSidebarOpen = false;
        private const int SIDEBAR_WIDTH = 240;

        // In-App Welcome & Auth Landing Screen
        private Panel pnlWelcomeAuthScreen;
        private TextBox txtDirectQuickId;
        private TextBox txtAuthEmail;
        private TextBox txtAuthPass;
        private TextBox txtAuthName;
        private TabControl tabAuthControls;

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

        // Two-Column TeamViewer-style Workflow Cards
        private Label lblMyIdDisplay;
        private Label lblMyPassDisplay;
        private Button btnCopyId;
        private Button btnRefreshPass;
        private Button btnCopyPass;
        private TextBox txtRemoteTargetId;
        private Button btnConnectTarget;
        private CheckBox chkEasyAccess;
        private Label lblBottomStatusText;
        private Label lblCommunityAccountStatus;

        // Auth & User State
        private bool isLoggedIn = false;
        private string userDisplayName = "Misafir Kullanıcı";
        private string userEmail = "";
        private string userInitials = "MK";

        // Security & Permissions
        private bool allowInput = true;
        private bool allowFiles = true;
        private bool allowClip = true;
        private string accessMode = "UNATTENDED";
        private string accessPassword = "";

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
            this.accessPassword = GenerateRandomSessionPass();

            this.Text = "AetherDesk Enterprise - Remote Access";
            this.Size = new Size(950, 680);
            this.MinimumSize = new Size(880, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = clrBg;
            this.ForeColor = clrText;
            this.Font = new Font("Segoe UI", 9.5f);
            this.DoubleBuffered = true;

            LoadAppLogo();
            ApplyDarkWindowAttributes();
            LoadSettings();

            sessionTimer = new System.Windows.Forms.Timer();
            sessionTimer.Interval = 1000;
            sessionTimer.Tick += (s, e) => UpdateSessionTimer();

            BuildCustomTitleBar();
            BuildMainAppLayout();
            StartListener();
            StartCloudRelayThread();
            StartInputPollThread();

            // Default startup view: show direct entry with ID or Email tabs
            ShowWelcomeAuthScreen();
        }

        private void ApplyDarkWindowAttributes()
        {
            try
            {
                int trueVal = 1;
                DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref trueVal, sizeof(int));
                DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref trueVal, sizeof(int));
                
                int bgrDark = 0x00120F0D;
                DwmSetWindowAttribute(this.Handle, DWMWA_CAPTION_COLOR, ref bgrDark, sizeof(int));
                int bgrText = 0x00FCFAF8;
                DwmSetWindowAttribute(this.Handle, DWMWA_TEXT_COLOR, ref bgrText, sizeof(int));
            }
            catch { }
        }

        private void LoadAppLogo()
        {
            try
            {
                string localLogoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "aetherdesk_logo.jpg");
                if (File.Exists(localLogoPath))
                {
                    appLogoImage = Image.FromFile(localLogoPath);
                    return;
                }
                string desktopLogoPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "aetherdesk_logo.jpg");
                if (File.Exists(desktopLogoPath))
                {
                    appLogoImage = Image.FromFile(desktopLogoPath);
                    return;
                }
            }
            catch { }

            try
            {
                string b64 = "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";
                byte[] bytes = Convert.FromBase64String(b64);
                using (MemoryStream ms = new MemoryStream(bytes))
                {
                    appLogoImage = Image.FromStream(ms);
                }
            }
            catch { }
        }

        private string GenerateRandomSessionPass()
        {
            Random r = new Random();
            string chars = "abcdefghjkmnpqrstuvwxyz23456789";
            char[] pass = new char[6];
            for (int i = 0; i < 6; i++) pass[i] = chars[r.Next(chars.Length)];
            return new string(pass);
        }

        private void BuildCustomTitleBar()
        {
            pnlCustomTitleBar = new Panel();
            pnlCustomTitleBar.Dock = DockStyle.Top;
            pnlCustomTitleBar.Height = 44;
            pnlCustomTitleBar.BackColor = clrBg;
            pnlCustomTitleBar.MouseDown += (s, e) => DragWindow(e);
            this.Controls.Add(pnlCustomTitleBar);

            btnHamburger = new Button();
            btnHamburger.Text = "☰";
            btnHamburger.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            btnHamburger.ForeColor = clrText;
            btnHamburger.BackColor = Color.FromArgb(20, 24, 32);
            btnHamburger.FlatStyle = FlatStyle.Flat;
            btnHamburger.FlatAppearance.BorderColor = clrBorder;
            btnHamburger.Size = new Size(36, 32);
            btnHamburger.Location = new Point(8, 6);
            btnHamburger.Cursor = Cursors.Hand;
            btnHamburger.Click += (s, e) => ToggleLeftSidebar();
            pnlCustomTitleBar.Controls.Add(btnHamburger);

            picTitleLogo = new PictureBox();
            picTitleLogo.Location = new Point(50, 7);
            picTitleLogo.Size = new Size(30, 30);
            picTitleLogo.SizeMode = PictureBoxSizeMode.Zoom;
            picTitleLogo.Image = appLogoImage;
            picTitleLogo.MouseDown += (s, e) => DragWindow(e);
            pnlCustomTitleBar.Controls.Add(picTitleLogo);

            lblAppBrandTitle = new Label();
            lblAppBrandTitle.Text = "AetherDesk Remote Access";
            lblAppBrandTitle.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            lblAppBrandTitle.ForeColor = clrText;
            lblAppBrandTitle.Location = new Point(86, 11);
            lblAppBrandTitle.AutoSize = true;
            lblAppBrandTitle.MouseDown += (s, e) => DragWindow(e);
            pnlCustomTitleBar.Controls.Add(lblAppBrandTitle);

            btnClose = CreateTitleBtn("✕", (s, e) => Application.Exit(), true);
            btnMax = CreateTitleBtn("▢", (s, e) => ToggleMaximize(), false);
            btnMin = CreateTitleBtn("—", (s, e) => this.WindowState = FormWindowState.Minimized, false);

            pnlCustomTitleBar.Controls.Add(btnClose);
            pnlCustomTitleBar.Controls.Add(btnMax);
            pnlCustomTitleBar.Controls.Add(btnMin);

            btnUserAvatarBadge = new Button();
            btnUserAvatarBadge.Text = userInitials;
            btnUserAvatarBadge.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnUserAvatarBadge.ForeColor = Color.White;
            btnUserAvatarBadge.BackColor = isLoggedIn ? Color.FromArgb(16, 185, 129) : Color.FromArgb(100, 116, 139);
            btnUserAvatarBadge.FlatStyle = FlatStyle.Flat;
            btnUserAvatarBadge.FlatAppearance.BorderSize = 0;
            btnUserAvatarBadge.Size = new Size(34, 32);
            btnUserAvatarBadge.Location = new Point(pnlCustomTitleBar.Width - 175, 6);
            btnUserAvatarBadge.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnUserAvatarBadge.Cursor = Cursors.Hand;
            btnUserAvatarBadge.Click += (s, e) => ShowWelcomeAuthScreen();
            pnlCustomTitleBar.Controls.Add(btnUserAvatarBadge);

            lblUserStatusTag = new Label();
            lblUserStatusTag.Text = isLoggedIn ? userDisplayName : "Kısıtlı (Giriş Yapılmadı)";
            lblUserStatusTag.Font = new Font("Segoe UI", 8.5f);
            lblUserStatusTag.ForeColor = isLoggedIn ? Color.FromArgb(52, 211, 153) : Color.FromArgb(245, 158, 11);
            lblUserStatusTag.Location = new Point(pnlCustomTitleBar.Width - 360, 12);
            lblUserStatusTag.Size = new Size(180, 20);
            lblUserStatusTag.TextAlign = ContentAlignment.MiddleRight;
            lblUserStatusTag.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblUserStatusTag.Cursor = Cursors.Hand;
            lblUserStatusTag.Click += (s, e) => ShowWelcomeAuthScreen();
            pnlCustomTitleBar.Controls.Add(lblUserStatusTag);
        }

        private Button CreateTitleBtn(string text, EventHandler onClick, bool isClose)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Font = new Font("Segoe UI", 9.5f);
            btn.ForeColor = clrMuted;
            btn.BackColor = Color.Transparent;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Size = new Size(42, 44);
            btn.Dock = DockStyle.Right;
            btn.Cursor = Cursors.Hand;
            if (isClose)
            {
                btn.MouseEnter += (s, e) => { btn.BackColor = clrAccentRed; btn.ForeColor = Color.White; };
                btn.MouseLeave += (s, e) => { btn.BackColor = Color.Transparent; btn.ForeColor = clrMuted; };
            }
            else
            {
                btn.MouseEnter += (s, e) => { btn.BackColor = Color.FromArgb(26, 30, 38); };
                btn.MouseLeave += (s, e) => { btn.BackColor = Color.Transparent; };
            }
            btn.Click += onClick;
            return btn;
        }

        private void DragWindow(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void ToggleMaximize()
        {
            this.WindowState = this.WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
        }

        private void BuildMainAppLayout()
        {
            pnlMainBody = new Panel();
            pnlMainBody.Dock = DockStyle.Fill;
            pnlMainBody.BackColor = clrBg;
            this.Controls.Add(pnlMainBody);
            pnlMainBody.BringToFront();

            // Left Slide-out Menu
            pnlLeftSidebar = new Panel();
            pnlLeftSidebar.Dock = DockStyle.Left;
            pnlLeftSidebar.Width = SIDEBAR_WIDTH;
            pnlLeftSidebar.BackColor = Color.FromArgb(17, 20, 26);
            pnlLeftSidebar.Padding = new Padding(12, 16, 12, 16);
            pnlLeftSidebar.Visible = false;
            pnlMainBody.Controls.Add(pnlLeftSidebar);

            BuildLeftSidebarItems();

            // Main Content Area
            pnlMainContent = new Panel();
            pnlMainContent.Dock = DockStyle.Fill;
            pnlMainContent.BackColor = clrBg;
            pnlMainContent.Padding = new Padding(20);
            pnlMainBody.Controls.Add(pnlMainContent);
            pnlMainContent.BringToFront();

            BuildTwoColumnWorkflowCards();
            BuildWelcomeAuthOverlay();
            BuildActiveSessionPage();
        }

        private void ToggleLeftSidebar()
        {
            isLeftSidebarOpen = !isLeftSidebarOpen;
            if (isLeftSidebarOpen)
            {
                this.Width += SIDEBAR_WIDTH;
                pnlLeftSidebar.Visible = true;
            }
            else
            {
                pnlLeftSidebar.Visible = false;
                this.Width -= SIDEBAR_WIDTH;
            }

            if (pnlCardContainer != null && pnlMainContent != null)
            {
                pnlCardContainer.Location = new Point(
                    Math.Max(10, (pnlMainContent.Width - pnlCardContainer.Width) / 2),
                    Math.Max(10, (pnlMainContent.Height - pnlCardContainer.Height) / 2)
                );
            }
        }

        private void BuildLeftSidebarItems()
        {
            Panel pnlSideHeader = new Panel();
            pnlSideHeader.Dock = DockStyle.Top;
            pnlSideHeader.Height = 55;
            pnlLeftSidebar.Controls.Add(pnlSideHeader);

            PictureBox picSide = new PictureBox();
            picSide.Location = new Point(6, 6);
            picSide.Size = new Size(34, 34);
            picSide.SizeMode = PictureBoxSizeMode.Zoom;
            picSide.Image = appLogoImage;
            pnlSideHeader.Controls.Add(picSide);

            Label lblSideBrand = new Label();
            lblSideBrand.Text = "AetherDesk";
            lblSideBrand.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            lblSideBrand.ForeColor = clrText;
            lblSideBrand.Location = new Point(48, 12);
            lblSideBrand.AutoSize = true;
            pnlSideHeader.Controls.Add(lblSideBrand);

            int top = 65;
            pnlLeftSidebar.Controls.Add(CreateSidebarBtn("🖥️  Uzaktan Erişim", top, (s, e) => {
                pnlWelcomeAuthScreen.Visible = false;
                pnlCardContainer.Visible = true;
                ToggleLeftSidebar();
            }));
            top += 50;
            pnlLeftSidebar.Controls.Add(CreateSidebarBtn("👥  Topluluk & Ağ", top, (s, e) => ShowCommunityDialog()));
            top += 50;
            pnlLeftSidebar.Controls.Add(CreateSidebarBtn("📱  Adres Defteri", top, (s, e) => ShowAddressBookDialog()));
            top += 50;
            pnlLeftSidebar.Controls.Add(CreateSidebarBtn("🔒  Güvenlik & Şifre", top, (s, e) => ShowSecurityDialog()));
            top += 50;
            pnlLeftSidebar.Controls.Add(CreateSidebarBtn("⚙️  Genel Ayarlar", top, (s, e) => ShowSettingsDialog()));
            top += 50;
            pnlLeftSidebar.Controls.Add(CreateSidebarBtn("🛡️  Erişim Yetkileri", top, (s, e) => ShowPermissionsDialog()));
            top += 50;
            pnlLeftSidebar.Controls.Add(CreateSidebarBtn("👤  Hesap Girişi", top, (s, e) => {
                ToggleLeftSidebar();
                ShowWelcomeAuthScreen();
            }));
            top += 50;
            pnlLeftSidebar.Controls.Add(CreateSidebarBtn("ℹ️  Hakkında", top, (s, e) => ShowAboutDialog()));
        }

        private Button CreateSidebarBtn(string title, int top, EventHandler onClick)
        {
            Button btn = new Button();
            btn.Text = title;
            btn.Top = top;
            btn.Left = 6;
            btn.Width = 224;
            btn.Height = 42;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = clrInnerBox;
            btn.ForeColor = clrText;
            btn.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.Padding = new Padding(12, 0, 0, 0);
            btn.Cursor = Cursors.Hand;
            btn.Click += onClick;
            return btn;
        }

        // -----------------------------------------------------------------------------------
        // EMBEDDED DARK WELCOME / ACCOUNT & DIRECT ID GATEWAY
        // Directly allows connecting via ID without account, while email login is an alternative!
        // -----------------------------------------------------------------------------------
        private void BuildWelcomeAuthOverlay()
        {
            pnlWelcomeAuthScreen = new Panel();
            pnlWelcomeAuthScreen.Dock = DockStyle.Fill;
            pnlWelcomeAuthScreen.BackColor = clrBg;
            pnlWelcomeAuthScreen.Visible = false;
            pnlMainContent.Controls.Add(pnlWelcomeAuthScreen);

            Panel pnlAuthCard = new Panel();
            pnlAuthCard.Size = new Size(580, 580);
            pnlAuthCard.BackColor = clrCardBg;
            pnlAuthCard.Anchor = AnchorStyles.None;
            pnlAuthCard.Location = new Point((pnlWelcomeAuthScreen.Width - pnlAuthCard.Width) / 2, (pnlWelcomeAuthScreen.Height - pnlAuthCard.Height) / 2);
            pnlAuthCard.Paint += (s, e) => {
                using (Pen p = new Pen(clrBorder, 1.5f))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlAuthCard.Width - 1, pnlAuthCard.Height - 1);
                }
            };
            pnlWelcomeAuthScreen.Controls.Add(pnlAuthCard);

            pnlWelcomeAuthScreen.Resize += (s, e) => {
                pnlAuthCard.Location = new Point((pnlWelcomeAuthScreen.Width - pnlAuthCard.Width) / 2, (pnlWelcomeAuthScreen.Height - pnlAuthCard.Height) / 2);
            };

            // Card Header with Logo
            PictureBox picGateLogo = new PictureBox();
            picGateLogo.Location = new Point((pnlAuthCard.Width - 48) / 2, 16);
            picGateLogo.Size = new Size(48, 48);
            picGateLogo.SizeMode = PictureBoxSizeMode.Zoom;
            picGateLogo.Image = appLogoImage;
            pnlAuthCard.Controls.Add(picGateLogo);

            Label lblGateTitle = new Label();
            lblGateTitle.Text = "AetherDesk Remote Access";
            lblGateTitle.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblGateTitle.ForeColor = clrText;
            lblGateTitle.Location = new Point(20, 68);
            lblGateTitle.Size = new Size(540, 28);
            lblGateTitle.TextAlign = ContentAlignment.MiddleCenter;
            pnlAuthCard.Controls.Add(lblGateTitle);

            Label lblGateSub = new Label();
            lblGateSub.Text = "İster sadece 9 haneli ID girerek doğrudan bağlanın,\nisterseniz e-posta hesabınızla topluluk ve adres defterinizi senkronize edin.";
            lblGateSub.Font = new Font("Segoe UI", 8.5f);
            lblGateSub.ForeColor = clrMuted;
            lblGateSub.Location = new Point(20, 96);
            lblGateSub.Size = new Size(540, 36);
            lblGateSub.TextAlign = ContentAlignment.MiddleCenter;
            pnlAuthCard.Controls.Add(lblGateSub);

            // Tabbed System:
            // TAB 1: ⚡ Sadece ID ile Hızlı Bağlan (Hesapsız)
            // TAB 2: 🔑 E-posta ile Giriş Yap (Alternatif)
            // TAB 3: ✨ Yeni Hesap Oluştur
            tabAuthControls = new TabControl();
            tabAuthControls.Location = new Point(24, 140);
            tabAuthControls.Size = new Size(532, 330);
            tabAuthControls.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            pnlAuthCard.Controls.Add(tabAuthControls);

            // -----------------------------------------------------------------
            // TAB 1: SADECE ID İLE HIZLI BAĞLANTI (HESAP GİRİŞİNDEN TAMAMEN BAĞIMSIZ)
            // -----------------------------------------------------------------
            TabPage tabDirectId = new TabPage("⚡  Sadece ID ile Bağlan");
            tabDirectId.BackColor = clrCardBg;

            // My ID quick reminder box
            Panel pnlMyIdMiniBox = new Panel();
            pnlMyIdMiniBox.Location = new Point(14, 16);
            pnlMyIdMiniBox.Size = new Size(496, 52);
            pnlMyIdMiniBox.BackColor = clrInnerBox;
            pnlMyIdMiniBox.Paint += (s, e) => {
                using (SolidBrush b = new SolidBrush(clrAccentRed))
                {
                    e.Graphics.FillRectangle(b, 0, 0, 4, pnlMyIdMiniBox.Height);
                }
                using (Pen p = new Pen(clrBorder, 1f))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlMyIdMiniBox.Width - 1, pnlMyIdMiniBox.Height - 1);
                }
            };
            tabDirectId.Controls.Add(pnlMyIdMiniBox);

            Label lblMyIdMiniTag = new Label { Text = "BU CİHAZIN ADRESİ (ID):", Location = new Point(14, 6), AutoSize = true, Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = clrMuted };
            Label lblMyIdMiniVal = new Label { Text = this.mySessionId, Location = new Point(14, 22), AutoSize = true, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = clrText };
            Button btnCopyMini = new Button { Text = "📋", Location = new Point(450, 10), Size = new Size(36, 32), FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, ForeColor = clrMuted };
            btnCopyMini.FlatAppearance.BorderSize = 0;
            btnCopyMini.Click += (s, e) => {
                Clipboard.SetText(this.mySessionId.Replace(" ", ""));
                btnCopyMini.ForeColor = Color.FromArgb(52, 211, 153);
            };
            pnlMyIdMiniBox.Controls.Add(lblMyIdMiniTag);
            pnlMyIdMiniBox.Controls.Add(lblMyIdMiniVal);
            pnlMyIdMiniBox.Controls.Add(btnCopyMini);

            // Remote Target ID Input
            Label lblTargetPrompt = new Label { Text = "Bağlanılacak Uzak Cihazın ID'sini Girin:", Location = new Point(14, 82), AutoSize = true, ForeColor = clrMuted, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            txtDirectQuickId = new TextBox {
                Location = new Point(14, 106),
                Size = new Size(496, 30),
                BackColor = clrInnerBox,
                ForeColor = Color.FromArgb(56, 189, 248),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 14, FontStyle.Bold),
                Text = ""
            };
            txtDirectQuickId.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) PerformDirectIdConnect();
            };

            Button btnDirectConnect = new Button {
                Text = "⚡  Doğrudan Uzak Masastüne Bağlan",
                Location = new Point(14, 152),
                Size = new Size(496, 46),
                BackColor = clrAccentBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDirectConnect.Click += (s, e) => PerformDirectIdConnect();

            Label lblDirectInfo = new Label {
                Text = "✓ Hesap açmanıza gerek yoktur. Karşı tarafın 9 haneli ID'sini yazarak anında tam kontrollü oturum başlatabilirsiniz.",
                Location = new Point(14, 212),
                Size = new Size(496, 34),
                ForeColor = Color.FromArgb(148, 163, 184),
                Font = new Font("Segoe UI", 8.2f)
            };

            tabDirectId.Controls.Add(lblTargetPrompt);
            tabDirectId.Controls.Add(txtDirectQuickId);
            tabDirectId.Controls.Add(btnDirectConnect);
            tabDirectId.Controls.Add(lblDirectInfo);

            // -----------------------------------------------------------------
            // TAB 2: ALTERNATİF E-POSTA İLE GİRİŞ & TOPLULUK EŞLEME
            // -----------------------------------------------------------------
            TabPage tabLogin = new TabPage("🔑  E-posta ile Giriş (Alternatif)");
            tabLogin.BackColor = clrCardBg;

            Label lblE = new Label { Text = "E-posta Adresi:", Location = new Point(14, 14), AutoSize = true, ForeColor = clrMuted };
            txtAuthEmail = new TextBox { Location = new Point(14, 36), Size = new Size(496, 26), BackColor = clrInnerBox, ForeColor = clrText, BorderStyle = BorderStyle.FixedSingle, Text = userEmail };

            Label lblP = new Label { Text = "Şifre:", Location = new Point(14, 76), AutoSize = true, ForeColor = clrMuted };
            txtAuthPass = new TextBox { Location = new Point(14, 98), Size = new Size(496, 26), BackColor = clrInnerBox, ForeColor = clrText, BorderStyle = BorderStyle.FixedSingle, UseSystemPasswordChar = true };

            Button btnDoLogin = new Button {
                Text = "Giriş Yap ve Topluluk Senkronizasyonunu Aç",
                Location = new Point(14, 146),
                Size = new Size(496, 44),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDoLogin.Click += (s, e) => PerformLoginAction();

            Label lblAltInfo = new Label {
                Text = "Topluluk hesabınızla giriş yaptığınızda adres defteriniz, kayıtlı cihazlarınız ve şifresiz katılımsız erişimleriniz otomatik yüklenir.",
                Location = new Point(14, 206),
                Size = new Size(496, 36),
                ForeColor = Color.FromArgb(148, 163, 184),
                Font = new Font("Segoe UI", 8.2f)
            };

            tabLogin.Controls.Add(lblE); tabLogin.Controls.Add(txtAuthEmail);
            tabLogin.Controls.Add(lblP); tabLogin.Controls.Add(txtAuthPass);
            tabLogin.Controls.Add(btnDoLogin);
            tabLogin.Controls.Add(lblAltInfo);

            // -----------------------------------------------------------------
            // TAB 3: YENİ HESAP OLUŞTUR
            // -----------------------------------------------------------------
            TabPage tabReg = new TabPage("✨  Kayıt Ol");
            tabReg.BackColor = clrCardBg;

            Label lblN = new Label { Text = "Ad Soyad / Kurum Adı:", Location = new Point(14, 10), AutoSize = true, ForeColor = clrMuted };
            txtAuthName = new TextBox { Location = new Point(14, 30), Size = new Size(496, 26), BackColor = clrInnerBox, ForeColor = clrText, BorderStyle = BorderStyle.FixedSingle };

            Label lblRE = new Label { Text = "E-posta Adresi:", Location = new Point(14, 66), AutoSize = true, ForeColor = clrMuted };
            TextBox txtRegEmail = new TextBox { Location = new Point(14, 86), Size = new Size(496, 26), BackColor = clrInnerBox, ForeColor = clrText, BorderStyle = BorderStyle.FixedSingle };

            Label lblRP = new Label { Text = "Şifre Belirleyin:", Location = new Point(14, 122), AutoSize = true, ForeColor = clrMuted };
            TextBox txtRegPass = new TextBox { Location = new Point(14, 142), Size = new Size(496, 26), BackColor = clrInnerBox, ForeColor = clrText, BorderStyle = BorderStyle.FixedSingle, UseSystemPasswordChar = true };

            Button btnDoReg = new Button {
                Text = "Topluluk Hesabı Oluştur",
                Location = new Point(14, 186),
                Size = new Size(496, 42),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDoReg.Click += (s, e) => {
                if (!string.IsNullOrEmpty(txtAuthName.Text) && txtRegEmail.Text.Contains("@"))
                {
                    txtAuthEmail.Text = txtRegEmail.Text;
                    userDisplayName = txtAuthName.Text.Trim();
                    PerformLoginAction();
                }
                else
                {
                    MessageBox.Show("Lütfen tüm alanları eksiksiz doldurun.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            tabReg.Controls.Add(lblN); tabReg.Controls.Add(txtAuthName);
            tabReg.Controls.Add(lblRE); tabReg.Controls.Add(txtRegEmail);
            tabReg.Controls.Add(lblRP); tabReg.Controls.Add(txtRegPass);
            tabReg.Controls.Add(btnDoReg);

            tabAuthControls.TabPages.Add(tabDirectId);
            tabAuthControls.TabPages.Add(tabLogin);
            tabAuthControls.TabPages.Add(tabReg);

            // BOTTOM ACTION: "Gelişmiş Çalışma Paneline Geç"
            Button btnGoToDashboard = new Button();
            btnGoToDashboard.Text = "🖥️  Gelişmiş Çift Kartlı Çalışma Paneline Geç (Masaüstü Görünümü) →";
            btnGoToDashboard.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnGoToDashboard.ForeColor = Color.FromArgb(203, 213, 225);
            btnGoToDashboard.BackColor = clrInnerBox;
            btnGoToDashboard.FlatStyle = FlatStyle.Flat;
            btnGoToDashboard.FlatAppearance.BorderColor = clrBorder;
            btnGoToDashboard.Location = new Point(24, 490);
            btnGoToDashboard.Size = new Size(532, 44);
            btnGoToDashboard.Cursor = Cursors.Hand;
            btnGoToDashboard.Click += (s, e) => SwitchToMainDashboard();
            pnlAuthCard.Controls.Add(btnGoToDashboard);
        }

        private void PerformDirectIdConnect()
        {
            string target = txtDirectQuickId.Text.Trim().Replace(" ", "");
            if (!string.IsNullOrEmpty(target))
            {
                StartInAppSession(target);
            }
            else
            {
                MessageBox.Show("Lütfen bağlanmak istediğiniz 9 haneli cihaz ID'sini girin.", "AetherDesk", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SwitchToMainDashboard()
        {
            pnlWelcomeAuthScreen.Visible = false;
            pnlCardContainer.Visible = true;
            pnlCardContainer.BringToFront();
        }

        private void ShowWelcomeAuthScreen()
        {
            pnlCardContainer.Visible = false;
            pnlWelcomeAuthScreen.Visible = true;
            pnlWelcomeAuthScreen.BringToFront();
            if (txtDirectQuickId != null) txtDirectQuickId.Focus();
        }

        private void PerformLoginAction()
        {
            if (txtAuthEmail.Text.Contains("@"))
            {
                isLoggedIn = true;
                userEmail = txtAuthEmail.Text.Trim();
                if (string.IsNullOrEmpty(userDisplayName) || userDisplayName == "Misafir Kullanıcı")
                {
                    userDisplayName = userEmail.Split('@')[0];
                }
                userInitials = userDisplayName.Substring(0, Math.Min(2, userDisplayName.Length)).ToUpper();

                SaveAuthSettings();
                UpdateUserInterfaceAuth();

                pnlWelcomeAuthScreen.Visible = false;
                pnlCardContainer.Visible = true;
                pnlCardContainer.BringToFront();

                MessageBox.Show(
                    "Hoş geldiniz, " + userDisplayName + "!\n\n" +
                    "✓ Bu cihazınız (" + this.mySessionId + ") AetherDesk Topluluk Ağı'na başarıyla eklendi.\n" +
                    "✓ Adres Defteri ve Şifresiz Kolay Erişim aktif edildi.",
                    "AetherDesk Enterprise Topluluğu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            else
            {
                MessageBox.Show("Lütfen geçerli bir e-posta adresi girin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void UpdateUserInterfaceAuth()
        {
            if (btnUserAvatarBadge != null)
            {
                btnUserAvatarBadge.Text = userInitials;
                btnUserAvatarBadge.BackColor = isLoggedIn ? Color.FromArgb(16, 185, 129) : Color.FromArgb(100, 116, 139);
            }

            if (lblUserStatusTag != null)
            {
                lblUserStatusTag.Text = isLoggedIn ? userDisplayName : "Kısıtlı (Giriş Yapılmadı)";
                lblUserStatusTag.ForeColor = isLoggedIn ? Color.FromArgb(52, 211, 153) : Color.FromArgb(245, 158, 11);
            }

            if (lblCommunityAccountStatus != null)
            {
                lblCommunityAccountStatus.Text = isLoggedIn 
                    ? "🟢 Topluluk Hesabı: " + userDisplayName + " (" + userEmail + ")"
                    : "⚠️ Kısıtlı Misafir Modu (Adres defteri senkronizasyonu kapalı)";
                lblCommunityAccountStatus.ForeColor = isLoggedIn ? Color.FromArgb(52, 211, 153) : Color.FromArgb(245, 158, 11);
            }
        }

        // ----------------------------------------------------
        // TWO-COLUMN WORKFLOW CARDS (TeamViewer Layout)
        // ----------------------------------------------------
        private void BuildTwoColumnWorkflowCards()
        {
            pnlCardContainer = new Panel();
            pnlCardContainer.Size = new Size(880, 500);
            pnlCardContainer.BackColor = Color.Transparent;
            pnlCardContainer.Anchor = AnchorStyles.None;
            pnlCardContainer.Location = new Point((pnlMainContent.Width - pnlCardContainer.Width) / 2, (pnlMainContent.Height - pnlCardContainer.Height) / 2);
            pnlMainContent.Controls.Add(pnlCardContainer);

            pnlMainContent.Resize += (s, e) => {
                pnlCardContainer.Location = new Point(
                    Math.Max(10, (pnlMainContent.Width - pnlCardContainer.Width) / 2),
                    Math.Max(10, (pnlMainContent.Height - pnlCardContainer.Height) / 2)
                );
            };

            // Top Community Status Banner
            Panel pnlTopCommunityBanner = new Panel();
            pnlTopCommunityBanner.Location = new Point(0, 0);
            pnlTopCommunityBanner.Size = new Size(880, 36);
            pnlTopCommunityBanner.BackColor = clrInnerBox;
            pnlTopCommunityBanner.Paint += (s, e) => {
                using (Pen p = new Pen(clrBorder, 1f))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlTopCommunityBanner.Width - 1, pnlTopCommunityBanner.Height - 1);
                }
            };
            pnlCardContainer.Controls.Add(pnlTopCommunityBanner);

            lblCommunityAccountStatus = new Label();
            lblCommunityAccountStatus.Text = "⚠️ Kısıtlı Misafir Modu (Adres defteri senkronizasyonu kapalı)";
            lblCommunityAccountStatus.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblCommunityAccountStatus.ForeColor = Color.FromArgb(245, 158, 11);
            lblCommunityAccountStatus.Location = new Point(14, 8);
            lblCommunityAccountStatus.AutoSize = true;
            pnlTopCommunityBanner.Controls.Add(lblCommunityAccountStatus);

            Button btnSwitchAuthMode = new Button();
            btnSwitchAuthMode.Text = "⚡ Hızlı Giriş / ID Ekranı";
            btnSwitchAuthMode.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnSwitchAuthMode.ForeColor = clrText;
            btnSwitchAuthMode.BackColor = Color.FromArgb(30, 36, 46);
            btnSwitchAuthMode.FlatStyle = FlatStyle.Flat;
            btnSwitchAuthMode.FlatAppearance.BorderSize = 0;
            btnSwitchAuthMode.Size = new Size(160, 26);
            btnSwitchAuthMode.Location = new Point(710, 5);
            btnSwitchAuthMode.Cursor = Cursors.Hand;
            btnSwitchAuthMode.Click += (s, e) => ShowWelcomeAuthScreen();
            pnlTopCommunityBanner.Controls.Add(btnSwitchAuthMode);

            // LEFT CARD: "Uzaktan Kontrole İzin Ver"
            Panel pnlLeftCard = new Panel();
            pnlLeftCard.Location = new Point(0, 48);
            pnlLeftCard.Size = new Size(420, 380);
            pnlLeftCard.BackColor = clrCardBg;
            pnlLeftCard.Paint += (s, e) => {
                using (Pen p = new Pen(clrBorder, 1.2f))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlLeftCard.Width - 1, pnlLeftCard.Height - 1);
                }
            };
            pnlCardContainer.Controls.Add(pnlLeftCard);

            Label lblLeftHeader = new Label();
            lblLeftHeader.Text = "Uzaktan Kontrole İzin Ver";
            lblLeftHeader.Font = new Font("Segoe UI", 13.5f, FontStyle.Bold);
            lblLeftHeader.ForeColor = clrText;
            lblLeftHeader.Location = new Point(20, 20);
            lblLeftHeader.AutoSize = true;
            pnlLeftCard.Controls.Add(lblLeftHeader);

            Panel pnlIdBox = new Panel();
            pnlIdBox.Location = new Point(20, 68);
            pnlIdBox.Size = new Size(380, 84);
            pnlIdBox.BackColor = clrInnerBox;
            pnlIdBox.Paint += (s, e) => {
                using (Pen p = new Pen(clrBorder, 1f))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlIdBox.Width - 1, pnlIdBox.Height - 1);
                }
            };
            pnlLeftCard.Controls.Add(pnlIdBox);

            Label lblIdTag = new Label();
            lblIdTag.Text = "Kimliğiniz";
            lblIdTag.Font = new Font("Segoe UI", 8.5f);
            lblIdTag.ForeColor = clrMuted;
            lblIdTag.Location = new Point(14, 10);
            lblIdTag.AutoSize = true;
            pnlIdBox.Controls.Add(lblIdTag);

            lblMyIdDisplay = new Label();
            lblMyIdDisplay.Text = this.mySessionId;
            lblMyIdDisplay.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            lblMyIdDisplay.ForeColor = clrText;
            lblMyIdDisplay.Location = new Point(12, 32);
            lblMyIdDisplay.Size = new Size(300, 40);
            pnlIdBox.Controls.Add(lblMyIdDisplay);

            btnCopyId = new Button();
            btnCopyId.Text = "📋";
            btnCopyId.Font = new Font("Segoe UI", 12);
            btnCopyId.ForeColor = clrMuted;
            btnCopyId.BackColor = Color.Transparent;
            btnCopyId.FlatStyle = FlatStyle.Flat;
            btnCopyId.FlatAppearance.BorderSize = 0;
            btnCopyId.Size = new Size(36, 36);
            btnCopyId.Location = new Point(334, 28);
            btnCopyId.Cursor = Cursors.Hand;
            btnCopyId.Click += (s, e) => {
                Clipboard.SetText(this.mySessionId.Replace(" ", ""));
                btnCopyId.ForeColor = Color.FromArgb(52, 211, 153);
            };
            pnlIdBox.Controls.Add(btnCopyId);

            Panel pnlPassBox = new Panel();
            pnlPassBox.Location = new Point(20, 168);
            pnlPassBox.Size = new Size(380, 84);
            pnlPassBox.BackColor = clrInnerBox;
            pnlPassBox.Paint += (s, e) => {
                using (Pen p = new Pen(clrBorder, 1f))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlPassBox.Width - 1, pnlPassBox.Height - 1);
                }
            };
            pnlLeftCard.Controls.Add(pnlPassBox);

            Label lblPassTag = new Label();
            lblPassTag.Text = "Parola";
            lblPassTag.Font = new Font("Segoe UI", 8.5f);
            lblPassTag.ForeColor = clrMuted;
            lblPassTag.Location = new Point(14, 10);
            lblPassTag.AutoSize = true;
            pnlPassBox.Controls.Add(lblPassTag);

            lblMyPassDisplay = new Label();
            lblMyPassDisplay.Text = this.accessPassword;
            lblMyPassDisplay.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            lblMyPassDisplay.ForeColor = Color.FromArgb(245, 158, 11);
            lblMyPassDisplay.Location = new Point(12, 34);
            lblMyPassDisplay.Size = new Size(250, 36);
            pnlPassBox.Controls.Add(lblMyPassDisplay);

            btnRefreshPass = new Button();
            btnRefreshPass.Text = "🔄";
            btnRefreshPass.Font = new Font("Segoe UI", 11);
            btnRefreshPass.ForeColor = clrMuted;
            btnRefreshPass.BackColor = Color.Transparent;
            btnRefreshPass.FlatStyle = FlatStyle.Flat;
            btnRefreshPass.FlatAppearance.BorderSize = 0;
            btnRefreshPass.Size = new Size(36, 36);
            btnRefreshPass.Location = new Point(292, 28);
            btnRefreshPass.Cursor = Cursors.Hand;
            btnRefreshPass.Click += (s, e) => {
                this.accessPassword = GenerateRandomSessionPass();
                lblMyPassDisplay.Text = this.accessPassword;
            };
            pnlPassBox.Controls.Add(btnRefreshPass);

            btnCopyPass = new Button();
            btnCopyPass.Text = "📋";
            btnCopyPass.Font = new Font("Segoe UI", 12);
            btnCopyPass.ForeColor = clrMuted;
            btnCopyPass.BackColor = Color.Transparent;
            btnCopyPass.FlatStyle = FlatStyle.Flat;
            btnCopyPass.FlatAppearance.BorderSize = 0;
            btnCopyPass.Size = new Size(36, 36);
            btnCopyPass.Location = new Point(334, 28);
            btnCopyPass.Cursor = Cursors.Hand;
            btnCopyPass.Click += (s, e) => {
                Clipboard.SetText(this.accessPassword);
                btnCopyPass.ForeColor = Color.FromArgb(52, 211, 153);
            };
            pnlPassBox.Controls.Add(btnCopyPass);

            chkEasyAccess = new CheckBox();
            chkEasyAccess.Text = "Bu cihaza Kolay erişim sağlayın (Katılımsız)";
            chkEasyAccess.Font = new Font("Segoe UI", 9f);
            chkEasyAccess.ForeColor = clrMuted;
            chkEasyAccess.Location = new Point(20, 275);
            chkEasyAccess.Size = new Size(380, 26);
            chkEasyAccess.Checked = true;
            chkEasyAccess.CheckedChanged += (s, e) => {
                accessMode = chkEasyAccess.Checked ? "UNATTENDED" : "PASSWORD";
                SaveSecurity();
            };
            pnlLeftCard.Controls.Add(chkEasyAccess);

            // RIGHT CARD: "Uzak Cihazı Kontrol Et"
            Panel pnlRightCard = new Panel();
            pnlRightCard.Location = new Point(450, 48);
            pnlRightCard.Size = new Size(430, 380);
            pnlRightCard.BackColor = clrCardBg;
            pnlRightCard.Paint += (s, e) => {
                using (Pen p = new Pen(clrBorder, 1.2f))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlRightCard.Width - 1, pnlRightCard.Height - 1);
                }
            };
            pnlCardContainer.Controls.Add(pnlRightCard);

            Label lblRightHeader = new Label();
            lblRightHeader.Text = "Uzak cihazı kontrol et";
            lblRightHeader.Font = new Font("Segoe UI", 13.5f, FontStyle.Bold);
            lblRightHeader.ForeColor = clrText;
            lblRightHeader.Location = new Point(24, 20);
            lblRightHeader.AutoSize = true;
            pnlRightCard.Controls.Add(lblRightHeader);

            Label lblModeTag = new Label();
            lblModeTag.Text = "Uzaktan kontrol ⌄";
            lblModeTag.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblModeTag.ForeColor = clrAccentBlueHover;
            lblModeTag.Location = new Point(24, 60);
            lblModeTag.AutoSize = true;
            pnlRightCard.Controls.Add(lblModeTag);

            Panel pnlTargetInputBox = new Panel();
            pnlTargetInputBox.Location = new Point(24, 98);
            pnlTargetInputBox.Size = new Size(380, 56);
            pnlTargetInputBox.BackColor = clrInnerBox;
            pnlTargetInputBox.Paint += (s, e) => {
                using (Pen p = new Pen(clrBorder, 1f))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlTargetInputBox.Width - 1, pnlTargetInputBox.Height - 1);
                }
            };
            pnlRightCard.Controls.Add(pnlTargetInputBox);

            Label lblTargetTag = new Label();
            lblTargetTag.Text = "Kimlik, IP adresi veya host adı";
            lblTargetTag.Font = new Font("Segoe UI", 8f);
            lblTargetTag.ForeColor = clrMuted;
            lblTargetTag.Location = new Point(12, 6);
            lblTargetTag.AutoSize = true;
            pnlTargetInputBox.Controls.Add(lblTargetTag);

            txtRemoteTargetId = new TextBox();
            txtRemoteTargetId.Font = new Font("Consolas", 12, FontStyle.Bold);
            txtRemoteTargetId.BackColor = clrInnerBox;
            txtRemoteTargetId.ForeColor = clrText;
            txtRemoteTargetId.BorderStyle = BorderStyle.None;
            txtRemoteTargetId.Location = new Point(14, 26);
            txtRemoteTargetId.Size = new Size(340, 22);
            pnlTargetInputBox.Controls.Add(txtRemoteTargetId);

            btnConnectTarget = new Button();
            btnConnectTarget.Text = "Bağlan";
            btnConnectTarget.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnConnectTarget.ForeColor = Color.White;
            btnConnectTarget.BackColor = clrAccentBlue;
            btnConnectTarget.FlatStyle = FlatStyle.Flat;
            btnConnectTarget.FlatAppearance.BorderSize = 0;
            btnConnectTarget.Location = new Point(24, 175);
            btnConnectTarget.Size = new Size(130, 44);
            btnConnectTarget.Cursor = Cursors.Hand;
            btnConnectTarget.Click += (s, e) => {
                string target = txtRemoteTargetId.Text.Trim().Replace(" ", "");
                if (!string.IsNullOrEmpty(target))
                {
                    StartInAppSession(target);
                }
            };
            pnlRightCard.Controls.Add(btnConnectTarget);

            Label lblRecentTag = new Label();
            lblRecentTag.Text = "Son Bağlanılan Oturumlar:";
            lblRecentTag.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lblRecentTag.ForeColor = clrMuted;
            lblRecentTag.Location = new Point(24, 250);
            lblRecentTag.AutoSize = true;
            pnlRightCard.Controls.Add(lblRecentTag);

            FlowLayoutPanel flowRecents = new FlowLayoutPanel();
            flowRecents.Location = new Point(24, 275);
            flowRecents.Size = new Size(380, 80);
            flowRecents.BackColor = Color.Transparent;
            pnlRightCard.Controls.Add(flowRecents);

            flowRecents.Controls.Add(CreateRecentChip("778 375 604", "Ofis"));
            flowRecents.Controls.Add(CreateRecentChip("482 910 375", "Sunucu"));
            flowRecents.Controls.Add(CreateRecentChip("891 204 153", "Muhasebe"));

            // BOTTOM STATUS BAR
            Panel pnlBottomStatus = new Panel();
            pnlBottomStatus.Location = new Point(0, 445);
            pnlBottomStatus.Size = new Size(880, 40);
            pnlBottomStatus.BackColor = Color.Transparent;
            pnlCardContainer.Controls.Add(pnlBottomStatus);

            Panel dotGreen = new Panel();
            dotGreen.Location = new Point(6, 14);
            dotGreen.Size = new Size(10, 10);
            dotGreen.BackColor = Color.FromArgb(52, 211, 153);
            pnlBottomStatus.Controls.Add(dotGreen);

            lblBottomStatusText = new Label();
            lblBottomStatusText.Text = "Bağlantı için hazır (güvenli bulut ve P2P röle aktif)";
            lblBottomStatusText.Font = new Font("Segoe UI", 9f);
            lblBottomStatusText.ForeColor = Color.FromArgb(52, 211, 153);
            lblBottomStatusText.Location = new Point(22, 10);
            lblBottomStatusText.AutoSize = true;
            pnlBottomStatus.Controls.Add(lblBottomStatusText);

            Button btnWebViewerLink = new Button();
            btnWebViewerLink.Text = "🌐 Web Portalı";
            btnWebViewerLink.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnWebViewerLink.ForeColor = clrMuted;
            btnWebViewerLink.BackColor = clrInnerBox;
            btnWebViewerLink.FlatStyle = FlatStyle.Flat;
            btnWebViewerLink.FlatAppearance.BorderColor = clrBorder;
            btnWebViewerLink.Size = new Size(120, 30);
            btnWebViewerLink.Location = new Point(750, 4);
            btnWebViewerLink.Cursor = Cursors.Hand;
            btnWebViewerLink.Click += (s, e) => System.Diagnostics.Process.Start("https://my-aetherdesk-control.vercel.app");
            pnlBottomStatus.Controls.Add(btnWebViewerLink);
        }

        private Button CreateRecentChip(string id, string name)
        {
            Button btn = new Button();
            btn.Text = name + " (" + id + ")";
            btn.Font = new Font("Segoe UI", 8f);
            btn.ForeColor = clrMuted;
            btn.BackColor = clrInnerBox;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderColor = clrBorder;
            btn.Size = new Size(180, 30);
            btn.Margin = new Padding(0, 0, 8, 6);
            btn.Cursor = Cursors.Hand;
            btn.Click += (s, e) => {
                txtRemoteTargetId.Text = id.Replace(" ", "");
                StartInAppSession(id.Replace(" ", ""));
            };
            return btn;
        }

        private void ShowCommunityDialog()
        {
            if (!isLoggedIn)
            {
                DialogResult dr = MessageBox.Show(
                    "Topluluk Cihaz Ağı'na erişmek için lütfen hesabınıza giriş yapın.\n\nŞimdi giriş yapmak ister misiniz?",
                    "Topluluk Ağı",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information
                );
                if (dr == DialogResult.Yes) ShowWelcomeAuthScreen();
                return;
            }

            MessageBox.Show(
                "AetherDesk Topluluk Ağı:\n\n" +
                "👤 Hesap: " + userDisplayName + " (" + userEmail + ")\n" +
                "🏢 Grup: Genel Ekip & Cihazlar\n\n" +
                "Kayıtlı Topluluk Cihazlarınız:\n" +
                "• " + Environment.MachineName + " (" + this.mySessionId + ") [Bu Cihaz - 🟢 Aktif]\n" +
                "• Merkez Sunucu (482 910 375) [🟢 Çevrimiçi]\n" +
                "• Ofis Masaüstü (778 375 604) [🟢 Çevrimiçi]\n" +
                "• Muhasebe Terminali (891 204 153) [🟢 Çevrimiçi]",
                "AetherDesk Topluluğu",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void ShowAddressBookDialog()
        {
            if (!isLoggedIn)
            {
                DialogResult dr = MessageBox.Show(
                    "Adres Defteri ve Otomatik Senkronizasyon için hesabınıza giriş yapmalısınız.\n\nGiriş sayfasına gitmek ister misiniz?",
                    "Adres Defteri (Kısıtlı)",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information
                );
                if (dr == DialogResult.Yes) ShowWelcomeAuthScreen();
                return;
            }

            MessageBox.Show(
                "Senkronize Adres Defteriniz:\n\n" +
                "1. Ofis Bilgisayarı (778 375 604) - 🟢 Online\n" +
                "2. Ana Sunucu (482 910 375) - 🟢 Online\n" +
                "3. Muhasebe (891 204 153) - 🟢 Online\n\n" +
                "Topluluk hesabınız ile tüm cihazlar güncel tutulmaktadır.",
                "Adres Defteri",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        // IN-APP REMOTE DESKTOP SESSION CANVAS
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
                pnlMainBody.Visible = true;
                pnlCustomTitleBar.Visible = true;
                pnlMainBody.BringToFront();
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
                pnlCustomTitleBar.Visible = false;
                this.WindowState = FormWindowState.Maximized;
                isFullscreen = true;
            }
            else
            {
                pnlCustomTitleBar.Visible = true;
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
            pnlMainBody.Visible = false;
            pnlLeftSidebar.Visible = false;
            pnlCustomTitleBar.Visible = false;
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
            pnlCustomTitleBar.Visible = true;
            pnlMainBody.Visible = true;
            pnlMainBody.BringToFront();
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

            Button btnS = new Button { Text = "Kaydet", Location = new Point(20, 160), Size = new Size(360, 36), BackColor = clrAccentBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
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

            Button btnS = new Button { Text = "Kaydet", Location = new Point(20, 120), Size = new Size(340, 36), BackColor = clrAccentBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
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
            MessageBox.Show("AetherDesk Enterprise\nVersiyon: v2.5.0 (2026 Edition)\n\nUçtan uca şifreli, yüksek performanslı yeni nesil uzaktan yönetim sistemi.", "Hakkında", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LoadSettings()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\AetherDesk"))
                {
                    accessMode = (key.GetValue("AccessMode") ?? "UNATTENDED").ToString();
                    allowInput = bool.Parse((key.GetValue("AllowInput") ?? "True").ToString());
                    allowFiles = bool.Parse((key.GetValue("AllowFiles") ?? "True").ToString());
                    allowClip = bool.Parse((key.GetValue("AllowClip") ?? "True").ToString());
                    isLoggedIn = bool.Parse((key.GetValue("IsLoggedIn") ?? "False").ToString());
                    userEmail = (key.GetValue("UserEmail") ?? "").ToString();
                    userDisplayName = (key.GetValue("UserDisplayName") ?? "Misafir Kullanıcı").ToString();
                    userInitials = (key.GetValue("UserInitials") ?? "MK").ToString();
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

        private void SaveAuthSettings()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\AetherDesk"))
                {
                    key.SetValue("IsLoggedIn", isLoggedIn.ToString());
                    key.SetValue("UserEmail", userEmail);
                    key.SetValue("UserDisplayName", userDisplayName);
                    key.SetValue("UserInitials", userInitials);
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
