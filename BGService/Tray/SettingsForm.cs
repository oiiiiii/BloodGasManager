using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;
using BGService.Config;

namespace BGService.Tray;

public class SettingsForm : Form
{
    private NumericUpDown nudPort;
    private CheckBox chkAutoStart;
    private CheckBox chkStartMinimized;
    private Button btnSave;
    private Button btnCancel;
    private Label lblStatus;

    public SettingsForm()
    {
        Text = "设置 - 血气分析服务";
        Width = 420;
        Height = 280;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("微软雅黑", 10f);
        BackColor = Color.FromArgb(249, 250, 251);

        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "normal.ico");
        if (File.Exists(iconPath))
            Icon = new Icon(iconPath);

        BuildUI();
        LoadSettings();
    }

    private void BuildUI()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            RowCount = 5,
            ColumnCount = 2
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140f));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));
        Controls.Add(layout);

        var lblPort = new Label { Text = "API 端口号:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };
        layout.Controls.Add(lblPort, 0, 0);

        nudPort = new NumericUpDown
        {
            Minimum = 1024,
            Maximum = 65535,
            Dock = DockStyle.Fill,
            Margin = new Padding(10, 5, 10, 5),
            Font = new Font("微软雅黑", 10f)
        };
        layout.Controls.Add(nudPort, 1, 0);

        var lblAutoStart = new Label { Text = "开机自动运行:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };
        layout.Controls.Add(lblAutoStart, 0, 1);

        chkAutoStart = new CheckBox { Text = "开机时自动启动服务", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(10, 5, 10, 5) };
        layout.Controls.Add(chkAutoStart, 1, 1);

        var lblMinimized = new Label { Text = "默认启动:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };
        layout.Controls.Add(lblMinimized, 0, 2);

        chkStartMinimized = new CheckBox { Text = "启动时最小化到托盘", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(10, 5, 10, 5) };
        layout.Controls.Add(chkStartMinimized, 1, 2);

        lblStatus = new Label { Dock = DockStyle.Fill, ForeColor = Color.FromArgb(75, 85, 99), TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(10, 0, 0, 0) };
        layout.Controls.Add(lblStatus, 0, 3);
        layout.SetColumnSpan(lblStatus, 2);

        var btnPanel = new Panel { Dock = DockStyle.Fill };
        btnSave = new Button { Text = "保存", Width = 100, Height = 35, Left = 120, Top = 5, Font = new Font("微软雅黑", 10f) };
        btnSave.Click += BtnSave_Click;
        btnCancel = new Button { Text = "取消", Width = 100, Height = 35, Left = 230, Top = 5, Font = new Font("微软雅黑", 10f) };
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        btnPanel.Controls.Add(btnSave);
        btnPanel.Controls.Add(btnCancel);
        layout.Controls.Add(btnPanel, 0, 4);
        layout.SetColumnSpan(btnPanel, 2);
    }

    private void LoadSettings()
    {
        var settings = SettingsManager.Current;
        nudPort.Value = settings.ApiPort;
        chkAutoStart.Checked = settings.AutoStart;
        chkStartMinimized.Checked = settings.StartMinimized;
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        try
        {
            var settings = SettingsManager.Current;
            int oldPort = settings.ApiPort;

            settings.ApiPort = (int)nudPort.Value;
            settings.AutoStart = chkAutoStart.Checked;
            settings.StartMinimized = chkStartMinimized.Checked;
            SettingsManager.Save();

            SetAutoStart(chkAutoStart.Checked);

            if (oldPort != settings.ApiPort)
            {
                lblStatus.Text = "保存成功！端口号修改后需重启服务生效。";
                lblStatus.ForeColor = Color.FromArgb(217, 119, 6);
            }
            else
            {
                lblStatus.Text = "保存成功！";
                lblStatus.ForeColor = Color.FromArgb(22, 163, 74);
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"保存失败: {ex.Message}";
            lblStatus.ForeColor = Color.Red;
        }
    }

    private static void SetAutoStart(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (key == null) return;

            string appPath = Application.ExecutablePath;
            string valueName = "BGBloodGasService";

            if (enable)
                key.SetValue(valueName, $"\"{appPath}\"");
            else if (key.GetValue(valueName) != null)
                key.DeleteValue(valueName, false);
        }
        catch
        {
        }
    }
}
