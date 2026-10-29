using System.Text;
using System.Windows.Forms;

namespace BGService.Tray;

public class ConsoleForm : Form
{
    private readonly TextBox _logTextBox;
    private readonly System.Windows.Forms.Timer _scrollTimer;
    private readonly object _lockObj = new();
    private readonly StringBuilder _logBuffer = new();
    private const int MAX_LINES = 500;
    private int _lineCount = 0;

    public ConsoleForm()
    {
        Text = "BGService 日志控制台";
        Width = 800;
        Height = 500;
        StartPosition = FormStartPosition.CenterScreen;

        // 窗口图标
        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "normal.ico");
        if (File.Exists(iconPath))
        {
            Icon = new Icon(iconPath);
        }

        _logTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            Font = new Font("Consolas", 10f),
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.FromArgb(200, 200, 200),
            WordWrap = false
        };
        Controls.Add(_logTextBox);

        // 工具栏
        var toolStrip = new ToolStrip
        {
            Dock = DockStyle.Top
        };

        var clearBtn = new ToolStripButton("清空");
        clearBtn.Click += (s, e) =>
        {
            lock (_lockObj)
            {
                _logTextBox.Clear();
                _logBuffer.Clear();
                _lineCount = 0;
            }
        };
        toolStrip.Items.Add(clearBtn);

        var saveBtn = new ToolStripButton("保存日志");
        saveBtn.Click += (s, e) =>
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "日志文件|*.log|所有文件|*.*",
                FileName = $"bgservice_log_{DateTime.Now:yyyyMMdd_HHmmss}.log"
            };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                lock (_lockObj)
                {
                    File.WriteAllText(sfd.FileName, _logTextBox.Text);
                }
                MessageBox.Show("日志已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        };
        toolStrip.Items.Add(saveBtn);

        Controls.Add(toolStrip);

        // 自动滚动定时器
        _scrollTimer = new System.Windows.Forms.Timer { Interval = 100 };
        _scrollTimer.Tick += (s, e) =>
        {
            if (_logTextBox.InvokeRequired)
            {
                _logTextBox.BeginInvoke(() =>
                {
                    _logTextBox.SelectionStart = _logTextBox.Text.Length;
                    _logTextBox.ScrollToCaret();
                });
            }
            else
            {
                _logTextBox.SelectionStart = _logTextBox.Text.Length;
                _logTextBox.ScrollToCaret();
            }
        };
        _scrollTimer.Start();

        FormClosing += (s, e) =>
        {
            e.Cancel = true;
            Hide();
        };
    }

    public void AppendLog(string message)
    {
        lock (_lockObj)
        {
            _logBuffer.AppendLine(message);
            _lineCount++;

            // 限制行数
            while (_lineCount > MAX_LINES && _logBuffer.Length > 0)
            {
                int idx = _logBuffer.ToString().IndexOf('\n');
                if (idx >= 0)
                {
                    _logBuffer.Remove(0, idx + 1);
                    _lineCount--;
                }
                else
                {
                    break;
                }
            }

            // 异步更新UI
            var text = _logBuffer.ToString();
            if (_logTextBox.InvokeRequired)
            {
                _logTextBox.BeginInvoke(() =>
                {
                    _logTextBox.Text = text;
                });
            }
            else
            {
                _logTextBox.Text = text;
            }
        }
    }

    public void ShowConsole()
    {
        Show();
        Activate();
    }

    public void HideConsole()
    {
        Hide();
    }

    public new void Close()
    {
        _scrollTimer.Stop();
        _scrollTimer.Dispose();
        base.Close();
    }
}
