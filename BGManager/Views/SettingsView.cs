using BGManager.Services;
using BGManager.Tools;

namespace BGManager.Views;

public class SettingsView : UserControl
{
    private readonly ApiClient _api;

    private TextBox txtServerUrl;
    private NumericUpDown nudAutoRefresh;
    private Button btnTest;
    private Button btnSave;
    private Label lblTestResult;
    private Label lblStatus;
    private Button btnImportLegacy;
    private Label lblImportStatus;
    private Button btnDisplaySettings;
    private Button btnPrintTemplateSettings;
    private Button btnShortcutSettings;

    private CheckBox chkAutoBackup;
    private NumericUpDown nudBackupInterval;
    private NumericUpDown nudBackupKeep;
    private NumericUpDown nudRetentionDays;
    private Label lblLastBackup;
    private Button btnSaveMaintenance;
    private Label lblMaintenanceStatus;

    public event EventHandler? SettingsChanged;

    public SettingsView(ApiClient api)
    {
        _api = api;
        Dock = DockStyle.Fill;
        BuildUI();
        LoadSettings();
        _ = LoadMaintenanceAsync();
    }

    private static readonly Color BgColor = Color.FromArgb(249, 250, 251);
    private static readonly Color LineColor = Color.FromArgb(226, 232, 240);
    private static readonly Color TitleColor = Color.FromArgb(30, 41, 59);
    private static readonly Color LabelColor = Color.FromArgb(51, 65, 85);
    private static readonly Color DescColor = Color.FromArgb(107, 114, 128);
    private static readonly Color PrimaryColor = Color.FromArgb(59, 130, 246);
    private static readonly Color SecondaryColor = Color.FromArgb(71, 85, 105);
    private static readonly Color WarningColor = Color.FromArgb(245, 158, 11);

    private const int CardWidth = 470;
    private const int ContentX = 25;
    private const int ContentWidth = 420;
    private const int BtnWidth = 120;
    private const int BtnHeight = 32;

    private static Label MakeSectionTitle(string text, int y) => new()
    {
        Text = text,
        Font = new Font("微软雅黑", 11f, FontStyle.Bold),
        AutoSize = true,
        Location = new Point(ContentX, y),
        ForeColor = TitleColor
    };

    private static Label MakeFieldLabel(string text, int x, int y) => new()
    {
        Text = text,
        AutoSize = true,
        Location = new Point(x, y),
        Font = new Font("微软雅黑", 9f, FontStyle.Bold),
        ForeColor = LabelColor
    };

    private static Label MakeDescLabel(string text, int x, int y) => new()
    {
        Text = text,
        AutoSize = true,
        Location = new Point(x, y),
        ForeColor = DescColor,
        Font = new Font("微软雅黑", 9f)
    };

    private static Panel MakeDivider(int y) => new()
    {
        Location = new Point(ContentX, y),
        Size = new Size(ContentWidth, 1),
        BackColor = LineColor
    };

