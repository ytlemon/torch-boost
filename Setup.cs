using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace TLISetup
{
    public class SetupForm : Form
    {
        private const string RES_NAME = "TLIOptimizerExe";
        private const string EXE_NAME = "TorchlightInfiniteOptimizer.exe";
        private TextBox txtDir;
        private Label lblInfo, lblStatus;
        private Button btnInstall, btnUninstall, btnBrowse;

        public SetupForm()
        {
            Text = "Torchlight Infinite Optimizer - Setup";
            Size = new Size(620, 360);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Color.FromArgb(26, 28, 34);
            ForeColor = Color.FromArgb(230, 232, 238);
            Font = new Font("Microsoft YaHei", 9F);

            var title = new Label { Text = "Torchlight: Infinite 优化工具 安装程序", Location = new Point(20, 16),
                Size = new Size(560, 36), Font = new Font("Microsoft YaHei", 15F, FontStyle.Bold),
                ForeColor = Color.FromArgb(90, 190, 255) };
            Controls.Add(title);

            lblInfo = new Label { Location = new Point(20, 62), Size = new Size(570, 48),
                ForeColor = Color.FromArgb(190, 194, 204) };
            lblInfo.Text = "将安装：Torchlight: Infinite 优化工具 v1.0（CPU/GPU/内存/网络四大优化模块）\n" +
                           "安装后会在桌面和开始菜单创建快捷方式。";
            Controls.Add(lblInfo);

            var l = new Label { Text = "安装目录：", Location = new Point(20, 126), Size = new Size(80, 26) }; Controls.Add(l);
            string def = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "TorchlightOptimizer");
            txtDir = new TextBox { Location = new Point(100, 124), Size = new Size(400, 28), Text = def }; Controls.Add(txtDir);
            btnBrowse = new Button { Text = "浏览...", Location = new Point(508, 124), Size = new Size(82, 28),
                FlatStyle = FlatStyle.Flat }; btnBrowse.FlatAppearance.BorderColor = Color.FromArgb(90,100,120);
            btnBrowse.Click += BtnBrowse_Click; Controls.Add(btnBrowse);

            btnInstall = new Button { Text = "安装", Location = new Point(20, 176), Size = new Size(160, 40),
                BackColor = Color.FromArgb(40, 120, 200), ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold) };
            btnInstall.Click += BtnInstall_Click; Controls.Add(btnInstall);

            btnUninstall = new Button { Text = "卸载", Location = new Point(196, 176), Size = new Size(120, 40),
                BackColor = Color.FromArgb(120, 70, 70), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnUninstall.Click += BtnUninstall_Click; Controls.Add(btnUninstall);

            lblStatus = new Label { Location = new Point(20, 238), Size = new Size(570, 60),
                ForeColor = Color.FromArgb(140, 230, 150) };
            Controls.Add(lblStatus);
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog { SelectedPath = txtDir.Text, Description = "选择安装目录" })
                if (fbd.ShowDialog() == DialogResult.OK) txtDir.Text = fbd.SelectedPath;
        }

        private void SetStatus(string s) { lblStatus.Text = "▶ " + s; }

        private void BtnInstall_Click(object sender, EventArgs e)
        {
            string dir = txtDir.Text.Trim();
            if (string.IsNullOrEmpty(dir)) { SetStatus("请先选择安装目录"); return; }
            try
            {
                Directory.CreateDirectory(dir);
                string target = Path.Combine(dir, EXE_NAME);
                var asm = Assembly.GetExecutingAssembly();
                using (Stream rs = asm.GetManifestResourceStream(RES_NAME))
                {
                    if (rs == null) { SetStatus("错误：内嵌资源未找到"); return; }
                    using (var fs = File.Create(target)) rs.CopyTo(fs);
                }
                CreateShortcut(target, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Torchlight Optimizer.lnk"), dir);
                string startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Torchlight Optimizer");
                Directory.CreateDirectory(startMenu);
                CreateShortcut(target, Path.Combine(startMenu, "Torchlight Optimizer.lnk"), dir);
                SetStatus("安装完成！\n" + target + "\n\n桌面和开始菜单已创建快捷方式。");
            }
            catch (Exception ex) { SetStatus("安装失败：" + ex.Message); }
        }

        private void BtnUninstall_Click(object sender, EventArgs e)
        {
            string dir = txtDir.Text.Trim();
            try
            {
                string target = Path.Combine(dir, EXE_NAME);
                if (File.Exists(target)) File.Delete(target);
                string desktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Torchlight Optimizer.lnk");
                if (File.Exists(desktop)) File.Delete(desktop);
                string startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Torchlight Optimizer");
                if (Directory.Exists(startMenu)) Directory.Delete(startMenu, true);
                if (Directory.Exists(dir) && Directory.GetFileSystemEntries(dir).Length == 0) Directory.Delete(dir);
                SetStatus("卸载完成。");
            }
            catch (Exception ex) { SetStatus("卸载失败：" + ex.Message); }
        }

        private void CreateShortcut(string target, string lnk, string workdir)
        {
            try
            {
                var ws = Type.GetTypeFromProgID("WScript.Shell");
                object sh = Activator.CreateInstance(ws);
                var sc = ws.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, sh, new object[] { lnk });
                var t = sc.GetType();
                t.InvokeMember("TargetPath", BindingFlags.SetProperty, null, sc, new object[] { target });
                t.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, sc, new object[] { workdir });
                t.InvokeMember("Description", BindingFlags.SetProperty, null, sc, new object[] { "Torchlight Infinite Optimizer" });
                t.InvokeMember("Save", BindingFlags.InvokeMethod, null, sc, null);
            }
            catch { }
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SetupForm());
        }
    }
}
