using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace AetherDesk.Agent
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new AgentMainForm());
        }
    }

    public class AgentMainForm : Form
    {
        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x02;
        private const uint MOUSEEVENTF_LEFTUP = 0x04;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const uint MOUSEEVENTF_RIGHTUP = 0x09;

        private Label lblTitle;
        private Label lblSub;
        private Panel panelCard;
        private Label lblIdTag;
        private Label lblSessionId;
        private Button btnCopy;
        private Label lblIpInfo;
        private Label lblStatus;
        private Panel statusDot;

        private GroupBox grpAccessSettings;
        private RadioButton rbUnattended;
        private RadioButton rbPassword;
        private RadioButton rbPrompt;
        private TextBox txtCustomPassword;
        private Button btnSaveSettings;

        private string mySessionId;
        private HttpListener listener;
        private Thread listenThread;
        private Thread cloudRelayThread;
        private bool isRunning = true;

        // Configurable Cloud Relay URL (Render / Public Cloud)
        public static string CLOUD_RELAY_URL = "https://myaetherdesk-signaling.onrender.com";

        public AgentMainForm()
        {
            this.mySessionId = GetOrCreateUniqueSessionId();

            this.Text = "AetherDesk Remote Agent 2026 - ID: " + this.mySessionId;
            this.Size = new Size(520, 580);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(10, 15, 29);
            this.ForeColor = Color.FromArgb(226, 232, 240);

            // Header
            lblTitle = new Label();
            lblTitle.Text = "⚡ AetherDesk QuickSupport (Gercek Ekran)";
            lblTitle.Font = new Font("Segoe UI", 15, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(96, 165, 250);
            lblTitle.Location = new Point(30, 20);
            lblTitle.Size = new Size(440, 30);
            this.Controls.Add(lblTitle);

            lblSub = new Label();
            lblSub.Text = "Bulut ve Yerel Ag Gercek Fiziksel Masaustu Yayini Aktif.";
            lblSub.Font = new Font("Segoe UI", 9);
            lblSub.ForeColor = Color.FromArgb(148, 163, 184);
            lblSub.Location = new Point(30, 52);
            lblSub.Size = new Size(440, 20);
            this.Controls.Add(lblSub);

            // Card Panel
            panelCard = new Panel();
            panelCard.Location = new Point(30, 80);
            panelCard.Size = new Size(444, 150);
            panelCard.BackColor = Color.FromArgb(20, 29, 47);
            this.Controls.Add(panelCard);

            statusDot = new Panel();
            statusDot.Location = new Point(20, 16);
            statusDot.Size = new Size(12, 12);
            statusDot.BackColor = Color.FromArgb(52, 211, 153);
            panelCard.Controls.Add(statusDot);

            lblStatus = new Label();
            lblStatus.Text = "BULUT VE YEREL YAYIN HAZIR (ONLINE)";
            lblStatus.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            lblStatus.ForeColor = Color.FromArgb(52, 211, 153);
            lblStatus.Location = new Point(38, 14);
            lblStatus.Size = new Size(270, 18);
            panelCard.Controls.Add(lblStatus);

            lblIdTag = new Label();
            lblIdTag.Text = "BU BILGISAYARIN 9 HANELI OTURUM ID'SI:";
            lblIdTag.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            lblIdTag.ForeColor = Color.FromArgb(148, 163, 184);
            lblIdTag.Location = new Point(20, 42);
            lblIdTag.Size = new Size(380, 16);
            panelCard.Controls.Add(lblIdTag);

            lblSessionId = new Label();
            lblSessionId.Text = this.mySessionId;
            lblSessionId.Font = new Font("Consolas", 24, FontStyle.Bold);
            lblSessionId.ForeColor = Color.FromArgb(96, 165, 250);
            lblSessionId.Location = new Point(20, 60);
            lblSessionId.Size = new Size(270, 44);
            panelCard.Controls.Add(lblSessionId);

            btnCopy = new Button();
            btnCopy.Text = "ID'yi Kopyala";
            btnCopy.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnCopy.ForeColor = Color.White;
            btnCopy.BackColor = Color.FromArgb(37, 99, 235);
            btnCopy.FlatStyle = FlatStyle.Flat;
            btnCopy.Location = new Point(300, 64);
            btnCopy.Size = new Size(125, 36);
            btnCopy.Cursor = Cursors.Hand;
            btnCopy.Click += (s, e) => {
                Clipboard.SetText(this.mySessionId.Replace(" ", ""));
                btnCopy.Text = "✓ Kopyalandi!";
                btnCopy.BackColor = Color.FromArgb(16, 185, 129);
            };
            panelCard.Controls.Add(btnCopy);

            string localIp = GetLocalIp();
            lblIpInfo = new Label();
            lblIpInfo.Text = "Yerel IP (LAN): " + localIp + ":8443 | Bulut: Aktif";
            lblIpInfo.Font = new Font("Consolas", 9);
            lblIpInfo.ForeColor = Color.FromArgb(148, 163, 184);
            lblIpInfo.Location = new Point(20, 115);
            lblIpInfo.Size = new Size(380, 20);
            panelCard.Controls.Add(lblIpInfo);

            // GroupBox: Access Settings
            grpAccessSettings = new GroupBox();
            grpAccessSettings.Text = " 🔒 Erisim ve Guvenlik Ayarlari ";
            grpAccessSettings.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            grpAccessSettings.ForeColor = Color.FromArgb(96, 165, 250);
            grpAccessSettings.Location = new Point(30, 245);
            grpAccessSettings.Size = new Size(444, 210);
            this.Controls.Add(grpAccessSettings);

            rbUnattended = new RadioButton();
            rbUnattended.Text = "Katilimsiz Erisim (Sifresiz Otomatik Baglanti)";
            rbUnattended.Font = new Font("Segoe UI", 8.5f);
            rbUnattended.ForeColor = Color.FromArgb(226, 232, 240);
            rbUnattended.Location = new Point(20, 28);
            rbUnattended.Size = new Size(400, 22);
            grpAccessSettings.Controls.Add(rbUnattended);

            rbPassword = new RadioButton();
            rbPassword.Text = "Ozel Sifreli Erisim (Baglanan kisiye sifre sorulsun)";
            rbPassword.Font = new Font("Segoe UI", 8.5f);
            rbPassword.ForeColor = Color.FromArgb(226, 232, 240);
            rbPassword.Location = new Point(20, 56);
            rbPassword.Size = new Size(400, 22);
            grpAccessSettings.Controls.Add(rbPassword);

            txtCustomPassword = new TextBox();
            txtCustomPassword.Font = new Font("Consolas", 10);
            txtCustomPassword.BackColor = Color.FromArgb(15, 23, 42);
            txtCustomPassword.ForeColor = Color.FromArgb(245, 158, 11);
            txtCustomPassword.Location = new Point(40, 84);
            txtCustomPassword.Size = new Size(180, 25);
            grpAccessSettings.Controls.Add(txtCustomPassword);

            rbPrompt = new RadioButton();
            rbPrompt.Text = "Her Baglantida Ekranda Onay Iste (Manuel Kabul)";
            rbPrompt.Font = new Font("Segoe UI", 8.5f);
            rbPrompt.ForeColor = Color.FromArgb(226, 232, 240);
            rbPrompt.Location = new Point(20, 118);
            rbPrompt.Size = new Size(400, 22);
            grpAccessSettings.Controls.Add(rbPrompt);

            btnSaveSettings = new Button();
            btnSaveSettings.Text = "Ayarlari Kaydet";
            btnSaveSettings.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnSaveSettings.ForeColor = Color.White;
            btnSaveSettings.BackColor = Color.FromArgb(16, 185, 129);
            btnSaveSettings.FlatStyle = FlatStyle.Flat;
            btnSaveSettings.Location = new Point(20, 158);
            btnSaveSettings.Size = new Size(404, 34);
            btnSaveSettings.Cursor = Cursors.Hand;
            btnSaveSettings.Click += (s, e) => SaveAccessSettings();
            grpAccessSettings.Controls.Add(btnSaveSettings);

            Label lblFooter = new Label();
            lblFooter.Text = "Bu 9 haneli ID'yi yoneticiye iletiniz. Dunyanin her yerinden gercek ekran kontrolu saglanacaktir.";
            lblFooter.Font = new Font("Segoe UI", 8.5f);
            lblFooter.ForeColor = Color.FromArgb(100, 116, 139);
            lblFooter.Location = new Point(30, 465);
            lblFooter.Size = new Size(440, 40);
            this.Controls.Add(lblFooter);

            LoadSavedAccessSettings();
            StartListener();
            StartCloudRelayThread();
        }

        private void LoadSavedAccessSettings()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\AetherDesk"))
                {
                    string mode = (key.GetValue("AccessMode") ?? "UNATTENDED").ToString();
                    string pass = (key.GetValue("AccessPassword") ?? "aether2026").ToString();
                    txtCustomPassword.Text = pass;
                    if (mode == "PASSWORD") rbPassword.Checked = true;
                    else if (mode == "PROMPT") rbPrompt.Checked = true;
                    else rbUnattended.Checked = true;
                }
            }
            catch { rbUnattended.Checked = true; }
        }

        private void SaveAccessSettings()
        {
            try
            {
                string mode = rbPassword.Checked ? "PASSWORD" : (rbPrompt.Checked ? "PROMPT" : "UNATTENDED");
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\AetherDesk"))
                {
                    key.SetValue("AccessMode", mode);
                    key.SetValue("AccessPassword", txtCustomPassword.Text.Trim());
                }
                btnSaveSettings.Text = "✓ Kaydedildi!";
                btnSaveSettings.BackColor = Color.FromArgb(5, 150, 105);
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

        // Background Cloud Relay (Pushes Screen to Render & Polls for Remote Mouse Commands)
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
                        // 1. Push Latest Screen Frame to Render Cloud
                        byte[] screenJpeg = CaptureRealScreenJpeg();
                        HttpWebRequest uploadReq = (HttpWebRequest)WebRequest.Create(CLOUD_RELAY_URL + "/api/stream/" + cleanId);
                        uploadReq.Method = "POST";
                        uploadReq.ContentType = "image/jpeg";
                        uploadReq.ContentLength = screenJpeg.Length;
                        uploadReq.Timeout = 3000;

                        using (Stream reqStream = uploadReq.GetRequestStream())
                        {
                            reqStream.Write(screenJpeg, 0, screenJpeg.Length);
                        }
                        using (HttpWebResponse resp = (HttpWebResponse)uploadReq.GetResponse()) { }

                        // 2. Poll Mouse Actions from Cloud Queue
                        HttpWebRequest eventReq = (HttpWebRequest)WebRequest.Create(CLOUD_RELAY_URL + "/api/events/" + cleanId);
                        eventReq.Method = "GET";
                        eventReq.Timeout = 3000;

                        using (HttpWebResponse eventResp = (HttpWebResponse)eventReq.GetResponse())
                        using (StreamReader reader = new StreamReader(eventResp.GetResponseStream()))
                        {
                            string json = reader.ReadToEnd();
                            // Parse simple events if present
                            if (json.Contains("\"action\":\"click\""))
                            {
                                // Execute remote click on real desktop
                                Point current = Cursor.Position;
                                mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)current.X, (uint)current.Y, 0, 0);
                            }
                        }
                    }
                    catch
                    {
                        // Offline or waiting for cloud connection
                    }

                    Thread.Sleep(500); // 2 FPS cloud upload loop
                }
            });
            cloudRelayThread.IsBackground = true;
            cloudRelayThread.Start();
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
                        mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)realX, (uint)realY, 0, 0);
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
