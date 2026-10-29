using System.Windows.Forms;
using BGManager.Services;

namespace BGManager.Views;

public class ShortcutSettingsForm : Form
{
    private readonly ShortcutKeys _keys;
    private readonly ShortcutKeys _originalKeys;
    
    private TextBox txtRefresh;
    private TextBox txtAudit;
    private TextBox txtUnarchive;
    private TextBox txtDelete;
    private TextBox txtSearch;
    private TextBox txtPrint;
    
    private Button btnReset;
    private Button btnCancel;
    private Button btnSave;

    public ShortcutSettingsForm(ShortcutKeys keys)
    {
        _keys = keys;
        _originalKeys = new ShortcutKeys
        {
            Refresh = keys.Refresh,
            Audit = keys.Audit,
            Unarchive = keys.Unarchive,
            Delete = keys.Delete,
            Search = keys.Search,
            Print = keys.Print
        };

        Text = "快捷键设置";
        Width = 420;
        Height = 380;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        BuildUI();
        LoadKeys();
    }

    private void BuildUI()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Color.White };
        Controls.Add(panel);

        int y = 10;

        var lblTitle = new Label
        {
            Text = "快捷键配置",
            Font = new Font("微软雅黑", 12f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, y),
            ForeColor = Color.FromArgb(30, 41, 59)
        };
        panel.Controls.Add(lblTitle);
        y += 30;

        var line = new Panel { Location = new Point(20, y), Size = new Size(340, 1), BackColor = Color.FromArgb(226, 232, 240) };
        panel.Controls.Add(line);
        y += 20;

        CreateShortcutRow(panel, ref y, "刷新数据", () => txtRefresh, ref txtRefresh);
        CreateShortcutRow(panel, ref y, "审核标本", () => txtAudit, ref txtAudit);
        CreateShortcutRow(panel, ref y, "取消审核", () => txtUnarchive, ref txtUnarchive);
        CreateShortcutRow(panel, ref y, "删除标本", () => txtDelete, ref txtDelete);
        CreateShortcutRow(panel, ref y, "搜索患者", () => txtSearch, ref txtSearch);
        CreateShortcutRow(panel, ref y, "打印报告", () => txtPrint, ref txtPrint);

        y += 20;
        btnReset = new Button
        {
            Text = "恢复默认",
            Width = 80,
            Height = 30,
            Location = new Point(20, y),
            BackColor = Color.FromArgb(248, 113, 113),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnReset.FlatAppearance.BorderSize = 0;
        btnReset.Click += BtnReset_Click;
        panel.Controls.Add(btnReset);

        btnCancel = new Button
        {
            Text = "取消",
            Width = 80,
            Height = 30,
            Location = new Point(160, y),
            BackColor = Color.FromArgb(156, 163, 175),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Click += (s, e) => Close();
        panel.Controls.Add(btnCancel);

        btnSave = new Button
        {
            Text = "保存",
            Width = 80,
            Height = 30,
            Location = new Point(260, y),
            BackColor = Color.FromArgb(59, 130, 246),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += BtnSave_Click;
        panel.Controls.Add(btnSave);

        var lblNote = new Label
        {
            Text = "提示：输入键名（如 F5、Ctrl+F、Del），多个键用 + 分隔",
            AutoSize = true,
            Location = new Point(20, y + 40),
            ForeColor = Color.Gray,
            Font = new Font("微软雅黑", 8f)
        };
        panel.Controls.Add(lblNote);
    }

    private void CreateShortcutRow(Panel panel, ref int y, string labelText, Func<TextBox> getTextBox, ref TextBox textBox)
    {
        var lbl = new Label
        {
            Text = labelText,
            AutoSize = true,
            Location = new Point(20, y),
            Font = new Font("微软雅黑", 9f),
            ForeColor = Color.FromArgb(51, 65, 85)
        };
        panel.Controls.Add(lbl);

        textBox = new TextBox
        {
            Location = new Point(150, y - 2),
            Width = 180,
            Height = 26,
            Font = new Font("微软雅黑", 9f)
        };
        panel.Controls.Add(textBox);

        y += 35;
    }

    private void LoadKeys()
    {
        txtRefresh.Text = _keys.Refresh;
        txtAudit.Text = _keys.Audit;
        txtUnarchive.Text = _keys.Unarchive;
        txtDelete.Text = _keys.Delete;
        txtSearch.Text = _keys.Search;
        txtPrint.Text = _keys.Print;
    }

    private void BtnReset_Click(object? sender, EventArgs e)
    {
        txtRefresh.Text = _originalKeys.Refresh;
        txtAudit.Text = _originalKeys.Audit;
        txtUnarchive.Text = _originalKeys.Unarchive;
        txtDelete.Text = _originalKeys.Delete;
        txtSearch.Text = _originalKeys.Search;
        txtPrint.Text = _originalKeys.Print;
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        _keys.Refresh = txtRefresh.Text.Trim();
        _keys.Audit = txtAudit.Text.Trim();
        _keys.Unarchive = txtUnarchive.Text.Trim();
        _keys.Delete = txtDelete.Text.Trim();
        _keys.Search = txtSearch.Text.Trim();
        _keys.Print = txtPrint.Text.Trim();

        DialogResult = DialogResult.OK;
        Close();
    }
}