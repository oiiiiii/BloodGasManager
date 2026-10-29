using System.Windows.Forms;
using BGService.Data;

namespace BGService.Tray;

public class BackupForm : Form
{
    private Button btnExport;
    private Button btnImport;
    private Label lblStatus;

    public BackupForm()
    {
        Text = "数据备份与恢复 - 血气分析服务";
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
    }

    private void BuildUI()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            RowCount = 3,
            ColumnCount = 2
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        Controls.Add(layout);

        btnExport = new Button
        {
            Text = "备份数据",
            Dock = DockStyle.Fill,
            Margin = new Padding(10),
            Font = new Font("微软雅黑", 11f)
        };
        btnExport.Click += BtnExport_Click;
        layout.Controls.Add(btnExport, 0, 0);

        btnImport = new Button
        {
            Text = "导入数据",
            Dock = DockStyle.Fill,
            Margin = new Padding(10),
            Font = new Font("微软雅黑", 11f)
        };
        btnImport.Click += BtnImport_Click;
        layout.Controls.Add(btnImport, 1, 0);

        var lblHint = new Label
        {
            Text = "备份文件为数据库文件 (.db)",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(156, 163, 175),
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(10),
            Font = new Font("微软雅黑", 9f)
        };
        layout.Controls.Add(lblHint, 0, 1);
        layout.SetColumnSpan(lblHint, 2);

        lblStatus = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(75, 85, 99),
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(10),
            Font = new Font("微软雅黑", 9f)
        };
        layout.Controls.Add(lblStatus, 0, 2);
        layout.SetColumnSpan(lblStatus, 2);
    }

    private void BtnExport_Click(object? sender, EventArgs e)
    {
        using var saveDialog = new SaveFileDialog
        {
            Filter = "数据库文件 (*.db)|*.db",
            FileName = $"血气分析数据备份_{DateTime.Now:yyyyMMdd_HHmmss}.db",
            Title = "选择备份文件保存位置"
        };

        if (saveDialog.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            var service = new BackupService();
            var (count, path) = service.ExportDatabase(saveDialog.FileName);
            
            lblStatus.Text = $"备份成功！共 {count} 份报告。";
            lblStatus.ForeColor = Color.FromArgb(22, 163, 74);
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"备份失败: {ex.Message}";
            lblStatus.ForeColor = Color.Red;
        }
    }

    private void BtnImport_Click(object? sender, EventArgs e)
    {
        using var openDialog = new OpenFileDialog
        {
            Filter = "数据库文件 (*.db)|*.db",
            Title = "选择要导入的备份文件"
        };

        if (openDialog.ShowDialog() != DialogResult.OK)
            return;

        var confirm = MessageBox.Show("确定要导入数据吗？重复的报告将被自动跳过。", "导入确认",
            MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
        if (confirm != DialogResult.OK)
            return;

        try
        {
            var service = new BackupService();
            var (imported, skipped) = service.ImportDatabase(openDialog.FileName);
            
            lblStatus.Text = $"导入完成！成功导入 {imported} 份，跳过 {skipped} 份重复。";
            lblStatus.ForeColor = Color.FromArgb(22, 163, 74);
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"导入失败: {ex.Message}";
            lblStatus.ForeColor = Color.Red;
        }
    }
}