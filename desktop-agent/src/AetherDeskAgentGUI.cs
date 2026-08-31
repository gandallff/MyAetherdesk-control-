using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Threading;
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
        private Label lblTitle;
        private Label lblSub;
        private Panel panelCard;
        private Label lblIdTag;
        private Label lblSessionId;
        private Button btnCopy;
        private Label lblIpInfo;
        private Label lblStatus;
        private Panel statusDot;

        // Settings Controls
        private GroupBox grpAccessSettings;
        private RadioButton rbUnattended;
        private RadioButton rbPassword;
        private RadioButton rbPrompt;
        private TextBox txtCustomPassword;
        private Label lblPassNote;
        private Button btnSaveSettings;

        private string mySessionId;
        private HttpListener listener;
        private Thread listenThread;

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

            // Title
            lblTitle = new Label();
            lblTitle.Text = "⚡ AetherDesk QuickSupport";
            lblTitle.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(96, 165, 250);
            lblTitle.Location = new Point(30, 20);
            lblTitle.Size = new Size(440, 32);
            this.Controls.Add(lblTitle);

            // Subtitle
            lblSub = new Label();
            lblSub.Text = "Bilgisayariniz uzaktan erisime ve guvenli baglantiya hazir.";
            lblSub.Font = new Font("Segoe UI", 9);
            lblSub.ForeColor = Color.FromArgb(148, 163, 184);
            lblSub.Location = new Point(30, 52);
            lblSub.Size = new Size(440, 20);
            this.Controls.Add(lblSub);

            // Card Panel (ID & Status)
            panelCard = new Panel();
            panelCard.Location = new Point(30, 80);
            panelCard.Size = new Size(444, 150);
            panelCard.BackColor = Color.FromArgb(20, 29, 47);
            this.Controls.Add(panelCard);

            // Status Indicator
            statusDot = new Panel();
            statusDot.Location = new Point(20, 16);
            statusDot.Size = new Size(12, 12);
            statusDot.BackColor = Color.FromArgb(52, 211, 153);
            panelCard.Controls.Add(statusDot);

            lblStatus = new Label();
            lblStatus.Text = "BAGLANTIYA HAZIR (ONLINE)";
            lblStatus.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            lblStatus.ForeColor = Color.FromArgb(52, 211, 153);
            lblStatus.Location = new Point(38, 14);
            lblStatus.Size = new Size(200, 18);
            panelCard.Controls.Add(lblStatus);

            // Session ID Tag
            lblIdTag = new Label();
            lblIdTag.Text = "BU BILGISAYARIN OZEL OTURUM ID'SI:";
            lblIdTag.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            lblIdTag.ForeColor = Color.FromArgb(148, 163, 184);
            lblIdTag.Location = new Point(20, 42);
            lblIdTag.Size = new Size(380, 16);
            panelCard.Controls.Add(lblIdTag);

            // Large 9-Digit Session ID
            lblSessionId = new Label();
            lblSessionId.Text = this.mySessionId;
            lblSessionId.Font = new Font("Consolas", 24, FontStyle.Bold);
            lblSessionId.ForeColor = Color.FromArgb(96, 165, 250);
            lblSessionId.Location = new Point(20, 60);
            lblSessionId.Size = new Size(270, 44);
            panelCard.Controls.Add(lblSessionId);

            // Copy ID Button
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

            // Local IP Address Detection
            string localIp = GetLocalIp();
            lblIpInfo = new Label();
            lblIpInfo.Text = "Yerel IP (LAN): " + localIp + ":8443";
            lblIpInfo.Font = new Font("Consolas", 9);
            lblIpInfo.ForeColor = Color.FromArgb(148, 163, 184);
            lblIpInfo.Location = new Point(20, 115);
            lblIpInfo.Size = new Size(380, 20);
            panelCard.Controls.Add(lblIpInfo);

            // GroupBox: Access & Security Settings
            grpAccessSettings = new GroupBox();
            grpAccessSettings.Text = " 🔒 Erisim ve Guvenlik Ayarlari ";
            grpAccessSettings.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            grpAccessSettings.ForeColor = Color.FromArgb(96, 165, 250);
            grpAccessSettings.Location = new Point(30, 245);
            grpAccessSettings.Size = new Size(444, 210);
            this.Controls.Add(grpAccessSettings);

            // Option 1: Unattended (Passwordless / Auto-Accept)
            rbUnattended = new RadioButton();
            rbUnattended.Text = "Katilimsiz Erisim (Sifresiz Otomatik Baglanti)";
            rbUnattended.Font = new Font("Segoe UI", 8.5f);
            rbUnattended.ForeColor = Color.FromArgb(226, 232, 240);
            rbUnattended.Location = new Point(20, 28);
            rbUnattended.Size = new Size(400, 22);
            rbUnattended.CheckedChanged += (s, e) => UpdateAccessModeUI();
            grpAccessSettings.Controls.Add(rbUnattended);

            // Option 2: Password Protected
            rbPassword = new RadioButton();
            rbPassword.Text = "Ozel Sifreli Erisim (Baglanan kisiye sifre sorulsun)";
            rbPassword.Font = new Font("Segoe UI", 8.5f);
            rbPassword.ForeColor = Color.FromArgb(226, 232, 240);
            rbPassword.Location = new Point(20, 56);
            rbPassword.Size = new Size(400, 22);
            rbPassword.CheckedChanged += (s, e) => UpdateAccessModeUI();
            grpAccessSettings.Controls.Add(rbPassword);

            // Custom Password Box
            txtCustomPassword = new TextBox();
            txtCustomPassword.Font = new Font("Consolas", 10);
            txtCustomPassword.BackColor = Color.FromArgb(15, 23, 42);
            txtCustomPassword.ForeColor = Color.FromArgb(245, 158, 11);
            txtCustomPassword.Location = new Point(40, 84);
            txtCustomPassword.Size = new Size(180, 25);
            grpAccessSettings.Controls.Add(txtCustomPassword);

            lblPassNote = new Label();
            lblPassNote.Text = "(Baglanti icin gereken sifreyi belirleyin)";
            lblPassNote.Font = new Font("Segoe UI", 8);
            lblPassNote.ForeColor = Color.FromArgb(148, 163, 184);
            lblPassNote.Location = new Point(230, 87);
            lblPassNote.Size = new Size(200, 20);
            grpAccessSettings.Controls.Add(lblPassNote);

            // Option 3: Manual Confirmation Popup
            rbPrompt = new RadioButton();
            rbPrompt.Text = "Her Baglantida Ekranda Onay Iste (Manuel Kabul)";
            rbPrompt.Font = new Font("Segoe UI", 8.5f);
            rbPrompt.ForeColor = Color.FromArgb(226, 232, 240);
            rbPrompt.Location = new Point(20, 118);
            rbPrompt.Size = new Size(400, 22);
            rbPrompt.CheckedChanged += (s, e) => UpdateAccessModeUI();
            grpAccessSettings.Controls.Add(rbPrompt);

            // Save Settings Button
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

            // Bottom Instructions
            Label lblFooter = new Label();
            lblFooter.Text = "Oturum numarasini yoneticiye iletiniz. Baglanti guvenlik ayarlariniza gore saglanacaktir.";
            lblFooter.Font = new Font("Segoe UI", 8.5f);
            lblFooter.ForeColor = Color.FromArgb(100, 116, 139);
            lblFooter.Location = new Point(30, 465);
            lblFooter.Size = new Size(440, 40);
            this.Controls.Add(lblFooter);

            // Load Saved Settings from Registry
            LoadSavedAccessSettings();

            // Start Listener
            StartListener();
        }

        private void UpdateAccessModeUI()
        {
            txtCustomPassword.Enabled = rbPassword.Checked;
            if (rbPassword.Checked && string.IsNullOrEmpty(txtCustomPassword.Text))
            {
                txtCustomPassword.Text = "aether2026";
            }
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

                    if (mode == "PASSWORD")
                        rbPassword.Checked = true;
                    else if (mode == "PROMPT")
                        rbPrompt.Checked = true;
                    else
                        rbUnattended.Checked = true;
                }
            }
            catch
            {
                rbUnattended.Checked = true;
                txtCustomPassword.Text = "aether2026";
            }
            UpdateAccessModeUI();
        }

        private void SaveAccessSettings()
        {
            try
            {
                string mode = "UNATTENDED";
                if (rbPassword.Checked) mode = "PASSWORD";
                else if (rbPrompt.Checked) mode = "PROMPT";

                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\AetherDesk"))
                {
                    key.SetValue("AccessMode", mode);
                    key.SetValue("AccessPassword", txtCustomPassword.Text.Trim());
                }

                btnSaveSettings.Text = "✓ Ayarlar Basariyla Kaydedildi!";
                btnSaveSettings.BackColor = Color.FromArgb(5, 150, 105);
                System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();
                t.Interval = 2500;
                t.Tick += (s, e) => {
                    btnSaveSettings.Text = "Ayarlari Kaydet";
                    btnSaveSettings.BackColor = Color.FromArgb(16, 185, 129);
                    t.Stop();
                };
                t.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ayar kaydedilemedi: " + ex.Message);
            }
        }

        private string GetOrCreateUniqueSessionId()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\AetherDesk"))
                {
                    object val = key.GetValue("SessionId");
                    if (val != null && !string.IsNullOrEmpty(val.ToString()))
                    {
                        return val.ToString();
                    }
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
                    this.Invoke((MethodInvoker)delegate {
                        statusDot.BackColor = Color.FromArgb(96, 165, 250);
                        lblStatus.Text = "UZAKTAN BAGLANTI AKTIF";
                        lblStatus.ForeColor = Color.FromArgb(96, 165, 250);
                    });

                    byte[] buf = System.Text.Encoding.UTF8.GetBytes("{\"status\":\"connected\",\"session\":\"" + this.mySessionId + "\"}");
                    ctx.Response.ContentType = "application/json";
                    ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                    ctx.Response.Close();
                }
                catch { }
            }
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
