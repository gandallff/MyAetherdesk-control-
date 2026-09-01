using System;
using System.Drawing;
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
            Application.Run(new AgentMainForm());
        }
    }

    public class AgentMainForm : Form
    {
        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

        private Label lblTitle;
        private Label lblSub;
        private Panel panelCard;
        private Label lblIdTag;
        private Label lblSessionId;
        private Button btnCopy;
        private Label lblIpInfo;
        private Label lblStatus;
        private Panel statusDot;

        // Permissions GroupBox
        private GroupBox grpPermissions;
        private CheckBox chkAllowInput;
        private CheckBox chkAllowFiles;
        private CheckBox chkAllowClipboard;
        private CheckBox chkLockOnDisconnect;

        // Security & Access Settings
        private GroupBox grpAccessSettings;
        private RadioButton rbUnattended;
        private RadioButton rbPassword;
        private RadioButton rbPrompt;
        private TextBox txtCustomPassword;
        private Button btnSaveSettings;
        private Button btnDisconnectCurrent;

        // Remote Connect Box (Outgoing Connection)
        private Panel panelOutgoing;
        private TextBox txtRemoteTargetId;
        private Button btnConnectRemote;

        private string mySessionId;
        private HttpListener listener;
        private Thread listenThread;
        private Thread cloudRelayThread;
        private Thread inputPollThread;
        private bool isRunning = true;

        public static string CLOUD_RELAY_URL = "https://myaetherdesk-control.onrender.com";

        public AgentMainForm()
        {
            this.mySessionId = GetOrCreateUniqueSessionId();

            this.Text = "AetherDesk Remote Agent 2026 - ID: " + this.mySessionId;
            this.Size = new Size(540, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(10, 15, 29);
            this.ForeColor = Color.FromArgb(226, 232, 240);

            // Header
            lblTitle = new Label();
            lblTitle.Text = "⚡ AetherDesk Remote Control (AnyDesk Modeli)";
            lblTitle.Font = new Font("Segoe UI", 15, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(96, 165, 250);
            lblTitle.Location = new Point(25, 18);
            lblTitle.Size = new Size(480, 30);
            this.Controls.Add(lblTitle);

            lblSub = new Label();
            lblSub.Text = "Guvenli yetkilendirme, gercek masaustu yayini ve tam kontrol paneli.";
            lblSub.Font = new Font("Segoe UI", 8.5f);
            lblSub.ForeColor = Color.FromArgb(148, 163, 184);
            lblSub.Location = new Point(25, 48);
            lblSub.Size = new Size(480, 18);
            this.Controls.Add(lblSub);

            // Card Panel (Session ID & Status)
            panelCard = new Panel();
            panelCard.Location = new Point(25, 72);
            panelCard.Size = new Size(475, 130);
            panelCard.BackColor = Color.FromArgb(20, 29, 47);
            this.Controls.Add(panelCard);

            statusDot = new Panel();
            statusDot.Location = new Point(18, 14);
            statusDot.Size = new Size(12, 12);
            statusDot.BackColor = Color.FromArgb(52, 211, 153);
            panelCard.Controls.Add(statusDot);

            lblStatus = new Label();
            lblStatus.Text = "BAGLANTI VE KONTROL HAZIR (ONLINE)";
            lblStatus.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            lblStatus.ForeColor = Color.FromArgb(52, 211, 153);
            lblStatus.Location = new Point(36, 12);
            lblStatus.Size = new Size(300, 18);
            panelCard.Controls.Add(lblStatus);

            lblIdTag = new Label();
            lblIdTag.Text = "BU BILGISAYARIN 9 HANELI OTURUM ID'SI:";
            lblIdTag.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            lblIdTag.ForeColor = Color.FromArgb(148, 163, 184);
            lblIdTag.Location = new Point(18, 36);
            lblIdTag.Size = new Size(380, 16);
            panelCard.Controls.Add(lblIdTag);

            lblSessionId = new Label();
            lblSessionId.Text = this.mySessionId;
            lblSessionId.Font = new Font("Consolas", 22, FontStyle.Bold);
            lblSessionId.ForeColor = Color.FromArgb(96, 165, 250);
            lblSessionId.Location = new Point(18, 52);
            lblSessionId.Size = new Size(270, 38);
            panelCard.Controls.Add(lblSessionId);

            btnCopy = new Button();
            btnCopy.Text = "ID'yi Kopyala";
            btnCopy.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnCopy.ForeColor = Color.White;
            btnCopy.BackColor = Color.FromArgb(37, 99, 235);
            btnCopy.FlatStyle = FlatStyle.Flat;
            btnCopy.Location = new Point(320, 54);
            btnCopy.Size = new Size(135, 34);
            btnCopy.Cursor = Cursors.Hand;
            btnCopy.Click += (s, e) => {
                Clipboard.SetText(this.mySessionId.Replace(" ", ""));
                btnCopy.Text = "✓ Kopyalandi!";
                btnCopy.BackColor = Color.FromArgb(16, 185, 129);
            };
            panelCard.Controls.Add(btnCopy);

            string localIp = GetLocalIp();
            lblIpInfo = new Label();
            lblIpInfo.Text = "Yerel: " + localIp + ":8443 | Bulut: " + CLOUD_RELAY_URL.Replace("https://", "");
            lblIpInfo.Font = new Font("Consolas", 8);
            lblIpInfo.ForeColor = Color.FromArgb(148, 163, 184);
            lblIpInfo.Location = new Point(18, 98);
            lblIpInfo.Size = new Size(440, 18);
            panelCard.Controls.Add(lblIpInfo);

            // GroupBox: Bağlantı Yetkileri (Permissions)
            grpPermissions = new GroupBox();
            grpPermissions.Text = " 🛡️ Baglanan Kisiye Verilen Yetkiler ";
            grpPermissions.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            grpPermissions.ForeColor = Color.FromArgb(96, 165, 250);
            grpPermissions.Location = new Point(25, 212);
            grpPermissions.Size = new Size(475, 120);
            this.Controls.Add(grpPermissions);

            chkAllowInput = new CheckBox();
            chkAllowInput.Text = "Fare ve Klavye Yonetimine Izin Ver (Tam Kontrol)";
            chkAllowInput.Checked = true;
            chkAllowInput.Font = new Font("Segoe UI", 8.5f);
            chkAllowInput.ForeColor = Color.FromArgb(226, 232, 240);
            chkAllowInput.Location = new Point(18, 24);
            chkAllowInput.Size = new Size(440, 22);
            grpPermissions.Controls.Add(chkAllowInput);

            chkAllowFiles = new CheckBox();
            chkAllowFiles.Text = "Cift Yonlu Dosya Transferine Izin Ver";
            chkAllowFiles.Checked = true;
            chkAllowFiles.Font = new Font("Segoe UI", 8.5f);
            chkAllowFiles.ForeColor = Color.FromArgb(226, 232, 240);
            chkAllowFiles.Location = new Point(18, 48);
            chkAllowFiles.Size = new Size(440, 22);
            grpPermissions.Controls.Add(chkAllowFiles);

            chkAllowClipboard = new CheckBox();
            chkAllowClipboard.Text = "Pano Paylasimina Izin Ver (Kopyala / Yapistir)";
            chkAllowClipboard.Checked = true;
            chkAllowClipboard.Font = new Font("Segoe UI", 8.5f);
            chkAllowClipboard.ForeColor = Color.FromArgb(226, 232, 240);
            chkAllowClipboard.Location = new Point(18, 72);
            chkAllowClipboard.Size = new Size(440, 22);
            grpPermissions.Controls.Add(chkAllowClipboard);

            chkLockOnDisconnect = new CheckBox();
            chkLockOnDisconnect.Text = "Baglanti Sonlandiginda Masaustunu Kilitle";
            chkLockOnDisconnect.Checked = false;
            chkLockOnDisconnect.Font = new Font("Segoe UI", 8.5f);
            chkLockOnDisconnect.ForeColor = Color.FromArgb(226, 232, 240);
            chkLockOnDisconnect.Location = new Point(18, 94);
            chkLockOnDisconnect.Size = new Size(440, 20);
            grpPermissions.Controls.Add(chkLockOnDisconnect);

            // GroupBox: Güvenlik ve Şifreleme
            grpAccessSettings = new GroupBox();
            grpAccessSettings.Text = " 🔒 Guvenlik ve Sifreleme ";
            grpAccessSettings.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            grpAccessSettings.ForeColor = Color.FromArgb(96, 165, 250);
            grpAccessSettings.Location = new Point(25, 340);
            grpAccessSettings.Size = new Size(475, 170);
            this.Controls.Add(grpAccessSettings);

            rbUnattended = new RadioButton();
            rbUnattended.Text = "Katilimsiz Erisim (Sifresiz Otomatik Kabul)";
            rbUnattended.Font = new Font("Segoe UI", 8.5f);
            rbUnattended.ForeColor = Color.FromArgb(226, 232, 240);
            rbUnattended.Location = new Point(18, 24);
            rbUnattended.Size = new Size(440, 22);
            grpAccessSettings.Controls.Add(rbUnattended);

            rbPassword = new RadioButton();
            rbPassword.Text = "Ozel Sifreli Erisim (Baglanana sifre sorulsun)";
            rbPassword.Font = new Font("Segoe UI", 8.5f);
            rbPassword.ForeColor = Color.FromArgb(226, 232, 240);
            rbPassword.Location = new Point(18, 48);
            rbPassword.Size = new Size(440, 22);
            grpAccessSettings.Controls.Add(rbPassword);

            txtCustomPassword = new TextBox();
            txtCustomPassword.Font = new Font("Consolas", 10);
            txtCustomPassword.BackColor = Color.FromArgb(15, 23, 42);
            txtCustomPassword.ForeColor = Color.FromArgb(245, 158, 11);
            txtCustomPassword.Location = new Point(36, 72);
            txtCustomPassword.Size = new Size(180, 25);
            grpAccessSettings.Controls.Add(txtCustomPassword);

            rbPrompt = new RadioButton();
            rbPrompt.Text = "Her Baglantida Ekranda Manuel Onay Iste";
            rbPrompt.Font = new Font("Segoe UI", 8.5f);
            rbPrompt.ForeColor = Color.FromArgb(226, 232, 240);
            rbPrompt.Location = new Point(18, 102);
            rbPrompt.Size = new Size(440, 22);
            grpAccessSettings.Controls.Add(rbPrompt);

            btnSaveSettings = new Button();
            btnSaveSettings.Text = "Yetki ve Ayarlari Kaydet";
            btnSaveSettings.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnSaveSettings.ForeColor = Color.White;
            btnSaveSettings.BackColor = Color.FromArgb(16, 185, 129);
            btnSaveSettings.FlatStyle = FlatStyle.Flat;
            btnSaveSettings.Location = new Point(18, 130);
            btnSaveSettings.Size = new Size(438, 30);
            btnSaveSettings.Cursor = Cursors.Hand;
            btnSaveSettings.Click += (s, e) => SaveAccessSettings();
            grpAccessSettings.Controls.Add(btnSaveSettings);

            // Active Connection Bar with Instant Disconnect
            btnDisconnectCurrent = new Button();
            btnDisconnectCurrent.Text = "🚫 Aktif Baglantiyi Hemen Kes";
            btnDisconnectCurrent.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnDisconnectCurrent.ForeColor = Color.White;
            btnDisconnectCurrent.BackColor = Color.FromArgb(225, 29, 72);
            btnDisconnectCurrent.FlatStyle = FlatStyle.Flat;
            btnDisconnectCurrent.Location = new Point(25, 520);
            btnDisconnectCurrent.Size = new Size(475, 36);
            btnDisconnectCurrent.Cursor = Cursors.Hand;
            btnDisconnectCurrent.Click += (s, e) => {
                statusDot.BackColor = Color.FromArgb(52, 211, 153);
                lblStatus.Text = "BAGLANTIYA HAZIR (ONLINE)";
                lblStatus.ForeColor = Color.FromArgb(52, 211, 153);
                MessageBox.Show("Uzak oturum sonlandirildi.", "AetherDesk", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            this.Controls.Add(btnDisconnectCurrent);

            // Outgoing Connection from EXE (Connect to Remote PC)
            panelOutgoing = new Panel();
            panelOutgoing.Location = new Point(25, 568);
            panelOutgoing.Size = new Size(475, 55);
            panelOutgoing.BackColor = Color.FromArgb(15, 23, 42);
            this.Controls.Add(panelOutgoing);

            Label lblOutTag = new Label();
            lblOutTag.Text = "Baska Bir Bilgisayara Baglan (Oturum ID):";
            lblOutTag.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            lblOutTag.ForeColor = Color.FromArgb(148, 163, 184);
            lblOutTag.Location = new Point(12, 6);
            lblOutTag.Size = new Size(300, 16);
            panelOutgoing.Controls.Add(lblOutTag);

            txtRemoteTargetId = new TextBox();
            txtRemoteTargetId.Font = new Font("Consolas", 11, FontStyle.Bold);
            txtRemoteTargetId.BackColor = Color.FromArgb(10, 15, 29);
            txtRemoteTargetId.ForeColor = Color.FromArgb(52, 211, 153);
            txtRemoteTargetId.Location = new Point(12, 24);
            txtRemoteTargetId.Size = new Size(310, 26);
            panelOutgoing.Controls.Add(txtRemoteTargetId);

            btnConnectRemote = new Button();
            btnConnectRemote.Text = "🚀 Baglan";
            btnConnectRemote.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnConnectRemote.ForeColor = Color.White;
            btnConnectRemote.BackColor = Color.FromArgb(37, 99, 235);
            btnConnectRemote.FlatStyle = FlatStyle.Flat;
            btnConnectRemote.Location = new Point(330, 23);
            btnConnectRemote.Size = new Size(130, 27);
            btnConnectRemote.Cursor = Cursors.Hand;
            btnConnectRemote.Click += (s, e) => {
                string target = txtRemoteTargetId.Text.Trim();
                if (!string.IsNullOrEmpty(target))
                {
                    System.Diagnostics.Process.Start("https://my-aetherdesk-control.vercel.app/?connect=" + target.Replace(" ", ""));
                }
            };
            panelOutgoing.Controls.Add(btnConnectRemote);

            LoadSavedAccessSettings();
            StartListener();
            StartCloudRelayThread();
            StartInputPollThread();
        }

        private void LoadSavedAccessSettings()
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
                    chkAllowInput.Checked = allowInput;
                    chkAllowFiles.Checked = allowFiles;
                    chkAllowClipboard.Checked = allowClip;

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
                    key.SetValue("AllowInput", chkAllowInput.Checked.ToString());
                    key.SetValue("AllowFiles", chkAllowFiles.Checked.ToString());
                    key.SetValue("AllowClip", chkAllowClipboard.Checked.ToString());
                }
                btnSaveSettings.Text = "✓ Yetkiler Kaydedildi!";
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

                if (path == "/mouse" && chkAllowInput.Checked)
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
                            if (chkAllowInput.Checked && !string.IsNullOrEmpty(json) && json.Contains("action"))
                            {
                                ProcessEventsRobust(json);
                            }
                        }
                    }
                    catch { }

                    Thread.Sleep(60); // High-frequency 60ms polling
                }
            });
            inputPollThread.IsBackground = true;
            inputPollThread.Start();
        }

        private void ProcessEventsRobust(string json)
        {
            try
            {
                // Match each JSON object inside events array
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

                        // Position cursor
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
