using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using WinPETool.Core;
using WinPETool.UI;

namespace WinPETool.UI.Pages
{
    /// <summary>
    /// Network Tools Page — IP config, ping, DNS, connectivity test, network shares
    /// </summary>
    public class NetworkPage : Panel
    {
        private TabControl _tabs;

        public NetworkPage(AppConfig config)
        {
            BackColor = Theme.BgDark;
            Dock      = DockStyle.Fill;
            BuildUI();
            LoadNetworkInfoAsync();
        }

        private void BuildUI()
        {
            var header = new Panel { Dock = DockStyle.Top, Height = 50 };
            var bar    = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = Theme.CatNetwork };
            var lbl    = new Label { Text = "🌐  Mạng & Kết nối", Font = Theme.FontTitle, ForeColor = Theme.TextPrimary, AutoSize = true, Location = new Point(14, 12) };
            header.Controls.Add(bar); header.Controls.Add(lbl);
            Controls.Add(header);

            _tabs = new TabControl { Dock = DockStyle.Fill, Font = Theme.FontBody };
            Controls.Add(_tabs);

            _tabs.TabPages.Add(BuildInfoTab());
            _tabs.TabPages.Add(BuildPingTab());
            _tabs.TabPages.Add(BuildDNSTab());
            _tabs.TabPages.Add(BuildShareTab());
            _tabs.TabPages.Add(BuildStaticIPTab());
        }

