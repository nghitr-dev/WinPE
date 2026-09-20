using System;
using System.Collections.Generic;
using System.Drawing;
using System.Management;
using System.Windows.Forms;
using WinPETool.Core;
using WinPETool.UI;

namespace WinPETool.UI.Pages
{
    /// <summary>
    /// Hardware Diagnostics Page — hiển thị CPU, RAM, Disk, GPU, Network, Battery
    /// Phân biệt rõ: Information vs Diagnostic vs Health Status
    /// </summary>
    public class HardwarePage : Panel
    {
        private TabControl _tabs;
        private Button _refreshBtn;
        private Label _statusLbl;

        public HardwarePage(AppConfig config)
        {
            BackColor = Theme.BgDark;
            Padding   = new Padding(20);
            Dock      = DockStyle.Fill;
            BuildUI();
            LoadHardwareAsync();
        }

        private void BuildUI()
        {
            // Header
            var header = MakeHeader("🖥️  Phần cứng & Chẩn đoán", Theme.CatHardware);
            Controls.Add(header);

            _refreshBtn = new Button
            {
                Text      = "🔄  Làm mới",
                Font      = Theme.FontBody,
                ForeColor = Theme.Accent,
                BackColor = Theme.BgCard,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(110, 32),
                Location  = new Point(Width - 140, 22),
                Anchor    = AnchorStyles.Top | AnchorStyles.Right,
                Cursor    = Cursors.Hand,
            };
            _refreshBtn.FlatAppearance.BorderColor = Theme.Accent;
            _refreshBtn.Click += (s, e) => LoadHardwareAsync();
            Controls.Add(_refreshBtn);

            _statusLbl = new Label
            {
                Text      = "⏳ Đang đọc thông tin phần cứng...",
                Font      = Theme.FontSmall,
                ForeColor = Theme.TextMuted,
                AutoSize  = true,
                Location  = new Point(20, 58),
            };
            Controls.Add(_statusLbl);

            // Tabs
            _tabs = new TabControl
            {
                Location  = new Point(0, 80),
                Size      = new Size(Width, Height - 90),
                Anchor    = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Font      = Theme.FontBody,
            };
            StyleTabs(_tabs);
            Controls.Add(_tabs);

            _tabs.TabPages.Add(MakeTabPage("💻 CPU"));
            _tabs.TabPages.Add(MakeTabPage("🧠 RAM"));
            _tabs.TabPages.Add(MakeTabPage("💿 Storage"));
            _tabs.TabPages.Add(MakeTabPage("🖥 GPU"));
            _tabs.TabPages.Add(MakeTabPage("🌐 Network"));
            _tabs.TabPages.Add(MakeTabPage("🔋 Battery"));
            _tabs.TabPages.Add(MakeTabPage("🔌 USB / Other"));
        }

