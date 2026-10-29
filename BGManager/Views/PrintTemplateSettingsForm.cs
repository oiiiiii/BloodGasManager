using BGShared.Config;
using BGShared.Models;

namespace BGManager.Views;

public class PrintTemplateSettingsForm : Form
{
    private PrintTemplate _currentTemplate;
    private bool _hasUnsavedChanges;

    private ComboBox cmbPaperSize;
    private CheckBox chkLandscape;
    private ComboBox cmbReportsPerPage;
    private NumericUpDown numLeftMargin;
    private NumericUpDown numTopMargin;
    private NumericUpDown numRightMargin;
    private NumericUpDown numBottomMargin;

    private TextBox txtHeaderTitle;
    private NumericUpDown numHeaderFontSize;
    private CheckBox chkHeaderBold;
    private NumericUpDown numHeaderSpacing;

    private CheckBox chkShowPatientInfo;
    private NumericUpDown numPatientInfoSpacing;
    private NumericUpDown numPatientInfoFontSize;
    private NumericUpDown numPatientInfoLineSpacing;
    private DataGridView dgvPatientFields;
    private Button btnMoveUp;
    private Button btnMoveDown;
    private Button btnMoveRowUp;
    private Button btnMoveRowDown;

    private CheckBox chkUseTwoColumnLayout;
    private NumericUpDown numColumnSpacing;
    private NumericUpDown numResultTableSpacing;
    private NumericUpDown numItemFontSize;
    private CheckBox chkShowReferenceRange;
    private CheckBox chkShowUnit;
    private CheckBox chkShowResultFlag;
    private NumericUpDown numRowHeight;

    private CheckBox chkShowFooter;
    private TextBox txtFooterLeft;
    private TextBox txtFooterRight;
    private NumericUpDown numFooterFontSize;
    private NumericUpDown numFooterSpacing;

    private Button btnSave;
    private Button btnCancel;
    private Button btnImport;
    private Button btnExport;
    private Button btnReset;
    private Label lblStatus;

    public PrintTemplateSettingsForm()
    {
        _currentTemplate = CloneTemplate(PrintTemplateManager.CurrentTemplate);
        _hasUnsavedChanges = false;

        InitializeComponent();
        LoadTemplate();
        UpdateStatus();
    }

    private PrintTemplate CloneTemplate(PrintTemplate source)
    {
        return new PrintTemplate
        {
            TemplateName = source.TemplateName,
            
            PaperSize = source.PaperSize,
            Landscape = source.Landscape,
            LeftMargin = source.LeftMargin,
            TopMargin = source.TopMargin,
            RightMargin = source.RightMargin,
            BottomMargin = source.BottomMargin,
            
            ReportsPerPage = source.ReportsPerPage,
            
            HeaderTitle = source.HeaderTitle,
            HeaderFontSize = source.HeaderFontSize,
            HeaderBold = source.HeaderBold,
            HeaderSpacing = source.HeaderSpacing,
            
            ShowPatientInfo = source.ShowPatientInfo,
            PatientInfoSpacing = source.PatientInfoSpacing,
            PatientInfoLayout = new PatientInfoLayout
            {
                FontSize = source.PatientInfoLayout.FontSize,
                LineSpacing = source.PatientInfoLayout.LineSpacing,
                Fields = source.PatientInfoLayout.Fields
                    .Select(f => new PatientInfoField
                    {
                        FieldName = f.FieldName,
                        DisplayName = f.DisplayName,
                        Label = f.Label,
                        Show = f.Show,
                        Row = f.Row,
                        Order = f.Order,
                        XOffset = f.XOffset
                    })
                    .ToList()
            },
            
            UseTwoColumnLayout = source.UseTwoColumnLayout,
            ColumnSpacing = source.ColumnSpacing,
            ResultTableSpacing = source.ResultTableSpacing,
            ItemFontSize = source.ItemFontSize,
            ShowReferenceRange = source.ShowReferenceRange,
            ShowUnit = source.ShowUnit,
            ShowResultFlag = source.ShowResultFlag,
            RowHeight = source.RowHeight,
            
            ShowFooter = source.ShowFooter,
            FooterLeft = source.FooterLeft,
            FooterRight = source.FooterRight,
            FooterFontSize = source.FooterFontSize,
            FooterSpacing = source.FooterSpacing
        };
    }

