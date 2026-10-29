using BGShared.Models;

namespace BGManager.Views;

public class PatientInfoPanel : Panel
{
    public TextBox TxtPatientName { get; private set; }
    public TextBox TxtBedNo { get; private set; }
    public TextBox TxtPatientId { get; private set; }
    public TextBox TxtOriginalPatientId { get; private set; }
    public ComboBox CboGender { get; private set; }
    public TextBox TxtDepartment { get; private set; }
    public TextBox TxtReportTime { get; private set; }
    public TextBox TxtSpecimenNo { get; private set; }
    public ComboBox CboSampleType { get; private set; }

    public PatientInfoPanel()
    {
        Dock = DockStyle.Fill;
        BuildUI();
    }

    private void BuildUI()
    {
        var group = new GroupBox { Text = "患者信息", Dock = DockStyle.Fill, Font = new Font("微软雅黑", 10f), Padding = new Padding(8) };
        Controls.Add(group);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 10,
            ColumnCount = 2,
            Padding = new Padding(5)
        };
        for (int i = 0; i < 10; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80f));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        group.Controls.Add(layout);

        int row = 0;
        AddLabeledTextBox(layout, "患者姓名:", out var txtName, row++);
        TxtPatientName = txtName;
        
        AddLabeledTextBox(layout, "床号:", out var txtBed, row++);
        TxtBedNo = txtBed;
        
        AddLabeledTextBox(layout, "病案号:", out var txtId, row++);
        TxtPatientId = txtId;
        
        AddLabeledTextBox(layout, "原始编号:", out var txtOriginal, row++);
        TxtOriginalPatientId = txtOriginal;
        
        AddLabeledComboBox(layout, "性别:", out var cboG, row++, new[] { "", "男", "女", "未知" });
        CboGender = cboG;
        
        AddLabeledTextBox(layout, "科室:", out var txtDept, row++);
        TxtDepartment = txtDept;
        
        AddLabeledTextBox(layout, "报告时间:", out var txtTime, row++, true);
        TxtReportTime = txtTime;
        
        AddLabeledTextBox(layout, "样本号:", out var txtSpec, row++, true);
        TxtSpecimenNo = txtSpec;
        
        AddLabeledComboBox(layout, "样本类型:", out var cboSample, row++, new[] { "未指定", "动脉", "静脉", "混合静脉" });
        CboSampleType = cboSample;
    }

    private static void AddLabeledTextBox(TableLayoutPanel layout, string label, out TextBox textBox, int row, bool readOnly = false)
    {
        var lbl = new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Font = new Font("微软雅黑", 10f) };
        layout.Controls.Add(lbl, 0, row);
        textBox = new TextBox { Dock = DockStyle.Fill, Font = new Font("微软雅黑", 10f), ReadOnly = readOnly, BackColor = readOnly ? Color.FromArgb(245, 245, 245) : Color.White };
        layout.Controls.Add(textBox, 1, row);
    }

    private static void AddLabeledComboBox(TableLayoutPanel layout, string label, out ComboBox comboBox, int row, string[] items)
    {
        var lbl = new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Font = new Font("微软雅黑", 10f) };
        layout.Controls.Add(lbl, 0, row);
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 4, 0, 4) };
        comboBox = new ComboBox
        {
            Dock = DockStyle.Top,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("微软雅黑", 10f),
            Enabled = false
        };
        comboBox.Items.AddRange(items);
        panel.Controls.Add(comboBox);
        layout.Controls.Add(panel, 1, row);
    }

    public void Clear()
    {
        TxtPatientName.Text = "";
        TxtBedNo.Text = "";
        TxtPatientId.Text = "";
        TxtOriginalPatientId.Text = "";
        CboGender.SelectedIndex = -1;
        TxtDepartment.Text = "";
        TxtReportTime.Text = "";
        TxtSpecimenNo.Text = "";
        CboSampleType.SelectedIndex = -1;
    }

    public void BindData(Specimen specimen)
    {
        bool isPending = specimen.Status == SpecimenStatus.Pending;
        
        TxtPatientName.Text = isPending ? specimen.PatientName ?? "" : specimen.SnapshotPatientName ?? "";
        TxtBedNo.Text = isPending ? specimen.BedNo ?? "" : specimen.SnapshotBedNo ?? "";
        TxtPatientId.Text = isPending ? specimen.PatientId ?? "" : specimen.SnapshotPatientId ?? "";
        TxtOriginalPatientId.Text = specimen.OriginalPatientId ?? "";
        
        string gender = isPending ? specimen.Gender ?? "" : specimen.SnapshotGender ?? "";
        CboGender.SelectedIndex = CboGender.Items.IndexOf(gender);
        
        TxtDepartment.Text = isPending ? specimen.Department ?? "" : specimen.SnapshotDepartment ?? "";
        TxtReportTime.Text = specimen.TestTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
        TxtSpecimenNo.Text = specimen.SpecimenNo ?? "";
        
        string sampleType = isPending ? specimen.SampleType ?? "" : specimen.SnapshotSampleType ?? "";
        CboSampleType.SelectedIndex = CboSampleType.Items.IndexOf(string.IsNullOrEmpty(sampleType) ? "未指定" : sampleType);
    }
}