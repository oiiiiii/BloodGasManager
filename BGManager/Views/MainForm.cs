using System.IO;
using System.Reflection;
using BGManager.Services;

namespace BGManager.Views;

public class MainForm : Form
{
    private readonly ApiClient _api = new();
    private System.Windows.Forms.Timer? _connTimer;

    private Panel topPanel;
    private Panel tabPanel;
    private Panel contentPanel;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel lblStatusBar;
    private ToolStripStatusLabel lblConnection;
    private ToolStripStatusLabel lblStats;

    private Button btnTabMain;
    private Button btnTabQuery;
    private Button btnTabPatient;
    private Button btnTabSettings;
    private Button btnTabAbout;

    private MainView? _mainView;
    private QueryView? _queryView;
    private PatientView? _patientView;
    private SettingsView? _settingsView;
    private AboutView? _aboutView;

    private Button _currentTabButton;

    public MainForm()
    {
        Text = "血气分析管理系统 V2";
        Width = 1280;
        Height = 768;
        MinimumSize = new Size(1024, 600);
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        Font = new Font("微软雅黑", 9f);

        // 设置任务栏图标
        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.ico");
        if (File.Exists(iconPath))
        {
            Icon = new Icon(iconPath);
        }
        else
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var iconResource = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("logo.ico", StringComparison.OrdinalIgnoreCase));
                if (iconResource != null)
                {
                    using (var stream = assembly.GetManifestResourceStream(iconResource))
                    {
                        if (stream != null)
                            Icon = new Icon(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载嵌入图标失败: {ex.Message}");
            }
        }

        BuildUI();
        ShowView("main");

        Load += async (s, e) =>
        {
            _connTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            _connTimer.Tick += async (s2, e2) => await CheckConnectionAsync();
            _connTimer.Start();
            await CheckConnectionAsync();
        };
    }

    private void BuildUI()
    {
        topPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = Color.White
        };
        Controls.Add(topPanel);

        tabPanel = new Panel
        {
            Height = 40,
            BackColor = Color.White,
            Left = 10,
            Top = 4,
            Width = 520
        };
        topPanel.Controls.Add(tabPanel);

        btnTabMain = CreateTabButton("主界面", 0);
        btnTabMain.Click += (s, e) => ShowView("main");
        tabPanel.Controls.Add(btnTabMain);

        btnTabQuery = CreateTabButton("查询", 1);
        btnTabQuery.Click += (s, e) => ShowView("query");
        tabPanel.Controls.Add(btnTabQuery);

        btnTabPatient = CreateTabButton("患者管理", 2);
        btnTabPatient.Click += (s, e) => ShowView("patient");
        tabPanel.Controls.Add(btnTabPatient);

        btnTabSettings = CreateTabButton("设置", 3);
        btnTabSettings.Click += (s, e) => ShowView("settings");
        tabPanel.Controls.Add(btnTabSettings);

        btnTabAbout = CreateTabButton("关于", 4);
        btnTabAbout.Click += (s, e) => ShowView("about");
        tabPanel.Controls.Add(btnTabAbout);

        contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(249, 250, 251)
        };
        Controls.Add(contentPanel);

        statusStrip = new StatusStrip { SizingGrip = false, BackColor = Color.White, Renderer = new CustomRenderer() };
        lblStatusBar = new ToolStripStatusLabel("就绪") { Font = new Font("微软雅黑", 12f), ForeColor = Color.Black };
        lblStats = new ToolStripStatusLabel("") { Font = new Font("微软雅黑", 12f), ForeColor = Color.Black };
        lblConnection = new ToolStripStatusLabel("连接状态: 检测中...") { Font = new Font("微软雅黑", 12f), ForeColor = Color.Black };
        statusStrip.Items.Add(lblStatusBar);
        statusStrip.Items.Add(new ToolStripStatusLabel { Spring = true });
        statusStrip.Items.Add(lblStats);
        statusStrip.Items.Add(lblConnection);
        Controls.Add(statusStrip);

        contentPanel.BringToFront();
    }

    private Button CreateTabButton(string text, int index)
    {
        var btn = new Button
        {
            Text = text,
            Width = 100,
            Height = 36,
            Left = index * 102,
            Top = 2,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("微软雅黑", 12f),
            ForeColor = Color.Black,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(230, 230, 230);
        btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(200, 200, 200);
        return btn;
    }

    private void SetActiveTab(Button activeBtn)
    {
        Button[] allTabs = [btnTabMain, btnTabQuery, btnTabPatient, btnTabSettings, btnTabAbout];
        foreach (var btn in allTabs)
        {
            btn.ForeColor = Color.Black;
            btn.BackColor = Color.Transparent;
            btn.Font = new Font("微软雅黑", 12f);
        }
        activeBtn.ForeColor = Color.FromArgb(59, 130, 246);
        activeBtn.Font = new Font("微软雅黑", 12f, FontStyle.Bold);
        _currentTabButton = activeBtn;
    }

    private void ShowView(string viewName)
    {
        contentPanel.Controls.Clear();

        switch (viewName)
        {
            case "main":
                _mainView ??= new MainView(_api);
                _mainView.StatsUpdated += (s, e) => UpdateStats();
                contentPanel.Controls.Add(_mainView);
                SetActiveTab(btnTabMain);
                break;

            case "query":
                _queryView ??= new QueryView(_api);
                contentPanel.Controls.Add(_queryView);
                SetActiveTab(btnTabQuery);
                break;

            case "patient":
                _patientView ??= new PatientView(_api);
                contentPanel.Controls.Add(_patientView);
                SetActiveTab(btnTabPatient);
                break;

            case "settings":
                _settingsView ??= new SettingsView(_api);
                _settingsView.SettingsChanged += (s, e) => _ = CheckConnectionAsync();
                contentPanel.Controls.Add(_settingsView);
                SetActiveTab(btnTabSettings);
                break;

            case "about":
                _aboutView ??= new AboutView();
                contentPanel.Controls.Add(_aboutView);
                SetActiveTab(btnTabAbout);
                break;
        }

        UpdateStatus($"已切换至 {GetViewName(viewName)}");
    }

    private static string GetViewName(string key)
    {
        return key switch
        {
            "main" => "主界面",
            "query" => "查询",
            "patient" => "患者管理",
            "settings" => "设置",
            "about" => "关于",
            _ => ""
        };
    }

    private void UpdateStats()
    {
        if (_mainView == null) return;
        var (total, pending) = _mainView.GetStats();
        if (statusStrip.InvokeRequired)
        {
            statusStrip.BeginInvoke(() => lblStats.Text = $"标本: {total} | 待审核: {pending}");
        }
        else
        {
            lblStats.Text = $"标本: {total} | 待审核: {pending}";
        }
    }

    private async Task CheckConnectionAsync()
    {
        var status = await _api.GetStatusAsync();
        if (statusStrip.InvokeRequired)
        {
            statusStrip.BeginInvoke(() => UpdateConnectionStatus(status));
        }
        else
        {
            UpdateConnectionStatus(status);
        }
    }

    private void UpdateConnectionStatus(BGShared.Models.ServiceStatus? status)
    {
        if (status == null)
        {
            lblConnection.Text = "○ 未连接服务端";
            lblConnection.ForeColor = Color.Gray;
            return;
        }

        string serialText = status.IsRunning
            ? $"串口 {status.SerialPort}@{status.BaudRate} 已连接"
            : $"串口 {status.SerialPort} 未连接";

        lblConnection.Text = $"● 服务端正常 | {serialText} | 今日 {status.TodaySpecimens} 份";
        lblConnection.ForeColor = status.IsRunning
            ? Color.FromArgb(34, 197, 94)
            : Color.FromArgb(245, 158, 11);
    }

    private void UpdateStatus(string msg)
    {
        lblStatusBar.Text = msg;
    }

    /// <summary>
    /// 显示主窗口（用于单实例激活）
    /// </summary>
    public void ShowMainWindow()
    {
        if (WindowState == FormWindowState.Minimized)
        {
            WindowState = FormWindowState.Maximized;
        }
        Show();
        Activate();
        BringToFront();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        var config = AppConfig.Load();
        var shortcut = config.ShortcutKeys;

        if (keyData == ParseShortcut(shortcut.Refresh))
        {
            _ = HandleRefresh();
            return true;
        }

        if (keyData == ParseShortcut(shortcut.Audit))
        {
            _ = HandleAudit();
            return true;
        }

        if (keyData == ParseShortcut(shortcut.Unarchive))
        {
            _ = HandleUnarchive();
            return true;
        }

        if (keyData == ParseShortcut(shortcut.Delete))
        {
            _ = HandleDelete();
            return true;
        }

        if (keyData == ParseShortcut(shortcut.Search))
        {
            _ = HandleSearch();
            return true;
        }

        if (keyData == ParseShortcut(shortcut.Print))
        {
            _ = HandlePrint();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private Keys ParseShortcut(string shortcut)
    {
        if (string.IsNullOrWhiteSpace(shortcut))
            return Keys.None;

        shortcut = shortcut.Trim();
        var parts = shortcut.Split('+');
        Keys result = Keys.None;

        foreach (var part in parts)
        {
            var keyPart = part.Trim();
            if (Enum.TryParse<Keys>(keyPart, true, out var key))
            {
                result |= key;
            }
            else
            {
                switch (keyPart.ToUpper())
                {
                    case "CTRL":
                        result |= Keys.Control;
                        break;
                    case "ALT":
                        result |= Keys.Alt;
                        break;
                    case "SHIFT":
                        result |= Keys.Shift;
                        break;
                }
            }
        }

        return result;
    }

    private async Task HandleRefresh()
    {
        if (_mainView != null)
            await _mainView.LoadDateDataAsync();
    }

    private async Task HandleAudit()
    {
        if (_mainView != null)
            await _mainView.AuditCurrentAsync();
    }

    private async Task HandleUnarchive()
    {
        if (_mainView != null)
            await _mainView.AuditCurrentAsync();
    }

    private async Task HandleDelete()
    {
        if (_mainView != null)
            await _mainView.DeleteCurrentAsync();
    }

    private async Task HandleSearch()
    {
        if (_queryView != null)
        {
            ShowView("query");
        }
    }

    private async Task HandlePrint()
    {
        if (_mainView != null)
        {
            _mainView.PrintReport();
        }
    }
}


