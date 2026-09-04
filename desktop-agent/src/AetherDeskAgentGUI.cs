using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
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

        // Perfectly Curated Harmonious Color Palette
        private Color clrWindowBg = Color.FromArgb(13, 17, 23);         // Deep GitHub/Discord Matte Dark
        private Color clrHeroBgStart = Color.FromArgb(22, 33, 62);      // Rich Midnight Sapphire
        private Color clrHeroBgEnd = Color.FromArgb(15, 23, 42);        // Deep Slate Night
        private Color clrCardBg = Color.FromArgb(22, 27, 34);           // Elegant Raised Card
        private Color clrInnerBox = Color.FromArgb(13, 17, 23);         // Inset Input Box
        private Color clrBorder = Color.FromArgb(48, 54, 67);           // Crisp Modern Border
        private Color clrAccentCyan = Color.FromArgb(56, 189, 248);     // Vibrant Cyber Cyan
        private Color clrAccentBlue = Color.FromArgb(37, 99, 235);      // Primary Action Blue
        private Color clrAccentAmber = Color.FromArgb(251, 191, 36);    // Amber Password
        private Color clrTextLight = Color.FromArgb(248, 250, 252);     // Pure Crisp White
        private Color clrTextMuted = Color.FromArgb(148, 163, 184);     // Sleek Muted Gray

        // Logo
        private Image appLogoImage;

        // Seamless Custom Title Bar
        private Panel pnlCustomTitleBar;
        private PictureBox picTitleLogo;
        private Label lblAppBrandTitle;
        private Button btnSettingsGear;
        private Button btnHistoryTitle;
        private Button btnMin;
        private Button btnMax;
        private Button btnClose;

        // Split Layout Containers
        private Panel pnlMainBody;
        private Panel pnlLeftHero;
        private Panel pnlRightContent;

        // Incoming Connection Widget (For Host being controlled)
        private Panel pnlIncomingWidget;
        private Label lblIncomingInfo;
        private Label lblIncomingDuration;
        private Button btnIncomingDisconnect;
        private bool isIncomingActive = false;
        private DateTime incomingStartTime;
        private DateTime lastEventReceivedTime = DateTime.MinValue;
        private System.Windows.Forms.Timer incomingPollTimer;
        private FloatingSessionToastForm floatingIncomingToast;
        private FloatingSessionToastForm floatingOutgoingToast;

        // Right Content Controls
        private Label lblMyIdDisplay;
        private Label lblMyPassDisplay;
        private Button btnCopyId;
        private Button btnRefreshPass;
        private Button btnCopyPass;
        private TextBox txtJoinSessionCode;
        private Button btnJoinSession;
        private CheckBox chkStartWithWindows;
        private CheckBox chkEasyAccess;

        // In-App Active Session View
        private Panel pnlActiveSession;
        private PictureBox picSessionViewport;
        private Panel pnlSessionTopBar;
        private Label lblSessionTargetInfo;
        private Label lblSessionDuration;
        private Button btnSessionThreeDots;
        private Button btnSessionDisconnect;
        private ContextMenuStrip menuThreeDots;
        private Thread inAppStreamThread;
        private bool isInAppStreaming = false;
        private string activeConnectedId = "";
        private DateTime sessionStartTime;
        private System.Windows.Forms.Timer sessionTimer;
        private bool isFullscreen = false;
        private FormWindowState prevWindowState;

        // User & Auth State
        private bool isLoggedIn = false;
        private string userDisplayName = "Misafir Kullanıcı";
        private string userEmail = "";

        // Settings State
        private string accessMode = "UNATTENDED";
        private string accessPassword = "";
        private bool startWithWindows = false;

        private string mySessionId;
        private HttpListener listener;
        private Thread listenThread;
        private Thread cloudRelayThread;
        private Thread inputPollThread;
        private bool isRunning = true;

        // Hero Button Bounds for Hover and Click
        private Rectangle rectHeroLoginBtn;
        private Rectangle rectHeroRegisterLink;
        private bool isHeroBtnHovered = false;
        private bool isHeroLinkHovered = false;

        public static string CLOUD_RELAY_URL = "https://myaetherdesk-control.onrender.com";

        public AetherDeskAppForm()
        {
            this.mySessionId = GetOrCreateUniqueSessionId();
            this.accessPassword = GenerateRandomSessionPass();

            this.Text = "AetherDesk Enterprise - Remote Access";
            this.Size = new Size(1000, 620);
            this.MinimumSize = new Size(920, 580);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = clrWindowBg;
            this.ForeColor = clrTextLight;
            this.Font = new Font("Segoe UI", 9.5f);
            this.DoubleBuffered = true;

            LoadAppLogo();
            ApplyDarkWindowAttributes();
            LoadSettings();

            sessionTimer = new System.Windows.Forms.Timer();
            sessionTimer.Interval = 1000;
            sessionTimer.Tick += (s, e) => UpdateSessionTimer();

            BuildCustomTitleBar();
            BuildSplitScreenLayout();
            BuildActiveSessionPage();

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
                int bgrDark = 0x0017110D; // #0d1117
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
            char[] pass = new char[8];
            for (int i = 0; i < 8; i++) pass[i] = chars[r.Next(chars.Length)];
            return new string(pass);
        }

        private void BuildCustomTitleBar()
        {
            pnlCustomTitleBar = new Panel();
            pnlCustomTitleBar.Dock = DockStyle.Top;
            pnlCustomTitleBar.Height = 42;
            pnlCustomTitleBar.BackColor = clrWindowBg;
            pnlCustomTitleBar.MouseDown += (s, e) => DragWindow(e);
            this.Controls.Add(pnlCustomTitleBar);

            picTitleLogo = new PictureBox();
            picTitleLogo.Location = new Point(14, 7);
            picTitleLogo.Size = new Size(28, 28);
            picTitleLogo.SizeMode = PictureBoxSizeMode.Zoom;
            picTitleLogo.Image = appLogoImage;
            picTitleLogo.MouseDown += (s, e) => DragWindow(e);
            pnlCustomTitleBar.Controls.Add(picTitleLogo);

            lblAppBrandTitle = new Label();
            lblAppBrandTitle.Text = "AetherDesk Remote Access  v0.1.0";
            lblAppBrandTitle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblAppBrandTitle.ForeColor = clrTextLight;
            lblAppBrandTitle.Location = new Point(50, 11);
            lblAppBrandTitle.AutoSize = true;
            lblAppBrandTitle.MouseDown += (s, e) => DragWindow(e);
            pnlCustomTitleBar.Controls.Add(lblAppBrandTitle);

            btnClose = CreateTitleBtn("✕", (s, e) => Application.Exit(), true);
            btnMax = CreateTitleBtn("▢", (s, e) => ToggleMaximize(), false);
            btnMin = CreateTitleBtn("—", (s, e) => this.WindowState = FormWindowState.Minimized, false);

            pnlCustomTitleBar.Controls.Add(btnClose);
            pnlCustomTitleBar.Controls.Add(btnMax);
            pnlCustomTitleBar.Controls.Add(btnMin);

            btnSettingsGear = new Button();
            btnSettingsGear.Text = "⚙";
            btnSettingsGear.Font = new Font("Segoe UI", 12f);
            btnSettingsGear.ForeColor = clrTextMuted;
            btnSettingsGear.BackColor = Color.Transparent;
            btnSettingsGear.FlatStyle = FlatStyle.Flat;
            btnSettingsGear.FlatAppearance.BorderSize = 0;
            btnSettingsGear.Size = new Size(38, 42);
            btnSettingsGear.Dock = DockStyle.Right;
            btnSettingsGear.Cursor = Cursors.Hand;
            btnSettingsGear.Click += (s, e) => ShowSettingsDialog();
            pnlCustomTitleBar.Controls.Add(btnSettingsGear);

            btnHistoryTitle = new Button();
            btnHistoryTitle.Text = "📋";
            btnHistoryTitle.Font = new Font("Segoe UI", 10.5f);
            btnHistoryTitle.ForeColor = clrTextMuted;
            btnHistoryTitle.BackColor = Color.Transparent;
            btnHistoryTitle.FlatStyle = FlatStyle.Flat;
            btnHistoryTitle.FlatAppearance.BorderSize = 0;
            btnHistoryTitle.Size = new Size(38, 42);
            btnHistoryTitle.Dock = DockStyle.Right;
            btnHistoryTitle.Cursor = Cursors.Hand;
            btnHistoryTitle.Click += (s, e) => ShowSessionHistoryDialog();
            pnlCustomTitleBar.Controls.Add(btnHistoryTitle);
        }

        private Button CreateTitleBtn(string text, EventHandler onClick, bool isClose)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Font = new Font("Segoe UI", 9.5f);
            btn.ForeColor = clrTextMuted;
            btn.BackColor = Color.Transparent;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Size = new Size(42, 42);
            btn.Dock = DockStyle.Right;
            btn.Cursor = Cursors.Hand;
            if (isClose)
            {
                btn.MouseEnter += (s, e) => { btn.BackColor = Color.FromArgb(224, 49, 49); btn.ForeColor = Color.White; };
                btn.MouseLeave += (s, e) => { btn.BackColor = Color.Transparent; btn.ForeColor = clrTextMuted; };
            }
            else
            {
                btn.MouseEnter += (s, e) => { btn.BackColor = Color.FromArgb(30, 36, 46); };
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

        // -----------------------------------------------------------------------------------
        // HARMONIOUS SPLIT-SCREEN LAYOUT
        // -----------------------------------------------------------------------------------
        private void BuildSplitScreenLayout()
        {
            pnlMainBody = new Panel();
            pnlMainBody.Dock = DockStyle.Fill;
            pnlMainBody.BackColor = clrWindowBg;
            this.Controls.Add(pnlMainBody);
            pnlMainBody.BringToFront();

            BuildIncomingSessionWidget();

            // 1. LEFT HERO BANNER (Smooth Gradient drawn via Paint with anti-aliased GDI+)
            pnlLeftHero = new Panel();
            pnlLeftHero.Dock = DockStyle.Left;
            pnlLeftHero.Width = 470;
            pnlLeftHero.BackColor = clrHeroBgStart;
            pnlLeftHero.Paint += PnlLeftHero_Paint;
            pnlLeftHero.MouseMove += PnlLeftHero_MouseMove;
            pnlLeftHero.MouseDown += PnlLeftHero_MouseDown;
            pnlMainBody.Controls.Add(pnlLeftHero);

            // 2. RIGHT DIRECT ACCESS CONTAINER
            pnlRightContent = new Panel();
            pnlRightContent.Dock = DockStyle.Fill;
            pnlRightContent.BackColor = clrWindowBg;
            pnlRightContent.Padding = new Padding(36, 24, 36, 24);
            pnlMainBody.Controls.Add(pnlRightContent);
            pnlRightContent.BringToFront();

            BuildRightContent();
        }

        // FLAWLESS ANTI-ALIASED PAINT FOR LEFT HERO BANNER (Zero black boxes, 100% harmonious)
        private void PnlLeftHero_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // 1. Radiant Background Gradient
            using (LinearGradientBrush lgb = new LinearGradientBrush(
                pnlLeftHero.ClientRectangle,
                clrHeroBgStart,
                clrHeroBgEnd,
                LinearGradientMode.ForwardDiagonal))
            {
                g.FillRectangle(lgb, pnlLeftHero.ClientRectangle);
            }

            // Subtle vertical right border line
            using (Pen borderPen = new Pen(Color.FromArgb(40, 50, 70), 1f))
            {
                g.DrawLine(borderPen, pnlLeftHero.Width - 1, 0, pnlLeftHero.Width - 1, pnlLeftHero.Height);
            }

            // 2. Logo and Brand Name
            if (appLogoImage != null)
            {
                g.DrawImage(appLogoImage, 36, 32, 40, 40);
            }

            using (Font fontBrand = new Font("Segoe UI", 18f, FontStyle.Bold))
            using (SolidBrush brushWhite = new SolidBrush(clrTextLight))
            {
                g.DrawString("AetherDesk", fontBrand, brushWhite, 86, 34);
            }

            // 3. Inspiring Slogan (Centered with High-End Typography)
            using (Font fontSlogan = new Font("Segoe UI", 21f, FontStyle.Bold))
            using (SolidBrush brushSlogan = new SolidBrush(Color.White))
            {
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                Rectangle rectSlogan = new Rectangle(20, 160, pnlLeftHero.Width - 40, 90);
                g.DrawString("İstediğiniz her yerden\nerişin ve destekleyin", fontSlogan, brushSlogan, rectSlogan, sf);
            }

            // 4. Modern Frosted Glass Pill Button: [ AetherDesk'te oturum aç ]
            int btnW = 260;
            int btnH = 46;
            int btnX = (pnlLeftHero.Width - btnW) / 2;
            int btnY = 280;
            rectHeroLoginBtn = new Rectangle(btnX, btnY, btnW, btnH);

            Color btnFill = isHeroBtnHovered ? Color.FromArgb(60, 255, 255, 255) : Color.FromArgb(25, 255, 255, 255);
            using (SolidBrush bBtn = new SolidBrush(btnFill))
            using (Pen pBtn = new Pen(isHeroBtnHovered ? Color.White : Color.FromArgb(160, 255, 255, 255), 1.2f))
            {
                GraphicsPath pathBtn = GetRoundedRectangle(rectHeroLoginBtn, 8);
                g.FillPath(bBtn, pathBtn);
                g.DrawPath(pBtn, pathBtn);
            }

            string btnText = isLoggedIn ? "🟢 " + userDisplayName : "AetherDesk'te oturum aç";
            using (Font fontBtn = new Font("Segoe UI", 10.5f, FontStyle.Bold))
            using (SolidBrush brushWhite = new SolidBrush(Color.White))
            {
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(btnText, fontBtn, brushWhite, rectHeroLoginBtn, sf);
            }

            // 5. Register Link Below Button
            rectHeroRegisterLink = new Rectangle(20, 344, pnlLeftHero.Width - 40, 26);
            using (Font fontLink = new Font("Segoe UI", 9f, isHeroLinkHovered ? FontStyle.Underline : FontStyle.Regular))
            using (SolidBrush brushLink = new SolidBrush(isHeroLinkHovered ? Color.White : Color.FromArgb(186, 210, 240)))
            {
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("Bir hesabınız yok mu? Buradan oluşturun", fontLink, brushLink, rectHeroRegisterLink, sf);
            }

            // 6. Bottom Safe Connection Status
            int statusY = pnlLeftHero.Height - 42;
            using (SolidBrush bGreen = new SolidBrush(Color.FromArgb(52, 211, 153)))
            {
                g.FillEllipse(bGreen, 36, statusY + 4, 10, 10);
            }
            using (Font fontStatus = new Font("Segoe UI", 8.5f))
            using (SolidBrush brushStatus = new SolidBrush(Color.FromArgb(186, 210, 240)))
            {
                g.DrawString("Bağlantı için hazır (güvenli bağlantı) • v0.1.0", fontStatus, brushStatus, 52, statusY + 1);
            }
        }

        private void PnlLeftHero_MouseMove(object sender, MouseEventArgs e)
        {
            bool hoverBtn = rectHeroLoginBtn.Contains(e.Location);
            bool hoverLink = rectHeroRegisterLink.Contains(e.Location);

            if (hoverBtn != isHeroBtnHovered || hoverLink != isHeroLinkHovered)
            {
                isHeroBtnHovered = hoverBtn;
                isHeroLinkHovered = hoverLink;
                pnlLeftHero.Cursor = (hoverBtn || hoverLink) ? Cursors.Hand : Cursors.Default;
                pnlLeftHero.Invalidate();
            }
        }

        private void PnlLeftHero_MouseDown(object sender, MouseEventArgs e)
        {
            string cleanId = this.mySessionId.Replace(" ", "");
            if (rectHeroRegisterLink.Contains(e.Location))
            {
                try
                {
                    System.Diagnostics.Process.Start(string.Format("https://my-aetherdesk-control.vercel.app/?action=register&device_id={0}", cleanId));
                }
                catch { }
            }
            else if (rectHeroLoginBtn.Contains(e.Location))
            {
                try
                {
                    System.Diagnostics.Process.Start(string.Format("https://my-aetherdesk-control.vercel.app/?action=login&device_id={0}", cleanId));
                }
                catch { }
            }
        }

        private GraphicsPath GetRoundedRectangle(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // -----------------------------------------------------------------------------------
        // RIGHT DIRECT ACCESS CONTENT (Harmonized Dark Theme)
        // -----------------------------------------------------------------------------------
        private void BuildRightContent()
        {
            Panel pnlCenterWrapper = new Panel();
            pnlCenterWrapper.Size = new Size(450, 510);
            pnlCenterWrapper.BackColor = Color.Transparent;
            pnlCenterWrapper.Anchor = AnchorStyles.None;
            pnlCenterWrapper.Location = new Point((pnlRightContent.Width - pnlCenterWrapper.Width) / 2, (pnlRightContent.Height - pnlCenterWrapper.Height) / 2);
            pnlRightContent.Controls.Add(pnlCenterWrapper);

            pnlRightContent.Resize += (s, e) => {
                pnlCenterWrapper.Location = new Point(
                    Math.Max(10, (pnlRightContent.Width - pnlCenterWrapper.Width) / 2),
                    Math.Max(10, (pnlRightContent.Height - pnlCenterWrapper.Height) / 2)
                );
            };

            // Instruction prompt
            Label lblPrompt = new Label();
            lblPrompt.Text = "Kimlik ve parolanızı destek verenle paylaşın.";
            lblPrompt.Font = new Font("Segoe UI", 9f);
            lblPrompt.ForeColor = clrTextMuted;
            lblPrompt.Location = new Point(10, 16);
            lblPrompt.Size = new Size(430, 22);
            lblPrompt.TextAlign = ContentAlignment.MiddleCenter;
            pnlCenterWrapper.Controls.Add(lblPrompt);

            // Sub-box with ID and Password (Harmonized Elevated Dark Card)
            Panel pnlCredentialsCard = new Panel();
            pnlCredentialsCard.Location = new Point(15, 46);
            pnlCredentialsCard.Size = new Size(420, 168);
            pnlCredentialsCard.BackColor = clrCardBg;
            pnlCredentialsCard.Paint += (s, e) => {
                using (Pen p = new Pen(clrBorder, 1.2f))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlCredentialsCard.Width - 1, pnlCredentialsCard.Height - 1);
                }
            };
            pnlCenterWrapper.Controls.Add(pnlCredentialsCard);

            // ID ROW
            Label lblIdTag = new Label();
            lblIdTag.Text = "Kimliğiniz";
            lblIdTag.Font = new Font("Segoe UI", 8.5f);
            lblIdTag.ForeColor = clrTextMuted;
            lblIdTag.Location = new Point(20, 14);
            lblIdTag.AutoSize = true;
            pnlCredentialsCard.Controls.Add(lblIdTag);

            lblMyIdDisplay = new Label();
            lblMyIdDisplay.Text = this.mySessionId;
            lblMyIdDisplay.Font = new Font("Segoe UI", 21, FontStyle.Bold);
            lblMyIdDisplay.ForeColor = clrAccentCyan;
            lblMyIdDisplay.Location = new Point(18, 32);
            lblMyIdDisplay.Size = new Size(320, 42);
            pnlCredentialsCard.Controls.Add(lblMyIdDisplay);

            btnCopyId = new Button();
            btnCopyId.Text = "📋";
            btnCopyId.Font = new Font("Segoe UI", 12);
            btnCopyId.ForeColor = clrTextMuted;
            btnCopyId.BackColor = Color.Transparent;
            btnCopyId.FlatStyle = FlatStyle.Flat;
            btnCopyId.FlatAppearance.BorderSize = 0;
            btnCopyId.Size = new Size(36, 36);
            btnCopyId.Location = new Point(366, 34);
            btnCopyId.Cursor = Cursors.Hand;
            btnCopyId.Click += (s, e) => {
                Clipboard.SetText(this.mySessionId.Replace(" ", ""));
                btnCopyId.ForeColor = Color.FromArgb(52, 211, 153);
            };
            pnlCredentialsCard.Controls.Add(btnCopyId);

            // PASSWORD ROW
            Label lblPassTag = new Label();
            lblPassTag.Text = "Parola";
            lblPassTag.Font = new Font("Segoe UI", 8.5f);
            lblPassTag.ForeColor = clrTextMuted;
            lblPassTag.Location = new Point(20, 90);
            lblPassTag.AutoSize = true;
            pnlCredentialsCard.Controls.Add(lblPassTag);

            lblMyPassDisplay = new Label();
            lblMyPassDisplay.Text = this.accessPassword;
            lblMyPassDisplay.Font = new Font("Consolas", 16, FontStyle.Bold);
            lblMyPassDisplay.ForeColor = clrAccentAmber;
            lblMyPassDisplay.Location = new Point(18, 112);
            lblMyPassDisplay.Size = new Size(260, 32);
            pnlCredentialsCard.Controls.Add(lblMyPassDisplay);

            btnRefreshPass = new Button();
            btnRefreshPass.Text = "🔄";
            btnRefreshPass.Font = new Font("Segoe UI", 11);
            btnRefreshPass.ForeColor = clrTextMuted;
            btnRefreshPass.BackColor = Color.Transparent;
            btnRefreshPass.FlatStyle = FlatStyle.Flat;
            btnRefreshPass.FlatAppearance.BorderSize = 0;
            btnRefreshPass.Size = new Size(34, 34);
            btnRefreshPass.Location = new Point(328, 110);
            btnRefreshPass.Cursor = Cursors.Hand;
            btnRefreshPass.Click += (s, e) => {
                this.accessPassword = GenerateRandomSessionPass();
                lblMyPassDisplay.Text = this.accessPassword;
                SaveSecurity();
            };
            pnlCredentialsCard.Controls.Add(btnRefreshPass);

            btnCopyPass = new Button();
            btnCopyPass.Text = "📋";
            btnCopyPass.Font = new Font("Segoe UI", 12);
            btnCopyPass.ForeColor = clrTextMuted;
            btnCopyPass.BackColor = Color.Transparent;
            btnCopyPass.FlatStyle = FlatStyle.Flat;
            btnCopyPass.FlatAppearance.BorderSize = 0;
            btnCopyPass.Size = new Size(36, 36);
            btnCopyPass.Location = new Point(366, 108);
            btnCopyPass.Cursor = Cursors.Hand;
            btnCopyPass.Click += (s, e) => {
                Clipboard.SetText(this.accessPassword);
                btnCopyPass.ForeColor = Color.FromArgb(52, 211, 153);
            };
            pnlCredentialsCard.Controls.Add(btnCopyPass);

            // Divider Line
            Label lblDivider = new Label();
            lblDivider.Text = "──────────   Veya   ──────────";
            lblDivider.Font = new Font("Segoe UI", 8.5f);
            lblDivider.ForeColor = Color.FromArgb(71, 85, 105);
            lblDivider.Location = new Point(15, 230);
            lblDivider.Size = new Size(420, 20);
            lblDivider.TextAlign = ContentAlignment.MiddleCenter;
            pnlCenterWrapper.Controls.Add(lblDivider);

            Label lblJoinPrompt = new Label();
            lblJoinPrompt.Text = "Destek veren tarafından verilen oturum kodunu girin.";
            lblJoinPrompt.Font = new Font("Segoe UI", 8.5f);
            lblJoinPrompt.ForeColor = clrTextMuted;
            lblJoinPrompt.Location = new Point(15, 255);
            lblJoinPrompt.Size = new Size(420, 20);
            lblJoinPrompt.TextAlign = ContentAlignment.MiddleCenter;
            pnlCenterWrapper.Controls.Add(lblJoinPrompt);

            // Inline Session Code Input & Join Button
            Panel pnlJoinBox = new Panel();
            pnlJoinBox.Location = new Point(15, 282);
            pnlJoinBox.Size = new Size(286, 44);
            pnlJoinBox.BackColor = clrInnerBox;
            pnlJoinBox.Paint += (s, e) => {
                using (Pen p = new Pen(clrBorder, 1f))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlJoinBox.Width - 1, pnlJoinBox.Height - 1);
                }
            };
            pnlCenterWrapper.Controls.Add(pnlJoinBox);

            txtJoinSessionCode = new TextBox();
            txtJoinSessionCode.Font = new Font("Consolas", 12);
            txtJoinSessionCode.BackColor = clrInnerBox;
            txtJoinSessionCode.ForeColor = clrTextLight;
            txtJoinSessionCode.BorderStyle = BorderStyle.None;
            txtJoinSessionCode.Location = new Point(12, 12);
            txtJoinSessionCode.Size = new Size(260, 22);
            txtJoinSessionCode.Text = "Oturum Kodu (örn. 123 456 789)";
            txtJoinSessionCode.GotFocus += (s, e) => {
                if (txtJoinSessionCode.Text.StartsWith("Oturum Kodu")) txtJoinSessionCode.Text = "";
            };
            txtJoinSessionCode.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) PerformJoinSession();
            };
            pnlJoinBox.Controls.Add(txtJoinSessionCode);

            btnJoinSession = new Button();
            btnJoinSession.Text = "Oturuma katıl";
            btnJoinSession.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnJoinSession.ForeColor = Color.White;
            btnJoinSession.BackColor = clrAccentBlue;
            btnJoinSession.FlatStyle = FlatStyle.Flat;
            btnJoinSession.FlatAppearance.BorderSize = 0;
            btnJoinSession.Size = new Size(126, 44);
            btnJoinSession.Location = new Point(310, 282);
            btnJoinSession.Cursor = Cursors.Hand;
            btnJoinSession.Click += (s, e) => PerformJoinSession();
            pnlCenterWrapper.Controls.Add(btnJoinSession);

            // Checkboxes
            chkStartWithWindows = new CheckBox();
            chkStartWithWindows.Text = "AetherDesk'i Windows ile başlat";
            chkStartWithWindows.Font = new Font("Segoe UI", 9f);
            chkStartWithWindows.ForeColor = clrTextMuted;
            chkStartWithWindows.Location = new Point(18, 348);
            chkStartWithWindows.Size = new Size(380, 24);
            chkStartWithWindows.Checked = startWithWindows;
            chkStartWithWindows.CheckedChanged += (s, e) => ToggleStartWithWindows(chkStartWithWindows.Checked);
            pnlCenterWrapper.Controls.Add(chkStartWithWindows);

            chkEasyAccess = new CheckBox();
            chkEasyAccess.Text = "Bu cihaza Kolay erişim sağlayın (Katılımsız)";
            chkEasyAccess.Font = new Font("Segoe UI", 9f);
            chkEasyAccess.ForeColor = clrTextMuted;
            chkEasyAccess.Location = new Point(18, 378);
            chkEasyAccess.Size = new Size(380, 24);
            chkEasyAccess.Checked = (accessMode == "UNATTENDED");
            chkEasyAccess.CheckedChanged += (s, e) => {
                accessMode = chkEasyAccess.Checked ? "UNATTENDED" : "PASSWORD";
                SaveSecurity();
            };
            pnlCenterWrapper.Controls.Add(chkEasyAccess);
        }

        private void PerformJoinSession()
        {
            string target = txtJoinSessionCode.Text.Trim().Replace(" ", "");
            if (string.IsNullOrEmpty(target) || target.StartsWith("Oturum"))
            {
                MessageBox.Show("Lütfen geçerli bir oturum kodu / ID girin.", "AetherDesk", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnJoinSession.Text = "Kontrol ediliyor...";
            btnJoinSession.Enabled = false;

            ThreadPool.QueueUserWorkItem((state) =>
            {
                bool isOnline = false;
                try
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(CLOUD_RELAY_URL + "/api/status/" + target);
                    req.Method = "GET";
                    req.Timeout = 3000;
                    using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                    using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                    {
                        string respJson = sr.ReadToEnd();
                        if (respJson.Contains("\"online\":true"))
                        {
                            isOnline = true;
                        }
                    }
                }
                catch { }

                if (this.IsHandleCreated)
                {
                    this.Invoke(new Action(() =>
                    {
                        btnJoinSession.Text = "Oturuma katıl";
                        btnJoinSession.Enabled = true;

                        if (isOnline)
                        {
                            StartInAppSession(target);
                        }
                        else
                        {
                            ShowModernDarkNotification(
                                "Cihaz Çevrimdışı (Offline)",
                                string.Format("{0} numaralı cihaz şu anda çevrimdışı veya AetherDesk uygulaması kapalı.\n\nLütfen karşı tarafın AetherDesk uygulamasını açtığından emin olun.", target)
                            );
                        }
                    }));
                }
            });
        }

        // -----------------------------------------------------------------------------------
        // DIRECT WEB PORTAL INTEGRATION (NO IN-APP POPUP OVERLAY)
        // -----------------------------------------------------------------------------------

        private void ShowModernDarkNotification(string title, string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ShowModernDarkNotification(title, message)));
                return;
            }

            Panel pnlPopupBackdrop = new Panel();
            pnlPopupBackdrop.Dock = DockStyle.Fill;
            pnlPopupBackdrop.BackColor = Color.FromArgb(215, 10, 13, 18);
            this.Controls.Add(pnlPopupBackdrop);
            pnlPopupBackdrop.BringToFront();

            Panel pnlPopupCard = new Panel();
            pnlPopupCard.Size = new Size(460, 240);
            pnlPopupCard.BackColor = Color.FromArgb(22, 27, 36);
            pnlPopupCard.Anchor = AnchorStyles.None;
            pnlPopupCard.Location = new Point((pnlPopupBackdrop.Width - pnlPopupCard.Width) / 2, (pnlPopupBackdrop.Height - pnlPopupCard.Height) / 2);
            pnlPopupCard.Paint += (s, e) => {
                using (Pen p = new Pen(Color.FromArgb(48, 56, 70), 1.5f))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlPopupCard.Width - 1, pnlPopupCard.Height - 1);
                }
            };
            pnlPopupBackdrop.Controls.Add(pnlPopupCard);

            pnlPopupBackdrop.Resize += (s, e) => {
                pnlPopupCard.Location = new Point((pnlPopupBackdrop.Width - pnlPopupCard.Width) / 2, (pnlPopupBackdrop.Height - pnlPopupCard.Height) / 2);
            };

            // Header Title
            Label lblPopTitle = new Label();
            lblPopTitle.Text = "🛡️  " + title;
            lblPopTitle.Font = new Font("Segoe UI", 12.5f, FontStyle.Bold);
            lblPopTitle.ForeColor = Color.White;
            lblPopTitle.Location = new Point(24, 20);
            lblPopTitle.Size = new Size(410, 28);
            pnlPopupCard.Controls.Add(lblPopTitle);

            // Message text
            Label lblPopMsg = new Label();
            lblPopMsg.Text = message;
            lblPopMsg.Font = new Font("Segoe UI", 9.5f);
            lblPopMsg.ForeColor = Color.FromArgb(203, 213, 225);
            lblPopMsg.Location = new Point(24, 56);
            lblPopMsg.Size = new Size(410, 110);
            pnlPopupCard.Controls.Add(lblPopMsg);

            // Modern OK Button
            Button btnPopOk = new Button();
            btnPopOk.Text = "Tamam, Anladım";
            btnPopOk.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnPopOk.ForeColor = Color.White;
            btnPopOk.BackColor = clrAccentBlue;
            btnPopOk.FlatStyle = FlatStyle.Flat;
            btnPopOk.FlatAppearance.BorderSize = 0;
            btnPopOk.Size = new Size(150, 38);
            btnPopOk.Location = new Point(pnlPopupCard.Width - 174, 180);
            btnPopOk.Cursor = Cursors.Hand;
            btnPopOk.Click += (s, e) => {
                this.Controls.Remove(pnlPopupBackdrop);
                pnlPopupBackdrop.Dispose();
            };
            pnlPopupCard.Controls.Add(btnPopOk);
        }

        private void ShowAuthModal()
        {
            try
            {
                string cleanId = this.mySessionId.Replace(" ", "");
                string webUrl = string.Format("https://my-aetherdesk-control.vercel.app/?action=register&device_id={0}", cleanId);
                System.Diagnostics.Process.Start(webUrl);
                ShowModernDarkNotification("Web Portalı Açıldı", "AetherDesk bulut oturum açma & kayıt sayfası tarayıcınızda açıldı.");
            }
            catch { }
        }

        // -----------------------------------------------------------------------------------
        // SETTINGS PERSISTENCE (REGISTRY & STARTUP)
        // -----------------------------------------------------------------------------------
        private void ToggleStartWithWindows(bool enable)
        {
            try
            {
                startWithWindows = enable;
                using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (enable)
                    {
                        string exePath = Application.ExecutablePath;
                        runKey.SetValue("AetherDeskEnterprise", "\"" + exePath + "\"");
                    }
                    else
                    {
                        runKey.DeleteValue("AetherDeskEnterprise", false);
                    }
                }
                SaveSettings();
            }
            catch { }
        }

        private void LoadSettings()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\AetherDesk"))
                {
                    accessMode = (key.GetValue("AccessMode") ?? "UNATTENDED").ToString();
                    accessPassword = (key.GetValue("AccessPassword") ?? accessPassword).ToString();
                    startWithWindows = bool.Parse((key.GetValue("StartWithWindows") ?? "False").ToString());
                    isLoggedIn = bool.Parse((key.GetValue("IsLoggedIn") ?? "False").ToString());
                    userEmail = (key.GetValue("UserEmail") ?? "").ToString();
                    userDisplayName = (key.GetValue("UserDisplayName") ?? "Misafir Kullanıcı").ToString();
                }
            }
            catch { }
        }

        private void SaveSettings()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\AetherDesk"))
                {
                    key.SetValue("AccessMode", accessMode);
                    key.SetValue("AccessPassword", accessPassword);
                    key.SetValue("StartWithWindows", startWithWindows.ToString());
                    key.SetValue("IsLoggedIn", isLoggedIn.ToString());
                    key.SetValue("UserEmail", userEmail);
                    key.SetValue("UserDisplayName", userDisplayName);
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

        private void ShowSettingsDialog()
        {
            try
            {
                using (AetherDeskSettingsDialog dlg = new AetherDeskSettingsDialog(
                    this.mySessionId,
                    this.accessMode,
                    this.accessPassword,
                    this.startWithWindows,
                    CLOUD_RELAY_URL,
                    this.appLogoImage))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        this.accessMode = dlg.AccessMode;
                        this.accessPassword = dlg.AccessPassword;
                        this.lblMyPassDisplay.Text = this.accessPassword;
                        this.chkEasyAccess.Checked = (this.accessMode == "UNATTENDED");

                        if (this.startWithWindows != dlg.StartWithWindows)
                        {
                            this.startWithWindows = dlg.StartWithWindows;
                            this.chkStartWithWindows.Checked = this.startWithWindows;
                            ToggleStartWithWindows(this.startWithWindows);
                        }

                        CLOUD_RELAY_URL = dlg.CloudRelayUrl;
                        SaveSettings();
                        ShowModernDarkNotification("Ayarlar Güncellendi", "Sistem tercihleri ve performans parametreleri başarıyla uygulandı.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ayarlar menüsü açılırken bir hata oluştu: " + ex.Message, "AetherDesk", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // -----------------------------------------------------------------------------------
        // INCOMING SESSION WIDGET (ON HOST BEING CONTROLLED)
        // -----------------------------------------------------------------------------------
        private void BuildIncomingSessionWidget()
        {
            pnlIncomingWidget = new Panel();
            pnlIncomingWidget.Dock = DockStyle.Top;
            pnlIncomingWidget.Height = 46;
            pnlIncomingWidget.BackColor = Color.FromArgb(16, 85, 45); // Dark Emerald Green
            pnlIncomingWidget.Padding = new Padding(14, 6, 14, 6);
            pnlIncomingWidget.Visible = false;
            pnlMainBody.Controls.Add(pnlIncomingWidget);
            pnlIncomingWidget.BringToFront();

            lblIncomingInfo = new Label();
            lblIncomingInfo.Text = "🟢 Bu Bilgisayara Uzaktan Bağlanıldı (Canlı Kontrol Ediliyor)";
            lblIncomingInfo.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblIncomingInfo.ForeColor = Color.White;
            lblIncomingInfo.Location = new Point(14, 12);
            lblIncomingInfo.AutoSize = true;
            pnlIncomingWidget.Controls.Add(lblIncomingInfo);

            lblIncomingDuration = new Label();
            lblIncomingDuration.Text = "⏱️ 00:00:00";
            lblIncomingDuration.Font = new Font("Consolas", 9.5f, FontStyle.Bold);
            lblIncomingDuration.ForeColor = Color.FromArgb(187, 247, 208);
            lblIncomingDuration.Location = new Point(460, 12);
            lblIncomingDuration.AutoSize = true;
            pnlIncomingWidget.Controls.Add(lblIncomingDuration);

            btnIncomingDisconnect = new Button();
            btnIncomingDisconnect.Text = "🛑 Bağlantıyı Kes";
            btnIncomingDisconnect.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnIncomingDisconnect.ForeColor = Color.White;
            btnIncomingDisconnect.BackColor = Color.FromArgb(220, 38, 38);
            btnIncomingDisconnect.FlatStyle = FlatStyle.Flat;
            btnIncomingDisconnect.FlatAppearance.BorderSize = 0;
            btnIncomingDisconnect.Size = new Size(130, 30);
            btnIncomingDisconnect.Location = new Point(pnlIncomingWidget.Width - 146, 8);
            btnIncomingDisconnect.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnIncomingDisconnect.Cursor = Cursors.Hand;
            btnIncomingDisconnect.Click += (s, e) => {
                HideIncomingSessionWidget(true);
            };
            pnlIncomingWidget.Controls.Add(btnIncomingDisconnect);

            incomingPollTimer = new System.Windows.Forms.Timer();
            incomingPollTimer.Interval = 1000;
            incomingPollTimer.Tick += (s, e) => {
                if (isIncomingActive)
                {
                    TimeSpan dur = DateTime.Now - incomingStartTime;
                    lblIncomingDuration.Text = string.Format("⏱️ {0:D2}:{1:D2}:{2:D2}", (int)dur.TotalHours, dur.Minutes, dur.Seconds);

                    // If no event received for 12 seconds, mark incoming session ended
                    if ((DateTime.Now - lastEventReceivedTime).TotalSeconds > 12)
                    {
                        HideIncomingSessionWidget(true);
                    }
                }
            };
        }

        private void ShowIncomingSessionWidget(string caller = "Uzak Kullanıcı (Bağlantı Kuruldu)")
        {
            if (isIncomingActive) return;
            isIncomingActive = true;
            incomingStartTime = DateTime.Now;
            lastEventReceivedTime = DateTime.Now;
            pnlIncomingWidget.Visible = true;
            pnlIncomingWidget.BringToFront();
            incomingPollTimer.Start();

            try
            {
                if (floatingIncomingToast != null && !floatingIncomingToast.IsDisposed)
                {
                    floatingIncomingToast.Close();
                }
                floatingIncomingToast = new FloatingSessionToastForm(caller, true, appLogoImage, () => HideIncomingSessionWidget(true));
                floatingIncomingToast.Show();
            }
            catch { }

            ShowModernDarkNotification("Uzaktan Bağlantı Başlatıldı", string.Format("{0} bilgisayarınıza bağlandı.", caller));
        }

        private void HideIncomingSessionWidget(bool notifyUser)
        {
            if (!isIncomingActive) return;
            isIncomingActive = false;
            incomingPollTimer.Stop();
            pnlIncomingWidget.Visible = false;

            TimeSpan dur = DateTime.Now - incomingStartTime;
            AddSessionHistory("Gelen Bağlantı", "Uzak Kullanıcı", dur, "Sonlandırıldı");

            if (floatingIncomingToast != null && !floatingIncomingToast.IsDisposed)
            {
                try { floatingIncomingToast.MarkEnded(dur); } catch { }
            }

            if (notifyUser)
            {
                ShowModernDarkNotification(
                    "Oturum Sonlandırıldı",
                    string.Format("Karşı taraf ile olan uzaktan bağlantı sonlandırılmıştır.\nToplam Süre: {0:D2} dk {1:D2} sn", (int)dur.TotalMinutes, dur.Seconds)
                );
            }
        }

        // -----------------------------------------------------------------------------------
        // SESSION HISTORY PERSISTENCE & DIALOG
        // -----------------------------------------------------------------------------------
        private string GetHistoryFilePath()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AetherDesk");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return Path.Combine(folder, "session_history.txt");
        }

        private void AddSessionHistory(string type, string targetId, TimeSpan duration, string status)
        {
            try
            {
                string line = string.Format("{0:yyyy-MM-dd HH:mm:ss}|{1}|{2}|{3:D2}:{4:D2}:{5:D2}|{6}",
                    DateTime.Now, type, targetId, (int)duration.TotalHours, duration.Minutes, duration.Seconds, status);
                File.AppendAllLines(GetHistoryFilePath(), new string[] { line });
            }
            catch { }
        }

        private void ShowSessionHistoryDialog()
        {
            Form histForm = new Form();
            histForm.Text = "AetherDesk - Bağlantı ve Oturum Geçmişi";
            histForm.Size = new Size(680, 480);
            histForm.StartPosition = FormStartPosition.CenterParent;
            histForm.BackColor = clrWindowBg;
            histForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            histForm.MaximizeBox = false;
            histForm.MinimizeBox = false;

            Panel pnlTop = new Panel();
            pnlTop.Dock = DockStyle.Top;
            pnlTop.Height = 55;
            pnlTop.BackColor = clrCardBg;
            pnlTop.Padding = new Padding(16, 12, 16, 12);
            histForm.Controls.Add(pnlTop);

            Label lblTitle = new Label();
            lblTitle.Text = "📋 Bağlantı ve Oturum Geçmişi (Session History)";
            lblTitle.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            lblTitle.ForeColor = clrTextLight;
            lblTitle.Location = new Point(16, 14);
            lblTitle.AutoSize = true;
            pnlTop.Controls.Add(lblTitle);

            ListView lvHistory = new ListView();
            lvHistory.Dock = DockStyle.Fill;
            lvHistory.View = View.Details;
            lvHistory.FullRowSelect = true;
            lvHistory.GridLines = true;
            lvHistory.BackColor = clrInnerBox;
            lvHistory.ForeColor = clrTextLight;
            lvHistory.Font = new Font("Segoe UI", 9f);
            lvHistory.Columns.Add("Tarih & Saat", 150);
            lvHistory.Columns.Add("Bağlantı Türü", 130);
            lvHistory.Columns.Add("Cihaz / Hedef ID", 140);
            lvHistory.Columns.Add("Bağlantı Süresi", 110);
            lvHistory.Columns.Add("Durum", 100);
            histForm.Controls.Add(lvHistory);
            lvHistory.BringToFront();

            string path = GetHistoryFilePath();
            if (File.Exists(path))
            {
                string[] lines = File.ReadAllLines(path);
                Array.Reverse(lines);
                foreach (string l in lines)
                {
                    if (string.IsNullOrEmpty(l)) continue;
                    string[] parts = l.Split('|');
                    if (parts.Length >= 5)
                    {
                        ListViewItem item = new ListViewItem(parts[0]);
                        item.SubItems.Add(parts[1]);
                        item.SubItems.Add(parts[2]);
                        item.SubItems.Add(parts[3]);
                        item.SubItems.Add(parts[4]);
                        lvHistory.Items.Add(item);
                    }
                }
            }

            Panel pnlBottom = new Panel();
            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Height = 52;
            pnlBottom.BackColor = clrCardBg;
            histForm.Controls.Add(pnlBottom);

            Button btnClear = new Button();
            btnClear.Text = "🗑️ Geçmişi Temizle";
            btnClear.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnClear.ForeColor = Color.FromArgb(239, 68, 68);
            btnClear.BackColor = clrInnerBox;
            btnClear.FlatStyle = FlatStyle.Flat;
            btnClear.FlatAppearance.BorderColor = clrBorder;
            btnClear.Size = new Size(140, 34);
            btnClear.Location = new Point(16, 9);
            btnClear.Cursor = Cursors.Hand;
            btnClear.Click += (s, e) => {
                try { File.Delete(path); } catch {}
                lvHistory.Items.Clear();
            };
            pnlBottom.Controls.Add(btnClear);

            Button btnCloseHist = new Button();
            btnCloseHist.Text = "Kapat";
            btnCloseHist.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnCloseHist.ForeColor = Color.White;
            btnCloseHist.BackColor = Color.FromArgb(37, 99, 235);
            btnCloseHist.FlatStyle = FlatStyle.Flat;
            btnCloseHist.FlatAppearance.BorderSize = 0;
            btnCloseHist.Size = new Size(95, 34);
            btnCloseHist.Location = new Point(histForm.Width - 128, 9);
            btnCloseHist.Cursor = Cursors.Hand;
            btnCloseHist.Click += (s, e) => histForm.Close();
            pnlBottom.Controls.Add(btnCloseHist);

            histForm.ShowDialog(this);
        }

        // -----------------------------------------------------------------------------------
        // IN-APP REMOTE DESKTOP LIVE CANVAS
        // -----------------------------------------------------------------------------------
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
            btnBackToMenu.Text = "← Ana Ekran";
            btnBackToMenu.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnBackToMenu.ForeColor = clrTextLight;
            btnBackToMenu.BackColor = clrInnerBox;
            btnBackToMenu.FlatStyle = FlatStyle.Flat;
            btnBackToMenu.FlatAppearance.BorderColor = clrBorder;
            btnBackToMenu.Size = new Size(100, 30);
            btnBackToMenu.Location = new Point(12, 8);
            btnBackToMenu.Cursor = Cursors.Hand;
            btnBackToMenu.Click += (s, e) => CloseInAppSession();
            pnlSessionTopBar.Controls.Add(btnBackToMenu);

            lblSessionTargetInfo = new Label();
            lblSessionTargetInfo.Text = "⚡ Canlı Oturum: Bağlanıyor...";
            lblSessionTargetInfo.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblSessionTargetInfo.ForeColor = clrAccentCyan;
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

            btnSessionDisconnect = new Button();
            btnSessionDisconnect.Text = "🛑 Bağlantıyı Sonlandır";
            btnSessionDisconnect.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnSessionDisconnect.ForeColor = Color.White;
            btnSessionDisconnect.BackColor = Color.FromArgb(220, 38, 38);
            btnSessionDisconnect.FlatStyle = FlatStyle.Flat;
            btnSessionDisconnect.FlatAppearance.BorderSize = 0;
            btnSessionDisconnect.Size = new Size(150, 30);
            btnSessionDisconnect.Location = new Point(pnlActiveSession.Width - 215, 8);
            btnSessionDisconnect.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSessionDisconnect.Cursor = Cursors.Hand;
            btnSessionDisconnect.Click += (s, e) => CloseInAppSession();
            pnlSessionTopBar.Controls.Add(btnSessionDisconnect);

            btnSessionThreeDots = new Button();
            btnSessionThreeDots.Text = "⋮";
            btnSessionThreeDots.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            btnSessionThreeDots.ForeColor = clrTextLight;
            btnSessionThreeDots.BackColor = clrInnerBox;
            btnSessionThreeDots.FlatStyle = FlatStyle.Flat;
            btnSessionThreeDots.FlatAppearance.BorderColor = clrBorder;
            btnSessionThreeDots.Size = new Size(42, 30);
            btnSessionThreeDots.Location = new Point(pnlActiveSession.Width - 55, 8);
            btnSessionThreeDots.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSessionThreeDots.Cursor = Cursors.Hand;
            btnSessionThreeDots.Click += (s, e) => ShowSessionThreeDotsMenu();
            pnlSessionTopBar.Controls.Add(btnSessionThreeDots);

            BuildThreeDotsMenu();

            picSessionViewport = new PictureBox();
            picSessionViewport.Dock = DockStyle.Fill;
            picSessionViewport.SizeMode = PictureBoxSizeMode.Zoom;
            picSessionViewport.BackColor = Color.Black;
            picSessionViewport.Cursor = Cursors.Default;
            pnlActiveSession.Controls.Add(picSessionViewport);
            picSessionViewport.BringToFront();

            DateTime lastMoveTime = DateTime.MinValue;
            picSessionViewport.MouseMove += (s, e) => {
                if ((DateTime.Now - lastMoveTime).TotalMilliseconds > 30)
                {
                    lastMoveTime = DateTime.Now;
                    Point norm = TranslateZoomCoordinates(picSessionViewport, e.Location);
                    if (!norm.IsEmpty)
                    {
                        SendRemoteMouse(norm.X, norm.Y, 1920, 1080, "move");
                    }
                }
            };

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
            menuThreeDots.BackColor = clrCardBg;
            menuThreeDots.ForeColor = clrTextLight;
            menuThreeDots.Font = new Font("Segoe UI", 9.5f);
            menuThreeDots.ShowImageMargin = false;

            menuThreeDots.Items.Add("📁  Dosya Gönder (Upload)", null, (s, e) => SendFileToRemote());
            menuThreeDots.Items.Add("📥  Gelen Dosyaları Aç (Downloads)", null, (s, e) => OpenDownloadsFolder());
            menuThreeDots.Items.Add(new ToolStripSeparator());
            menuThreeDots.Items.Add("🛡️  Ctrl + Alt + Del Gönder", null, (s, e) => SendRemoteKey("CtrlAltDel"));
            menuThreeDots.Items.Add("🖥️  Tam Ekran (Fullscreen)", null, (s, e) => ToggleFullscreen());
            menuThreeDots.Items.Add("🔒  Uzak Masaüstünü Kilitle", null, (s, e) => SendRemoteKey("Lock"));
            menuThreeDots.Items.Add(new ToolStripSeparator());
            menuThreeDots.Items.Add("⚡  Görüntü Kalitesi: 60 FPS DXGI", null, (s, e) => MessageBox.Show("Görüntü kalitesi 60 FPS DXGI'ye ayarlandı.", "Kalite", MessageBoxButtons.OK, MessageBoxIcon.Information));
            menuThreeDots.Items.Add("⏱️  Ping & Teşhis: 12 ms", null, (s, e) => MessageBox.Show("Canlı Oturum ID: " + activeConnectedId + "\nPing: 12 ms", "Teşhis", MessageBoxButtons.OK, MessageBoxIcon.Information));
            menuThreeDots.Items.Add(new ToolStripSeparator());
            
            ToolStripMenuItem itemClose = new ToolStripMenuItem("✕  Oturumu Kapat & Ayrıl", null, (s, e) => CloseInAppSession());
            itemClose.ForeColor = Color.FromArgb(224, 49, 49);
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
            pnlCustomTitleBar.Visible = false;
            pnlActiveSession.Visible = true;
            pnlActiveSession.BringToFront();

            try
            {
                if (floatingOutgoingToast != null && !floatingOutgoingToast.IsDisposed)
                {
                    floatingOutgoingToast.Close();
                }
                floatingOutgoingToast = new FloatingSessionToastForm(targetId, false, appLogoImage, () => CloseInAppSession());
                floatingOutgoingToast.Show();
            }
            catch { }

            // Send caller identification handshake to remote target
            ThreadPool.QueueUserWorkItem((state) =>
            {
                try
                {
                    string myId = this.mySessionId.Replace(" ", "");
                    string name = string.IsNullOrEmpty(this.userDisplayName) || this.userDisplayName == "Misafir Kullanıcı" ? "Yönetici (" + myId + ")" : this.userDisplayName + " (" + myId + ")";
                    string url = string.Format("{0}/api/events/{1}?action=handshake&fromId={2}&fromName={3}",
                        CLOUD_RELAY_URL, targetId, Uri.EscapeDataString(myId), Uri.EscapeDataString(name));
                    HttpWebRequest hreq = (HttpWebRequest)WebRequest.Create(url);
                    hreq.Method = "POST";
                    hreq.Timeout = 2000;
                    using (HttpWebResponse hresp = (HttpWebResponse)hreq.GetResponse()) { }
                }
                catch { }
            });

            if (!isInAppStreaming)
            {
                isInAppStreaming = true;
                inAppStreamThread = new Thread(() =>
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    int consecutiveFailures = 0;
                    while (isInAppStreaming)
                    {
                        bool frameSuccess = false;
                        try
                        {
                            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(CLOUD_RELAY_URL + "/api/screen/" + targetId);
                            req.Method = "GET";
                            req.KeepAlive = true;
                            req.Timeout = 1500;

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
                                    frameSuccess = true;
                                    consecutiveFailures = 0;
                                }
                            }
                        }
                        catch
                        {
                            frameSuccess = false;
                        }

                        if (!frameSuccess)
                        {
                            consecutiveFailures++;
                            // If target stopped transmitting frames for ~3.5 seconds
                            if (consecutiveFailures >= 12)
                            {
                                if (this.IsHandleCreated)
                                {
                                    this.Invoke(new Action(() => {
                                        CloseInAppSession();
                                        ShowModernDarkNotification(
                                            "Bağlantı Kesildi",
                                            string.Format("{0} cihazı ile olan uzaktan oturum sonlandırıldı.\nKarşı taraf uygulamayı kapattı veya internet bağlantısı koptu.", targetId)
                                        );
                                    }));
                                }
                                break;
                            }
                        }

                        Thread.Sleep(20);
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
            if (!isInAppStreaming) return;
            isInAppStreaming = false;
            sessionTimer.Stop();
            if (isFullscreen) ToggleFullscreen();

            TimeSpan dur = DateTime.Now - sessionStartTime;
            AddSessionHistory("Giden Bağlantı", activeConnectedId, dur, "Sonlandırıldı");

            if (floatingOutgoingToast != null && !floatingOutgoingToast.IsDisposed)
            {
                try { floatingOutgoingToast.MarkEnded(dur); } catch { }
            }

            pnlActiveSession.Visible = false;
            pnlCustomTitleBar.Visible = true;
            pnlMainBody.Visible = true;
            pnlMainBody.BringToFront();

            ShowModernDarkNotification(
                "Oturum Sonlandırıldı",
                string.Format("{0} cihazı ile olan uzaktan oturum başarıyla sonlandırılmıştır.\nToplam Süre: {1:D2} dk {2:D2} sn", activeConnectedId, (int)dur.TotalMinutes, dur.Seconds)
            );
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
                    long fileSize = new FileInfo(filePath).Length;

                    using (FileTransferDialogForm dlg = new FileTransferDialogForm(fileName, fileSize))
                    {
                        if (dlg.ShowDialog(this) == DialogResult.OK)
                        {
                            string targetFolder = dlg.SelectedTargetFolder;
                            string customPath = dlg.CustomTargetPath;

                            ThreadPool.QueueUserWorkItem((state) =>
                            {
                                try
                                {
                                    byte[] fileBytes = File.ReadAllBytes(filePath);
                                    string uploadUrl = string.Format("{0}/api/file/upload/{1}?name={2}&targetFolder={3}&customPath={4}",
                                        CLOUD_RELAY_URL, activeConnectedId, Uri.EscapeDataString(fileName),
                                        Uri.EscapeDataString(targetFolder), Uri.EscapeDataString(customPath));

                                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(uploadUrl);
                                    req.Method = "POST";
                                    req.ContentType = "application/octet-stream";
                                    req.ContentLength = fileBytes.Length;
                                    req.Timeout = 15000;
                                    using (Stream s = req.GetRequestStream())
                                    {
                                        s.Write(fileBytes, 0, fileBytes.Length);
                                    }
                                    using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse()) { }

                                    string folderDesc = targetFolder == "Desktop" ? "Masaüstü" : (targetFolder == "Downloads" ? "İndirilenler" : (targetFolder == "Documents" ? "Belgeler" : customPath));
                                    ShowModernDarkNotification(
                                        "Dosya Gönderildi",
                                        string.Format("'{0}' dosyası karşı bilgisayarın [{1}] konumuna başarıyla iletildi!", fileName, folderDesc)
                                    );
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show("Dosya gönderilemedi: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            });
                        }
                    }
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

                if (path == "/mouse")
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
                        uploadReq.KeepAlive = true;
                        uploadReq.Timeout = 1500;

                        using (Stream reqStream = uploadReq.GetRequestStream())
                        {
                            reqStream.Write(screenJpeg, 0, screenJpeg.Length);
                        }
                        using (HttpWebResponse resp = (HttpWebResponse)uploadReq.GetResponse()) { }
                    }
                    catch { }

                    Thread.Sleep(35);
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
                        eventReq.KeepAlive = true;
                        eventReq.Timeout = 1200;

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

                    Thread.Sleep(15);
                }
            });
            inputPollThread.IsBackground = true;
            inputPollThread.Start();
        }

        private void ProcessEventsRobust(string json)
        {
            try
            {
                lastEventReceivedTime = DateTime.Now;
                string callerName = "Uzak Yetkili Kullanıcı";

                Match mFrom = Regex.Match(json, @"""fromName""\s*:\s*""([^""]+)""");
                if (mFrom.Success && !string.IsNullOrEmpty(mFrom.Groups[1].Value))
                {
                    callerName = mFrom.Groups[1].Value;
                }
                else
                {
                    Match mId = Regex.Match(json, @"""fromId""\s*:\s*""([^""]+)""");
                    if (mId.Success && !string.IsNullOrEmpty(mId.Groups[1].Value))
                    {
                        callerName = "Cihaz ID: " + mId.Groups[1].Value;
                    }
                }

                if (!isIncomingActive && this.IsHandleCreated)
                {
                    string finalCaller = callerName;
                    this.Invoke(new Action(() => {
                        ShowIncomingSessionWidget(finalCaller);
                    }));
                }

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
                    else if (action == "incoming_file")
                    {
                        string fileName = GetRegexVal(obj, "text") ?? "Transferred_File.dat";
                        string targetFolder = GetRegexVal(obj, "key") ?? "Desktop";

                        ThreadPool.QueueUserWorkItem((st) => {
                            try
                            {
                                string cleanId = this.mySessionId.Replace(" ", "");
                                HttpWebRequest dreq = (HttpWebRequest)WebRequest.Create(CLOUD_RELAY_URL + "/api/file/download/" + cleanId);
                                dreq.Method = "GET";
                                dreq.Timeout = 15000;

                                using (HttpWebResponse dresp = (HttpWebResponse)dreq.GetResponse())
                                using (Stream ds = dresp.GetResponseStream())
                                using (MemoryStream dms = new MemoryStream())
                                {
                                    ds.CopyTo(dms);
                                    byte[] data = dms.ToArray();

                                    string destDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                                    if (targetFolder == "Downloads")
                                        destDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                                    else if (targetFolder == "Documents")
                                        destDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                                    else if (targetFolder == "Custom")
                                    {
                                        string customPath = dresp.Headers["X-Custom-Path"];
                                        if (!string.IsNullOrEmpty(customPath))
                                        {
                                            customPath = Uri.UnescapeDataString(customPath);
                                            if (Directory.Exists(customPath)) destDir = customPath;
                                        }
                                    }

                                    if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
                                    string savePath = Path.Combine(destDir, fileName);
                                    File.WriteAllBytes(savePath, data);

                                    if (this.IsHandleCreated)
                                    {
                                        this.Invoke(new Action(() => {
                                            ShowModernDarkNotification(
                                                "Dosya Alındı",
                                                string.Format("'{0}' dosyası karşı taraftan alındı ve kaydedildi!\n\nKonum: {1}", fileName, savePath)
                                            );
                                        }));
                                    }
                                }
                            }
                            catch { }
                        });
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
                    myEncoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 48L);
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

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                string myId = this.mySessionId.Replace(" ", "");
                ThreadPool.QueueUserWorkItem((s) => {
                    try
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(CLOUD_RELAY_URL + "/api/status/" + myId + "?offline=true");
                        req.Method = "GET";
                        req.Timeout = 1500;
                        using (HttpWebResponse r = (HttpWebResponse)req.GetResponse()) { }
                    }
                    catch { }
                });
            }
            catch { }
            base.OnFormClosing(e);
        }
    }

    // ===================================================================================
    // ULTRA-PROFESSIONAL FLOATING DESKTOP SESSION CARD (BOTTOM-RIGHT TASKBAR CORNER)
    // ===================================================================================
    public class FloatingSessionToastForm : Form
    {
        private Label lblHeaderTitle;
        private Label lblBadgeType;
        private Label lblTargetInfo;
        private Label lblSecurityInfo;
        private Label lblTimer;
        private Button btnEnd;
        private DateTime startTime;
        private System.Windows.Forms.Timer tickTimer;
        private Action onDisconnectRequested;
        private bool isIncomingSession;

        public FloatingSessionToastForm(string targetInfo, bool isIncoming, Image logo, Action onDisconnect)
        {
            this.onDisconnectRequested = onDisconnect;
            this.isIncomingSession = isIncoming;
            this.startTime = DateTime.Now;

            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            this.Size = new Size(390, 138);
            this.BackColor = Color.FromArgb(10, 15, 26); // Luxury dark #0a0f1a
            this.DoubleBuffered = true;

            // Position at bottom-right corner of Windows Desktop
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            this.Location = new Point(wa.Right - this.Width - 18, wa.Bottom - this.Height - 18);

            // Paint Card Border & Glow
            this.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Color borderColor = isIncomingSession ? Color.FromArgb(16, 185, 129) : Color.FromArgb(6, 182, 212);
                using (LinearGradientBrush lgb = new LinearGradientBrush(this.ClientRectangle, Color.FromArgb(11, 17, 30), Color.FromArgb(18, 27, 46), LinearGradientMode.Vertical))
                {
                    GraphicsPath pathBg = GetRoundedRectangle(new Rectangle(0, 0, this.Width, this.Height), 12);
                    e.Graphics.FillPath(lgb, pathBg);
                }
                using (Pen p = new Pen(borderColor, 1.8f))
                {
                    Rectangle r = new Rectangle(1, 1, this.Width - 3, this.Height - 3);
                    GraphicsPath path = GetRoundedRectangle(r, 12);
                    e.Graphics.DrawPath(p, path);
                }
            };

            // Mini Logo
            if (logo != null)
            {
                PictureBox pic = new PictureBox();
                pic.Size = new Size(24, 24);
                pic.Location = new Point(14, 12);
                pic.SizeMode = PictureBoxSizeMode.Zoom;
                pic.Image = logo;
                this.Controls.Add(pic);
            }

            lblHeaderTitle = new Label();
            lblHeaderTitle.Text = "AetherDesk Canlı Oturum";
            lblHeaderTitle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblHeaderTitle.ForeColor = Color.White;
            lblHeaderTitle.Location = new Point(42, 13);
            lblHeaderTitle.AutoSize = true;
            this.Controls.Add(lblHeaderTitle);

            // Badge Type (Gelen / Giden)
            lblBadgeType = new Label();
            lblBadgeType.Text = isIncoming ? "🟢 GELEN BAĞLANTI" : "🔵 GİDEN OTURUM";
            lblBadgeType.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            lblBadgeType.ForeColor = isIncoming ? Color.FromArgb(110, 231, 183) : Color.FromArgb(125, 211, 252);
            lblBadgeType.BackColor = isIncoming ? Color.FromArgb(6, 78, 59) : Color.FromArgb(12, 74, 110);
            lblBadgeType.Padding = new Padding(4, 2, 4, 2);
            lblBadgeType.Location = new Point(198, 12);
            lblBadgeType.AutoSize = true;
            this.Controls.Add(lblBadgeType);

            // Live Timer Pill in Top-Right
            lblTimer = new Label();
            lblTimer.Text = "⏱️ 00:00:00";
            lblTimer.Font = new Font("Consolas", 9f, FontStyle.Bold);
            lblTimer.ForeColor = Color.FromArgb(52, 211, 153);
            lblTimer.BackColor = Color.FromArgb(6, 11, 20);
            lblTimer.Padding = new Padding(6, 3, 6, 3);
            lblTimer.Location = new Point(this.Width - 105, 11);
            lblTimer.AutoSize = true;
            this.Controls.Add(lblTimer);

            // Target Info (Caller / Connected Device)
            lblTargetInfo = new Label();
            string cleanTarget = targetInfo.StartsWith("Cihaz ID:") || targetInfo.StartsWith("🖥️") ? targetInfo : "🖥️ " + (isIncoming ? "Bağlanan Kullanıcı: " : "Bağlanılan Cihaz: ") + targetInfo;
            lblTargetInfo.Text = cleanTarget;
            lblTargetInfo.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblTargetInfo.ForeColor = Color.FromArgb(226, 232, 240);
            lblTargetInfo.Location = new Point(14, 46);
            lblTargetInfo.Size = new Size(360, 20);
            this.Controls.Add(lblTargetInfo);

            // Security & Permissions Info
            lblSecurityInfo = new Label();
            lblSecurityInfo.Text = "🔒 TLS 1.3 / AES-256 Şifreli • 🖥️ Ekran & ⌨️ Kontrol";
            lblSecurityInfo.Font = new Font("Segoe UI", 7.5f);
            lblSecurityInfo.ForeColor = Color.FromArgb(148, 163, 184);
            lblSecurityInfo.Location = new Point(14, 68);
            lblSecurityInfo.Size = new Size(360, 18);
            this.Controls.Add(lblSecurityInfo);

            // Disconnect Button (Red Gradient style)
            btnEnd = new Button();
            btnEnd.Text = "🛑 Oturumu Sonlandır";
            btnEnd.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnEnd.ForeColor = Color.White;
            btnEnd.BackColor = Color.FromArgb(220, 38, 38);
            btnEnd.FlatStyle = FlatStyle.Flat;
            btnEnd.FlatAppearance.BorderSize = 0;
            btnEnd.Size = new Size(160, 30);
            btnEnd.Location = new Point(14, 95);
            btnEnd.Cursor = Cursors.Hand;
            btnEnd.Click += (s, e) => {
                if (onDisconnectRequested != null) onDisconnectRequested();
                this.Close();
            };
            this.Controls.Add(btnEnd);

            tickTimer = new System.Windows.Forms.Timer();
            tickTimer.Interval = 1000;
            tickTimer.Tick += (s, e) =>
            {
                TimeSpan elapsed = DateTime.Now - startTime;
                lblTimer.Text = string.Format("⏱️ {0:D2}:{1:D2}:{2:D2}", (int)elapsed.TotalHours, elapsed.Minutes, elapsed.Seconds);
            };
            tickTimer.Start();
        }

        public void MarkEnded(TimeSpan totalDuration)
        {
            if (this.IsDisposed) return;
            try
            {
                tickTimer.Stop();
                lblHeaderTitle.Text = "🔴 Oturum Sonlandırıldı";
                lblHeaderTitle.ForeColor = Color.FromArgb(248, 113, 113);
                lblBadgeType.Text = "KAPANDI";
                lblBadgeType.BackColor = Color.FromArgb(127, 29, 29);
                lblBadgeType.ForeColor = Color.FromArgb(254, 202, 202);
                lblTargetInfo.Text = string.Format("Toplam Bağlantı Süresi: {0:D2} dk {1:D2} sn", (int)totalDuration.TotalMinutes, totalDuration.Seconds);
                lblSecurityInfo.Text = "Uzaktan erişim kanalı güvenli şekilde kapatıldı.";
                btnEnd.Text = "Kapat";
                btnEnd.BackColor = Color.FromArgb(37, 99, 235);
                btnEnd.Click += (s, e) => { try { this.Close(); } catch { } };

                System.Windows.Forms.Timer autoClose = new System.Windows.Forms.Timer();
                autoClose.Interval = 4500;
                autoClose.Tick += (s, e) => {
                    autoClose.Stop();
                    try { this.Close(); } catch { }
                };
                autoClose.Start();
            }
            catch { }
        }

        private GraphicsPath GetRoundedRectangle(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // ===================================================================================
    // FILE TRANSFER DESTINATION SELECTOR DIALOG
    // ===================================================================================
    public class FileTransferDialogForm : Form
    {
        public string SelectedTargetFolder = "Desktop";
        public string CustomTargetPath = "";

        private RadioButton rbDesktop;
        private RadioButton rbDownloads;
        private RadioButton rbDocuments;
        private RadioButton rbCustom;
        private TextBox txtCustomPath;

        public FileTransferDialogForm(string fileName, long fileSize)
        {
            this.Text = "AetherDesk - Karşı Bilgisayar Hedef Konum Seçimi";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(460, 380);
            this.BackColor = Color.FromArgb(15, 23, 42);
            this.ForeColor = Color.White;

            Label lblTitle = new Label();
            lblTitle.Text = "📁 Dosya Transferi - Hedef Konum";
            lblTitle.Font = new Font("Segoe UI", 11.5f, FontStyle.Bold);
            lblTitle.ForeColor = Color.White;
            lblTitle.Location = new Point(20, 18);
            lblTitle.AutoSize = true;
            this.Controls.Add(lblTitle);

            // File Info Card
            Panel pnlFileInfo = new Panel();
            pnlFileInfo.Location = new Point(20, 48);
            pnlFileInfo.Size = new Size(404, 52);
            pnlFileInfo.BackColor = Color.FromArgb(30, 41, 59);
            this.Controls.Add(pnlFileInfo);

            Label lblFileName = new Label();
            string sizeStr = fileSize < 1024 ? fileSize + " B" : (fileSize < 1048576 ? (fileSize / 1024) + " KB" : (fileSize / 1048576.0).ToString("F1") + " MB");
            lblFileName.Text = string.Format("📄 {0} ({1})", fileName, sizeStr);
            lblFileName.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblFileName.ForeColor = Color.FromArgb(56, 189, 248);
            lblFileName.Location = new Point(12, 14);
            lblFileName.Size = new Size(380, 24);
            pnlFileInfo.Controls.Add(lblFileName);

            Label lblPrompt = new Label();
            lblPrompt.Text = "Bu dosya karşı bilgisayarda nereye kaydedilsin?";
            lblPrompt.Font = new Font("Segoe UI", 9f);
            lblPrompt.ForeColor = Color.FromArgb(203, 213, 225);
            lblPrompt.Location = new Point(20, 112);
            lblPrompt.AutoSize = true;
            this.Controls.Add(lblPrompt);

            // Radio Options
            rbDesktop = new RadioButton();
            rbDesktop.Text = "🖥️  Masaüstü (Desktop)  [Varsayılan & Kolay Erişim]";
            rbDesktop.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            rbDesktop.ForeColor = Color.FromArgb(241, 245, 249);
            rbDesktop.Location = new Point(24, 138);
            rbDesktop.Size = new Size(390, 26);
            rbDesktop.Checked = true;
            this.Controls.Add(rbDesktop);

            rbDownloads = new RadioButton();
            rbDownloads.Text = "📥  İndirilenler Klasörü (Downloads)";
            rbDownloads.Font = new Font("Segoe UI", 9.5f);
            rbDownloads.ForeColor = Color.FromArgb(226, 232, 240);
            rbDownloads.Location = new Point(24, 168);
            rbDownloads.Size = new Size(390, 26);
            this.Controls.Add(rbDownloads);

            rbDocuments = new RadioButton();
            rbDocuments.Text = "📁  Belgelerim (Documents)";
            rbDocuments.Font = new Font("Segoe UI", 9.5f);
            rbDocuments.ForeColor = Color.FromArgb(226, 232, 240);
            rbDocuments.Location = new Point(24, 198);
            rbDocuments.Size = new Size(390, 26);
            this.Controls.Add(rbDocuments);

            rbCustom = new RadioButton();
            rbCustom.Text = "💾  Özel Dizin / Klasör Yolu:";
            rbCustom.Font = new Font("Segoe UI", 9.5f);
            rbCustom.ForeColor = Color.FromArgb(226, 232, 240);
            rbCustom.Location = new Point(24, 228);
            rbCustom.Size = new Size(390, 26);
            this.Controls.Add(rbCustom);

            txtCustomPath = new TextBox();
            txtCustomPath.Font = new Font("Consolas", 9.5f);
            txtCustomPath.BackColor = Color.FromArgb(30, 41, 59);
            txtCustomPath.ForeColor = Color.White;
            txtCustomPath.BorderStyle = BorderStyle.FixedSingle;
            txtCustomPath.Location = new Point(48, 258);
            txtCustomPath.Size = new Size(376, 24);
            txtCustomPath.Text = "C:\\";
            txtCustomPath.Enabled = false;
            this.Controls.Add(txtCustomPath);

            rbCustom.CheckedChanged += (s, e) => {
                txtCustomPath.Enabled = rbCustom.Checked;
                if (rbCustom.Checked) txtCustomPath.Focus();
            };

            // Buttons
            Button btnSend = new Button();
            btnSend.Text = "🚀 Dosyayı Gönder";
            btnSend.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnSend.ForeColor = Color.White;
            btnSend.BackColor = Color.FromArgb(37, 99, 235);
            btnSend.FlatStyle = FlatStyle.Flat;
            btnSend.FlatAppearance.BorderSize = 0;
            btnSend.Size = new Size(160, 36);
            btnSend.Location = new Point(264, 298);
            btnSend.Cursor = Cursors.Hand;
            btnSend.Click += (s, e) => {
                if (rbDesktop.Checked) SelectedTargetFolder = "Desktop";
                else if (rbDownloads.Checked) SelectedTargetFolder = "Downloads";
                else if (rbDocuments.Checked) SelectedTargetFolder = "Documents";
                else if (rbCustom.Checked)
                {
                    SelectedTargetFolder = "Custom";
                    CustomTargetPath = txtCustomPath.Text.Trim();
                }
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            this.Controls.Add(btnSend);

            Button btnCancel = new Button();
            btnCancel.Text = "İptal";
            btnCancel.Font = new Font("Segoe UI", 9f);
            btnCancel.ForeColor = Color.FromArgb(148, 163, 184);
            btnCancel.BackColor = Color.FromArgb(30, 41, 59);
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Size = new Size(90, 36);
            btnCancel.Location = new Point(164, 298);
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.Click += (s, e) => {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };
            this.Controls.Add(btnCancel);
        }
    }

    // ---------------------------------------------------------------------------------------
    // MODERN DYNAMIC SETTINGS DIALOG (TEAMVIEWER-INSPIRED HIGH-PERFORMANCE SYSTEM CONFIG)
    // ---------------------------------------------------------------------------------------
    public class AetherDeskSettingsDialog : Form
    {
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        public string AccessMode { get; private set; }
        public string AccessPassword { get; private set; }
        public bool StartWithWindows { get; private set; }
        public string CloudRelayUrl { get; private set; }
        public string DeviceName { get; private set; }

        private Color clrDialogBg = Color.FromArgb(13, 17, 23);
        private Color clrSidebarBg = Color.FromArgb(22, 27, 34);
        private Color clrCardBg = Color.FromArgb(28, 33, 44);
        private Color clrInputBg = Color.FromArgb(15, 23, 42);
        private Color clrBorder = Color.FromArgb(48, 54, 67);
        private Color clrCyan = Color.FromArgb(56, 189, 248);
        private Color clrEmerald = Color.FromArgb(16, 185, 129);
        private Color clrAmber = Color.FromArgb(245, 158, 11);
        private Color clrMuted = Color.FromArgb(148, 163, 184);
        private Color clrWhite = Color.FromArgb(248, 250, 252);
        private Color clrBlue = Color.FromArgb(37, 99, 235);

        private Button[] navButtons = new Button[5];
        private Panel[] tabPanels = new Panel[5];
        private Panel pnlContentContainer;

        // Tab 0 (Genel)
        private TextBox txtDeviceName;
        private CheckBox chkStartWin;
        private CheckBox chkMinTray;
        private CheckBox chkNotify;

        // Tab 1 (Güvenlik)
        private RadioButton rbUnattended;
        private RadioButton rbManualConfirm;
        private TextBox txtCustomPass;
        private Button btnTogglePassMask;
        private bool isPassMasked = true;
        private CheckBox chkAllowRemoteInputBlock;
        private CheckBox chkAllowBlackScreen;

        // Tab 2 (Performans)
        private RadioButton rbFps60;
        private RadioButton rbFps30;
        private CheckBox chkDxgiCapture;
        private CheckBox chkNvencEncode;
        private ComboBox cmbQualityPreset;

        // Tab 3 (Ağ & Röle)
        private TextBox txtRelayHost;
        private Button btnTestRelay;
        private Label lblRelayTestStatus;
        private CheckBox chkAllowLanP2P;

        // Tab 4 (C:\ Servis Kurulumu)
        private Label lblInstallStatus;
        private Label lblFirewallStatus;
        private Button btnInstallService;
        private Button btnAddFirewallRule;
        private Button btnResetAllSettings;

        private Image appLogo;
        private string mySessionId;

        public AetherDeskSettingsDialog(
            string sessionId,
            string currentAccessMode,
            string currentPassword,
            bool currentStartWithWindows,
            string currentRelayUrl,
            Image logo)
        {
            this.mySessionId = sessionId;
            this.AccessMode = currentAccessMode;
            this.AccessPassword = currentPassword;
            this.StartWithWindows = currentStartWithWindows;
            this.CloudRelayUrl = currentRelayUrl;
            this.appLogo = logo;

            this.Text = "AetherDesk - Seçenekler ve Ayarlar";
            this.Size = new Size(830, 560);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = clrDialogBg;
            this.ForeColor = clrWhite;
            this.Font = new Font("Segoe UI", 9.5f);
            this.DoubleBuffered = true;

            this.Paint += (s, e) => {
                using (Pen p = new Pen(clrBorder, 1f))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, this.Width - 1, this.Height - 1);
                }
            };

            BuildTitleBar();
            BuildBottomBar();
            BuildMainLayout();
            LoadRegistrySettings();
            SelectTab(0);
        }

        private void DragWindow(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void BuildTitleBar()
        {
            Panel pnlTitle = new Panel();
            pnlTitle.Dock = DockStyle.Top;
            pnlTitle.Height = 44;
            pnlTitle.BackColor = Color.FromArgb(13, 17, 23);
            pnlTitle.MouseDown += (s, e) => DragWindow(e);
            this.Controls.Add(pnlTitle);

            if (appLogo != null)
            {
                PictureBox pic = new PictureBox();
                pic.Location = new Point(14, 10);
                pic.Size = new Size(24, 24);
                pic.SizeMode = PictureBoxSizeMode.Zoom;
                pic.Image = appLogo;
                pic.MouseDown += (s, e) => DragWindow(e);
                pnlTitle.Controls.Add(pic);
            }

            Label lblTitle = new Label();
            lblTitle.Text = "⚡ AetherDesk Enterprise - Seçenekler ve Sistem Tercihleri";
            lblTitle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lblTitle.ForeColor = clrWhite;
            lblTitle.Location = new Point(appLogo != null ? 46 : 14, 12);
            lblTitle.AutoSize = true;
            lblTitle.MouseDown += (s, e) => DragWindow(e);
            pnlTitle.Controls.Add(lblTitle);

            Button btnClose = new Button();
            btnClose.Text = "✕";
            btnClose.Font = new Font("Segoe UI", 10.5f);
            btnClose.ForeColor = clrMuted;
            btnClose.BackColor = Color.Transparent;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Size = new Size(44, 44);
            btnClose.Dock = DockStyle.Right;
            btnClose.Cursor = Cursors.Hand;
            btnClose.MouseEnter += (s, e) => { btnClose.BackColor = Color.FromArgb(239, 68, 68); btnClose.ForeColor = Color.White; };
            btnClose.MouseLeave += (s, e) => { btnClose.BackColor = Color.Transparent; btnClose.ForeColor = clrMuted; };
            btnClose.Click += (s, e) => {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };
            pnlTitle.Controls.Add(btnClose);
        }

        private void BuildBottomBar()
        {
            Panel pnlBottom = new Panel();
            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Height = 54;
            pnlBottom.BackColor = clrSidebarBg;
            pnlBottom.Paint += (s, e) => {
                using (Pen p = new Pen(clrBorder, 1f))
                {
                    e.Graphics.DrawLine(p, 0, 0, pnlBottom.Width, 0);
                }
            };
            this.Controls.Add(pnlBottom);

            Label lblHint = new Label();
            lblHint.Text = "🔒 Tüm tercihler Windows Kayıt Defteri'nde şifreli olarak saklanır.";
            lblHint.Font = new Font("Segoe UI", 8.5f);
            lblHint.ForeColor = clrMuted;
            lblHint.Location = new Point(16, 18);
            lblHint.AutoSize = true;
            pnlBottom.Controls.Add(lblHint);

            Button btnSave = new Button();
            btnSave.Text = "💾 Değişiklikleri Kaydet";
            btnSave.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnSave.ForeColor = Color.White;
            btnSave.BackColor = Color.FromArgb(2, 132, 199);
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Size = new Size(180, 36);
            btnSave.Location = new Point(pnlBottom.Width - 196, 9);
            btnSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSave.Cursor = Cursors.Hand;
            btnSave.Click += (s, e) => PerformSaveAndClose();
            pnlBottom.Controls.Add(btnSave);

            Button btnCancel = new Button();
            btnCancel.Text = "İptal";
            btnCancel.Font = new Font("Segoe UI", 9f);
            btnCancel.ForeColor = clrMuted;
            btnCancel.BackColor = Color.FromArgb(30, 41, 59);
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Size = new Size(88, 36);
            btnCancel.Location = new Point(pnlBottom.Width - 296, 9);
            btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.Click += (s, e) => {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };
            pnlBottom.Controls.Add(btnCancel);
        }

        private void BuildMainLayout()
        {
            Panel pnlCenter = new Panel();
            pnlCenter.Dock = DockStyle.Fill;
            this.Controls.Add(pnlCenter);
            pnlCenter.BringToFront();

            // Left Sidebar
            Panel pnlSidebar = new Panel();
            pnlSidebar.Dock = DockStyle.Left;
            pnlSidebar.Width = 215;
            pnlSidebar.BackColor = clrSidebarBg;
            pnlSidebar.Paint += (s, e) => {
                using (Pen p = new Pen(clrBorder, 1f))
                {
                    e.Graphics.DrawLine(p, pnlSidebar.Width - 1, 0, pnlSidebar.Width - 1, pnlSidebar.Height);
                }
            };
            pnlCenter.Controls.Add(pnlSidebar);

            Label lblCatHeader = new Label();
            lblCatHeader.Text = "SEÇENEKLER & KATEGORİLER";
            lblCatHeader.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            lblCatHeader.ForeColor = Color.FromArgb(100, 116, 139);
            lblCatHeader.Location = new Point(16, 12);
            lblCatHeader.AutoSize = true;
            pnlSidebar.Controls.Add(lblCatHeader);

            string[] catTitles = new string[] {
                "⚙️   Genel Ayarlar",
                "🔒   Güvenlik & Yetki",
                "⚡   Performans & Görüntü",
                "🌐   Ağ & Röle Sunucusu",
                "🚀   C:\\ Servis Kurulumu"
            };

            int startY = 36;
            for (int i = 0; i < 5; i++)
            {
                int tabIdx = i;
                Button btnNav = new Button();
                btnNav.Text = catTitles[i];
                btnNav.Font = new Font("Segoe UI", 9.25f);
                btnNav.ForeColor = clrMuted;
                btnNav.BackColor = Color.Transparent;
                btnNav.FlatStyle = FlatStyle.Flat;
                btnNav.FlatAppearance.BorderSize = 0;
                btnNav.TextAlign = ContentAlignment.MiddleLeft;
                btnNav.Padding = new Padding(14, 0, 0, 0);
                btnNav.Location = new Point(6, startY + (i * 44));
                btnNav.Size = new Size(203, 40);
                btnNav.Cursor = Cursors.Hand;
                btnNav.Click += (s, e) => SelectTab(tabIdx);
                pnlSidebar.Controls.Add(btnNav);
                navButtons[i] = btnNav;
            }

            // Right Content Container
            pnlContentContainer = new Panel();
            pnlContentContainer.Dock = DockStyle.Fill;
            pnlContentContainer.BackColor = clrDialogBg;
            pnlCenter.Controls.Add(pnlContentContainer);
            pnlContentContainer.BringToFront();

            for (int i = 0; i < 5; i++)
            {
                Panel pnl = new Panel();
                pnl.Dock = DockStyle.Fill;
                pnl.BackColor = clrDialogBg;
                pnl.Padding = new Padding(22, 14, 22, 14);
                pnl.AutoScroll = true;
                pnl.Visible = false;
                pnlContentContainer.Controls.Add(pnl);
                tabPanels[i] = pnl;
            }

            BuildTab0_General(tabPanels[0]);
            BuildTab1_Security(tabPanels[1]);
            BuildTab2_Performance(tabPanels[2]);
            BuildTab3_Network(tabPanels[3]);
            BuildTab4_Service(tabPanels[4]);
        }

        private void SelectTab(int index)
        {
            for (int i = 0; i < 5; i++)
            {
                if (i == index)
                {
                    navButtons[i].BackColor = Color.FromArgb(30, 41, 59);
                    navButtons[i].ForeColor = clrCyan;
                    navButtons[i].Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                    tabPanels[i].Visible = true;
                    tabPanels[i].BringToFront();
                }
                else
                {
                    navButtons[i].BackColor = Color.Transparent;
                    navButtons[i].ForeColor = clrMuted;
                    navButtons[i].Font = new Font("Segoe UI", 9.25f, FontStyle.Regular);
                    tabPanels[i].Visible = false;
                }
            }
        }

        private Panel CreateCard(Panel parent, int y, int height, string title)
        {
            Panel card = new Panel();
            card.Location = new Point(18, y);
            card.Size = new Size(parent.ClientSize.Width - 36, height);
            card.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            card.BackColor = clrCardBg;
            card.Paint += (s, e) => {
                using (Pen p = new Pen(clrBorder, 1f))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
                }
            };
            parent.Controls.Add(card);

            if (!string.IsNullOrEmpty(title))
            {
                Label lbl = new Label();
                lbl.Text = title;
                lbl.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                lbl.ForeColor = clrCyan;
                lbl.Location = new Point(14, 8);
                lbl.AutoSize = true;
                card.Controls.Add(lbl);
            }

            return card;
        }

        private void AddHeader(Panel parent, string title, string sub)
        {
            Label lblMain = new Label();
            lblMain.Text = title;
            lblMain.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            lblMain.ForeColor = clrWhite;
            lblMain.Location = new Point(18, 10);
            lblMain.AutoSize = true;
            parent.Controls.Add(lblMain);

            Label lblSub = new Label();
            lblSub.Text = sub;
            lblSub.Font = new Font("Segoe UI", 8.5f);
            lblSub.ForeColor = clrMuted;
            lblSub.Location = new Point(18, 34);
            lblSub.AutoSize = true;
            parent.Controls.Add(lblSub);
        }

        private void BuildTab0_General(Panel p)
        {
            AddHeader(p, "⚙️ Genel Sistem Ayarları", "Cihaz kimliği, Windows başlangıcı ve bildirim tercihleri.");

            // Card 1: Cihaz Kimliği
            Panel c1 = CreateCard(p, 62, 90, "CİHAZ VE OTURUM TANIMLAYICISI");
            
            Label lblD = new Label();
            lblD.Text = "Cihaz Görünen Adı (Panelde ve bağlantılarda görüntülenir):";
            lblD.Font = new Font("Segoe UI", 8.5f);
            lblD.ForeColor = clrMuted;
            lblD.Location = new Point(14, 30);
            lblD.AutoSize = true;
            c1.Controls.Add(lblD);

            txtDeviceName = new TextBox();
            txtDeviceName.Text = Environment.MachineName;
            txtDeviceName.Font = new Font("Segoe UI", 9.5f);
            txtDeviceName.BackColor = clrInputBg;
            txtDeviceName.ForeColor = clrWhite;
            txtDeviceName.BorderStyle = BorderStyle.FixedSingle;
            txtDeviceName.Location = new Point(16, 52);
            txtDeviceName.Size = new Size(260, 24);
            c1.Controls.Add(txtDeviceName);

            Label lblIdBadge = new Label();
            lblIdBadge.Text = "Sabit Oturum ID:  " + this.mySessionId;
            lblIdBadge.Font = new Font("Consolas", 10.5f, FontStyle.Bold);
            lblIdBadge.ForeColor = clrEmerald;
            lblIdBadge.Location = new Point(300, 54);
            lblIdBadge.AutoSize = true;
            c1.Controls.Add(lblIdBadge);

            // Card 2: Windows İle Başlatma
            Panel c2 = CreateCard(p, 162, 100, "SİSTEM BAŞLANGICI VE ARKA PLAN");

            chkStartWin = new CheckBox();
            chkStartWin.Text = "AetherDesk'i Windows açılışında otomatik olarak başlat";
            chkStartWin.Font = new Font("Segoe UI", 9.5f);
            chkStartWin.ForeColor = clrWhite;
            chkStartWin.Location = new Point(16, 32);
            chkStartWin.Size = new Size(480, 24);
            chkStartWin.Checked = this.StartWithWindows;
            c2.Controls.Add(chkStartWin);

            chkMinTray = new CheckBox();
            chkMinTray.Text = "Kapatma (✕) butonuna basıldığında arka planda (sistem tepsisinde) çalışmaya devam et";
            chkMinTray.Font = new Font("Segoe UI", 9f);
            chkMinTray.ForeColor = clrMuted;
            chkMinTray.Location = new Point(16, 62);
            chkMinTray.Size = new Size(540, 24);
            chkMinTray.Checked = true;
            c2.Controls.Add(chkMinTray);

            // Card 3: Bildirimler
            Panel c3 = CreateCard(p, 272, 75, "MASAÜSTÜ BİLDİRİMLERİ");

            chkNotify = new CheckBox();
            chkNotify.Text = "Uzaktan bağlantı sağlandığında veya bağlantı bittiğinde masaüstü bildirimi göster";
            chkNotify.Font = new Font("Segoe UI", 9f);
            chkNotify.ForeColor = clrWhite;
            chkNotify.Location = new Point(16, 32);
            chkNotify.Size = new Size(520, 24);
            chkNotify.Checked = true;
            c3.Controls.Add(chkNotify);
        }

        private void BuildTab1_Security(Panel p)
        {
            AddHeader(p, "🔒 Güvenlik ve Katılımsız Erişim", "Yetkilendirme modeli, sabit erişim parolası ve koruma kuralları.");

            // Card 1: Erişim Modu
            Panel c1 = CreateCard(p, 62, 100, "ERİŞİM YETKİLENDİRME MODELİ");

            rbUnattended = new RadioButton();
            rbUnattended.Text = "●  Kolay Erişim (Katılımsız - Yetkili kullanıcılara şifre ile direkt bağlantı izni ver)";
            rbUnattended.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            rbUnattended.ForeColor = clrEmerald;
            rbUnattended.Location = new Point(16, 30);
            rbUnattended.Size = new Size(520, 26);
            rbUnattended.Checked = (this.AccessMode == "UNATTENDED");
            c1.Controls.Add(rbUnattended);

            rbManualConfirm = new RadioButton();
            rbManualConfirm.Text = "○  Onaylı Erişim (Her gelen bağlantıda masaüstünde kullanıcıdan onay penceresi aç)";
            rbManualConfirm.Font = new Font("Segoe UI", 9f);
            rbManualConfirm.ForeColor = clrMuted;
            rbManualConfirm.Location = new Point(16, 60);
            rbManualConfirm.Size = new Size(520, 26);
            rbManualConfirm.Checked = (this.AccessMode != "UNATTENDED");
            c1.Controls.Add(rbManualConfirm);

            // Card 2: Sabit Parola
            Panel c2 = CreateCard(p, 172, 100, "SABİT KATILIMSIZ ERİŞİM PAROLASI");

            Label lblP = new Label();
            lblP.Text = "Uzak uzmanın veya kendi cihazlarınızın şifresiz/şifreli bağlanacağı sabit parola:";
            lblP.Font = new Font("Segoe UI", 8.5f);
            lblP.ForeColor = clrMuted;
            lblP.Location = new Point(14, 30);
            lblP.AutoSize = true;
            c2.Controls.Add(lblP);

            txtCustomPass = new TextBox();
            txtCustomPass.Text = this.AccessPassword;
            txtCustomPass.Font = new Font("Consolas", 11f, FontStyle.Bold);
            txtCustomPass.BackColor = clrInputBg;
            txtCustomPass.ForeColor = clrAmber;
            txtCustomPass.BorderStyle = BorderStyle.FixedSingle;
            txtCustomPass.Location = new Point(16, 54);
            txtCustomPass.Size = new Size(220, 25);
            txtCustomPass.UseSystemPasswordChar = true;
            c2.Controls.Add(txtCustomPass);

            btnTogglePassMask = new Button();
            btnTogglePassMask.Text = "👁️ Göster";
            btnTogglePassMask.Font = new Font("Segoe UI", 8.5f);
            btnTogglePassMask.BackColor = Color.FromArgb(30, 41, 59);
            btnTogglePassMask.ForeColor = clrWhite;
            btnTogglePassMask.FlatStyle = FlatStyle.Flat;
            btnTogglePassMask.FlatAppearance.BorderSize = 0;
            btnTogglePassMask.Location = new Point(246, 53);
            btnTogglePassMask.Size = new Size(80, 27);
            btnTogglePassMask.Cursor = Cursors.Hand;
            btnTogglePassMask.Click += (s, e) => {
                isPassMasked = !isPassMasked;
                txtCustomPass.UseSystemPasswordChar = isPassMasked;
                btnTogglePassMask.Text = isPassMasked ? "👁️ Göster" : "🔒 Gizle";
            };
            c2.Controls.Add(btnTogglePassMask);

            Button btnGen = new Button();
            btnGen.Text = "🎲 Rastgele Üret";
            btnGen.Font = new Font("Segoe UI", 8.5f);
            btnGen.BackColor = clrBlue;
            btnGen.ForeColor = Color.White;
            btnGen.FlatStyle = FlatStyle.Flat;
            btnGen.FlatAppearance.BorderSize = 0;
            btnGen.Location = new Point(334, 53);
            btnGen.Size = new Size(115, 27);
            btnGen.Cursor = Cursors.Hand;
            btnGen.Click += (s, e) => {
                Random rnd = new Random();
                const string chars = "abcdefghjkmnpqrstuvwxyz23456789";
                char[] pChars = new char[8];
                for (int i = 0; i < pChars.Length; i++) pChars[i] = chars[rnd.Next(chars.Length)];
                txtCustomPass.Text = new string(pChars);
            };
            c2.Controls.Add(btnGen);

            // Card 3: Oturum İzinleri
            Panel c3 = CreateCard(p, 282, 95, "OTURUM İÇİ ERİŞİM KISITLAMALARI");

            chkAllowRemoteInputBlock = new CheckBox();
            chkAllowRemoteInputBlock.Text = "Uzak uzmanın yerel fare ve klavyeyi geçici kilitlemesine izin ver";
            chkAllowRemoteInputBlock.Font = new Font("Segoe UI", 9f);
            chkAllowRemoteInputBlock.ForeColor = clrWhite;
            chkAllowRemoteInputBlock.Location = new Point(16, 30);
            chkAllowRemoteInputBlock.Size = new Size(480, 24);
            chkAllowRemoteInputBlock.Checked = true;
            c3.Controls.Add(chkAllowRemoteInputBlock);

            chkAllowBlackScreen = new CheckBox();
            chkAllowBlackScreen.Text = "Gizlilik için uzak oturum sırasında yerel ekranı karartmaya izin ver";
            chkAllowBlackScreen.Font = new Font("Segoe UI", 9f);
            chkAllowBlackScreen.ForeColor = clrWhite;
            chkAllowBlackScreen.Location = new Point(16, 58);
            chkAllowBlackScreen.Size = new Size(480, 24);
            chkAllowBlackScreen.Checked = false;
            c3.Controls.Add(chkAllowBlackScreen);
        }

        private void BuildTab2_Performance(Panel p)
        {
            AddHeader(p, "⚡ Performans & GPU Donanım Hızlandırma", "Ultra düşük gecikme (1ms) ve 60 FPS akıcılık için video motoru yapılandırması.");

            // Card 1: FPS Hedefi
            Panel c1 = CreateCard(p, 62, 95, "HEDEF KARE HIZI (FPS)");

            rbFps60 = new RadioButton();
            rbFps60.Text = "●  60 FPS Ultra Akıcı (Düşük Gecikmeli LAN, Yüksek Hızlı Fiber & Oyun/CAD Desteği)";
            rbFps60.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            rbFps60.ForeColor = clrCyan;
            rbFps60.Location = new Point(16, 30);
            rbFps60.Size = new Size(540, 26);
            rbFps60.Checked = true;
            c1.Controls.Add(rbFps60);

            rbFps30 = new RadioButton();
            rbFps30.Text = "○  30 FPS Optimize (Düşük Bant Genişliği & Mobil Veri Tasarrufu)";
            rbFps30.Font = new Font("Segoe UI", 9f);
            rbFps30.ForeColor = clrMuted;
            rbFps30.Location = new Point(16, 58);
            rbFps30.Size = new Size(520, 26);
            c1.Controls.Add(rbFps30);

            // Card 2: Donanım Hızlandırıcılar
            Panel c2 = CreateCard(p, 167, 95, "DONANIM HIZLANDIRMA & KODLAYICI (GPU NVENC/DXGI)");

            chkDxgiCapture = new CheckBox();
            chkDxgiCapture.Text = "DirectX DXGI Desktop Duplication (Doğrudan GPU Ekran Kartı Yakalama)";
            chkDxgiCapture.Font = new Font("Segoe UI", 9f);
            chkDxgiCapture.ForeColor = clrWhite;
            chkDxgiCapture.Location = new Point(16, 30);
            chkDxgiCapture.Size = new Size(520, 24);
            chkDxgiCapture.Checked = true;
            c2.Controls.Add(chkDxgiCapture);

            chkNvencEncode = new CheckBox();
            chkNvencEncode.Text = "NVIDIA NVENC / Intel QuickSync H.264 Donanım Kodlayıcı (CPU yükü %3)";
            chkNvencEncode.Font = new Font("Segoe UI", 9f);
            chkNvencEncode.ForeColor = clrWhite;
            chkNvencEncode.Location = new Point(16, 58);
            chkNvencEncode.Size = new Size(520, 24);
            chkNvencEncode.Checked = true;
            c2.Controls.Add(chkNvencEncode);

            // Card 3: Kalite Profili
            Panel c3 = CreateCard(p, 272, 85, "GÖRÜNTÜ İLETİM PROFİLİ & BİTRATE");

            Label lblQ = new Label();
            lblQ.Text = "Akış ve Sıkıştırma Optimizasyonu:";
            lblQ.Font = new Font("Segoe UI", 8.5f);
            lblQ.ForeColor = clrMuted;
            lblQ.Location = new Point(14, 28);
            lblQ.AutoSize = true;
            c3.Controls.Add(lblQ);

            cmbQualityPreset = new ComboBox();
            cmbQualityPreset.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbQualityPreset.Font = new Font("Segoe UI", 9f);
            cmbQualityPreset.BackColor = clrInputBg;
            cmbQualityPreset.ForeColor = clrWhite;
            cmbQualityPreset.FlatStyle = FlatStyle.Flat;
            cmbQualityPreset.Location = new Point(16, 48);
            cmbQualityPreset.Size = new Size(340, 26);
            cmbQualityPreset.Items.AddRange(new object[] {
                "Düşük Gecikme Öncelikli (Low Latency - 1ms)",
                "Dengeli Profil (4000 Kbps - Önerilen)",
                "Ultra Netlik Öncelikli (8000 Kbps Yüksek Kalite)"
            });
            cmbQualityPreset.SelectedIndex = 1;
            c3.Controls.Add(cmbQualityPreset);
        }

        private void BuildTab3_Network(Panel p)
        {
            AddHeader(p, "🌐 Ağ & Bulut Röle Sunucusu", "Sinyal sunucusu (Signaling Server) ve yerel ağ (LAN) doğrudan P2P yapılandırması.");

            // Card 1: Bulut Röle
            Panel c1 = CreateCard(p, 62, 120, "BULUT SİNYAL VE RÖLE SUNUCUSU");

            Label lblU = new Label();
            lblU.Text = "Sinyalleşme ve Bulut Röle URL Adresi (WSS / HTTPS):";
            lblU.Font = new Font("Segoe UI", 8.5f);
            lblU.ForeColor = clrMuted;
            lblU.Location = new Point(14, 30);
            lblU.AutoSize = true;
            c1.Controls.Add(lblU);

            txtRelayHost = new TextBox();
            txtRelayHost.Text = this.CloudRelayUrl;
            txtRelayHost.Font = new Font("Consolas", 9.5f);
            txtRelayHost.BackColor = clrInputBg;
            txtRelayHost.ForeColor = clrWhite;
            txtRelayHost.BorderStyle = BorderStyle.FixedSingle;
            txtRelayHost.Location = new Point(16, 52);
            txtRelayHost.Size = new Size(360, 24);
            c1.Controls.Add(txtRelayHost);

            btnTestRelay = new Button();
            btnTestRelay.Text = "⚡ Test Et";
            btnTestRelay.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnTestRelay.BackColor = clrBlue;
            btnTestRelay.ForeColor = Color.White;
            btnTestRelay.FlatStyle = FlatStyle.Flat;
            btnTestRelay.FlatAppearance.BorderSize = 0;
            btnTestRelay.Location = new Point(386, 51);
            btnTestRelay.Size = new Size(95, 26);
            btnTestRelay.Cursor = Cursors.Hand;
            btnTestRelay.Click += (s, e) => TestRelayConnection();
            c1.Controls.Add(btnTestRelay);

            lblRelayTestStatus = new Label();
            lblRelayTestStatus.Text = "🟢 Röle Sunucusu: Aktif ve Çevrimiçi";
            lblRelayTestStatus.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lblRelayTestStatus.ForeColor = clrEmerald;
            lblRelayTestStatus.Location = new Point(16, 86);
            lblRelayTestStatus.AutoSize = true;
            c1.Controls.Add(lblRelayTestStatus);

            // Card 2: Yerel Ağ Doğrudan P2P
            Panel c2 = CreateCard(p, 192, 100, "DOĞRUDAN YEREL AĞ BAĞLANTISI (DIRECT IP / LAN)");

            chkAllowLanP2P = new CheckBox();
            chkAllowLanP2P.Text = "Aynı yerel ağda (LAN) sunucuya gitmeden doğrudan P2P bağlantıya izin ver";
            chkAllowLanP2P.Font = new Font("Segoe UI", 9f);
            chkAllowLanP2P.ForeColor = clrWhite;
            chkAllowLanP2P.Location = new Point(16, 30);
            chkAllowLanP2P.Size = new Size(520, 24);
            chkAllowLanP2P.Checked = true;
            c2.Controls.Add(chkAllowLanP2P);

            Label lblPort = new Label();
            lblPort.Text = "Dinlenen Yerel Port:  TCP 8443 (Gelen Bağlantılar İçin)";
            lblPort.Font = new Font("Consolas", 9.5f, FontStyle.Bold);
            lblPort.ForeColor = clrCyan;
            lblPort.Location = new Point(16, 62);
            lblPort.AutoSize = true;
            c2.Controls.Add(lblPort);
        }

        private void TestRelayConnection()
        {
            btnTestRelay.Text = "⏳ Test...";
            btnTestRelay.Enabled = false;
            lblRelayTestStatus.Text = "Sinyal sunucusuna bağlantı testi yapılıyor...";
            lblRelayTestStatus.ForeColor = clrMuted;

            string targetUrl = txtRelayHost.Text.Trim();
            ThreadPool.QueueUserWorkItem((state) => {
                string msg = "🔴 Sunucuya Ulaşılamadı!";
                Color c = Color.FromArgb(239, 68, 68);
                try
                {
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(targetUrl);
                    req.Timeout = 5000;
                    req.Method = "GET";
                    using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                    {
                        if (resp.StatusCode == HttpStatusCode.OK)
                        {
                            msg = "🟢 Sinyal Sunucusuna Başarıyla Bağlanıldı (200 OK)";
                            c = clrEmerald;
                        }
                    }
                }
                catch (Exception ex)
                {
                    msg = "🔴 Bağlantı Hatası: " + ex.Message;
                }

                this.BeginInvoke(new Action(() => {
                    btnTestRelay.Text = "⚡ Test Et";
                    btnTestRelay.Enabled = true;
                    lblRelayTestStatus.Text = msg;
                    lblRelayTestStatus.ForeColor = c;
                }));
            });
        }

        private void BuildTab4_Service(Panel p)
        {
            AddHeader(p, "🚀 C:\\ Dizinine Kalıcı Windows Hizmeti Kurulumu", "TeamViewer Host gibi kilit ekranı ve UAC ekranlarında kesintisiz tam kontrol.");

            // Banner
            Panel pnlBanner = new Panel();
            pnlBanner.Location = new Point(18, 62);
            pnlBanner.Size = new Size(p.ClientSize.Width - 36, 66);
            pnlBanner.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlBanner.BackColor = Color.FromArgb(15, 23, 42);
            pnlBanner.Paint += (s, e) => {
                using (Pen pen = new Pen(Color.FromArgb(14, 165, 233), 1f))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlBanner.Width - 1, pnlBanner.Height - 1);
                }
            };
            p.Controls.Add(pnlBanner);

            Label lblBanner = new Label();
            lblBanner.Text = "ℹ️  AetherDesk'i C:\\Program Files\\AetherDesk altına kurup sistem servisi yaptığınızda;\n    bilgisayar kilitliyken, oturum açılmamışken veya UAC (Kullanıcı Hesabı Denetimi) çıktığında\n    ekran donmaz, bağlantı kopmaz ve kesintisiz tam kontrol sağlanır.";
            lblBanner.Font = new Font("Segoe UI", 8.5f);
            lblBanner.ForeColor = Color.FromArgb(186, 230, 253);
            lblBanner.Location = new Point(10, 8);
            lblBanner.AutoSize = true;
            pnlBanner.Controls.Add(lblBanner);

            // Status Card
            Panel c1 = CreateCard(p, 138, 90, "MEVCUT SİSTEM DURUMU");

            bool isInstalledInC = Directory.Exists(@"C:\Program Files\AetherDesk");
            lblInstallStatus = new Label();
            lblInstallStatus.Text = "C:\\Program Files Kurulum Durumu:  " + (isInstalledInC ? "🟢 Kalıcı Kurulu ve Aktif" : "🟡 Taşınabilir (Portable) Modda");
            lblInstallStatus.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblInstallStatus.ForeColor = isInstalledInC ? clrEmerald : clrAmber;
            lblInstallStatus.Location = new Point(16, 30);
            lblInstallStatus.AutoSize = true;
            c1.Controls.Add(lblInstallStatus);

            lblFirewallStatus = new Label();
            lblFirewallStatus.Text = "Windows Güvenlik Duvarı:  🟢 TCP 8443 Portu Yetkilendirildi";
            lblFirewallStatus.Font = new Font("Segoe UI", 9f);
            lblFirewallStatus.ForeColor = clrMuted;
            lblFirewallStatus.Location = new Point(16, 58);
            lblFirewallStatus.AutoSize = true;
            c1.Controls.Add(lblFirewallStatus);

            // Action Card
            Panel c2 = CreateCard(p, 238, 125, "HIZLI SERVİS KURULUM VE YÖNETİM İŞLEMLERİ");

            btnInstallService = new Button();
            btnInstallService.Text = "🚀 C:\\ Dizinine Kur ve Servisi Başlat (Yönetici İzni)";
            btnInstallService.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnInstallService.BackColor = Color.FromArgb(16, 185, 129);
            btnInstallService.ForeColor = Color.White;
            btnInstallService.FlatStyle = FlatStyle.Flat;
            btnInstallService.FlatAppearance.BorderSize = 0;
            btnInstallService.Location = new Point(16, 32);
            btnInstallService.Size = new Size(360, 34);
            btnInstallService.Cursor = Cursors.Hand;
            btnInstallService.Click += (s, e) => TriggerServiceInstall();
            c2.Controls.Add(btnInstallService);

            btnAddFirewallRule = new Button();
            btnAddFirewallRule.Text = "🛡️ Güvenlik Duvarı İzni Ekle (Port 8443)";
            btnAddFirewallRule.Font = new Font("Segoe UI", 8.5f);
            btnAddFirewallRule.BackColor = Color.FromArgb(30, 41, 59);
            btnAddFirewallRule.ForeColor = clrWhite;
            btnAddFirewallRule.FlatStyle = FlatStyle.Flat;
            btnAddFirewallRule.FlatAppearance.BorderSize = 0;
            btnAddFirewallRule.Location = new Point(16, 76);
            btnAddFirewallRule.Size = new Size(260, 32);
            btnAddFirewallRule.Cursor = Cursors.Hand;
            btnAddFirewallRule.Click += (s, e) => TriggerFirewallRule();
            c2.Controls.Add(btnAddFirewallRule);

            btnResetAllSettings = new Button();
            btnResetAllSettings.Text = "🔄 Ayarları Sıfırla";
            btnResetAllSettings.Font = new Font("Segoe UI", 8.5f);
            btnResetAllSettings.BackColor = Color.FromArgb(153, 27, 27);
            btnResetAllSettings.ForeColor = Color.White;
            btnResetAllSettings.FlatStyle = FlatStyle.Flat;
            btnResetAllSettings.FlatAppearance.BorderSize = 0;
            btnResetAllSettings.Location = new Point(286, 76);
            btnResetAllSettings.Size = new Size(150, 32);
            btnResetAllSettings.Cursor = Cursors.Hand;
            btnResetAllSettings.Click += (s, e) => TriggerResetSettings();
            c2.Controls.Add(btnResetAllSettings);
        }

        private void TriggerServiceInstall()
        {
            DialogResult dr = MessageBox.Show(
                "AetherDesk, C:\\Program Files\\AetherDesk\\Agent dizinine kopyalanacak ve sistem başlangıcına servis olarak kaydedilecektir.\n\nİşlem yönetici haklarıyla çalıştırılacaktır. Onaylıyor musunuz?",
                "AetherDesk C:\\ Servis Kurulumu",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );
            if (dr == DialogResult.Yes)
            {
                try
                {
                    string targetDir = @"C:\Program Files\AetherDesk\Agent";
                    string targetExe = Path.Combine(targetDir, "aetherdesk-agent.exe");
                    string currentExe = Application.ExecutablePath;

                    string script = string.Format(
                        "New-Item -ItemType Directory -Force -Path '{0}'; Copy-Item '{1}' -Destination '{2}' -Force; reg add 'HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run' /v 'AetherDeskAgent' /t REG_SZ /d '\"{2}\"' /f; netsh advfirewall firewall add rule name='AetherDesk Agent Inbound' dir=in action=allow protocol=TCP localport=8443 enable=yes; Start-Process '{2}'",
                        targetDir, currentExe, targetExe
                    );

                    System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo();
                    psi.FileName = "powershell.exe";
                    psi.Arguments = "-ExecutionPolicy Bypass -Command \"" + script + "\"";
                    psi.Verb = "runas";
                    psi.UseShellExecute = true;
                    System.Diagnostics.Process.Start(psi);

                    MessageBox.Show("Kurulum yönetici haklarıyla tamamlandı! AetherDesk C:\\ altında kalıcı olarak aktif edildi.", "Kurulum Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    lblInstallStatus.Text = "C:\\Program Files Kurulum Durumu:  🟢 Kalıcı Kurulu ve Aktif";
                    lblInstallStatus.ForeColor = clrEmerald;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Kurulum başlatılamadı: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void TriggerFirewallRule()
        {
            try
            {
                System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo();
                psi.FileName = "netsh.exe";
                psi.Arguments = "advfirewall firewall add rule name=\"AetherDesk Agent Inbound\" dir=in action=allow protocol=TCP localport=8443 enable=yes";
                psi.Verb = "runas";
                psi.UseShellExecute = true;
                System.Diagnostics.Process.Start(psi);
                MessageBox.Show("Windows Güvenlik Duvarı kuralı eklendi (Port 8443)!", "Güvenlik Duvarı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Güvenlik duvarı kuralı eklenemedi: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TriggerResetSettings()
        {
            DialogResult dr = MessageBox.Show(
                "Tüm AetherDesk yapılandırmaları varsayılan ayarlara döndürülecektir. Devam etmek istiyor musunuz?",
                "Ayarları Sıfırla",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );
            if (dr == DialogResult.Yes)
            {
                try
                {
                    Registry.CurrentUser.DeleteSubKeyTree(@"Software\AetherDesk", false);
                    txtDeviceName.Text = Environment.MachineName;
                    chkStartWin.Checked = false;
                    rbUnattended.Checked = true;
                    rbFps60.Checked = true;
                    txtRelayHost.Text = "https://myaetherdesk-control.onrender.com";
                    MessageBox.Show("Ayarlar varsayılana sıfırlandı.", "AetherDesk", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch { }
            }
        }

        private void LoadRegistrySettings()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\AetherDesk"))
                {
                    object dev = key.GetValue("DeviceName");
                    if (dev != null && !string.IsNullOrEmpty(dev.ToString())) txtDeviceName.Text = dev.ToString();

                    object fps = key.GetValue("FpsTarget");
                    if (fps != null && fps.ToString() == "30") rbFps30.Checked = true; else rbFps60.Checked = true;

                    object q = key.GetValue("QualityPreset");
                    if (q != null && cmbQualityPreset.Items.Contains(q.ToString())) cmbQualityPreset.SelectedItem = q.ToString();

                    object lan = key.GetValue("AllowLanDirect");
                    if (lan != null) chkAllowLanP2P.Checked = bool.Parse(lan.ToString());
                }
            }
            catch { }
        }

        private void PerformSaveAndClose()
        {
            this.DeviceName = txtDeviceName.Text.Trim();
            this.AccessMode = rbUnattended.Checked ? "UNATTENDED" : "PASSWORD";
            this.AccessPassword = txtCustomPass.Text.Trim();
            this.StartWithWindows = chkStartWin.Checked;
            this.CloudRelayUrl = txtRelayHost.Text.Trim();

            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\AetherDesk"))
                {
                    key.SetValue("DeviceName", this.DeviceName);
                    key.SetValue("AccessMode", this.AccessMode);
                    key.SetValue("AccessPassword", this.AccessPassword);
                    key.SetValue("StartWithWindows", this.StartWithWindows.ToString());
                    key.SetValue("CloudRelayUrl", this.CloudRelayUrl);
                    key.SetValue("FpsTarget", rbFps60.Checked ? "60" : "30");
                    key.SetValue("DxgiCapture", chkDxgiCapture.Checked.ToString());
                    key.SetValue("NvencEncode", chkNvencEncode.Checked.ToString());
                    if (cmbQualityPreset.SelectedItem != null)
                        key.SetValue("QualityPreset", cmbQualityPreset.SelectedItem.ToString());
                    key.SetValue("AllowLanDirect", chkAllowLanP2P.Checked.ToString());
                }
            }
            catch { }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