    private void SetNumericUpDownValue(NumericUpDown numeric, decimal value)
    {
        if (value < numeric.Minimum)
            numeric.Value = numeric.Minimum;
        else if (value > numeric.Maximum)
            numeric.Value = numeric.Maximum;
        else
            numeric.Value = value;
    }

    private void LoadTemplate()
    {
        cmbPaperSize.SelectedItem = _currentTemplate.PaperSize;
        chkLandscape.Checked = _currentTemplate.Landscape;
        cmbReportsPerPage.SelectedItem = _currentTemplate.ReportsPerPage == 1 ? "单份" : "双份";
        SetNumericUpDownValue(numLeftMargin, (decimal)_currentTemplate.LeftMargin);
        SetNumericUpDownValue(numTopMargin, (decimal)_currentTemplate.TopMargin);
        SetNumericUpDownValue(numRightMargin, (decimal)_currentTemplate.RightMargin);
        SetNumericUpDownValue(numBottomMargin, (decimal)_currentTemplate.BottomMargin);
        
        txtHeaderTitle.Text = _currentTemplate.HeaderTitle;
        SetNumericUpDownValue(numHeaderFontSize, (decimal)_currentTemplate.HeaderFontSize);
        chkHeaderBold.Checked = _currentTemplate.HeaderBold;
        SetNumericUpDownValue(numHeaderSpacing, (decimal)_currentTemplate.HeaderSpacing);
        
        chkShowPatientInfo.Checked = _currentTemplate.ShowPatientInfo;
        SetNumericUpDownValue(numPatientInfoSpacing, (decimal)_currentTemplate.PatientInfoSpacing);
        SetNumericUpDownValue(numPatientInfoFontSize, (decimal)_currentTemplate.PatientInfoLayout.FontSize);
        SetNumericUpDownValue(numPatientInfoLineSpacing, (decimal)_currentTemplate.PatientInfoLayout.LineSpacing);
        
        LoadPatientFieldsToGrid();
        
        chkUseTwoColumnLayout.Checked = _currentTemplate.UseTwoColumnLayout;
        SetNumericUpDownValue(numColumnSpacing, (decimal)_currentTemplate.ColumnSpacing);
        SetNumericUpDownValue(numResultTableSpacing, (decimal)_currentTemplate.ResultTableSpacing);
        SetNumericUpDownValue(numItemFontSize, (decimal)_currentTemplate.ItemFontSize);
        chkShowReferenceRange.Checked = _currentTemplate.ShowReferenceRange;
        chkShowUnit.Checked = _currentTemplate.ShowUnit;
        chkShowResultFlag.Checked = _currentTemplate.ShowResultFlag;
        SetNumericUpDownValue(numRowHeight, (decimal)_currentTemplate.RowHeight);
        
        chkShowFooter.Checked = _currentTemplate.ShowFooter;
        txtFooterLeft.Text = _currentTemplate.FooterLeft;
        txtFooterRight.Text = "（由标本备注自动填充，不可预设）";
        SetNumericUpDownValue(numFooterFontSize, (decimal)_currentTemplate.FooterFontSize);
        SetNumericUpDownValue(numFooterSpacing, (decimal)_currentTemplate.FooterSpacing);
    }

    private void LoadPatientFieldsToGrid()
    {
        dgvPatientFields.Rows.Clear();
        var fields = _currentTemplate.PatientInfoLayout.Fields.OrderBy(f => f.Row).ThenBy(f => f.Order).ToList();
        
        foreach (var field in fields)
        {
            dgvPatientFields.Rows.Add(
                field.DisplayName,
                field.Show,
                field.Row,
                field.Order,
                field.XOffset
            );
        }
    }

