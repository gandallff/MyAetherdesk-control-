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
        private Label lblPassTag;
        private TextBox txtPassword;
        private string mySessionId;
        private HttpListener listener;
        private Thread listenThread;

        public AgentMainForm()
        {
            this.mySessionId = GetOrCreateUniqueSessionId();

            this.Text = "AetherDesk Remote Agent 2026 - ID: " + this.mySessionId;
            this.Size = new Size(500, 490);
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
            lblTitle.Location = new Point(30, 24);
            lblTitle.Size = new Size(420, 32);
            this.Controls.Add(lblTitle);

            // Subtitle
            lblSub = new Label();
            lblSub.Text = "Bilgisayariniz uzaktan erisime ve guvenli baglantiya hazir.";
            lblSub.Font = new Font("Segoe UI", 9);
            lblSub.ForeColor = Color.FromArgb(148, 163, 184);
            lblSub.Location = new Point(30, 58);
            lblSub.Size = new Size(420, 20);
            this.Controls.Add(lblSub);

            // Card Panel
            panelCard = new Panel();
            panelCard.Location = new Point(30, 90);
            panelCard.Size = new Size(424, 215);
            panelCard.BackColor = Color.FromArgb(20, 29, 47);
            this.Controls.Add(panelCard);

            // Status Indicator
            statusDot = new Panel();
            statusDot.Location = new Point(20, 18);
            statusDot.Size = new Size(12, 12);
            statusDot.BackColor = Color.FromArgb(52, 211, 153);
            panelCard.Controls.Add(statusDot);

            lblStatus = new Label();
            lblStatus.Text = "BAGLANTIYA HAZIR (ONLINE)";
            lblStatus.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            lblStatus.ForeColor = Color.FromArgb(52, 211, 153);
            lblStatus.Location = new Point(38, 16);
            lblStatus.Size = new Size(200, 18);
            panelCard.Controls.Add(lblStatus);

            // Session ID Tag
            lblIdTag = new Label();
            lblIdTag.Text = "BU BILGISAYARIN OZEL OTURUM ID'SI:";
            lblIdTag.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            lblIdTag.ForeColor = Color.FromArgb(148, 163, 184);
            lblIdTag.Location = new Point(20, 48);
            lblIdTag.Size = new Size(380, 16);
            panelCard.Controls.Add(lblIdTag);

            // Large 9-Digit Session ID
            lblSessionId = new Label();
            lblSessionId.Text = this.mySessionId;
            lblSessionId.Font = new Font("Consolas", 24, FontStyle.Bold);
            lblSessionId.ForeColor = Color.FromArgb(96, 165, 250);
            lblSessionId.Location = new Point(20, 68);
            lblSessionId.Size = new Size(260, 44);
            panelCard.Controls.Add(lblSessionId);

            // Copy ID Button
            btnCopy = new Button();
            btnCopy.Text = "ID'yi Kopyala";
            btnCopy.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnCopy.ForeColor = Color.White;
            btnCopy.BackColor = Color.FromArgb(37, 99, 235);
            btnCopy.FlatStyle = FlatStyle.Flat;
            btnCopy.Location = new Point(285, 72);
            btnCopy.Size = new Size(120, 36);
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
            lblIpInfo.Location = new Point(20, 125);
            lblIpInfo.Size = new Size(380, 20);
            panelCard.Controls.Add(lblIpInfo);

            // Unattended Access Password
            lblPassTag = new Label();
            lblPassTag.Text = "Sifresiz/Otomatik Erisim:";
            lblPassTag.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lblPassTag.ForeColor = Color.FromArgb(203, 213, 225);
            lblPassTag.Location = new Point(20, 162);
            lblPassTag.Size = new Size(150, 22);
            panelCard.Controls.Add(lblPassTag);

            txtPassword = new TextBox();
            txtPassword.Text = "aether2026";
            txtPassword.Font = new Font("Consolas", 10);
            txtPassword.BackColor = Color.FromArgb(15, 23, 42);
            txtPassword.ForeColor = Color.FromArgb(245, 158, 11);
            txtPassword.Location = new Point(175, 160);
            txtPassword.Size = new Size(110, 24);
            txtPassword.ReadOnly = true;
            panelCard.Controls.Add(txtPassword);

            // Bottom Instructions
            Label lblFooter = new Label();
            lblFooter.Text = "Bu 9 haneli numarayi portala (veya yoneticiye) iletiniz. Baglanti kuruldugunda otomatik erisim saglanacak ve bildirim goruntulenecektir.";
            lblFooter.Font = new Font("Segoe UI", 8.5f);
            lblFooter.ForeColor = Color.FromArgb(100, 116, 139);
            lblFooter.Location = new Point(30, 325);
            lblFooter.Size = new Size(420, 50);
            this.Controls.Add(lblFooter);

            // Start Direct Socket / Incoming Listener
            StartListener();
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
                    // Generate new unique 9-digit format (XXX XXX XXX)
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
