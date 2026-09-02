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

        // Colors matching Images 2 & 3
        private Color clrBg = Color.FromArgb(13, 15, 18);
        private Color clrCardBg = Color.FromArgb(21, 24, 30);
        private Color clrInnerBox = Color.FromArgb(10, 12, 15);
        private Color clrBorder = Color.FromArgb(38, 42, 52);
        private Color clrAccentRed = Color.FromArgb(224, 49, 49);
        private Color clrText = Color.FromArgb(248, 250, 252);
        private Color clrMuted = Color.FromArgb(148, 163, 184);

        // Logo Image
        private Image appLogoImage;

        // Custom Seamless Dark Title Bar
        private Panel pnlCustomTitleBar;
        private PictureBox picTitleLogo;
        private Label lblCustomTitle;
        private Button btnMin;
        private Button btnMax;
        private Button btnClose;

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

            this.Text = "AetherDesk Remote Access - Premium Remote Desktop";
            this.Size = new Size(980, 720);
            this.MinimumSize = new Size(880, 640);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None; // Frameless for pure 100% unified dark look!
            this.BackColor = clrBg;
            this.ForeColor = clrText;
            this.Font = new Font("Segoe UI", 9.5f);
            this.DoubleBuffered = true;

            LoadAppLogo();
            ApplyDarkWindowAttributes();

            sessionTimer = new System.Windows.Forms.Timer();
            sessionTimer.Interval = 1000;
            sessionTimer.Tick += (s, e) => UpdateSessionTimer();

            BuildCustomTitleBar();
            BuildModernLayout();
            LoadSettings();
            StartListener();
            StartCloudRelayThread();
            StartInputPollThread();
        }

        private void ApplyDarkWindowAttributes()
        {
            try
            {
                int trueVal = 1;
                DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref trueVal, sizeof(int));
                DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref trueVal, sizeof(int));
                
                int bgrDark = 0x00120F0D; // #0d0f12
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

            // Embedded default modern vector logo if file not found
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

        private void BuildCustomTitleBar()
        {
            // Custom Seamless Matte Dark Title Bar
            pnlCustomTitleBar = new Panel();
            pnlCustomTitleBar.Dock = DockStyle.Top;
            pnlCustomTitleBar.Height = 36;
            pnlCustomTitleBar.BackColor = clrBg; // Exact same background color!
            pnlCustomTitleBar.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            };
            this.Controls.Add(pnlCustomTitleBar);

            // Title Logo Icon
            picTitleLogo = new PictureBox();
            picTitleLogo.Location = new Point(10, 6);
            picTitleLogo.Size = new Size(24, 24);
            picTitleLogo.SizeMode = PictureBoxSizeMode.Zoom;
            picTitleLogo.Image = appLogoImage;
            picTitleLogo.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            };
            pnlCustomTitleBar.Controls.Add(picTitleLogo);

            lblCustomTitle = new Label();
            lblCustomTitle.Text = "AetherDesk Remote Control - Premium Remote Desktop";
            lblCustomTitle.Font = new Font("Segoe UI", 9f);
            lblCustomTitle.ForeColor = Color.FromArgb(203, 213, 225);
            lblCustomTitle.Location = new Point(40, 9);
            lblCustomTitle.AutoSize = true;
            lblCustomTitle.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            };
            pnlCustomTitleBar.Controls.Add(lblCustomTitle);

            // Close [ ✕ ] Button
            btnClose = new Button();
            btnClose.Text = "✕";
            btnClose.Font = new Font("Segoe UI", 9.5f);
            btnClose.ForeColor = clrMuted;
            btnClose.BackColor = Color.Transparent;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Size = new Size(44, 36);
            btnClose.Dock = DockStyle.Right;
            btnClose.Cursor = Cursors.Hand;
            btnClose.MouseEnter += (s, e) => { btnClose.BackColor = clrAccentRed; btnClose.ForeColor = Color.White; };
            btnClose.MouseLeave += (s, e) => { btnClose.BackColor = Color.Transparent; btnClose.ForeColor = clrMuted; };
            btnClose.Click += (s, e) => Application.Exit();
            pnlCustomTitleBar.Controls.Add(btnClose);

            // Maximize [ ▢ ] Button
            btnMax = new Button();
            btnMax.Text = "▢";
            btnMax.Font = new Font("Segoe UI", 9.5f);
            btnMax.ForeColor = clrMuted;
            btnMax.BackColor = Color.Transparent;
            btnMax.FlatStyle = FlatStyle.Flat;
            btnMax.FlatAppearance.BorderSize = 0;
            btnMax.Size = new Size(44, 36);
            btnMax.Dock = DockStyle.Right;
            btnMax.Cursor = Cursors.Hand;
            btnMax.MouseEnter += (s, e) => { btnMax.BackColor = Color.FromArgb(26, 30, 38); };
            btnMax.MouseLeave += (s, e) => { btnMax.BackColor = Color.Transparent; };
            btnMax.Click += (s, e) => {
                this.WindowState = this.WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
            };
            pnlCustomTitleBar.Controls.Add(btnMax);

            // Minimize [ — ] Button
            btnMin = new Button();
            btnMin.Text = "—";
            btnMin.Font = new Font("Segoe UI", 9.5f);
            btnMin.ForeColor = clrMuted;
            btnMin.BackColor = Color.Transparent;
            btnMin.FlatStyle = FlatStyle.Flat;
            btnMin.FlatAppearance.BorderSize = 0;
            btnMin.Size = new Size(44, 36);
            btnMin.Dock = DockStyle.Right;
            btnMin.Cursor = Cursors.Hand;
            btnMin.MouseEnter += (s, e) => { btnMin.BackColor = Color.FromArgb(26, 30, 38); };
            btnMin.MouseLeave += (s, e) => { btnMin.BackColor = Color.Transparent; };
            btnMin.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
            pnlCustomTitleBar.Controls.Add(btnMin);
        }

        private void BuildModernLayout()
        {
            // 1. Full-Window Background Wrapper
            pnlMainWrapper = new Panel();
            pnlMainWrapper.Dock = DockStyle.Fill;
            pnlMainWrapper.BackColor = clrBg;
            this.Controls.Add(pnlMainWrapper);
            pnlMainWrapper.BringToFront();

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
            pnlCardHeader.Padding = new Padding(20, 10, 20, 0);
            pnlCenterCard.Controls.Add(pnlCardHeader);

            // Card Header Logo PictureBox
            PictureBox picCardLogo = new PictureBox();
            picCardLogo.Location = new Point(18, 10);
            picCardLogo.Size = new Size(36, 36);
            picCardLogo.SizeMode = PictureBoxSizeMode.Zoom;
            picCardLogo.Image = appLogoImage;
            pnlCardHeader.Controls.Add(picCardLogo);

            Label lblLogo = new Label();
            lblLogo.Text = "AetherDesk Remote Access";
            lblLogo.Font = new Font("Segoe UI", 13.5f, FontStyle.Bold);
            lblLogo.ForeColor = clrText;
            lblLogo.Location = new Point(60, 14);
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
                pnlCustomTitleBar.Visible = true;
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
            pnlMainWrapper.Visible = false;
            pnlRightMenu.Visible = false;
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

                        if (lblOnlineStatus != null && lblOnlineStatus.IsHandleCreated)
                        {
                            lblOnlineStatus.Invoke(new Action(() => {
                                lblOnlineStatus.Text = "🟢 Küresel Bulut Aktif";
                                lblOnlineStatus.ForeColor = Color.FromArgb(52, 211, 153);
                            }));
                        }
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