    private void SaveTemplateToCurrent()
    {
        _currentTemplate.PaperSize = cmbPaperSize.SelectedItem?.ToString() ?? "A4";
        _currentTemplate.Landscape = chkLandscape.Checked;
        _currentTemplate.ReportsPerPage = cmbReportsPerPage.SelectedItem?.ToString() == "单份" ? 1 : 2;
        _currentTemplate.LeftMargin = (float)numLeftMargin.Value;
        _currentTemplate.TopMargin = (float)numTopMargin.Value;
        _currentTemplate.RightMargin = (float)numRightMargin.Value;
        _currentTemplate.BottomMargin = (float)numBottomMargin.Value;
        
        _currentTemplate.HeaderTitle = txtHeaderTitle.Text;
        _currentTemplate.HeaderFontSize = (float)numHeaderFontSize.Value;
        _currentTemplate.HeaderBold = chkHeaderBold.Checked;
        _currentTemplate.HeaderSpacing = (float)numHeaderSpacing.Value;
        
        _currentTemplate.ShowPatientInfo = chkShowPatientInfo.Checked;
        _currentTemplate.PatientInfoSpacing = (float)numPatientInfoSpacing.Value;
        _currentTemplate.PatientInfoLayout.FontSize = (float)numPatientInfoFontSize.Value;
        _currentTemplate.PatientInfoLayout.LineSpacing = (float)numPatientInfoLineSpacing.Value;
        
        SavePatientFieldsFromGrid();
        
        _currentTemplate.UseTwoColumnLayout = chkUseTwoColumnLayout.Checked;
        _currentTemplate.ColumnSpacing = (float)numColumnSpacing.Value;
        _currentTemplate.ResultTableSpacing = (float)numResultTableSpacing.Value;
        _currentTemplate.ItemFontSize = (float)numItemFontSize.Value;
        _currentTemplate.ShowReferenceRange = chkShowReferenceRange.Checked;
        _currentTemplate.ShowUnit = chkShowUnit.Checked;
        _currentTemplate.ShowResultFlag = chkShowResultFlag.Checked;
        _currentTemplate.RowHeight = (float)numRowHeight.Value;
        
        _currentTemplate.ShowFooter = chkShowFooter.Checked;
        _currentTemplate.FooterLeft = txtFooterLeft.Text;
        _currentTemplate.FooterRight = ""; // 右侧内容由标本备注自动填充，模板不再保存预设值
        _currentTemplate.FooterFontSize = (float)numFooterFontSize.Value;
        _currentTemplate.FooterSpacing = (float)numFooterSpacing.Value;
    }

    private void SavePatientFieldsFromGrid()
    {
        for (int i = 0; i < dgvPatientFields.Rows.Count; i++)
        {
            var row = dgvPatientFields.Rows[i];
            var field = _currentTemplate.PatientInfoLayout.Fields.FirstOrDefault(f => 
                f.DisplayName == (row.Cells[0].Value?.ToString() ?? ""));
            
            if (field != null)
            {
                field.Show = Convert.ToBoolean(row.Cells[1].Value ?? false);
                field.Row = Convert.ToInt32(row.Cells[2].Value ?? 0);
                field.Order = Convert.ToInt32(row.Cells[3].Value ?? 0);
                field.XOffset = Convert.ToSingle(row.Cells[4].Value ?? 0f);
            }
        }
    }

    private void MarkUnsaved()
    {
        _hasUnsavedChanges = true;
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        if (_hasUnsavedChanges)
        {
            lblStatus.Text = "有未保存的更改";
            lblStatus.ForeColor = Color.Orange;
        }
        else
        {
            lblStatus.Text = "已保存";
            lblStatus.ForeColor = Color.Green;
        }
    }

