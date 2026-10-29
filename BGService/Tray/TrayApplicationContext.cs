using System.Windows.Forms;
using System.Diagnostics;
using BGService.Config;

namespace BGService.Tray;

public class TrayApplicationContext : ApplicationContext
{
    private readonly TrayIconManager _tray;
    private readonly Func<Task> _onExit;
    private SettingsForm? _settingsForm;
    private ConsoleForm? _consoleForm;

    public TrayIconManager TrayIcon => _tray;

    public TrayApplicationContext(Func<Task> onExit)
    {
        _onExit = onExit;
        _tray = new TrayIconManager();
        _tray.Initialize();

        _tray.ExitRequested += async (s, e) => await ExitApp();
        _tray.SettingsRequested += OnSettingsRequested;
        _tray.ShowLogRequested += OnShowLog;
        _tray.BackupRequested += OnBackupRequested;
        _tray.ShowConsoleRequested += OnShowConsole;
    }

    public void Log(string message)
    {
        _consoleForm?.AppendLog(message);
    }

    private void OnShowConsole(object? sender, EventArgs e)
    {
        if (_consoleForm == null || _consoleForm.IsDisposed)
        {
            _consoleForm = new ConsoleForm();
            _consoleForm.FormClosed += (s, e) => { _consoleForm = null; };
        }
        _consoleForm.Show();
        _consoleForm.Activate();
    }

    private BackupForm? _backupForm;

    private void OnBackupRequested(object? sender, EventArgs e)
    {
        if (_backupForm != null && !_backupForm.IsDisposed)
        {
            _backupForm.Activate();
            return;
        }

        _backupForm = new BackupForm();
        _backupForm.FormClosed += (s, e) => { _backupForm = null; };
        _backupForm.Show();
    }

    private void OnShowLog(object? sender, EventArgs e)
    {
        try
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsManager.Current.LogPath);
            if (!Directory.Exists(logPath))
                Directory.CreateDirectory(logPath);
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = logPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开日志目录失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnSettingsRequested(object? sender, EventArgs e)
    {
        if (_settingsForm != null && !_settingsForm.IsDisposed)
        {
            _settingsForm.Activate();
            return;
        }

        _settingsForm = new SettingsForm();
        _settingsForm.FormClosed += (s, e) => { _settingsForm = null; };
        _settingsForm.Show();
    }

    private async Task ExitApp()
    {
        var result = MessageBox.Show("确定要退出血气分析服务吗？", "退出确认",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes) return;

        try
        {
            _tray.UpdateStatus("正在退出...");
            await _onExit();
        }
        catch (Exception ex)
        {
            Program.SafeLog($"退出回调执行失败: {ex.Message}");
        }

        _tray.Dispose();
        ExitThread();
    }
}