        // ─── TAB 1: Network Info ─────────────────────────────────────────────
        private TabPage BuildInfoTab()
        {
            var tp  = new TabPage("📋 Thông tin mạng") { BackColor = Theme.BgDark, Padding = new Padding(12) };
            var rtb = MakeRTB();
            var refreshBtn = MakeBtn("🔄 Làm mới", () =>
            {
                rtb.Clear();
                LoadNetworkInfoAsync(rtb);
            });
            var panel = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = Theme.BgToolbar, Padding = new Padding(8, 4, 0, 4) };
            panel.Controls.Add(refreshBtn);
            tp.Controls.Add(rtb);
            tp.Controls.Add(panel);
            return tp;
        }

        private void LoadNetworkInfoAsync(RichTextBox rtb = null)
        {
            var target = rtb ?? (GetTab(0)?.Controls[0] as RichTextBox);
            if (target == null) return;

            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                // ipconfig /all
                var result = ProcessRunner.RunCmd("ipconfig /all");
                AppendRTB(target, "=== IPCONFIG /ALL ===\n", Theme.Accent, Theme.FontHeader);
                AppendRTB(target, result.Output, Theme.TextSecondary);

                // Interface details via PowerShell
                var psResult = ProcessRunner.RunPowerShell(
                    "Get-NetIPConfiguration | Select-Object InterfaceAlias, IPv4Address, IPv4DefaultGateway, DNSServer | Format-List | Out-String");
                if (psResult.Success)
                {
                    AppendRTB(target, "\n=== PS: IP CONFIGURATION ===\n", Theme.Accent, Theme.FontHeader);
                    AppendRTB(target, psResult.Output, Theme.TextPrimary);
                }

                // Connectivity test
                AppendRTB(target, "\n=== KIỂM TRA KẾT NỐI ===\n", Theme.Accent, Theme.FontHeader);
                TestConnectivity(target);
            });
        }

        private void TestConnectivity(RichTextBox rtb)
        {
            var tests = new[] { ("8.8.8.8", "Google DNS"), ("1.1.1.1", "Cloudflare DNS"), ("google.com", "Internet") };
            foreach (var (host, label) in tests)
            {
                try
                {
                    using (var ping = new Ping())
                    {
                        var reply = ping.Send(host, 2000);
                        bool ok = reply.Status == IPStatus.Success;
                        AppendRTB(rtb,
                            $"  {(ok ? "✅" : "❌")} {label} ({host}) — {(ok ? $"{reply.RoundtripTime}ms" : reply.Status.ToString())}\n",
                            ok ? Theme.TextSuccess : Theme.TextError);
                    }
                }
                catch (Exception ex)
                {
                    AppendRTB(rtb, $"  ❌ {label} — {ex.Message}\n", Theme.TextError);
                }
            }

            // Check Wi-Fi — warn if not available
            var wifiResult = ProcessRunner.RunCmd("netsh wlan show interfaces 2>&1");
            if (wifiResult.Output.Contains("There is no wireless interface"))
            {
                AppendRTB(rtb, "\n  ⚠️  Wi-Fi: Không có adapter Wi-Fi hoặc driver chưa được load.\n", Theme.TextWarning);
                AppendRTB(rtb, "      → Inject Wi-Fi driver vào WinPE để sử dụng Wi-Fi.\n", Theme.TextMuted);
            }
        }

        // ─── TAB 2: Ping ─────────────────────────────────────────────────────
        private TabPage BuildPingTab()
        {
            var tp     = new TabPage("📡 Ping") { BackColor = Theme.BgDark, Padding = new Padding(12) };
            var lbl    = new Label { Text = "Host / IP:", Font = Theme.FontBody, ForeColor = Theme.TextSecondary, Location = new Point(12, 18), AutoSize = true };
            var input  = new TextBox { Location = new Point(90, 14), Width = 240, BackColor = Theme.BgInput, ForeColor = Theme.TextPrimary, Font = Theme.FontBody, BorderStyle = BorderStyle.FixedSingle };
            var cntLbl = new Label { Text = "Số lần:", Font = Theme.FontBody, ForeColor = Theme.TextSecondary, Location = new Point(340, 18), AutoSize = true };
            var cntBox = new NumericUpDown { Location = new Point(395, 14), Width = 60, Minimum = 1, Maximum = 100, Value = 4, BackColor = Theme.BgInput, ForeColor = Theme.TextPrimary };
            var btn    = new Button { Text = "▶ Ping", Location = new Point(465, 12), Width = 80, BackColor = Theme.Accent, ForeColor = Theme.BgDark, FlatStyle = FlatStyle.Flat, Font = Theme.FontButton, Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderSize = 0;
            var rtb = MakeRTB();
            rtb.Location = new Point(0, 50); rtb.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            btn.Click += (s, e) =>
            {
                var host = input.Text.Trim();
                if (string.IsNullOrEmpty(host)) { MessageBox.Show("Nhập host/IP", "WinPE"); return; }
                rtb.Clear();
                int count = (int)cntBox.Value;
                btn.Enabled = false;

                System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                {
                    AppendRTB(rtb, $"Pinging {host} x{count}...\n", Theme.Accent, Theme.FontHeader);
                    for (int i = 0; i < count; i++)
                    {
                        try
                        {
                            using var ping = new Ping();
                            var reply = ping.Send(host, 3000);
                            bool ok = reply.Status == IPStatus.Success;
                            AppendRTB(rtb,
                                $"  [{i+1}/{count}] {(ok ? "✅" : "❌")} {reply.Status} — {(ok ? $"{reply.RoundtripTime}ms" : "timeout")}\n",
                                ok ? Theme.TextSuccess : Theme.TextError);
                        }
                        catch (Exception ex) { AppendRTB(rtb, $"  ❌ Error: {ex.Message}\n", Theme.TextError); }
                        System.Threading.Thread.Sleep(200);
                    }
                    AppendRTB(rtb, "Done.\n", Theme.TextMuted);
                    SafeUpdate(() => btn.Enabled = true);
                });
            };

            input.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) btn.PerformClick(); };
            tp.Controls.AddRange(new Control[] { lbl, input, cntLbl, cntBox, btn, rtb });
            return tp;
        }

        // ─── TAB 3: DNS ──────────────────────────────────────────────────────
        private TabPage BuildDNSTab()
        {
            var tp    = new TabPage("🔍 DNS Lookup") { BackColor = Theme.BgDark, Padding = new Padding(12) };
            var lbl   = new Label { Text = "Domain:", Font = Theme.FontBody, ForeColor = Theme.TextSecondary, Location = new Point(12, 18), AutoSize = true };
            var input = new TextBox { Location = new Point(75, 14), Width = 280, BackColor = Theme.BgInput, ForeColor = Theme.TextPrimary, Font = Theme.FontBody };
            var btn   = new Button { Text = "Lookup", Location = new Point(365, 12), Width = 80, BackColor = Theme.Accent, ForeColor = Theme.BgDark, FlatStyle = FlatStyle.Flat, Font = Theme.FontButton, Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderSize = 0;
            var rtb   = MakeRTB();
            rtb.Location = new Point(0, 50); rtb.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            btn.Click += (s, e) =>
            {
                var domain = input.Text.Trim();
                if (string.IsNullOrEmpty(domain)) return;
                rtb.Clear();
                System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        var ips = System.Net.Dns.GetHostAddresses(domain);
                        AppendRTB(rtb, $"DNS Lookup: {domain}\n", Theme.Accent, Theme.FontHeader);
                        foreach (var ip in ips)
                            AppendRTB(rtb, $"  → {ip}\n", Theme.TextSuccess);
                    }
                    catch (Exception ex) { AppendRTB(rtb, $"❌ {ex.Message}\n", Theme.TextError); }

                    var ns = ProcessRunner.RunCmd($"nslookup {domain}");
                    AppendRTB(rtb, "\nnslookup output:\n", Theme.Accent, Theme.FontHeader);
                    AppendRTB(rtb, ns.Output, Theme.TextSecondary);
                });
            };

            tp.Controls.AddRange(new Control[] { lbl, input, btn, rtb });
            return tp;
        }

        // ─── TAB 4: Network Share ────────────────────────────────────────────
        private TabPage BuildShareTab()
        {
            var tp     = new TabPage("📂 Network Share") { BackColor = Theme.BgDark, Padding = new Padding(12) };
            var lbl1   = new Label { Text = "UNC Path:", Font = Theme.FontBody, ForeColor = Theme.TextSecondary, Location = new Point(12, 18), AutoSize = true };
            var pathIn = new TextBox { Location = new Point(90, 14), Width = 240, BackColor = Theme.BgInput, ForeColor = Theme.TextPrimary, Font = Theme.FontBody, Text = @"\\server\share" };
            var lbl2   = new Label { Text = "Ký tự:", Font = Theme.FontBody, ForeColor = Theme.TextSecondary, Location = new Point(340, 18), AutoSize = true };
            var letterIn = new TextBox { Location = new Point(385, 14), Width = 30, BackColor = Theme.BgInput, ForeColor = Theme.TextPrimary, Font = Theme.FontBody, Text = "Z" };
            var mapBtn = new Button { Text = "Map Drive", Location = new Point(425, 12), Width = 90, BackColor = Theme.Accent, ForeColor = Theme.BgDark, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            mapBtn.FlatAppearance.BorderSize = 0;
            var rtb    = MakeRTB();
            rtb.Location = new Point(0, 50); rtb.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            mapBtn.Click += (s, e) =>
            {
                var path = pathIn.Text.Trim();
                var letter = letterIn.Text.Trim().TrimEnd(':').ToUpper();
                if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(letter)) return;

                AppendRTB(rtb, $"Mapping {path} → {letter}:\n", Theme.TextWarning, Theme.FontHeader);
                var result = ProcessRunner.RunCmd($"net use {letter}: \"{path}\" /persistent:no");
                AppendRTB(rtb, result.Success ? $"✅ {result.Output}\n" : $"❌ {result.Error}\n",
                    result.Success ? Theme.TextSuccess : Theme.TextError);
            };

            tp.Controls.AddRange(new Control[] { lbl1, pathIn, lbl2, letterIn, mapBtn, rtb });
            return tp;
        }

        // ─── TAB 5: Static IP ────────────────────────────────────────────────
        private TabPage BuildStaticIPTab()
        {
            var tp = new TabPage("⚙ Cấu hình IP") { BackColor = Theme.BgDark, Padding = new Padding(16) };

            int y = 16;
            var note = new Label { Text = "⚠️  Cẩn thận khi thay đổi cấu hình IP trong WinPE", Font = Theme.FontBody, ForeColor = Theme.TextWarning, Location = new Point(0, y), AutoSize = true };
            tp.Controls.Add(note); y += 30;

            (TextBox ip, int ny)  = AddField(tp, "Địa chỉ IP:",  "192.168.1.100", y); y = ny;
            (TextBox mask, int ny2) = AddField(tp, "Subnet Mask:", "255.255.255.0", y); y = ny2;
            (TextBox gw, int ny3)  = AddField(tp, "Gateway:",     "192.168.1.1",   y); y = ny3;
            (TextBox dns, int ny4) = AddField(tp, "DNS Primary:", "8.8.8.8",       y); y = ny4;

            var adapLbl = new Label { Text = "Adapter:", Font = Theme.FontBody, ForeColor = Theme.TextSecondary, Location = new Point(0, y), AutoSize = true };
            var adapCb  = new ComboBox { Location = new Point(130, y - 3), Width = 300, BackColor = Theme.BgInput, ForeColor = Theme.TextPrimary, DropDownStyle = ComboBoxStyle.DropDownList };
            tp.Controls.Add(adapLbl); tp.Controls.Add(adapCb); y += 32;

            // Load adapters
            var nics = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            foreach (var nic in nics)
                if (nic.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
                    adapCb.Items.Add($"{nic.Name} — {nic.Description}");
            if (adapCb.Items.Count > 0) adapCb.SelectedIndex = 0;

            var applyBtn = new Button { Text = "Áp dụng IP tĩnh", Location = new Point(0, y), Width = 140, BackColor = Theme.Accent, ForeColor = Theme.BgDark, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            var dhcpBtn  = new Button { Text = "Dùng DHCP", Location = new Point(150, y), Width = 120, BackColor = Theme.BgCard, ForeColor = Theme.TextPrimary, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            applyBtn.FlatAppearance.BorderSize = 0;
            dhcpBtn.FlatAppearance.BorderColor = Theme.Border;
            tp.Controls.Add(applyBtn); tp.Controls.Add(dhcpBtn);

            applyBtn.Click += (s, e) =>
            {
                var adaptName = adapCb.SelectedItem?.ToString()?.Split('—')[0]?.Trim() ?? "";
                if (string.IsNullOrEmpty(adaptName)) return;
                var cmd = $"netsh interface ip set address \"{adaptName}\" static {ip.Text} {mask.Text} {gw.Text}";
                var r = ProcessRunner.RunCmd(cmd);
                MessageBox.Show(r.Success ? "✅ IP tĩnh đã được áp dụng." : $"❌ Thất bại: {r.Error}",
                    "WinPE", MessageBoxButtons.OK, r.Success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
            };

            dhcpBtn.Click += (s, e) =>
            {
                var adaptName = adapCb.SelectedItem?.ToString()?.Split('—')[0]?.Trim() ?? "";
                ProcessRunner.RunCmd($"netsh interface ip set address \"{adaptName}\" dhcp");
                MessageBox.Show("✅ Đã chuyển sang DHCP.", "WinPE", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            return tp;
        }

        // ─── HELPERS ─────────────────────────────────────────────────────────
        private (TextBox, int) AddField(Control parent, string label, string defVal, int y)
        {
            var lbl = new Label { Text = label, Font = Theme.FontBody, ForeColor = Theme.TextSecondary, Location = new Point(0, y + 3), AutoSize = true };
            var tb  = new TextBox { Location = new Point(130, y), Width = 200, BackColor = Theme.BgInput, ForeColor = Theme.TextPrimary, Font = Theme.FontBody, Text = defVal };
            parent.Controls.Add(lbl); parent.Controls.Add(tb);
            return (tb, y + 32);
        }

        private RichTextBox MakeRTB()
        {
            return new RichTextBox
            {
                Dock        = DockStyle.Fill,
                BackColor   = Theme.BgCard,
                ForeColor   = Theme.TextPrimary,
                Font        = Theme.FontMono,
                ReadOnly    = true,
                BorderStyle = BorderStyle.None,
                ScrollBars  = RichTextBoxScrollBars.Both,
            };
        }

        private Button MakeBtn(string text, Action click)
        {
            var btn = new Button { Text = text, Font = Theme.FontSmall, ForeColor = Theme.Accent, BackColor = Theme.BgCard, FlatStyle = FlatStyle.Flat, AutoSize = true, Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderColor = Theme.Border;
            btn.Click += (s, e) => click();
            return btn;
        }

        private void AppendRTB(RichTextBox rtb, string text, Color color, Font font = null)
        {
            SafeUpdate(() =>
            {
                rtb.SelectionStart  = rtb.TextLength;
                rtb.SelectionColor  = color;
                if (font != null) rtb.SelectionFont = font;
                rtb.AppendText(text);
            });
        }

        private TabPage GetTab(int idx) => _tabs.TabPages.Count > idx ? _tabs.TabPages[idx] : null;

        private void SafeUpdate(Action action)
        {
            if (IsDisposed) return;
            try { if (InvokeRequired) Invoke(action); else action(); }
            catch { }
        }
    }
}