    private void btnSave_Click(object? sender, EventArgs e)
    {
        SaveTemplateToCurrent();
        PrintTemplateManager.SaveTemplate(_currentTemplate);
        _hasUnsavedChanges = false;
        UpdateStatus();
        MessageBox.Show("模板已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void btnCancel_Click(object? sender, EventArgs e)
    {
        if (_hasUnsavedChanges)
        {
            var result = MessageBox.Show("有未保存的更改，确定要退出吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
                return;
        }
        Close();
    }

    private void btnReset_Click(object? sender, EventArgs e)
    {
        var result = MessageBox.Show("确定要重置为默认模板吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes)
            return;
        
        PrintTemplateManager.ResetToDefault();
        _currentTemplate = CloneTemplate(PrintTemplateManager.CurrentTemplate);
        _hasUnsavedChanges = false;
        LoadTemplate();
        UpdateStatus();
    }

    private void btnImport_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog();
        dialog.Filter = "JSON 文件|*.json|所有文件|*.*";
        dialog.Title = "导入打印模板";
        
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            var template = PrintTemplateManager.ImportTemplate(dialog.FileName);
            if (template != null)
            {
                _currentTemplate = template;
                _hasUnsavedChanges = true;
                LoadTemplate();
                UpdateStatus();
                MessageBox.Show("模板已导入", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("导入失败，请检查文件格式", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void btnExport_Click(object? sender, EventArgs e)
    {
        SaveTemplateToCurrent();
        using var dialog = new SaveFileDialog();
        dialog.Filter = "JSON 文件|*.json|所有文件|*.*";
        dialog.Title = "导出打印模板";
        dialog.FileName = "print_template.json";
        
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            PrintTemplateManager.ExportTemplate(dialog.FileName);
            MessageBox.Show("模板已导出", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void btnMoveUp_Click(object? sender, EventArgs e)
    {
        if (dgvPatientFields.SelectedRows.Count == 0)
            return;
        
        int selectedIndex = dgvPatientFields.SelectedRows[0].Index;
        if (selectedIndex <= 0)
            return;
        
        var currentRow = dgvPatientFields.Rows[selectedIndex];
        var prevRow = dgvPatientFields.Rows[selectedIndex - 1];
        
        var currentOrder = Convert.ToInt32(currentRow.Cells[3].Value ?? 0);
        var prevOrder = Convert.ToInt32(prevRow.Cells[3].Value ?? 0);
        
        currentRow.Cells[3].Value = prevOrder;
        prevRow.Cells[3].Value = currentOrder;
        
        dgvPatientFields.Rows.RemoveAt(selectedIndex);
        dgvPatientFields.Rows.Insert(selectedIndex - 1, currentRow);
        
        dgvPatientFields.ClearSelection();
        dgvPatientFields.Rows[selectedIndex - 1].Selected = true;
        MarkUnsaved();
    }

    private void btnMoveDown_Click(object? sender, EventArgs e)
    {
        if (dgvPatientFields.SelectedRows.Count == 0)
            return;
        
        int selectedIndex = dgvPatientFields.SelectedRows[0].Index;
        if (selectedIndex >= dgvPatientFields.Rows.Count - 1)
            return;
        
        var currentRow = dgvPatientFields.Rows[selectedIndex];
        var nextRow = dgvPatientFields.Rows[selectedIndex + 1];
        
        var currentOrder = Convert.ToInt32(currentRow.Cells[3].Value ?? 0);
        var nextOrder = Convert.ToInt32(nextRow.Cells[3].Value ?? 0);
        
        currentRow.Cells[3].Value = nextOrder;
        nextRow.Cells[3].Value = currentOrder;
        
        dgvPatientFields.Rows.RemoveAt(selectedIndex);
        dgvPatientFields.Rows.Insert(selectedIndex + 1, currentRow);
        
        dgvPatientFields.ClearSelection();
        dgvPatientFields.Rows[selectedIndex + 1].Selected = true;
        MarkUnsaved();
    }

    private void btnMoveRowUp_Click(object? sender, EventArgs e)
    {
        if (dgvPatientFields.SelectedRows.Count == 0)
            return;
        
        int selectedIndex = dgvPatientFields.SelectedRows[0].Index;
        var row = dgvPatientFields.Rows[selectedIndex];
        var currentRowNum = Convert.ToInt32(row.Cells[2].Value ?? 0);
        
        if (currentRowNum <= 0)
            return;
        
        row.Cells[2].Value = currentRowNum - 1;
        MarkUnsaved();
    }

    private void btnMoveRowDown_Click(object? sender, EventArgs e)
    {
        if (dgvPatientFields.SelectedRows.Count == 0)
            return;
        
        int selectedIndex = dgvPatientFields.SelectedRows[0].Index;
        var row = dgvPatientFields.Rows[selectedIndex];
        var currentRowNum = Convert.ToInt32(row.Cells[2].Value ?? 0);
        
        row.Cells[2].Value = currentRowNum + 1;
        MarkUnsaved();
    }

    private void InitializeComponent()
    {
        cmbPaperSize = new ComboBox();
        chkLandscape = new CheckBox();
        cmbReportsPerPage = new ComboBox();
        numLeftMargin = new NumericUpDown();
        numTopMargin = new NumericUpDown();
        numRightMargin = new NumericUpDown();
        numBottomMargin = new NumericUpDown();
        txtHeaderTitle = new TextBox();
        numHeaderFontSize = new NumericUpDown();
        chkHeaderBold = new CheckBox();
        numHeaderSpacing = new NumericUpDown();
        chkShowPatientInfo = new CheckBox();
        numPatientInfoSpacing = new NumericUpDown();
        numPatientInfoFontSize = new NumericUpDown();
        numPatientInfoLineSpacing = new NumericUpDown();
        dgvPatientFields = new DataGridView();
        btnMoveUp = new Button();
        btnMoveDown = new Button();
        btnMoveRowUp = new Button();
        btnMoveRowDown = new Button();
        chkUseTwoColumnLayout = new CheckBox();
        numColumnSpacing = new NumericUpDown();
        numResultTableSpacing = new NumericUpDown();
        numItemFontSize = new NumericUpDown();
        chkShowReferenceRange = new CheckBox();
        chkShowUnit = new CheckBox();
        chkShowResultFlag = new CheckBox();
        numRowHeight = new NumericUpDown();
        chkShowFooter = new CheckBox();
        txtFooterLeft = new TextBox();
        txtFooterRight = new TextBox();
        numFooterFontSize = new NumericUpDown();
        numFooterSpacing = new NumericUpDown();
        btnSave = new Button();
        btnCancel = new Button();
        btnImport = new Button();
        btnExport = new Button();
        btnReset = new Button();
        lblStatus = new Label();
        
        var scrollPanel = new Panel();
        var layout = new TableLayoutPanel();
        var buttonPanel = new TableLayoutPanel();
        
        SuspendLayout();
        
        scrollPanel.Dock = DockStyle.Fill;
        scrollPanel.AutoScroll = true;
        
        layout.Dock = DockStyle.Top;
        layout.ColumnCount = 2;
        layout.RowCount = 0;
        layout.Padding = new Padding(10);
        layout.AutoSize = true;
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150f));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        
        AddSectionHeader(layout, "页面设置");
        AddLabel(layout, "纸张大小:");
        cmbPaperSize.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbPaperSize.Items.AddRange(new object[] { "A5", "A4", "Letter" });
        cmbPaperSize.Width = 200;
        cmbPaperSize.SelectedIndexChanged += (s, e) => MarkUnsaved();
        layout.Controls.Add(cmbPaperSize, 1, layout.RowCount - 1);
        
        AddLabel(layout, "横版:");
        chkLandscape.AutoSize = true;
        chkLandscape.CheckedChanged += (s, e) => MarkUnsaved();
        layout.Controls.Add(chkLandscape, 1, layout.RowCount - 1);
        
        AddLabel(layout, "每页报告数量:");
        cmbReportsPerPage.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbReportsPerPage.Items.AddRange(new object[] { "单份", "双份" });
        cmbReportsPerPage.Width = 200;
        cmbReportsPerPage.SelectedIndexChanged += (s, e) => MarkUnsaved();
        layout.Controls.Add(cmbReportsPerPage, 1, layout.RowCount - 1);
        
        AddLabel(layout, "左边距:");
        AddNumericUpDown(layout, 0, 200, numLeftMargin, true);
        
        AddLabel(layout, "上边距:");
        AddNumericUpDown(layout, 0, 200, numTopMargin, true);
        
        AddLabel(layout, "右边距:");
        AddNumericUpDown(layout, 0, 200, numRightMargin, true);
        
        AddLabel(layout, "下边距:");
        AddNumericUpDown(layout, 0, 200, numBottomMargin, true);
        
        AddSectionHeader(layout, "页眉设置");
        AddLabel(layout, "标题:");
        txtHeaderTitle.Width = 300;
        txtHeaderTitle.TextChanged += (s, e) => MarkUnsaved();
        layout.Controls.Add(txtHeaderTitle, 1, layout.RowCount - 1);
        
        AddLabel(layout, "字号:");
        AddNumericUpDown(layout, 8, 48, numHeaderFontSize, true);
        
        AddLabel(layout, "加粗:");
        chkHeaderBold.AutoSize = true;
        chkHeaderBold.CheckedChanged += (s, e) => MarkUnsaved();
        layout.Controls.Add(chkHeaderBold, 1, layout.RowCount - 1);
        
        AddLabel(layout, "下方间距:");
        AddNumericUpDown(layout, 0, 100, numHeaderSpacing, true);
        
        AddSectionHeader(layout, "患者信息");
        AddLabel(layout, "显示患者信息:");
        chkShowPatientInfo.AutoSize = true;
        chkShowPatientInfo.CheckedChanged += (s, e) => MarkUnsaved();
        layout.Controls.Add(chkShowPatientInfo, 1, layout.RowCount - 1);
        
        AddLabel(layout, "字号:");
        AddNumericUpDown(layout, 8, 24, numPatientInfoFontSize, true);
        
        AddLabel(layout, "下方间距:");
        AddNumericUpDown(layout, 0, 100, numPatientInfoSpacing, true);
        
        AddLabel(layout, "每行间距:");
        AddNumericUpDown(layout, 10, 100, numPatientInfoLineSpacing, true);
        
        AddLabel(layout, "字段配置:");
        var patientFieldPanel = new Panel();
        patientFieldPanel.Height = 420;
        patientFieldPanel.Width = 520;
        
        dgvPatientFields.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        dgvPatientFields.Location = new Point(0, 0);
        dgvPatientFields.AllowUserToAddRows = false;
        dgvPatientFields.AllowUserToDeleteRows = false;
        dgvPatientFields.RowHeadersVisible = false;
        dgvPatientFields.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvPatientFields.MultiSelect = false;
        dgvPatientFields.CellValueChanged += (s, e) => MarkUnsaved();
        dgvPatientFields.Height = 380;
        dgvPatientFields.Width = 520;
        dgvPatientFields.ScrollBars = ScrollBars.Vertical;
        
        dgvPatientFields.Columns.Add("DisplayName", "字段名称");
        dgvPatientFields.Columns.Add("Show", "显示");
        dgvPatientFields.Columns.Add("Row", "行号");
        dgvPatientFields.Columns.Add("Order", "顺序");
        dgvPatientFields.Columns.Add("XOffset", "X偏移");
        
        dgvPatientFields.Columns["DisplayName"].Width = 140;
        dgvPatientFields.Columns["Show"].Width = 60;
        dgvPatientFields.Columns["Row"].Width = 60;
        dgvPatientFields.Columns["Order"].Width = 60;
        dgvPatientFields.Columns["XOffset"].Width = 80;
        dgvPatientFields.Columns["DisplayName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        
        dgvPatientFields.Columns["DisplayName"].ReadOnly = true;
        dgvPatientFields.Columns["Show"].ReadOnly = false;
        
        var btnPanel = new FlowLayoutPanel();
        btnPanel.FlowDirection = FlowDirection.LeftToRight;
        btnPanel.Height = 35;
        btnPanel.Dock = DockStyle.Bottom;
        btnPanel.Location = new Point(0, 385);
        
        btnMoveUp.Text = "上移";
        btnMoveUp.Size = new Size(60, 30);
        btnMoveUp.UseVisualStyleBackColor = true;
        btnMoveUp.Click += btnMoveUp_Click;
        btnPanel.Controls.Add(btnMoveUp);
        
        btnMoveDown.Text = "下移";
        btnMoveDown.Size = new Size(60, 30);
        btnMoveDown.UseVisualStyleBackColor = true;
        btnMoveDown.Click += btnMoveDown_Click;
        btnPanel.Controls.Add(btnMoveDown);
        
        btnMoveRowUp.Text = "上移行";
        btnMoveRowUp.Size = new Size(70, 30);
        btnMoveRowUp.UseVisualStyleBackColor = true;
        btnMoveRowUp.Click += btnMoveRowUp_Click;
        btnPanel.Controls.Add(btnMoveRowUp);
        
        btnMoveRowDown.Text = "下移行";
        btnMoveRowDown.Size = new Size(70, 30);
        btnMoveRowDown.UseVisualStyleBackColor = true;
        btnMoveRowDown.Click += btnMoveRowDown_Click;
        btnPanel.Controls.Add(btnMoveRowDown);
        
        patientFieldPanel.Controls.Add(dgvPatientFields);
        patientFieldPanel.Controls.Add(btnPanel);
        layout.Controls.Add(patientFieldPanel, 1, layout.RowCount - 1);
        layout.RowStyles[layout.RowCount - 1] = new RowStyle(SizeType.Absolute, 430f);
        
        AddSectionHeader(layout, "结果栏设置");
        AddLabel(layout, "双栏布局:");
        chkUseTwoColumnLayout.AutoSize = true;
        chkUseTwoColumnLayout.CheckedChanged += (s, e) => MarkUnsaved();
        layout.Controls.Add(chkUseTwoColumnLayout, 1, layout.RowCount - 1);
        
        AddLabel(layout, "两栏间距:");
        AddNumericUpDown(layout, 0, 100, numColumnSpacing, true);
        
        AddLabel(layout, "上方间距:");
        AddNumericUpDown(layout, 0, 100, numResultTableSpacing, true);
        
        AddLabel(layout, "字号:");
        AddNumericUpDown(layout, 8, 24, numItemFontSize, true);
        
        AddLabel(layout, "显示参考范围:");
        chkShowReferenceRange.AutoSize = true;
        chkShowReferenceRange.CheckedChanged += (s, e) => MarkUnsaved();
        layout.Controls.Add(chkShowReferenceRange, 1, layout.RowCount - 1);
        
        AddLabel(layout, "显示单位:");
        chkShowUnit.AutoSize = true;
        chkShowUnit.CheckedChanged += (s, e) => MarkUnsaved();
        layout.Controls.Add(chkShowUnit, 1, layout.RowCount - 1);
        
        AddLabel(layout, "显示结果标志:");
        chkShowResultFlag.AutoSize = true;
        chkShowResultFlag.CheckedChanged += (s, e) => MarkUnsaved();
        layout.Controls.Add(chkShowResultFlag, 1, layout.RowCount - 1);
        
        AddLabel(layout, "行高:");
        AddNumericUpDown(layout, 15, 50, numRowHeight, true);
        
        AddSectionHeader(layout, "页脚设置");
        AddLabel(layout, "显示页脚:");
        chkShowFooter.AutoSize = true;
        chkShowFooter.CheckedChanged += (s, e) => MarkUnsaved();
        layout.Controls.Add(chkShowFooter, 1, layout.RowCount - 1);
        
        AddLabel(layout, "左侧内容:");
        txtFooterLeft.Width = 300;
        txtFooterLeft.TextChanged += (s, e) => MarkUnsaved();
        layout.Controls.Add(txtFooterLeft, 1, layout.RowCount - 1);
        
        AddLabel(layout, "右侧内容:");
        var rightPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 5, 0, 5) };
        txtFooterRight.Width = 300;
        txtFooterRight.ReadOnly = true;
        txtFooterRight.BackColor = Color.FromArgb(245, 245, 245);
        txtFooterRight.Text = "（由标本备注自动填充，不可预设）";
        txtFooterRight.TextChanged += (s, e) => MarkUnsaved();
        rightPanel.Controls.Add(txtFooterRight);
        layout.Controls.Add(rightPanel, 1, layout.RowCount - 1);
        
        AddLabel(layout, "字号:");
        AddNumericUpDown(layout, 8, 24, numFooterFontSize, true);
        
        AddLabel(layout, "上方间距:");
        AddNumericUpDown(layout, 0, 100, numFooterSpacing, true);
        
        scrollPanel.Controls.Add(layout);
        
        buttonPanel.Dock = DockStyle.Bottom;
        buttonPanel.Height = 110;
        buttonPanel.RowCount = 2;
        buttonPanel.ColumnCount = 1;
        buttonPanel.Padding = new Padding(10);
        buttonPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f));
        buttonPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        
        var buttonFlow = new FlowLayoutPanel();
        buttonFlow.Dock = DockStyle.Fill;
        buttonFlow.FlowDirection = FlowDirection.LeftToRight;
        buttonFlow.Padding = new Padding(0, 10, 0, 0);
        
        btnSave.Text = "保存";
        btnSave.Size = new Size(80, 30);
        btnSave.UseVisualStyleBackColor = true;
        btnSave.Click += btnSave_Click;
        buttonFlow.Controls.Add(btnSave);
        
        btnCancel.Text = "取消";
        btnCancel.Size = new Size(80, 30);
        btnCancel.UseVisualStyleBackColor = true;
        btnCancel.Click += btnCancel_Click;
        buttonFlow.Controls.Add(btnCancel);
        
        btnImport.Text = "导入";
        btnImport.Size = new Size(80, 30);
        btnImport.UseVisualStyleBackColor = true;
        btnImport.Click += btnImport_Click;
        buttonFlow.Controls.Add(btnImport);
        
        btnExport.Text = "导出";
        btnExport.Size = new Size(80, 30);
        btnExport.UseVisualStyleBackColor = true;
        btnExport.Click += btnExport_Click;
        buttonFlow.Controls.Add(btnExport);
        
        btnReset.Text = "重置";
        btnReset.Size = new Size(80, 30);
        btnReset.UseVisualStyleBackColor = true;
        btnReset.Click += btnReset_Click;
        buttonFlow.Controls.Add(btnReset);
        
        buttonPanel.Controls.Add(buttonFlow, 0, 0);
        
        lblStatus.AutoSize = true;
        lblStatus.Dock = DockStyle.Bottom;
        lblStatus.Padding = new Padding(0, 10, 0, 0);
        buttonPanel.Controls.Add(lblStatus, 0, 1);
        
        Controls.Add(scrollPanel);
        Controls.Add(buttonPanel);
        
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(750, 900);
        MinimumSize = new Size(720, 800);
        Text = "打印模板设置";
        StartPosition = FormStartPosition.CenterParent;
        ResumeLayout(false);
    }
    
    private void AddSectionHeader(TableLayoutPanel layout, string text)
    {
        layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
        
        var label = new Label();
        label.Text = text;
        label.Font = new Font(label.Font, FontStyle.Bold);
        label.AutoSize = true;
        label.Margin = new Padding(0, 10, 0, 0);
        
        layout.Controls.Add(label, 0, layout.RowCount - 1);
        layout.SetColumnSpan(label, 2);
    }
    
    private void AddLabel(TableLayoutPanel layout, string text)
    {
        layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35f));
        
        var label = new Label();
        label.Text = text;
        label.AutoSize = true;
        label.Margin = new Padding(0, 5, 0, 5);
        label.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        
        layout.Controls.Add(label, 0, layout.RowCount - 1);
    }
    
    private void AddNumericUpDown(TableLayoutPanel layout, decimal min, decimal max, NumericUpDown numeric, bool registerMarkUnsaved)
    {
        numeric.Width = 100;
        numeric.Minimum = min;
        numeric.Maximum = max;
        numeric.DecimalPlaces = 1;
        numeric.Increment = 1;
        numeric.Margin = new Padding(0, 5, 0, 5);
        if (registerMarkUnsaved)
            numeric.ValueChanged += (s, e) => MarkUnsaved();
        layout.Controls.Add(numeric, 1, layout.RowCount - 1);
    }
}