    private static Button MakeButton(string text, Color color, int x, int y)
    {
        var btn = new Button
        {
            Text = text,
            Width = BtnWidth,
            Height = BtnHeight,
            Location = new Point(x, y),
            BackColor = color,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("微软雅黑", 9f)
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private void BuildUI()
    {
        var container = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = BgColor };
        Controls.Add(container);

        var cardLeft = new Panel
        {
            Width = CardWidth,
            Left = 30,
            Top = 30,
            BackColor = Color.White
        };
        container.Controls.Add(cardLeft);

        var cardRight = new Panel
        {
            Width = CardWidth,
            Left = 30 + CardWidth + 30,
            Top = 30,
            BackColor = Color.White
        };
        container.Controls.Add(cardRight);

        BuildLeftCard(cardLeft);
        BuildRightCard(cardRight);

        // 两张卡片等高，保证布局整齐
        int height = Math.Max(cardLeft.Height, cardRight.Height);
        cardLeft.Height = height;
        cardRight.Height = height;
    }

    private void BuildLeftCard(Panel card)
    {
        card.Controls.Add(MakeSectionTitle("基础设置", 20));
        card.Controls.Add(MakeDivider(55));

        int y = 72;
        card.Controls.Add(MakeFieldLabel("服务端地址", ContentX, y));
        y += 26;
        txtServerUrl = new TextBox { Location = new Point(ContentX, y), Width = 290, Font = new Font("微软雅黑", 9f) };
        card.Controls.Add(txtServerUrl);

        btnTest = MakeButton("测试连接", SecondaryColor, 325, y - 1);
        btnTest.Width = 110;
        btnTest.Click += async (s, e) => await TestConnectionAsync();
        card.Controls.Add(btnTest);
        y += 34;

        lblTestResult = new Label { Text = "", AutoSize = true, Location = new Point(ContentX, y), ForeColor = DescColor, Font = new Font("微软雅黑", 9f) };
        card.Controls.Add(lblTestResult);
        y += 28;

        card.Controls.Add(MakeFieldLabel("自动刷新间隔（秒）", ContentX, y));
        y += 26;
        nudAutoRefresh = new NumericUpDown { Location = new Point(ContentX, y), Width = 100, Minimum = 1, Maximum = 600, Value = 5, Font = new Font("微软雅黑", 9f) };
        card.Controls.Add(nudAutoRefresh);
        y += 40;

        btnSave = MakeButton("保存设置", PrimaryColor, ContentX, y);
        btnSave.Click += (s, e) => SaveSettings();
        card.Controls.Add(btnSave);

        lblStatus = new Label { Text = "", AutoSize = true, Location = new Point(ContentX + BtnWidth + 12, y + 8), ForeColor = Color.FromArgb(34, 197, 94), Font = new Font("微软雅黑", 9f) };
        card.Controls.Add(lblStatus);

        y += 56;
        card.Controls.Add(MakeDivider(y));

        y += 16;
        card.Controls.Add(MakeSectionTitle("数据迁移", y));
        y += 32;
        card.Controls.Add(MakeDescLabel("选择旧版 bloodgas.db 文件进行数据迁移", ContentX, y));
        y += 28;
        btnImportLegacy = MakeButton("导入旧版数据库", WarningColor, ContentX, y);
        btnImportLegacy.Width = 140;
        btnImportLegacy.Click += BtnImportLegacy_Click;
        card.Controls.Add(btnImportLegacy);

        lblImportStatus = new Label { Text = "", AutoSize = true, Location = new Point(ContentX + 152, y + 8), ForeColor = DescColor, Font = new Font("微软雅黑", 9f) };
        card.Controls.Add(lblImportStatus);

        card.Height = y + 56;
    }

    private void BuildRightCard(Panel card)
    {
        card.Controls.Add(MakeSectionTitle("服务端维护", 20));
        card.Controls.Add(MakeDivider(55));

        int y = 72;
        chkAutoBackup = new CheckBox
        {
            Text = "启用自动备份",
            AutoSize = true,
            Location = new Point(ContentX, y + 2),
            Font = new Font("微软雅黑", 9f),
            Checked = true
        };
        card.Controls.Add(chkAutoBackup);

        card.Controls.Add(MakeFieldLabel("备份周期（小时）", 160, y + 4));
        nudBackupInterval = new NumericUpDown { Location = new Point(280, y), Width = 80, Minimum = 1, Maximum = 720, Value = 24, Font = new Font("微软雅黑", 9f) };
        card.Controls.Add(nudBackupInterval);
        y += 36;

        card.Controls.Add(MakeFieldLabel("备份保留份数", ContentX, y + 4));
        nudBackupKeep = new NumericUpDown { Location = new Point(120, y), Width = 80, Minimum = 1, Maximum = 100, Value = 10, Font = new Font("微软雅黑", 9f) };
        card.Controls.Add(nudBackupKeep);

        card.Controls.Add(MakeFieldLabel("数据保留天数（0=永久）", 220, y + 4));
        nudRetentionDays = new NumericUpDown { Location = new Point(360, y), Width = 80, Minimum = 0, Maximum = 36500, Value = 0, Font = new Font("微软雅黑", 9f) };
        card.Controls.Add(nudRetentionDays);
        y += 38;

        lblLastBackup = new Label { Text = "上次自动备份：-", AutoSize = true, Location = new Point(ContentX, y), ForeColor = DescColor, Font = new Font("微软雅黑", 9f) };
        card.Controls.Add(lblLastBackup);
        y += 28;

        btnSaveMaintenance = MakeButton("保存维护设置", PrimaryColor, ContentX, y);
        btnSaveMaintenance.Click += async (s, e) => await SaveMaintenanceAsync();
        card.Controls.Add(btnSaveMaintenance);

        lblMaintenanceStatus = new Label { Text = "", AutoSize = true, Location = new Point(ContentX + BtnWidth + 12, y + 8), ForeColor = Color.FromArgb(34, 197, 94), Font = new Font("微软雅黑", 9f) };
        card.Controls.Add(lblMaintenanceStatus);

        y += 56;
        card.Controls.Add(MakeDivider(y));

        y += 16;
        card.Controls.Add(MakeSectionTitle("功能配置", y));

        y += 34;
        btnDisplaySettings = MakeButton("显示设置", SecondaryColor, ContentX, y);
        btnDisplaySettings.Click += BtnDisplaySettings_Click;
        card.Controls.Add(btnDisplaySettings);
        card.Controls.Add(MakeDescLabel("配置检验结果的显示顺序、上下限和参考值", ContentX + BtnWidth + 12, y + 8));

        y += 44;
        btnPrintTemplateSettings = MakeButton("打印模板设置", SecondaryColor, ContentX, y);
        btnPrintTemplateSettings.Click += BtnPrintTemplateSettings_Click;
        card.Controls.Add(btnPrintTemplateSettings);
        card.Controls.Add(MakeDescLabel("配置打印报告的页面、页眉、页脚等模板", ContentX + BtnWidth + 12, y + 8));

        y += 44;
        btnShortcutSettings = MakeButton("快捷键配置", SecondaryColor, ContentX, y);
        btnShortcutSettings.Click += BtnShortcutSettings_Click;
        card.Controls.Add(btnShortcutSettings);
        card.Controls.Add(MakeDescLabel("配置常用操作的快捷键", ContentX + BtnWidth + 12, y + 8));

        card.Height = y + 56;
    }

    private void BtnDisplaySettings_Click(object? sender, EventArgs e)
    {
        using var form = new DisplaySettingsForm();
        form.ShowDialog();
    }

    private void BtnPrintTemplateSettings_Click(object? sender, EventArgs e)
    {
        using var form = new PrintTemplateSettingsForm();
        form.ShowDialog();
    }

    private void BtnShortcutSettings_Click(object? sender, EventArgs e)
    {
        var config = AppConfig.Load();
        using var form = new ShortcutSettingsForm(config.ShortcutKeys);
        if (form.ShowDialog() == DialogResult.OK)
        {
            config.Save();
        }
    }

    private void LoadSettings()
    {
        txtServerUrl.Text = _api.BaseUrl;
    }

    private async Task LoadMaintenanceAsync()
    {
        var config = await _api.GetConfigDictAsync();
        if (config == null) return;

        if (InvokeRequired)
        {
            BeginInvoke(() => ApplyMaintenanceConfig(config));
            return;
        }
        ApplyMaintenanceConfig(config);
    }

    private void ApplyMaintenanceConfig(Dictionary<string, string> config)
    {
        chkAutoBackup.Checked = GetConfigBool(config, "backup.enabled", true);
        nudBackupInterval.Value = ClampNud(nudBackupInterval, GetConfigInt(config, "backup.interval_hours", 24));
        nudBackupKeep.Value = ClampNud(nudBackupKeep, GetConfigInt(config, "backup.keep_count", 10));
        nudRetentionDays.Value = ClampNud(nudRetentionDays, GetConfigInt(config, "data.retention_days", 0));

        var lastRun = config.TryGetValue("backup.last_run", out var t) && !string.IsNullOrWhiteSpace(t) ? t : "从未";
        lblLastBackup.Text = $"上次自动备份：{lastRun}";
    }

    private static bool GetConfigBool(Dictionary<string, string> config, string key, bool defaultValue)
    {
        return config.TryGetValue(key, out var v) && bool.TryParse(v, out var b) ? b : defaultValue;
    }

    private static int GetConfigInt(Dictionary<string, string> config, string key, int defaultValue)
    {
        return config.TryGetValue(key, out var v) && int.TryParse(v, out var n) ? n : defaultValue;
    }

    private static decimal ClampNud(NumericUpDown nud, int value)
    {
        return Math.Clamp(value, (int)nud.Minimum, (int)nud.Maximum);
    }

    private async Task SaveMaintenanceAsync()
    {
        btnSaveMaintenance.Enabled = false;
        lblMaintenanceStatus.Text = "";
        try
        {
            bool ok = true;
            ok &= await _api.SetConfigAsync("backup.enabled", chkAutoBackup.Checked.ToString().ToLower(), "backup", "是否启用自动备份");
            ok &= await _api.SetConfigAsync("backup.interval_hours", ((int)nudBackupInterval.Value).ToString(), "backup", "自动备份周期（小时）");
            ok &= await _api.SetConfigAsync("backup.keep_count", ((int)nudBackupKeep.Value).ToString(), "backup", "备份保留份数");
            ok &= await _api.SetConfigAsync("data.retention_days", ((int)nudRetentionDays.Value).ToString(), "data", "数据保留天数，0表示永久保留");

            lblMaintenanceStatus.Text = ok ? "✓ 已保存（服务端一小时内生效）" : "✗ 保存失败，请检查连接";
            lblMaintenanceStatus.ForeColor = ok ? Color.FromArgb(34, 197, 94) : Color.Red;
        }
        finally
        {
            btnSaveMaintenance.Enabled = true;
        }
    }

    private async Task TestConnectionAsync()
    {
        lblTestResult.Text = "测试中...";
        lblTestResult.ForeColor = Color.Gray;

        try
        {
            var url = txtServerUrl.Text.Trim();
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var resp = await client.GetAsync($"{url.TrimEnd('/')}/api/status");
            if (resp.IsSuccessStatusCode)
            {
                lblTestResult.Text = "✓ 连接成功";
                lblTestResult.ForeColor = Color.FromArgb(34, 197, 94);
            }
            else
            {
                lblTestResult.Text = $"✗ 连接失败 ({(int)resp.StatusCode})";
                lblTestResult.ForeColor = Color.Red;
            }
        }
        catch (Exception ex)
        {
            lblTestResult.Text = $"✗ {ex.Message}";
            lblTestResult.ForeColor = Color.Red;
        }
    }

    private void SaveSettings()
    {
        var url = txtServerUrl.Text.Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            MessageBox.Show("请输入服务端地址", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _api.BaseUrl = url;
        
        var config = AppConfig.Load();
        config.ServerUrl = url;
        config.Save();
        
        lblStatus.Text = "设置已保存";
        SettingsChanged?.Invoke(this, EventArgs.Empty);

        var timer = new System.Windows.Forms.Timer { Interval = 2000 };
        timer.Tick += (s, e) => { lblStatus.Text = ""; timer.Stop(); };
        timer.Start();
    }

    private void BtnImportLegacy_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "SQLite 数据库 (*.db)|*.db|所有文件 (*.*)|*.*",
            Title = "选择旧版数据库文件",
            FileName = "bloodgas.db"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        var result = MessageBox.Show(
            "确定要导入旧版数据库吗？\n\n导入后会生成新格式的数据库文件（bloodgas_new.db），不会覆盖当前数据库。",
            "确认导入",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Question);

        if (result != DialogResult.OK)
            return;

        try
        {
            lblImportStatus.Text = "正在导入...";
            lblImportStatus.ForeColor = Color.FromArgb(59, 130, 246);
            btnImportLegacy.Enabled = false;

            string oldDbPath = dialog.FileName;
            string? dir = Path.GetDirectoryName(oldDbPath);
            string newDbPath = Path.Combine(dir ?? ".", "bloodgas_new.db");

            if (File.Exists(newDbPath))
                File.Delete(newDbPath);

            LegacyDbMigrator.Migrate(oldDbPath, newDbPath);

            lblImportStatus.Text = $"导入完成！新数据库: {Path.GetFileName(newDbPath)}";
            lblImportStatus.ForeColor = Color.FromArgb(34, 197, 94);

            MessageBox.Show(
                $"数据导入完成！\n\n新数据库位置:\n{newDbPath}\n\n请将新数据库文件复制到 BGService 运行目录，替换原有的 bloodgas.db",
                "导入成功",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            lblImportStatus.Text = "导入失败";
            lblImportStatus.ForeColor = Color.Red;
            MessageBox.Show($"导入失败: {ex.Message}\n\n{ex.StackTrace}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnImportLegacy.Enabled = true;
        }
    }
}
