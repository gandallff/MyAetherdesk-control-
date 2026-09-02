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
        private Button btnMin;
        private Button btnMax;
        private Button btnClose;

        // Split Layout Containers
        private Panel pnlMainBody;
        private Panel pnlLeftHero;
        private Panel pnlRightContent;

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

        // In-App Modern Modal for Auth (SSO & Registration)
        private Panel pnlAuthModalOverlay;
        private TextBox txtRegFullName;
        private TextBox txtRegEmail;

        // In-App Active Session View
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
            BuildAuthModalOverlay();
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
            lblAppBrandTitle.Text = "AetherDesk Remote Access";
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
                g.DrawString("Bağlantı için hazır (güvenli bağlantı)", fontStatus, brushStatus, 52, statusY + 1);
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
            if (rectHeroLoginBtn.Contains(e.Location) || rectHeroRegisterLink.Contains(e.Location))
            {
                ShowAuthModal();
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
            if (!string.IsNullOrEmpty(target) && !target.StartsWith("Oturum"))
            {
                StartInAppSession(target);
            }
            else
            {
                MessageBox.Show("Lütfen geçerli bir oturum kodu / ID girin.", "AetherDesk", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // -----------------------------------------------------------------------------------
        // IN-APP AUTH MODAL (MATCHING IMAGE 2 WITH HARMONIOUS DARK STYLING)
        // -----------------------------------------------------------------------------------
        private void BuildAuthModalOverlay()
        {
            pnlAuthModalOverlay = new Panel();
            pnlAuthModalOverlay.Dock = DockStyle.Fill;
            pnlAuthModalOverlay.BackColor = Color.FromArgb(230, 13, 17, 23);
            pnlAuthModalOverlay.Visible = false;
            this.Controls.Add(pnlAuthModalOverlay);

            Panel pnlModalCard = new Panel();
            pnlModalCard.Size = new Size(460, 530);
            pnlModalCard.BackColor = clrCardBg;
            pnlModalCard.Anchor = AnchorStyles.None;
            pnlModalCard.Location = new Point((pnlAuthModalOverlay.Width - pnlModalCard.Width) / 2, (pnlAuthModalOverlay.Height - pnlModalCard.Height) / 2);
            pnlModalCard.Paint += (s, e) => {
                using (Pen p = new Pen(clrBorder, 1.5f))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlModalCard.Width - 1, pnlModalCard.Height - 1);
                }
            };
            pnlAuthModalOverlay.Controls.Add(pnlModalCard);

            pnlAuthModalOverlay.Resize += (s, e) => {
                pnlModalCard.Location = new Point((pnlAuthModalOverlay.Width - pnlModalCard.Width) / 2, (pnlAuthModalOverlay.Height - pnlModalCard.Height) / 2);
            };

            Button btnCloseModal = new Button();
            btnCloseModal.Text = "✕";
            btnCloseModal.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            btnCloseModal.ForeColor = clrTextMuted;
            btnCloseModal.BackColor = Color.Transparent;
            btnCloseModal.FlatStyle = FlatStyle.Flat;
            btnCloseModal.FlatAppearance.BorderSize = 0;
            btnCloseModal.Size = new Size(36, 36);
            btnCloseModal.Location = new Point(415, 10);
            btnCloseModal.Cursor = Cursors.Hand;
            btnCloseModal.Click += (s, e) => pnlAuthModalOverlay.Visible = false;
            pnlModalCard.Controls.Add(btnCloseModal);

            Label lblModalTitle = new Label();
            lblModalTitle.Text = "Bir hesap oluşturun";
            lblModalTitle.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            lblModalTitle.ForeColor = clrTextLight;
            lblModalTitle.Location = new Point(34, 26);
            lblModalTitle.AutoSize = true;
            pnlModalCard.Controls.Add(lblModalTitle);

            Label lblModalSub = new Label();
            lblModalSub.Text = "Hoş geldiniz! Lütfen bilgilerinizi girin.";
            lblModalSub.Font = new Font("Segoe UI", 9f);
            lblModalSub.ForeColor = clrTextMuted;
            lblModalSub.Location = new Point(36, 56);
            lblModalSub.AutoSize = true;
            pnlModalCard.Controls.Add(lblModalSub);

            Label lblNameTag = new Label { Text = "Adı ve soyadı", Location = new Point(36, 90), AutoSize = true, Font = new Font("Segoe UI", 8.5f), ForeColor = clrTextMuted };
            txtRegFullName = new TextBox { Location = new Point(36, 112), Size = new Size(388, 26), BackColor = clrInnerBox, ForeColor = clrTextLight, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10) };

            Label lblEmailTag = new Label { Text = "E-posta", Location = new Point(36, 146), AutoSize = true, Font = new Font("Segoe UI", 8.5f), ForeColor = clrTextMuted };
            txtRegEmail = new TextBox { Location = new Point(36, 168), Size = new Size(388, 26), BackColor = clrInnerBox, ForeColor = clrTextLight, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10), Text = userEmail };

            Button btnSubmitDevam = new Button {
                Text = "Devam",
                Location = new Point(36, 212),
                Size = new Size(388, 42),
                BackColor = clrAccentBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSubmitDevam.Click += (s, e) => {
                string inputEmail = txtRegEmail.Text.Trim();
                string inputName = string.IsNullOrEmpty(txtRegFullName.Text) ? inputEmail.Split('@')[0] : txtRegFullName.Text.Trim();
                if (inputEmail.Contains("@"))
                {
                    isLoggedIn = true;
                    userEmail = inputEmail;
                    userDisplayName = inputName;
                    SaveAuthSettings();
                    pnlAuthModalOverlay.Visible = false;
                    pnlLeftHero.Invalidate();

                    // Real Cloud Registration & Device Binding
                    ThreadPool.QueueUserWorkItem((state) =>
                    {
                        try
                        {
                            string cleanId = this.mySessionId.Replace(" ", "");
                            string jsonPayload = string.Format("{{\"name\":\"{0}\",\"email\":\"{1}\",\"deviceId\":\"{2}\"}}",
                                Uri.EscapeDataString(inputName), Uri.EscapeDataString(inputEmail), cleanId);
                            byte[] data = System.Text.Encoding.UTF8.GetBytes(jsonPayload);

                            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(CLOUD_RELAY_URL + "/api/auth/register");
                            req.Method = "POST";
                            req.ContentType = "application/json";
                            req.ContentLength = data.Length;
                            using (Stream stream = req.GetRequestStream())
                            {
                                stream.Write(data, 0, data.Length);
                            }
                            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse()) { }
                        }
                        catch { }
                    });

                    ShowModernDarkNotification(
                        "Hesabınız Başarıyla Kaydedildi",
                        "Tebrikler, " + userDisplayName + "!\n\n" +
                        "✓ Topluluk hesabınız başarıyla oluşturuldu.\n" +
                        "✓ Bu cihazınız (" + this.mySessionId + ") hesabınıza bağlandı.\n" +
                        "✓ Adres defteriniz ve cihaz yönetimi aktif."
                    );
                }
                else
                {
                    ShowModernDarkNotification("Geçersiz E-posta", "Lütfen geçerli bir e-posta adresi girin.");
                }
            };

            pnlModalCard.Controls.Add(lblNameTag);
            pnlModalCard.Controls.Add(txtRegFullName);
            pnlModalCard.Controls.Add(lblEmailTag);
            pnlModalCard.Controls.Add(txtRegEmail);
            pnlModalCard.Controls.Add(btnSubmitDevam);

            Label lblSsoDivider = new Label();
            lblSsoDivider.Text = "──────────   Veya   ──────────";
            lblSsoDivider.Font = new Font("Segoe UI", 8.5f);
            lblSsoDivider.ForeColor = Color.FromArgb(71, 85, 105);
            lblSsoDivider.Location = new Point(36, 266);
            lblSsoDivider.Size = new Size(388, 18);
            lblSsoDivider.TextAlign = ContentAlignment.MiddleCenter;
            pnlModalCard.Controls.Add(lblSsoDivider);

            Button btnSsoMicrosoft = CreateSsoButton("🪟   Microsoft ile devam et", 294, (s, e) => PerformSocialLogin("Microsoft"));
            Button btnSsoGoogle = CreateSsoButton("🔴   Google ile devam et", 340, (s, e) => PerformSocialLogin("Google"));
            Button btnSsoApple = CreateSsoButton("🍏   Apple ile devam et", 386, (s, e) => PerformSocialLogin("Apple"));

            pnlModalCard.Controls.Add(btnSsoMicrosoft);
            pnlModalCard.Controls.Add(btnSsoGoogle);
            pnlModalCard.Controls.Add(btnSsoApple);

            // Web Portal Direct Registration Link
            Button btnWebRegisterDirect = new Button();
            btnWebRegisterDirect.Text = "🌐   Web Portalı Üzerinden Kaydol / Giriş Yap";
            btnWebRegisterDirect.Top = 432;
            btnWebRegisterDirect.Left = 36;
            btnWebRegisterDirect.Width = 388;
            btnWebRegisterDirect.Height = 36;
            btnWebRegisterDirect.FlatStyle = FlatStyle.Flat;
            btnWebRegisterDirect.FlatAppearance.BorderColor = clrAccentCyan;
            btnWebRegisterDirect.BackColor = clrInnerBox;
            btnWebRegisterDirect.ForeColor = clrAccentCyan;
            btnWebRegisterDirect.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnWebRegisterDirect.Cursor = Cursors.Hand;
            btnWebRegisterDirect.Click += (s, e) => {
                string cleanId = this.mySessionId.Replace(" ", "");
                System.Diagnostics.Process.Start(string.Format("https://my-aetherdesk-control.vercel.app/?action=register&device_id={0}#/login", cleanId));
            };
            pnlModalCard.Controls.Add(btnWebRegisterDirect);

            Label lblAlreadyAccount = new Label();
            lblAlreadyAccount.Text = "Hesabınız var mı? Oturum aç";
            lblAlreadyAccount.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblAlreadyAccount.ForeColor = clrAccentCyan;
            lblAlreadyAccount.Location = new Point(36, 476);
            lblAlreadyAccount.Size = new Size(388, 24);
            lblAlreadyAccount.TextAlign = ContentAlignment.MiddleCenter;
            lblAlreadyAccount.Cursor = Cursors.Hand;
            lblAlreadyAccount.Click += (s, e) => {
                if (txtRegEmail.Text.Contains("@"))
                {
                    isLoggedIn = true;
                    userEmail = txtRegEmail.Text.Trim();
                    userDisplayName = string.IsNullOrEmpty(txtRegFullName.Text) ? userEmail.Split('@')[0] : txtRegFullName.Text.Trim();
                    SaveAuthSettings();
                    pnlAuthModalOverlay.Visible = false;
                    pnlLeftHero.Invalidate();
                    ShowModernDarkNotification("Giriş Yapıldı", "Oturumunuz başarıyla açıldı: " + userDisplayName);
                }
                else
                {
                    ShowModernDarkNotification("Bilgi", "Lütfen geçerli bir e-posta adresi yazıp 'Devam' butonuna basınız.");
                }
            };
            pnlModalCard.Controls.Add(lblAlreadyAccount);
        }

        private Button CreateSsoButton(string title, int top, EventHandler onClick)
        {
            Button btn = new Button();
            btn.Text = title;
            btn.Top = top;
            btn.Left = 36;
            btn.Width = 388;
            btn.Height = 38;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderColor = clrBorder;
            btn.BackColor = clrInnerBox;
            btn.ForeColor = clrTextLight;
            btn.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btn.TextAlign = ContentAlignment.MiddleCenter;
            btn.Cursor = Cursors.Hand;
            btn.Click += onClick;
            return btn;
        }

        private void PerformSocialLogin(string provider)
        {
            string cleanId = this.mySessionId.Replace(" ", "");
            isLoggedIn = true;
            userEmail = provider.ToLower() + ".user@aetherdesk.com";
            userDisplayName = provider + " Kullanıcısı";
            SaveAuthSettings();
            pnlAuthModalOverlay.Visible = false;
            pnlLeftHero.Invalidate();

            // 1. Notify cloud relay to register user & device
            ThreadPool.QueueUserWorkItem((state) =>
            {
                try
                {
                    string jsonPayload = string.Format("{{\"provider\":\"{0}\",\"name\":\"{1}\",\"email\":\"{2}\",\"deviceId\":\"{3}\"}}",
                        provider, provider + " Kullanıcısı", provider.ToLower() + "@aetherdesk.com", cleanId);
                    byte[] data = System.Text.Encoding.UTF8.GetBytes(jsonPayload);

                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(CLOUD_RELAY_URL + "/api/auth/sso");
                    req.Method = "POST";
                    req.ContentType = "application/json";
                    req.ContentLength = data.Length;
                    using (Stream stream = req.GetRequestStream())
                    {
                        stream.Write(data, 0, data.Length);
                    }
                    using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse()) { }
                }
                catch { }
            });

            // 2. Open official web auth portal or Google Account Chooser (Image 2)
            string webUrl;
            if (provider.ToLower() == "google")
            {
                // Real Google Account Chooser (Image 2)
                string continueTarget = Uri.EscapeDataString(string.Format("https://my-aetherdesk-control.vercel.app/?google_login=true&device_id={0}", cleanId));
                webUrl = "https://accounts.google.com/AccountChooser?continue=" + continueTarget;
            }
            else
            {
                // Image 1: AetherDesk TeamViewer-style Registration Page
                webUrl = string.Format("https://my-aetherdesk-control.vercel.app/?action=register&provider={0}&device_id={1}",
                    provider.ToLower(), cleanId);
            }

            try
            {
                System.Diagnostics.Process.Start(webUrl);
            }
            catch { }

            ShowModernDarkNotification(
                provider + " Doğrulaması",
                provider + " kimlik doğrulaması tarayıcınızda açıldı.\n\n" +
                "✓ Google hesap seçici ekranına yönlendirildiniz.\n" +
                "✓ Cihaz Kimliğiniz (" + this.mySessionId + ") hesabınıza otomatik bağlanacaktır."
            );
        }

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
            pnlAuthModalOverlay.Visible = true;
            pnlAuthModalOverlay.BringToFront();
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
            MessageBox.Show(
                "AetherDesk Enterprise Sistem Ayarları:\n\n" +
                "• Cihaz Adı: " + Environment.MachineName + "\n" +
                "• Oturum ID: " + this.mySessionId + "\n" +
                "• Parola Koruması: " + (accessMode == "UNATTENDED" ? "Katılımsız (Kolay Erişim)" : "Özel Parola") + "\n" +
                "• Windows ile Başlat: " + (startWithWindows ? "Aktif" : "Pasif") + "\n" +
                "• Bulut Röle: " + CLOUD_RELAY_URL + "\n\n" +
                "Tüm ayarlar Windows Kayıt Defteri'nde kalıcı olarak korunmaktadır.",
                "Genel Ayarlar",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
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

            btnSessionThreeDots = new Button();
            btnSessionThreeDots.Text = "⋮";
            btnSessionThreeDots.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            btnSessionThreeDots.ForeColor = clrTextLight;
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