        private void LoadHardwareAsync()
        {
            SafeUpdate(() =>
            {
                _statusLbl.Text = "⏳ Đang đọc thông tin phần cứng...";
                _refreshBtn.Enabled = false;
                foreach (TabPage tp in _tabs.TabPages)
                {
                    GetRTB(tp)?.Clear();
                }
            });

            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                LoadCPU();
                LoadRAM();
                LoadStorage();
                LoadGPU();
                LoadNetwork();
                LoadBattery();
                LoadUSB();
                SafeUpdate(() =>
                {
                    _statusLbl.Text = $"✅ Cập nhật lúc {DateTime.Now:HH:mm:ss}";
                    _refreshBtn.Enabled = true;
                });
            });
        }

        // ─── CPU ──────────────────────────────────────────────────────────────
        private void LoadCPU()
        {
            var rtb = GetRTB(_tabs.TabPages[0]);
            try
            {
                using var q = new ManagementObjectSearcher(
                    "SELECT Name, Manufacturer, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed, CurrentVoltage, Architecture, L2CacheSize, L3CacheSize FROM Win32_Processor");

                foreach (ManagementObject o in q.Get())
                {
                    AppendLine(rtb, "=== THÔNG TIN CPU ===", Theme.Accent, Theme.FontHeader);
                    AppendKV(rtb, "Tên",           o["Name"]?.ToString()?.Trim());
                    AppendKV(rtb, "Nhà sản xuất",  o["Manufacturer"]?.ToString());
                    AppendKV(rtb, "Kiến trúc",     DecodeArch(o["Architecture"]?.ToString()));
                    AppendKV(rtb, "Số nhân (Core)", o["NumberOfCores"]?.ToString());
                    AppendKV(rtb, "Luồng (Thread)", o["NumberOfLogicalProcessors"]?.ToString());
                    var mhz = o["MaxClockSpeed"]?.ToString();
                    AppendKV(rtb, "Tốc độ tối đa",
                        double.TryParse(mhz, out double m) ? $"{m/1000:F2} GHz ({mhz} MHz)" : mhz);
                    AppendKV(rtb, "Cache L2",      o["L2CacheSize"] + " KB");
                    AppendKV(rtb, "Cache L3",      o["L3CacheSize"] + " KB");

                    AppendLine(rtb, "\n=== CHẨN ĐOÁN ===", Theme.Accent, Theme.FontHeader);
                    AppendStatus(rtb, "CPU được phát hiện", true);
                    AppendStatus(rtb, "Số nhân > 0", int.TryParse(o["NumberOfCores"]?.ToString(), out int c) && c > 0);
                    AppendLine(rtb, "\nℹ️  Lưu ý: WinPE không hỗ trợ đọc nhiệt độ CPU nếu không có driver sensor.", Theme.TextMuted);
                }
            }
            catch (Exception ex) { AppendError(rtb, ex.Message); }
        }

        // ─── RAM ──────────────────────────────────────────────────────────────
        private void LoadRAM()
        {
            var rtb = GetRTB(_tabs.TabPages[1]);
            try
            {
                // Total
                using var cs = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
                foreach (ManagementObject o in cs.Get())
                {
                    var totalBytes = Convert.ToInt64(o["TotalPhysicalMemory"]);
                    AppendLine(rtb, "=== TỔNG QUAN RAM ===", Theme.Accent, Theme.FontHeader);
                    AppendKV(rtb, "RAM tổng cộng", $"{totalBytes / 1024.0 / 1024 / 1024:F2} GB");
                }

                // Per slot
                AppendLine(rtb, "\n=== CHI TIẾT TỪNG THANH RAM ===", Theme.Accent, Theme.FontHeader);
                int slot = 0;
                using var ram = new ManagementObjectSearcher(
                    "SELECT Manufacturer, Capacity, Speed, MemoryType, FormFactor, PartNumber, BankLabel FROM Win32_PhysicalMemory");
                foreach (ManagementObject o in ram.Get())
                {
                    slot++;
                    var cap = Convert.ToInt64(o["Capacity"] ?? 0);
                    AppendLine(rtb, $"\nSlot #{slot} — {o["BankLabel"]}", Theme.TextWarning, Theme.FontBody);
                    AppendKV(rtb, "Dung lượng",    $"{cap / 1024.0 / 1024 / 1024:F0} GB");
                    AppendKV(rtb, "Tốc độ",        o["Speed"] + " MHz");
                    AppendKV(rtb, "Nhà sản xuất",  o["Manufacturer"]?.ToString()?.Trim());
                    AppendKV(rtb, "Part Number",   o["PartNumber"]?.ToString()?.Trim());
                    AppendKV(rtb, "Loại",          DecodeMemType(o["MemoryType"]?.ToString()));
                    AppendKV(rtb, "Form Factor",   DecodeFormFactor(o["FormFactor"]?.ToString()));
                }

                if (slot == 0)
                    AppendLine(rtb, "⚠️  Không đọc được thông tin slot RAM chi tiết.", Theme.TextWarning);
            }
            catch (Exception ex) { AppendError(rtb, ex.Message); }
        }

        // ─── STORAGE ─────────────────────────────────────────────────────────
        private void LoadStorage()
        {
            var rtb = GetRTB(_tabs.TabPages[2]);
            try
            {
                AppendLine(rtb, "=== Ổ ĐĨA VẬT LÝ ===", Theme.Accent, Theme.FontHeader);
                using var disks = new ManagementObjectSearcher(
                    "SELECT Index, Model, Manufacturer, Size, MediaType, SerialNumber, InterfaceType FROM Win32_DiskDrive");
                foreach (ManagementObject o in disks.Get())
                {
                    var sizeBytes = Convert.ToInt64(o["Size"] ?? 0);
                    var idx = o["Index"]?.ToString() ?? "?";
                    AppendLine(rtb, $"\nDisk {idx}: {o["Model"]?.ToString()?.Trim()}", Theme.TextWarning, Theme.FontBody);
                    AppendKV(rtb, "Nhà sản xuất",  o["Manufacturer"]?.ToString()?.Trim());
                    AppendKV(rtb, "Kích thước",    $"{sizeBytes / 1024.0 / 1024 / 1024:F1} GB");
                    AppendKV(rtb, "Interface",     o["InterfaceType"]?.ToString());
                    AppendKV(rtb, "Media Type",    o["MediaType"]?.ToString());
                    AppendKV(rtb, "Serial",        o["SerialNumber"]?.ToString()?.Trim());

                    AppendStatus(rtb, "Disk accessible", sizeBytes > 0);
                }

                AppendLine(rtb, "\n=== PHÂN VÙNG ===", Theme.Accent, Theme.FontHeader);
                using var parts = new ManagementObjectSearcher(
                    "SELECT DiskIndex, Index, Name, Size, Type FROM Win32_DiskPartition");
                foreach (ManagementObject o in parts.Get())
                {
                    var sizeBytes = Convert.ToInt64(o["Size"] ?? 0);
                    AppendLine(rtb,
                        $"Disk{o["DiskIndex"]} Partition{o["Index"]}: {o["Type"]} — {sizeBytes/1024/1024/1024} GB",
                        Theme.TextSecondary);
                }

                AppendLine(rtb, "\n=== VOLUMES ===", Theme.Accent, Theme.FontHeader);
                foreach (var drive in System.IO.DriveInfo.GetDrives())
                {
                    if (!drive.IsReady) continue;
                    AppendLine(rtb,
                        $"{drive.Name}  {drive.DriveFormat}  Total:{drive.TotalSize/1024/1024/1024}GB  Free:{drive.AvailableFreeSpace/1024/1024/1024}GB",
                        Theme.TextSecondary);
                }

                AppendLine(rtb, "\nℹ️  SMART data cần tool ngoài (crystaldiskinfo portable) — không có sẵn trong WinPE base.", Theme.TextMuted);
            }
            catch (Exception ex) { AppendError(rtb, ex.Message); }
        }

        // ─── GPU ─────────────────────────────────────────────────────────────
        private void LoadGPU()
        {
            var rtb = GetRTB(_tabs.TabPages[3]);
            try
            {
                AppendLine(rtb, "=== GPU / VIDEO ADAPTER ===", Theme.Accent, Theme.FontHeader);
                using var q = new ManagementObjectSearcher(
                    "SELECT Name, AdapterRAM, DriverVersion, VideoModeDescription, CurrentRefreshRate FROM Win32_VideoController");
                int idx = 0;
                foreach (ManagementObject o in q.Get())
                {
                    idx++;
                    AppendLine(rtb, $"\nGPU #{idx}: {o["Name"]?.ToString()?.Trim()}", Theme.TextWarning, Theme.FontBody);
                    var vram = Convert.ToInt64(o["AdapterRAM"] ?? 0);
                    AppendKV(rtb, "VRAM",           vram > 0 ? $"{vram/1024/1024} MB" : "Unknown");
                    AppendKV(rtb, "Driver",          o["DriverVersion"]?.ToString());
                    AppendKV(rtb, "Resolution",      o["VideoModeDescription"]?.ToString());
                    AppendKV(rtb, "Refresh Rate",    o["CurrentRefreshRate"] + " Hz");
                }
                if (idx == 0) AppendLine(rtb, "⚠️  Không phát hiện GPU.", Theme.TextWarning);
                AppendLine(rtb, "\nℹ️  WinPE chạy với driver VESA/generic — GPU acceleration không hoạt động.", Theme.TextMuted);
            }
            catch (Exception ex) { AppendError(rtb, ex.Message); }
        }

        // ─── NETWORK ─────────────────────────────────────────────────────────
        private void LoadNetwork()
        {
            var rtb = GetRTB(_tabs.TabPages[4]);
            try
            {
                AppendLine(rtb, "=== NETWORK ADAPTERS ===", Theme.Accent, Theme.FontHeader);
                using var q = new ManagementObjectSearcher(
                    "SELECT Description, MACAddress, Speed, NetEnabled FROM Win32_NetworkAdapter WHERE PhysicalAdapter=True");
                int idx = 0;
                foreach (ManagementObject o in q.Get())
                {
                    idx++;
                    AppendLine(rtb, $"\nAdapter #{idx}: {o["Description"]?.ToString()?.Trim()}", Theme.TextWarning, Theme.FontBody);
                    AppendKV(rtb, "MAC Address",  o["MACAddress"]?.ToString());
                    var spd = o["Speed"]?.ToString();
                    AppendKV(rtb, "Speed",        !string.IsNullOrEmpty(spd) ? $"{Convert.ToInt64(spd)/1000000} Mbps" : "Unknown");
                    AppendKV(rtb, "Enabled",      o["NetEnabled"]?.ToString());
                }

                AppendLine(rtb, "\n=== IP CONFIGURATION ===", Theme.Accent, Theme.FontHeader);
                var ipResult = ProcessRunner.RunCmd("ipconfig /all");
                AppendLine(rtb, ipResult.Output, Theme.TextSecondary);
            }
            catch (Exception ex) { AppendError(rtb, ex.Message); }
        }

        // ─── BATTERY ─────────────────────────────────────────────────────────
        private void LoadBattery()
        {
            var rtb = GetRTB(_tabs.TabPages[5]);
            try
            {
                using var q = new ManagementObjectSearcher(
                    "SELECT EstimatedChargeRemaining, BatteryStatus, DesignCapacity, FullChargeCapacity FROM Win32_Battery");
                int found = 0;
                foreach (ManagementObject o in q.Get())
                {
                    found++;
                    AppendLine(rtb, "=== BATTERY ===", Theme.Accent, Theme.FontHeader);
                    AppendKV(rtb, "Charge",         o["EstimatedChargeRemaining"] + "%");
                    AppendKV(rtb, "Status",         DecodeBatteryStatus(o["BatteryStatus"]?.ToString()));
                    AppendKV(rtb, "Design Capacity", o["DesignCapacity"]?.ToString());
                    AppendKV(rtb, "Full Charge Cap", o["FullChargeCapacity"]?.ToString());
                }
                if (found == 0)
                {
                    AppendLine(rtb, "ℹ️  Không phát hiện battery.", Theme.TextMuted);
                    AppendLine(rtb, "    (Máy tính để bàn hoặc driver battery không có trong WinPE)", Theme.TextMuted);
                }
            }
            catch (Exception ex) { AppendError(rtb, ex.Message); }
        }

        // ─── USB ─────────────────────────────────────────────────────────────
        private void LoadUSB()
        {
            var rtb = GetRTB(_tabs.TabPages[6]);
            try
            {
                AppendLine(rtb, "=== USB DEVICES ===", Theme.Accent, Theme.FontHeader);
                using var q = new ManagementObjectSearcher(
                    "SELECT Description, DeviceID, Manufacturer FROM Win32_USBHub");
                foreach (ManagementObject o in q.Get())
                {
                    AppendLine(rtb, $"• {o["Description"]?.ToString()?.Trim()} [{o["DeviceID"]}]", Theme.TextSecondary);
                }

                AppendLine(rtb, "\n=== USB CONTROLLERS ===", Theme.Accent, Theme.FontHeader);
                using var ctrl = new ManagementObjectSearcher(
                    "SELECT Name FROM Win32_USBController");
                foreach (ManagementObject o in ctrl.Get())
                {
                    AppendLine(rtb, $"• {o["Name"]?.ToString()?.Trim()}", Theme.TextSecondary);
                }
            }
            catch (Exception ex) { AppendError(rtb, ex.Message); }
        }

        // ─── HELPERS ──────────────────────────────────────────────────────────
        private Panel MakeHeader(string text, Color accent)
        {
            var p = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.Transparent };
            var bar = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = accent };
            var lbl = new Label { Text = text, Font = Theme.FontTitle, ForeColor = Theme.TextPrimary, AutoSize = true, Location = new Point(14, 12) };
            p.Controls.Add(bar); p.Controls.Add(lbl);
            return p;
        }

        private TabPage MakeTabPage(string title)
        {
            var tp = new TabPage(title) { BackColor = Theme.BgDark, Padding = new Padding(8) };
            var rtb = new RichTextBox
            {
                Dock        = DockStyle.Fill,
                BackColor   = Theme.BgCard,
                ForeColor   = Theme.TextPrimary,
                Font        = Theme.FontMono,
                ReadOnly    = true,
                BorderStyle = BorderStyle.None,
                Padding     = new Padding(12),
                ScrollBars  = RichTextBoxScrollBars.Vertical,
            };
            tp.Controls.Add(rtb);
            return tp;
        }

        private static void StyleTabs(TabControl tc)
        {
            tc.DrawMode   = TabDrawMode.OwnerDrawFixed;
            tc.Appearance = TabAppearance.Normal;
            tc.BackColor  = Theme.BgDark;
        }

        private RichTextBox GetRTB(TabPage tp) => tp.Controls[0] as RichTextBox;

        private void AppendLine(RichTextBox rtb, string text, Color? color = null, Font font = null)
        {
            SafeUpdate(() =>
            {
                rtb.SelectionStart  = rtb.TextLength;
                rtb.SelectionColor  = color ?? Theme.TextPrimary;
                rtb.SelectionFont   = font ?? Theme.FontMono;
                rtb.AppendText(text + "\n");
            });
        }

        private void AppendKV(RichTextBox rtb, string key, string value)
        {
            SafeUpdate(() =>
            {
                rtb.SelectionStart = rtb.TextLength;
                rtb.SelectionColor = Theme.TextMuted;
                rtb.SelectionFont  = Theme.FontMono;
                rtb.AppendText($"  {key,-22}: ");
                rtb.SelectionColor = Theme.TextPrimary;
                rtb.AppendText((value ?? "(null)") + "\n");
            });
        }

        private void AppendStatus(RichTextBox rtb, string label, bool ok)
        {
            SafeUpdate(() =>
            {
                rtb.SelectionStart = rtb.TextLength;
                rtb.SelectionColor = ok ? Theme.TextSuccess : Theme.TextError;
                rtb.AppendText($"  {(ok ? "✅" : "❌")} {label}\n");
            });
        }

        private void AppendError(RichTextBox rtb, string msg)
        {
            SafeUpdate(() =>
            {
                rtb.SelectionColor = Theme.TextError;
                rtb.AppendText($"❌ Lỗi: {msg}\n");
            });
        }

        private void SafeUpdate(Action action)
        {
            if (IsDisposed) return;
            try
            {
                if (InvokeRequired) Invoke(action);
                else action();
            }
            catch { }
        }

        // ─── Decode helpers ───────────────────────────────────────────────────
        private static string DecodeArch(string code) => code switch
        {
            "0" => "x86", "1" => "MIPS", "2" => "Alpha", "3" => "PowerPC",
            "5" => "ARM", "6" => "ia64", "9" => "x64 (AMD64)", _ => code ?? "Unknown"
        };

        private static string DecodeMemType(string code) => code switch
        {
            "20" => "DDR", "21" => "DDR2", "22" => "DDR2 FB-DIMM", "24" => "DDR3",
            "26" => "DDR4", "34" => "DDR5", _ => $"Type {code}"
        };

        private static string DecodeFormFactor(string code) => code switch
        {
            "8" => "DIMM", "12" => "SODIMM", "13" => "SRIMM", _ => $"Code {code}"
        };

        private static string DecodeBatteryStatus(string code) => code switch
        {
            "1" => "Discharging", "2" => "AC Power (Plugged in)", "3" => "Fully Charged",
            "4" => "Low", "5" => "Critical", "6" => "Charging", "7" => "Charging + High",
            "8" => "Charging + Low", "9" => "Charging + Critical", "10" => "Undefined",
            "11" => "Partially Charged", _ => $"Unknown ({code})"
        };
    }
}
