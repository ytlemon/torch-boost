using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace TLIOptimizer
{
    public class MainForm : Form
    {
        private const string APP_TITLE = "Torchlight Infinite Optimizer v1.0";
        private static readonly string CFG_DIR = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TLIOptimizer");
        private static readonly string BACKUP_FILE = Path.Combine(CFG_DIR, "backup.txt");
        private static readonly string SETTINGS_FILE = Path.Combine(CFG_DIR, "settings.txt");

        private static readonly string[] PROC_NAMES = {
            "TorchlightInfinite", "Torchlight", "TLI", "TorchlightInfinite-Win64-Shipping",
            "Torchlight_Infinite", "TLInfinite", "TorchlightMobile", "ProjectT"
        };
        private static readonly string[] GAME_DIR_HINTS = {
            "Torchlight Infinite", "TorchlightInfinite", "Torchlight", "火炬之光无限", "火炬之光：无限"
        };

        private Label lblStatus, lblDetect, lblMem, lblNet, lblAdmin;
        private CheckBox[] checks;
        private ComboBox cmboDNS, cmboPower;
        private Button btnApply, btnRestore, btnDetect, btnPing;
        private ListView lvProcesses;
        private System.Windows.Forms.Timer timer;
        private string detectedExe = "", detectedProc = "";

        public MainForm()
        {
            Text = APP_TITLE;
            Size = new Size(860, 660);
            StartPosition = FormStartPosition.Manual;
            Location = new Point(100, 40);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Color.FromArgb(26, 28, 34);
            ForeColor = Color.FromArgb(230, 232, 238);
            Font = new Font("Microsoft YaHei", 9F);
            Directory.CreateDirectory(CFG_DIR);

            BuildUI();
            DetectGame();
            RefreshProcesses();
            RefreshStatus();
            LoadSettings();

            timer = new System.Windows.Forms.Timer { Interval = 2000 };
            timer.Tick += (s, e) => RefreshStatus();
            timer.Start();
        }

        private Control Panel(string title, int y, int h)
        {
            var g = new GroupBox { Text = title, Location = new Point(14, y), Size = new Size(816, h),
                ForeColor = Color.FromArgb(110, 190, 255), Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold) };
            Controls.Add(g); return g;
        }

        private CheckBox Chk(Control p, string text, int x, int y, int w = 780)
        {
            var c = new CheckBox { Text = text, Location = new Point(x, y), Size = new Size(w, 22) };
            p.Controls.Add(c); return c;
        }

        private Button Btn(Control p, string text, int x, int y, int w, int h, Color? bg = null)
        {
            var b = new Button { Text = text, Location = new Point(x, y), Size = new Size(w, h),
                BackColor = bg ?? Color.FromArgb(52, 58, 70), ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 9F) };
            b.FlatAppearance.BorderColor = Color.FromArgb(88, 98, 118);
            p.Controls.Add(b); return b;
        }

        private void BuildUI()
        {
            var title = new Label { Text = "Torchlight: Infinite 优化工具", Location = new Point(14, 10),
                Size = new Size(500, 32), Font = new Font("Microsoft YaHei", 15F, FontStyle.Bold),
                ForeColor = Color.FromArgb(90, 190, 255) };
            Controls.Add(title);
            lblAdmin = new Label { Location = new Point(560, 16), Size = new Size(270, 22),
                ForeColor = Color.FromArgb(200, 204, 214), TextAlign = ContentAlignment.MiddleRight };
            Controls.Add(lblAdmin);
            lblAdmin.Text = IsAdmin() ? "管理员权限：是" : "管理员权限：否（部分功能受限）";

            // ① 检测区
            var g0 = Panel("① 游戏检测", 48, 78);
            lblDetect = new Label { Location = new Point(14, 24), Size = new Size(620, 46),
                ForeColor = Color.FromArgb(210, 214, 224) };
            g0.Controls.Add(lblDetect);
            btnDetect = Btn(g0, "重新检测", 686, 24, 116, 30);
            btnDetect.Click += (s, e) => { DetectGame(); RefreshProcesses(); };

            // ② CPU
            var g1 = Panel("② CPU 优化", 134, 112);
            checks = new CheckBox[8];
            checks[0] = Chk(g1, "进程优先级设为“高于正常”（游戏运行时点优化生效）", 14, 22);
            checks[1] = Chk(g1, "启用 Windows 游戏模式（Game Mode，优先调度游戏）", 14, 44);
            checks[2] = Chk(g1, "电源计划设为“高性能”（释放 CPU 全频）", 14, 66);
            checks[3] = Chk(g1, "清理后台占 CPU 进程（下方列表勾选）", 14, 88);
            checks[0].Checked = checks[1].Checked = checks[2].Checked = true;

            // ③ GPU
            var g2 = Panel("③ GPU / 画质优化", 254, 90);
            checks[4] = Chk(g2, "硬件加速 GPU 调度（HAGS，需 Win10 1903+ 和重启）", 14, 22);
            checks[5] = Chk(g2, "关闭全屏优化（减少输入延迟和掉帧）", 14, 44);
            checks[6] = Chk(g2, "GPU 电源管理设为“最高性能”（NVIDIA/AMD 全局）", 14, 66);
            checks[4].Checked = true;

            // ④ 内存
            var g3 = Panel("④ 内存优化", 352, 68);
            checks[7] = Chk(g3, "清理系统工作集 + 关闭后台占内存进程", 14, 22);
            checks[7].Checked = true;

            // ⑤ 网络
            var g4 = Panel("⑤ 网络优化", 428, 92);
            var l1 = new Label { Text = "DNS：", Location = new Point(14, 24), Size = new Size(50, 24) }; g4.Controls.Add(l1);
            cmboDNS = new ComboBox { Location = new Point(64, 22), Size = new Size(240, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            cmboDNS.Items.AddRange(new object[] { "不改（保持系统默认）", "阿里 DNS 223.5.5.5 / 223.6.6.6", "腾讯 DNSPod 119.29.29.29", "Google 8.8.8.8 / 8.8.4.4", "Cloudflare 1.1.1.1 / 1.0.0.1" });
            cmboDNS.SelectedIndex = 0; g4.Controls.Add(cmboDNS);
            btnPing = Btn(g4, "测试延迟", 320, 21, 110, 26);
            btnPing.Click += BtnPing_Click;
            lblNet = new Label { Location = new Point(440, 22), Size = new Size(360, 46), ForeColor = Color.FromArgb(200, 204, 214) };
            g4.Controls.Add(lblNet);
            var l2 = new Label { Text = "电源：", Location = new Point(14, 56), Size = new Size(50, 24) }; g4.Controls.Add(l2);
            cmboPower = new ComboBox { Location = new Point(64, 54), Size = new Size(240, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            cmboPower.Items.AddRange(new object[] { "自动（不改）", "高性能", "卓越性能" });
            cmboPower.SelectedIndex = 0; g4.Controls.Add(cmboPower);

            // 进程列表
            var g5 = Panel("⑥ 可关闭的后台进程（勾选后一键优化会结束它们）", 528, 62);
            lvProcesses = new ListView { Location = new Point(14, 20), Size = new Size(640, 40),
                View = View.Details, CheckBoxes = true, FullRowSelect = true,
                BackColor = Color.FromArgb(36, 38, 45), ForeColor = Color.FromArgb(230, 232, 238) };
            lvProcesses.Columns.Add("进程名", 150); lvProcesses.Columns.Add("PID", 60);
            lvProcesses.Columns.Add("内存(MB)", 90); lvProcesses.Columns.Add("说明", 300);
            g5.Controls.Add(lvProcesses);
            Btn(g5, "刷新", 664, 20, 70, 26).Click += (s, e) => RefreshProcesses();
            Btn(g5, "结束选中", 738, 20, 70, 26).Click += (s, e) => KillChecked();

            // 底部
            btnApply = new Button { Text = "一键优化", Location = new Point(14, 598), Size = new Size(180, 38),
                BackColor = Color.FromArgb(40, 120, 200), ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold) };
            btnApply.Click += BtnApply_Click; Controls.Add(btnApply);
            btnRestore = Btn(this, "一键恢复", 204, 598, 140, 38, Color.FromArgb(120, 70, 70));
            btnRestore.Click += BtnRestore_Click;
            var help = Btn(this, "使用说明", 354, 598, 110, 38); help.Click += Help_Click;
            lblMem = new Label { Location = new Point(480, 600), Size = new Size(350, 22),
                ForeColor = Color.FromArgb(200, 204, 214) }; Controls.Add(lblMem);
            lblStatus = new Label { Location = new Point(14, 640), Size = new Size(816, 20),
                ForeColor = Color.FromArgb(140, 230, 150) }; Controls.Add(lblStatus);
        }

        // ---------- 检测 ----------

        private bool IsAdmin()
        {
            using (var id = System.Security.Principal.WindowsIdentity.GetCurrent())
                return new System.Security.Principal.WindowsPrincipal(id).IsInRole(
                    System.Security.Principal.WindowsBuiltInRole.Administrator);
        }

        private void DetectGame()
        {
            detectedExe = ""; detectedProc = "";
            foreach (var n in PROC_NAMES)
            {
                var ps = Process.GetProcessesByName(n);
                if (ps.Length > 0) { detectedProc = ps[0].ProcessName; try { detectedExe = ps[0].MainModule.FileName; } catch { } break; }
            }
            if (string.IsNullOrEmpty(detectedExe))
            {
                foreach (var baseDir in new[] { @"C:\Program Files", @"C:\Program Files (x86)", @"D:\", @"E:\", @"F:\" })
                {
                    foreach (var hint in GAME_DIR_HINTS)
                    {
                        try
                        {
                            var dirs = Directory.GetDirectories(baseDir, "*" + hint + "*", SearchOption.TopDirectoryOnly);
                            foreach (var d in dirs)
                            {
                                var exes = Directory.GetFiles(d, "*.exe", SearchOption.TopDirectoryOnly);
                                var exe = exes.FirstOrDefault(x => x.IndexOf("Torch", StringComparison.OrdinalIgnoreCase) >= 0 || x.IndexOf("TLI", StringComparison.OrdinalIgnoreCase) >= 0);
                                if (!string.IsNullOrEmpty(exe)) { detectedExe = exe; break; }
                            }
                        }
                        catch { }
                        if (!string.IsNullOrEmpty(detectedExe)) break;
                    }
                    if (!string.IsNullOrEmpty(detectedExe)) break;
                }
            }
            lblDetect.Text = string.IsNullOrEmpty(detectedExe)
                ? "未检测到游戏。可先启动游戏，或手动把游戏 exe 放到下面路径：\n" + Path.Combine(CFG_DIR, "game_path.txt")
                : "检测到游戏：" + detectedExe + (string.IsNullOrEmpty(detectedProc) ? "（未运行）" : "（运行中：" + detectedProc + "）");
        }

        private string GetGameExe()
        {
            if (!string.IsNullOrEmpty(detectedExe) && File.Exists(detectedExe)) return detectedExe;
            var pf = Path.Combine(CFG_DIR, "game_path.txt");
            if (File.Exists(pf)) { var s = File.ReadAllText(pf).Trim(); if (File.Exists(s)) return s; }
            return detectedExe;
        }

        // ---------- 备份机制 ----------

        private Dictionary<string, string> LoadBackup()
        {
            var d = new Dictionary<string, string>();
            if (!File.Exists(BACKUP_FILE)) return d;
            foreach (var line in File.ReadAllLines(BACKUP_FILE))
            {
                int i = line.IndexOf('=');
                if (i > 0) d[line.Substring(0, i)] = line.Substring(i + 1);
            }
            return d;
        }
        private void SaveBackup(Dictionary<string, string> d)
        {
            var lines = d.Select(kv => kv.Key + "=" + kv.Value).ToArray();
            File.WriteAllLines(BACKUP_FILE, lines);
        }
        private void Record(Dictionary<string, string> bak, string key, string original)
        {
            if (!bak.ContainsKey(key)) bak[key] = original;
        }
        private bool RegSet(string path, string name, object value, Dictionary<string, string> bak)
        {
            try
            {
                using (var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(path, true))
                {
                    if (k == null) return false;
                    string key = "REG|" + path + "|" + name;
                    object cur = k.GetValue(name, null);
                    Record(bak, key, cur == null ? "<null>" : cur.ToString());
                    k.SetValue(name, value);
                    return true;
                }
            }
            catch { return false; }
        }
        private bool RegSetMachine(string path, string name, object value, Dictionary<string, string> bak, Microsoft.Win32.RegistryValueKind kind)
        {
            try
            {
                using (var k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(path, true))
                {
                    if (k == null) return false;
                    string key = "REG|HKLM|" + path + "|" + name;
                    object cur = k.GetValue(name, null);
                    Record(bak, key, cur == null ? "<null>" : cur.ToString());
                    k.SetValue(name, value, kind);
                    return true;
                }
            }
            catch { return false; }
        }

        private void SetStatus(string s) { lblStatus.Text = "▶ " + s; }

        // ---------- 优化执行 ----------

        private void BtnApply_Click(object sender, EventArgs e)
        {
            var bak = LoadBackup();
            int n = 0;
            try
            {
                // CPU
                if (checks[0].Checked) n += SetGamePriority(ProcessPriorityClass.AboveNormal);
                if (checks[1].Checked) n += RegSet(@"Software\Microsoft\GameBar", "AutoGameModeEnabled", 1, bak) ? 1 : 0;
                if (checks[2].Checked || cmboPower.SelectedIndex > 0) n += SetPowerPlan(cmboPower.SelectedIndex, bak);
                if (checks[3].Checked || checks[7].Checked) { KillChecked(true); n++; }

                // GPU
                if (checks[4].Checked) n += RegSetMachine(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2, bak, Microsoft.Win32.RegistryValueKind.DWord) ? 1 : 0;
                if (checks[5].Checked) n += SetFullscreenOptimizationOff(bak);
                if (checks[6].Checked) n += SetGpuMaxPerformance(bak);

                // Memory
                if (checks[7].Checked) n += TrimAllWorkingSets();

                // Network
                if (cmboDNS.SelectedIndex > 0) n += SetDNS(cmboDNS.SelectedIndex, bak);
                n += TcpOptimize(bak);

                SaveBackup(bak);
                SaveSettings();
                SetStatus(string.Format("优化完成，共执行 {0} 项。部分设置（HAGS/DNS/电源）需重启或重连网络生效。", n));
            }
            catch (Exception ex) { SetStatus("优化出错：" + ex.Message); }
        }

        private int SetGamePriority(ProcessPriorityClass pri)
        {
            int n = 0;
            foreach (var name in PROC_NAMES)
                foreach (var p in Process.GetProcessesByName(name))
                {
                    try { p.PriorityClass = pri; n++; } catch { }
                }
            return n;
        }

        private int SetPowerPlan(int idx, Dictionary<string, string> bak)
        {
            try
            {
                var cur = RunCmd("powercfg", "/getactivescheme");
                var m = System.Text.RegularExpressions.Regex.Match(cur, "([0-9a-fA-F-]{36})");
                if (m.Success) Record(bak, "POWERPLAN", m.Groups[1].Value);
                string target;
                if (idx == 1) target = RunCmd("powercfg", "/list").Split('\n').FirstOrDefault(l => l.Contains("高性能"))?.Split('(')[1]?.Split(')')[0] ?? "";
                else if (idx == 2) target = RunCmd("powercfg", "/list").Split('\n').FirstOrDefault(l => l.Contains("卓越性能"))?.Split('(')[1]?.Split(')')[0] ?? "";
                else target = "";
                if (!string.IsNullOrEmpty(target)) { RunCmd("powercfg", "/s " + target); return 1; }
            }
            catch { }
            return 0;
        }

        private int SetFullscreenOptimizationOff(Dictionary<string, string> bak)
        {
            string exe = GetGameExe();
            if (string.IsNullOrEmpty(exe) || !File.Exists(exe)) return 0;
            string path = @"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers";
            string key = "REG|" + path + "|" + exe;
            try
            {
                using (var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(path, true))
                {
                    if (k == null) return 0;
                    object cur = k.GetValue(exe, null);
                    Record(bak, key, cur == null ? "<null>" : cur.ToString());
                    k.SetValue(exe, "~ DISABLEDXMAXIMIZEDWINDOWEDMODE");
                    return 1;
                }
            }
            catch { return 0; }
        }

        private int SetGpuMaxPerformance(Dictionary<string, string> bak)
        {
            int n = 0;
            // NVIDIA global preferred max performance
            n += RegSetMachine(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers\PowerManagement", "PerfLevelSrc", 0x2222, bak, Microsoft.Win32.RegistryValueKind.DWord) ? 1 : 0;
            return n;
        }

        [DllImport("psapi.dll")] private static extern bool EmptyWorkingSet(IntPtr h);
        private int TrimAllWorkingSets()
        {
            int n = 0;
            try
            {
                foreach (var p in Process.GetProcesses())
                {
                    try { if (EmptyWorkingSet(p.Handle)) n++; } catch { }
                }
            }
            catch { }
            return n;
        }

        private int SetDNS(int idx, Dictionary<string, string> bak)
        {
            try
            {
                string[] dns;
                switch (idx)
                {
                    case 1: dns = new[] { "223.5.5.5", "223.6.6.6" }; break;
                    case 2: dns = new[] { "119.29.29.29", "114.114.114.114" }; break;
                    case 3: dns = new[] { "8.8.8.8", "8.8.4.4" }; break;
                    case 4: dns = new[] { "1.1.1.1", "1.0.0.1" }; break;
                    default: return 0;
                }
                var iface = GetActiveInterface();
                if (string.IsNullOrEmpty(iface)) return 0;
                string key = "DNS|" + iface;
                string cur = RunCmd("netsh", "interface ip show dnsservers name=\"" + iface + "\"");
                Record(bak, key, cur.Replace("\r", "").Replace("\n", "|"));
                RunCmd("netsh", "interface ip set dnsservers name=\"" + iface + "\" static " + dns[0] + " primary");
                RunCmd("netsh", "interface ip add dnsservers name=\"" + iface + "\" " + dns[1] + " index=2");
                return 1;
            }
            catch { return 0; }
        }

        private string GetActiveInterface()
        {
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                    if (ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                        return ni.Name;
            }
            catch { }
            return null;
        }

        private int TcpOptimize(Dictionary<string, string> bak)
        {
            int n = 0;
            try
            {
                Record(bak, "TCP_AUTOTUNE", RunCmd("netsh", "int tcp show global").Trim());
                RunCmd("netsh", "int tcp set global autotuninglevel=normal");
                RunCmd("netsh", "int tcp set global rss=enabled");
                n++;
                using (var k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", true))
                {
                    if (k != null)
                    {
                        Record(bak, "TCP_ACK", k.GetValue("TcpAckFrequency", "<null>").ToString());
                        Record(bak, "TCP_NODELAY", k.GetValue("TCPNoDelay", "<null>").ToString());
                        k.SetValue("TcpAckFrequency", 1, Microsoft.Win32.RegistryValueKind.DWord);
                        k.SetValue("TCPNoDelay", 1, Microsoft.Win32.RegistryValueKind.DWord);
                        n++;
                    }
                }
            }
            catch { }
            return n;
        }

        private string RunCmd(string exe, string args)
        {
            try
            {
                var psi = new ProcessStartInfo(exe, args) { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
                var p = Process.Start(psi);
                string s = p.StandardOutput.ReadToEnd();
                p.WaitForExit(5000);
                return s;
            }
            catch { return ""; }
        }

        private void BtnRestore_Click(object sender, EventArgs e)
        {
            var bak = LoadBackup();
            if (bak.Count == 0) { SetStatus("没有备份可恢复"); return; }
            int n = 0;
            foreach (var kv in bak)
            {
                try
                {
                    if (kv.Key.StartsWith("REG|"))
                    {
                        var parts = kv.Key.Split('|');
                        bool machine = parts.Length > 2 && parts[1] == "HKLM";
                        string path = machine ? parts[2] : parts[1];
                        string name = machine ? parts[3] : parts[2];
                        using (var k = machine ? Microsoft.Win32.Registry.LocalMachine.OpenSubKey(path, true) : Microsoft.Win32.Registry.CurrentUser.OpenSubKey(path, true))
                        {
                            if (k == null) continue;
                            if (kv.Value == "<null>") k.DeleteValue(name, false);
                            else k.SetValue(name, Convert.ToInt32(kv.Value), Microsoft.Win32.RegistryValueKind.DWord);
                            n++;
                        }
                    }
                    else if (kv.Key == "POWERPLAN" && kv.Value.Length == 36)
                    { RunCmd("powercfg", "/s " + kv.Value); n++; }
                    else if (kv.Key.StartsWith("DNS|"))
                    {
                        string iface = kv.Key.Substring(4);
                        RunCmd("netsh", "interface ip set dnsservers name=\"" + iface + "\" dhcp"); n++;
                    }
                    else if (kv.Key == "TCP_AUTOTUNE") { RunCmd("netsh", "int tcp reset"); n++; }
                    else if (kv.Key == "TCP_ACK" || kv.Key == "TCP_NODELAY")
                    {
                        using (var k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", true))
                        {
                            if (k == null) continue;
                            string nm = kv.Key == "TCP_ACK" ? "TcpAckFrequency" : "TCPNoDelay";
                            if (kv.Value == "<null>") k.DeleteValue(nm, false); else k.SetValue(nm, Convert.ToInt32(kv.Value));
                            n++;
                        }
                    }
                }
                catch { }
            }
            SaveBackup(new Dictionary<string, string>());
            SetStatus("已恢复 " + n + " 项原始设置");
        }

        private void BtnPing_Click(object sender, EventArgs e)
        {
            string[] hosts = { "223.5.5.5", "8.8.8.8", "www.baidu.com" };
            var sb = new StringBuilder();
            foreach (var h in hosts)
            {
                try
                {
                    var p = new Ping();
                    var r = p.Send(h, 1500);
                    sb.AppendLine(h + " : " + (r.Status == System.Net.NetworkInformation.IPStatus.Success ? r.RoundtripTime + " ms" : "超时"));
                }
                catch { sb.AppendLine(h + " : 失败"); }
            }
            lblNet.Text = sb.ToString();
        }

        private void KillChecked(bool silent = false)
        {
            int n = 0;
            foreach (ListViewItem it in lvProcesses.CheckedItems)
            {
                try
                {
                    int pid = Convert.ToInt32(it.SubItems[1].Text);
                    var p = Process.GetProcessById(pid);
                    if (p.Id == Process.GetCurrentProcess().Id) continue;
                    p.Kill(); n++;
                }
                catch { }
            }
            if (!silent) { SetStatus("已结束 " + n + " 个进程"); RefreshProcesses(); }
        }

        private void RefreshProcesses()
        {
            lvProcesses.Items.Clear();
            string[] targets = { "chrome", "msedge", "firefox", "WorkBuddy", "CodeBuddy", "steamwebhelper", "steam",
                "SearchApp", "MsMpEng", "explorer", "QQ", "WeChat", "WeChatAppEx", "WXWork", "DingTalk", "Spotify", "discord" };
            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    string name = p.ProcessName;
                    if (!targets.Any(t => name.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0)) continue;
                    long mb = p.WorkingSet64 / 1048576;
                    if (mb < 40) continue;
                    string note = name.IndexOf("CodeBuddy", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  name.IndexOf("WorkBuddy", StringComparison.OrdinalIgnoreCase) >= 0
                                  ? "注意：可能是你正在用的工具" : "非必需后台进程";
                    lvProcesses.Items.Add(new ListViewItem(new[] { name, p.Id.ToString(), mb.ToString(), note }));
                }
                catch { }
            }
        }

        private void RefreshStatus()
        {
            var games = new List<Process>();
            foreach (var n in PROC_NAMES) games.AddRange(Process.GetProcessesByName(n));
            if (games.Count > 0)
            {
                var g = games[0];
                lblMem.Text = string.Format("游戏运行中  内存 {0} MB  提交 {1} MB  CPU {2}s",
                    g.WorkingSet64 / 1048576, g.PrivateMemorySize64 / 1048576, (int)g.TotalProcessorTime.TotalSeconds);
            }
            else lblMem.Text = "游戏：未运行（检测进程名：" + string.Join("/", PROC_NAMES.Take(3)) + "）";
        }

        private void SaveSettings()
        {
            var lines = new List<string>();
            for (int i = 0; i < checks.Length; i++) lines.Add("chk" + i + "=" + (checks[i].Checked ? "1" : "0"));
            lines.Add("dns=" + cmboDNS.SelectedIndex);
            lines.Add("power=" + cmboPower.SelectedIndex);
            File.WriteAllLines(SETTINGS_FILE, lines);
        }
        private void LoadSettings()
        {
            if (!File.Exists(SETTINGS_FILE)) return;
            foreach (var line in File.ReadAllLines(SETTINGS_FILE))
            {
                var kv = line.Split('='); if (kv.Length != 2) continue;
                if (kv[0].StartsWith("chk"))
                {
                    int i; if (int.TryParse(kv[0].Substring(3), out i) && i < checks.Length) checks[i].Checked = kv[1] == "1";
                }
                else if (kv[0] == "dns") { int i; if (int.TryParse(kv[1], out i) && i < cmboDNS.Items.Count) cmboDNS.SelectedIndex = i; }
                else if (kv[0] == "power") { int i; if (int.TryParse(kv[1], out i) && i < cmboPower.Items.Count) cmboPower.SelectedIndex = i; }
            }
        }

        private void Help_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "Torchlight: Infinite 优化工具 使用说明\n\n" +
                "1) 把本 exe 复制到任意 Windows 电脑，双击运行（无需安装）。\n" +
                "2) 自动检测游戏；若未检测到，可启动游戏后点“重新检测”，\n" +
                "   或在 " + CFG_DIR + "\\game_path.txt 里写游戏 exe 的完整路径。\n" +
                "3) 勾选需要的优化项，点“一键优化”。\n" +
                "4) 所有改动自动备份到 " + BACKUP_FILE + "，点“一键恢复”可还原。\n\n" +
                "模块说明：\n" +
                "· CPU：进程优先级、游戏模式、高性能电源、清后台进程\n" +
                "· GPU：硬件加速GPU调度(HAGS)、关闭全屏优化、显卡最高性能\n" +
                "· 内存：清理所有进程工作集，释放被缓存的物理内存\n" +
                "· 网络：DNS 切换、TCP 参数优化、延迟测试\n\n" +
                "注意：HAGS/DNS/电源计划需重启电脑或重连网络才完全生效。\n" +
                "需要管理员权限才能改注册表和电源计划，请以管理员身份运行。",
                "使用说明", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
