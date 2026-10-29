using BGShared.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace BGManager.Views;

public class PatientEditForm : Form
{
    public Patient Patient { get; private set; }

    private TextBox txtPatientId;
    private TextBox txtPatientName;
    private TextBox txtNameAbbr;
    private ComboBox cboGender;
    private TextBox txtBedNo;
    private TextBox txtDepartment;
    private Button btnOk;
    private Button btnCancel;
    private Button btnFloat;
    private TextBox txtPaste;
    private Button btnSmartParse;

    private bool _isNew;
    private readonly List<Patient> _allPatients;

    private bool _floating;
    private Form? _ownerForm;
    private FormWindowState _ownerWindowState;

    public PatientEditForm(Patient patient, List<Patient> allPatients)
    {
        Patient = patient;
        _allPatients = allPatients;
        _isNew = string.IsNullOrEmpty(patient.PatientId);
        Text = _isNew ? "新增患者" : "编辑患者";
        Width = 580;
        Height = 760;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("微软雅黑", 12f);

        BuildUI();
        LoadData();
        AttachValidationEvents();
        ValidateInputs();
    }

    private void BuildUI()
    {
        int y = 30;
        int labelX = 25;
        int inputX = 140;
        int inputWidth = 380;
        var labelFont = new Font("微软雅黑", 12f, FontStyle.Bold);
        const int rowStep = 50;

        var lblId = new Label { Text = "病案号:", AutoSize = true, Location = new Point(labelX, y + 4), Font = labelFont };
        Controls.Add(lblId);
        txtPatientId = new TextBox { Location = new Point(inputX, y), Width = inputWidth };
        Controls.Add(txtPatientId);
        y += rowStep;

        var lblName = new Label { Text = "姓名:", AutoSize = true, Location = new Point(labelX, y + 4), Font = labelFont };
        Controls.Add(lblName);
        txtPatientName = new TextBox { Location = new Point(inputX, y), Width = inputWidth };
        Controls.Add(txtPatientName);
        y += rowStep;

        var lblAbbr = new Label { Text = "姓名缩写:", AutoSize = true, Location = new Point(labelX, y + 4), Font = labelFont };
        Controls.Add(lblAbbr);
        txtNameAbbr = new TextBox { Location = new Point(inputX, y), Width = inputWidth };
        Controls.Add(txtNameAbbr);
        y += rowStep;

        var lblBed = new Label { Text = "床号:", AutoSize = true, Location = new Point(labelX, y + 4), Font = labelFont };
        Controls.Add(lblBed);
        txtBedNo = new TextBox { Location = new Point(inputX, y), Width = inputWidth };
        Controls.Add(txtBedNo);
        y += rowStep;

        var lblDept = new Label { Text = "科室:", AutoSize = true, Location = new Point(labelX, y + 4), Font = labelFont };
        Controls.Add(lblDept);
        txtDepartment = new TextBox { Location = new Point(inputX, y), Width = inputWidth, Text = "重症医学科" };
        Controls.Add(txtDepartment);
        y += rowStep;

        var lblGender = new Label { Text = "性别:", AutoSize = true, Location = new Point(labelX, y + 4), Font = labelFont };
        Controls.Add(lblGender);
        cboGender = new ComboBox { Location = new Point(inputX, y), Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
        cboGender.Items.AddRange(new object[] { "男", "女" });
        Controls.Add(cboGender);
        y += rowStep;

        btnOk = new Button
        {
            Text = "确定",
            Width = 100,
            Height = 40,
            Location = new Point(310, y + 12),
            Font = new Font("微软雅黑", 12f, FontStyle.Bold),
            DialogResult = DialogResult.OK
        };
        btnOk.Click += (s, e) => SaveData();
        Controls.Add(btnOk);

        btnCancel = new Button
        {
            Text = "取消",
            Width = 100,
            Height = 40,
            Location = new Point(420, y + 12),
            DialogResult = DialogResult.Cancel
        };
        Controls.Add(btnCancel);

        btnFloat = new Button
        {
            Text = "弹窗显示",
            Width = 120,
            Height = 40,
            Location = new Point(25, y + 66)
        };
        btnFloat.Click += BtnFloat_Click;
        Controls.Add(btnFloat);

        var lblFloatHint = new Label
        {
            Text = "弹窗显示时窗口将置顶为悬浮窗并隐藏主界面，可方便查看 HIS 系统界面，对照录入患者信息；点击确定后恢复主界面。",
            AutoSize = false,
            Width = 375,
            Height = 46,
            Location = new Point(155, y + 66),
            ForeColor = Color.Gray,
            Font = new Font("微软雅黑", 9.5f)
        };
        Controls.Add(lblFloatHint);

        // 智能识别区
        int parseY = y + 130;

        var divider = new Panel
        {
            Location = new Point(25, parseY),
            Size = new Size(515, 1),
            BackColor = Color.FromArgb(226, 232, 240)
        };
        Controls.Add(divider);

        var lblPaste = new Label
        {
            Text = "粘贴患者信息，点击「智能识别」自动填充",
            AutoSize = true,
            Location = new Point(25, parseY + 12),
            Font = new Font("微软雅黑", 11f, FontStyle.Bold)
        };
        Controls.Add(lblPaste);

        txtPaste = new TextBox
        {
            Location = new Point(25, parseY + 42),
            Width = 515,
            Height = 100,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("微软雅黑", 10.5f)
        };
        Controls.Add(txtPaste);

        btnSmartParse = new Button
        {
            Text = "智能识别",
            Width = 120,
            Height = 40,
            Location = new Point(25, parseY + 152),
            BackColor = Color.FromArgb(59, 130, 246),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("微软雅黑", 11f, FontStyle.Bold)
        };
        btnSmartParse.FlatAppearance.BorderSize = 0;
        btnSmartParse.Click += BtnSmartParse_Click;
        Controls.Add(btnSmartParse);

        var lblParseHint = new Label
        {
            Text = "粘贴患者顶部信息，如：01床 张三 30岁 男 诊断：呼衰 职工医保 总费用：1元 住院号：000001",
            AutoSize = false,
            Width = 385,
            Height = 44,
            Location = new Point(155, parseY + 150),
            ForeColor = Color.Gray,
            Font = new Font("微软雅黑", 9.5f)
        };
        Controls.Add(lblParseHint);

        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }

    private void BtnSmartParse_Click(object? sender, EventArgs e)
    {
        var text = txtPaste.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            MessageBox.Show("请先粘贴患者信息文本。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // 多行粘贴时取第一个有效行
        var line = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                       .Select(l => l.Trim())
                       .FirstOrDefault(l => l.Length > 0) ?? string.Empty;

        int filled = 0;

        // 床号：如「01床」，忽略前导 0
        var mBed = Regex.Match(line, @"(\d{1,3})\s*床");
        if (mBed.Success)
        {
            txtBedNo.Text = int.Parse(mBed.Groups[1].Value).ToString();
            filled++;
        }

        // 姓名：床号之后的首个中文姓名（2-4 字，支持「·」）
        var mName = Regex.Match(line, @"(?:\d{1,3}\s*床\s*)?([\u4e00-\u9fa5·]{2,4})(?=\s|\d+岁|男|女|$)");
        if (mName.Success)
        {
            var name = mName.Groups[1].Value;
            txtPatientName.Text = name;
            txtNameAbbr.Text = GetPinyinInitials(name);
            filled += 2;
        }

        // 性别
        var mGender = Regex.Match(line, @"(?<![\u4e00-\u9fa5])(男|女)(?![\u4e00-\u9fa5])");
        if (mGender.Success)
        {
            cboGender.SelectedItem = mGender.Groups[1].Value;
            filled++;
        }

        // 病案号（住院号）：如「住院号：220254」
        var mHosp = Regex.Match(line, @"住院号[：:]\s*(\d+)");
        if (mHosp.Success)
        {
            txtPatientId.Text = mHosp.Groups[1].Value;
            txtPatientId.BackColor = SystemColors.Window;
            filled++;
        }
        else
        {
            // 兼容「病案号」写法
            var mPat = Regex.Match(line, @"病案号[：:]\s*(\w+)");
            if (mPat.Success)
            {
                txtPatientId.Text = mPat.Groups[1].Value;
                txtPatientId.BackColor = SystemColors.Window;
                filled++;
            }
        }

        if (filled == 0)
        {
            MessageBox.Show("未能从文本中识别出患者信息，请检查格式，例如：\r\n01床 张三 60岁 男 住院号：220254",
                "智能识别", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    /// <summary>
    /// 获取中文姓名的拼音首字母缩写（基于 GB2312 区位码按拼音排序的特性）
    /// </summary>
    private static string GetPinyinInitials(string name)
    {
        var gb = Encoding.GetEncoding("GB2312");
        var sb = new StringBuilder();

        foreach (var c in name)
        {
            if (char.IsAscii(c) && char.IsLetter(c))
            {
                sb.Append(char.ToLowerInvariant(c));
                continue;
            }

            var bytes = gb.GetBytes(new[] { c });
            if (bytes.Length != 2) continue;

            int code = bytes[0] * 256 + bytes[1];
            char initial = code switch
            {
                >= 0xB0A1 and <= 0xB0C4 => 'a',
                >= 0xB0C5 and <= 0xB2C0 => 'b',
                >= 0xB2C1 and <= 0xB4ED => 'c',
                >= 0xB4EE and <= 0xB6E9 => 'd',
                >= 0xB6EA and <= 0xB7A1 => 'e',
                >= 0xB7A2 and <= 0xB8C0 => 'f',
                >= 0xB8C1 and <= 0xB9FD => 'g',
                >= 0xB9FE and <= 0xBBF6 => 'h',
                >= 0xBBF7 and <= 0xBFA5 => 'j',
                >= 0xBFA6 and <= 0xC0AB => 'k',
                >= 0xC0AC and <= 0xC2E7 => 'l',
                >= 0xC2E8 and <= 0xC4C2 => 'm',
                >= 0xC4C3 and <= 0xC5B5 => 'n',
                >= 0xC5B6 and <= 0xC5BD => 'o',
                >= 0xC5BE and <= 0xC6D9 => 'p',
                >= 0xC6DA and <= 0xC8BA => 'q',
                >= 0xC8BB and <= 0xC8F5 => 'r',
                >= 0xC8F6 and <= 0xCBF9 => 's',
                >= 0xCBFA and <= 0xCDD9 => 't',
                >= 0xCDDA and <= 0xCEF3 => 'w',
                >= 0xCEF4 and <= 0xD1B8 => 'x',
                >= 0xD1B9 and <= 0xD4D0 => 'y',
                >= 0xD4D1 and <= 0xD7F9 => 'z',
                _ => '\0'
            };
            if (initial != '\0') sb.Append(initial);
        }

        return sb.ToString();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _ownerForm = Application.OpenForms.OfType<Form>().FirstOrDefault(f => f != this);
    }

    private void BtnFloat_Click(object? sender, EventArgs e)
    {
        _floating = true;
        TopMost = true;
        btnFloat.Enabled = false;
        btnFloat.Text = "已置顶悬浮";

        if (_ownerForm != null)
        {
            // 解除 ownership：否则 Windows 会在主窗口隐藏时连带隐藏本弹窗
            Owner = null;
            _ownerWindowState = _ownerForm.WindowState;
            _ownerForm.Visible = false;
        }

        Activate();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        if (_floating && _ownerForm != null)
        {
            _ownerForm.Visible = true;
            _ownerForm.WindowState = _ownerWindowState;
            _ownerForm.Activate();
        }
        base.OnFormClosed(e);
    }

    private void LoadData()
    {
        txtPatientId.Text = Patient.PatientId;
        txtPatientName.Text = Patient.PatientName;
        txtNameAbbr.Text = Patient.NameAbbreviation;
        txtBedNo.Text = Patient.BedNo;
        txtDepartment.Text = string.IsNullOrEmpty(Patient.Department) ? "重症医学科" : Patient.Department;
        cboGender.SelectedItem = Patient.Gender;
    }

    private void AttachValidationEvents()
    {
        txtPatientId.TextChanged += (s, e) => ValidateInputs();
        txtPatientName.TextChanged += (s, e) => ValidateInputs();
        txtBedNo.TextChanged += (s, e) => ValidateInputs();
    }

    private void ValidateInputs()
    {
        var patientId = txtPatientId.Text.Trim();
        var patientName = txtPatientName.Text.Trim();
        var bedNo = txtBedNo.Text.Trim();

        var patientIdDuplicate = !string.IsNullOrEmpty(patientId) && CheckDuplicate(p => p.PatientId == patientId);
        var bedNoDuplicate = !string.IsNullOrEmpty(bedNo) && CheckDuplicate(p => p.BedNo == bedNo);

        txtPatientId.BackColor = patientIdDuplicate ? Color.LightCoral : Color.White;
        txtBedNo.BackColor = bedNoDuplicate ? Color.LightCoral : Color.White;

        btnOk.Enabled = !patientIdDuplicate && !bedNoDuplicate && 
                        !string.IsNullOrEmpty(patientId) && 
                        !string.IsNullOrEmpty(patientName) && 
                        !string.IsNullOrEmpty(bedNo);
    }

    private bool CheckDuplicate(Func<Patient, bool> predicate)
    {
        return _allPatients.Any(p => p.IsActive && predicate(p) && p.PatientId != Patient.PatientId);
    }

    private void SaveData()
    {
        var patientId = txtPatientId.Text.Trim();
        var patientName = txtPatientName.Text.Trim();
        var bedNo = txtBedNo.Text.Trim();

        if (string.IsNullOrWhiteSpace(patientName))
        {
            MessageBox.Show("请输入姓名", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            txtPatientName.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(patientId))
        {
            MessageBox.Show("请输入病案号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            txtPatientId.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(bedNo))
        {
            MessageBox.Show("请输入床号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            txtBedNo.Focus();
            return;
        }

        var patientIdDuplicate = CheckDuplicate(p => p.PatientId == patientId);
        if (patientIdDuplicate)
        {
            MessageBox.Show("病案号已存在", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            txtPatientId.Focus();
            return;
        }

        var bedNoDuplicate = CheckDuplicate(p => p.BedNo == bedNo);
        if (bedNoDuplicate)
        {
            MessageBox.Show("床号已存在", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            txtBedNo.Focus();
            return;
        }

        Patient.PatientId = patientId;
        Patient.PatientName = patientName;
        Patient.NameAbbreviation = txtNameAbbr.Text.Trim();
        Patient.BedNo = bedNo;
        Patient.Department = txtDepartment.Text.Trim();
        Patient.Gender = cboGender.SelectedItem?.ToString() ?? "";
        Patient.IsActive = true;
    }
}
