using System.Windows.Forms;
using BGService.Config;

namespace BGService.Tray;

public class TrayIconManager : IDisposable
{
    private NotifyIcon? _notifyIcon;
    private Icon? _normalIcon;
    private Icon? _workingIcon;
    private System.Windows.Forms.Timer? _blinkTimer;
    private bool _isWorkingState = false;
    private bool _isBlinking = false;
    private bool _disposed = false;

    public event EventHandler? ExitRequested;
    public event EventHandler? SettingsRequested;
    public event EventHandler? ShowLogRequested;
    public event EventHandler? BackupRequested;
    public event EventHandler? ShowConsoleRequested;

    public void Initialize()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string normalPath = Path.Combine(baseDir, "normal.ico");
        string workingPath = Path.Combine(baseDir, "working.ico");

        try
        {
            if (File.Exists(normalPath))
                _normalIcon = new Icon(normalPath);
            if (File.Exists(workingPath))
                _workingIcon = new Icon(workingPath);
        }
        catch (Exception ex)
        {
            Program.SafeLog($"加载托盘图标失败: {ex.Message}");
        }

        if (_normalIcon == null)
            _normalIcon = SystemIcons.Application;
        if (_workingIcon == null)
            _workingIcon = SystemIcons.Information;

        var contextMenu = new ContextMenuStrip();

        var consoleMenuItem = new ToolStripMenuItem("日志控制台");
        consoleMenuItem.Click += (s, e) => ShowConsoleRequested?.Invoke(this, EventArgs.Empty);
        contextMenu.Items.Add(consoleMenuItem);

        var logMenuItem = new ToolStripMenuItem("打开日志目录");
        logMenuItem.Click += (s, e) => ShowLogRequested?.Invoke(this, EventArgs.Empty);
        contextMenu.Items.Add(logMenuItem);

        var settingsMenuItem = new ToolStripMenuItem("设置");
        settingsMenuItem.Click += (s, e) => SettingsRequested?.Invoke(this, EventArgs.Empty);
        contextMenu.Items.Add(settingsMenuItem);

        var backupMenuItem = new ToolStripMenuItem("数据备份");
        backupMenuItem.Click += (s, e) => BackupRequested?.Invoke(this, EventArgs.Empty);
        contextMenu.Items.Add(backupMenuItem);

        contextMenu.Items.Add(new ToolStripSeparator());

        var exitMenuItem = new ToolStripMenuItem("退出程序");
        exitMenuItem.Click += (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty);
        contextMenu.Items.Add(exitMenuItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = _normalIcon,
            Text = "血气分析服务 V2",
            Visible = true,
            ContextMenuStrip = contextMenu
        };

        _notifyIcon.DoubleClick += (s, e) => ShowLogRequested?.Invoke(this, EventArgs.Empty);

        _blinkTimer = new System.Windows.Forms.Timer { Interval = 200 };
        _blinkTimer.Tick += (s, e) =>
        {
            if (_notifyIcon == null) return;
            _isWorkingState = !_isWorkingState;
            _notifyIcon.Icon = _isWorkingState ? _workingIcon : _normalIcon;
        };
    }

    public void StartWorking()
    {
        if (_isBlinking) return;
        _isBlinking = true;
        _blinkTimer?.Start();
    }

    public void StopWorking()
    {
        _isBlinking = false;
        _blinkTimer?.Stop();
        if (_notifyIcon != null && _normalIcon != null)
            _notifyIcon.Icon = _normalIcon;
        _isWorkingState = false;
    }

    public void ShowBalloon(string message, ToolTipIcon icon = ToolTipIcon.Info, int timeoutMs = 3000)
    {
        if (_notifyIcon == null) return;
        _notifyIcon.ShowBalloonTip(timeoutMs, "血气分析服务", message, icon);
    }

    public void UpdateStatus(string status)
    {
        if (_notifyIcon == null) return;

        // NotifyIcon.Text 最多64字符，且不能包含换行
        string text = status;
        // 移除时间戳前缀 [yyyy-MM-dd HH:mm:ss.fff] 如果存在
        if (text.Length > 24 && text[0] == '[' && text[23] == ']')
        {
            text = text.Substring(24).TrimStart();
        }
        // 限制最大长度
        if (text.Length > 60)
        {
            text = text.Substring(0, 57) + "...";
        }
        _notifyIcon.Text = $"血气分析服务 V2 - {text}";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _blinkTimer?.Stop();
        _blinkTimer?.Dispose();
        _blinkTimer = null;

        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        _normalIcon?.Dispose();
        _workingIcon?.Dispose();
        _normalIcon = null;
        _workingIcon = null;
    }
}
