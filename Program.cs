using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;

namespace SooFunPkg
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        private TextBox txtZipPath = null!;
        private TextBox txtVersion = null!;
        private TextBox txtGameName = null!;
        private TextBox txtStripDir = null!;
        private TextBox txtOutputDir = null!;
        private Button btnBrowseZip = null!;
        private Button btnBrowseOutput = null!;
        private Button btnGenerate = null!;
        private Button btnOpenOutput = null!;
        private Label lblStatus = null!;
        private ProgressBar progressBar = null!;

        private string? generatedFilePath;

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "SooFun游戏重打包工具";
            this.Size = new Size(650, 420);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            int labelWidth = 140;
            int controlLeft = 150;
            int controlWidth = 350;
            int rowHeight = 35;
            int startY = 20;

            // Title
            var lblTitle = new Label
            {
                Text = "SooFun游戏重打包工具",
                Font = new Font("Microsoft YaHei UI", 14, FontStyle.Bold),
                Location = new Point(20, startY),
                AutoSize = true
            };
            this.Controls.Add(lblTitle);

            // Row 1: ZIP File
            int y = startY + 50;
            var lblZip = new Label { Text = "SooFun原始zip文件:", Location = new Point(20, y), Size = new Size(labelWidth, 25) };
            txtZipPath = new TextBox { Location = new Point(controlLeft, y), Size = new Size(controlWidth - 90, 25) };
            btnBrowseZip = new Button { Text = "浏览", Location = new Point(controlLeft + controlWidth - 80, y - 2), Size = new Size(70, 30) };
            btnBrowseZip.Click += BtnBrowseZip_Click;
            this.Controls.AddRange(new Control[] { lblZip, txtZipPath, btnBrowseZip });

            // Row 2: Version
            y += rowHeight;
            var lblVersion = new Label { Text = "版本号 (x.y.z):", Location = new Point(20, y), Size = new Size(labelWidth, 25) };
            txtVersion = new TextBox { Location = new Point(controlLeft, y), Size = new Size(controlWidth, 25), PlaceholderText = "例如 1.3.9" };
            this.Controls.AddRange(new Control[] { lblVersion, txtVersion });

            // Row 3: Game Name
            y += rowHeight;
            var lblGameName = new Label { Text = "游戏名称:", Location = new Point(20, y), Size = new Size(labelWidth, 25) };
            txtGameName = new TextBox { Location = new Point(controlLeft, y), Size = new Size(controlWidth, 25) };
            this.Controls.AddRange(new Control[] { lblGameName, txtGameName });

            // Row 4: Strip Dir
            y += rowHeight;
            var lblStripDir = new Label { Text = "目录剥离:", Location = new Point(20, y), Size = new Size(labelWidth, 25) };
            txtStripDir = new TextBox { Location = new Point(controlLeft, y), Size = new Size(controlWidth, 25), PlaceholderText = "例如 web-mobile" };
            txtStripDir.Text = "web-mobile";
            this.Controls.AddRange(new Control[] { lblStripDir, txtStripDir });

            // Row 5: Output Dir
            y += rowHeight;
            var lblOutputDir = new Label { Text = "输出目录:", Location = new Point(20, y), Size = new Size(labelWidth, 25) };
            txtOutputDir = new TextBox { Location = new Point(controlLeft, y), Size = new Size(controlWidth - 90, 25) };
            btnBrowseOutput = new Button { Text = "浏览", Location = new Point(controlLeft + controlWidth - 80, y - 2), Size = new Size(70, 30) };
            btnBrowseOutput.Click += BtnBrowseOutput_Click;
            this.Controls.AddRange(new Control[] { lblOutputDir, txtOutputDir, btnBrowseOutput });

            // Buttons
            y += rowHeight + 10;
            btnGenerate = new Button { Text = "生成", Location = new Point(controlLeft, y), Size = new Size(80, 35) };
            btnGenerate.Click += BtnGenerate_Click;
            btnOpenOutput = new Button { Text = "打开输出目录", Location = new Point(controlLeft + 95, y), Size = new Size(100, 35), Enabled = false };
            btnOpenOutput.Click += BtnOpenOutput_Click;
            this.Controls.AddRange(new Control[] { btnGenerate, btnOpenOutput });

            // Progress Bar
            y += 45;
            progressBar = new ProgressBar { Location = new Point(controlLeft, y), Size = new Size(controlWidth, 20), Style = ProgressBarStyle.Marquee, Visible = false };

            // Status Label
            y += 30;
            lblStatus = new Label { Text = "", Location = new Point(20, y), Size = new Size(600, 25), ForeColor = Color.Blue };
            this.Controls.Add(progressBar);
            this.Controls.Add(lblStatus);
        }

        private void BtnBrowseZip_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "选择 ZIP 文件",
                Filter = "ZIP 文件 (*.zip)|*.zip",
                CheckFileExists = true
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtZipPath.Text = dialog.FileName;
            }
        }

        private void BtnBrowseOutput_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "选择输出目录"
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtOutputDir.Text = dialog.SelectedPath;
            }
        }

        private void BtnOpenOutput_Click(object? sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(generatedFilePath) && File.Exists(generatedFilePath))
            {
                string? directory = Path.GetDirectoryName(generatedFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Process.Start("explorer.exe", $"/select,\"{generatedFilePath}\"");
                }
            }
        }

        private void BtnGenerate_Click(object? sender, EventArgs e)
        {
            if (!ValidateInputs()) return;

            btnGenerate.Enabled = false;
            btnOpenOutput.Enabled = false;
            progressBar.Visible = true;
            lblStatus.Text = "正在处理...";
            lblStatus.ForeColor = Color.Blue;

            string zipPath = txtZipPath.Text.Trim();
            string version = txtVersion.Text.Trim();
            string gameName = txtGameName.Text.Trim();
            string outputDir = txtOutputDir.Text.Trim();
            string stripDir = txtStripDir.Text.Trim();

            Task.Run(() => ProcessAsync(zipPath, version, gameName, outputDir, stripDir));
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(txtZipPath.Text))
            {
                ShowError("请选择 ZIP 文件");
                return false;
            }
            if (!File.Exists(txtZipPath.Text))
            {
                ShowError("ZIP 文件不存在");
                return false;
            }

            string version = txtVersion.Text.Trim();
            var parts = version.Split('.');
            if (parts.Length != 3 || !parts.All(p => int.TryParse(p, out _)))
            {
                ShowError("版本号格式错误，请使用 x.y.z 格式（如 1.3.9）");
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtGameName.Text))
            {
                ShowError("请输入游戏名称");
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtStripDir.Text))
            {
                ShowError("请输入目录剥离名称");
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtOutputDir.Text))
            {
                ShowError("请选择输出目录");
                return false;
            }

            if (!Directory.Exists(txtOutputDir.Text))
            {
                ShowError("输出目录不存在");
                return false;
            }

            return true;
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void ShowSuccess(string message)
        {
            MessageBox.Show(message, "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ProcessAsync(string zipPath, string version, string gameName, string outputDir, string stripDir)
        {
            string? tempDir = null;
            try
            {
                tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

                // 1. 创建临时目录
                Directory.CreateDirectory(tempDir);
                UpdateStatus("正在解压文件...");

                // 2. 解压 zip
                ZipFile.ExtractToDirectory(zipPath, tempDir, true);

                // 2.5 进入 stripDir 指定子目录
                string targetDir = Path.Combine(tempDir, stripDir);
                if (!Directory.Exists(targetDir))
                {
                    throw new Exception($"解压后的文件中未找到目录: {stripDir}");
                }

                // 3. 写入 version.json
                UpdateStatus("正在创建 version.json...");
                var versionData = new { version = version, gameName = gameName };
                string jsonContent = JsonSerializer.Serialize(versionData, new JsonSerializerOptions { WriteIndented = true });
                string versionFilePath = Path.Combine(targetDir, "version.json");
                File.WriteAllText(versionFilePath, jsonContent);

                // 4. 设置权限 755
                UpdateStatus("正在修改文件权限...");
                ApplyPermissionsRecursively(targetDir);

                // 5. 重新压缩
                UpdateStatus("正在压缩文件...");
                string outputFileName = $"{gameName}.{version}.zip";
                string outputPath = Path.Combine(outputDir, outputFileName);

                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }

                ZipFile.CreateFromDirectory(targetDir, outputPath, CompressionLevel.Optimal, false);

                generatedFilePath = outputPath;

                // 6. 清理临时目录
                Directory.Delete(tempDir, true);

                this.Invoke(() =>
                {
                    progressBar.Visible = false;
                    btnGenerate.Enabled = true;
                    btnOpenOutput.Enabled = true;
                    lblStatus.Text = $"生成成功！文件保存至: {outputPath}";
                    lblStatus.ForeColor = Color.Green;
                    ShowSuccess($"文件已生成:\n{outputPath}");
                });
            }
            catch (Exception ex)
            {
                if (tempDir != null && Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }

                this.Invoke(() =>
                {
                    progressBar.Visible = false;
                    btnGenerate.Enabled = true;
                    lblStatus.Text = $"处理失败: {ex.Message}";
                    lblStatus.ForeColor = Color.Red;
                    ShowError($"处理过程中出现错误:\n{ex.Message}");
                });
            }
        }

        private void UpdateStatus(string message)
        {
            this.Invoke(() => lblStatus.Text = message);
        }

        private void ApplyPermissionsRecursively(string path)
        {
            // 在 Windows 上设置文件权限为可读可执行
            foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }
            foreach (string dir in Directory.GetDirectories(path, "*", SearchOption.AllDirectories))
            {
                Directory.SetAttributes(dir, FileAttributes.Normal);
            }
            // 设置可执行权限 (在Windows上主要是确保可读)
            var dirInfo = new DirectoryInfo(path);
            foreach (var info in dirInfo.GetFileSystemInfos("*", SearchOption.AllDirectories))
            {
                try
                {
                    if ((info.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        DirectoryInfo di = (DirectoryInfo)info;
                        foreach (var fi in di.GetFiles())
                        {
                            File.SetAttributes(fi.FullName, FileAttributes.Normal);
                        }
                    }
                }
                catch { }
            }
        }
    }
}
