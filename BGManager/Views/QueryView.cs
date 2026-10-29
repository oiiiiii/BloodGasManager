using System.Drawing.Printing;
using BGManager.Services;
using BGShared.Models;
using BGShared.Config;

namespace BGManager.Views;

public class QueryView : UserControl
{
    private readonly ApiClient _api;
    private List<Specimen> _allSpecimens = new();
    private List<Specimen> _filteredSpecimens = new();

    private Panel searchPanel;
    private SplitContainer splitContainer;
    private DataGridView dgvSpecimens;
    private DataGridView dgvResults;
    private Panel bottomPanel;

    private RadioButton rdoDateYes;
    private RadioButton rdoDateNo;
    private DateTimePicker dtpStartDate;
    private DateTimePicker dtpEndDate;
    private TextBox txtPatientId;
    private TextBox txtPatientName;
    private TextBox txtSpecimenNo;
    private TextBox txtBedNo;
    private ComboBox cmbAuditStatus;
    private Button btnSearch;
    private Button btnReset;

    private Button btnSelectAll;
    private Button btnInvertSelection;
    private Button btnPreview;
    private Button btnPrint;
    private Label lblSelectionCount;

    private int _printIndex;
    private List<Specimen> _printSpecimens = new();

    public QueryView(ApiClient api)
    {
        _api = api;
        Dock = DockStyle.Fill;
        BuildUI();
        this.Load += (s, e) => UpdateSplitterDistance();
    }

    private void UpdateSplitterDistance()
    {
        if (splitContainer == null || this.Width <= 0) return;
        int totalWidth = splitContainer.Width - splitContainer.SplitterWidth;
        if (totalWidth > 0)
            splitContainer.SplitterDistance = (int)(totalWidth * 0.65);
    }

    private void BuildUI()
    {
        searchPanel = new Panel { Dock = DockStyle.Top, Height = 75, Padding = new Padding(8), BackColor = Color.White };
        BuildSearchPanel();
        Controls.Add(searchPanel);

        bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(10), BackColor = Color.FromArgb(241, 245, 249) };
        BuildBottomPanel();
        Controls.Add(bottomPanel);

        splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 700,
            SplitterWidth = 6,
            FixedPanel = FixedPanel.None
        };
        this.Resize += (s, e) => UpdateSplitterDistance();

        dgvSpecimens = CreateSpecimenGrid();
        dgvSpecimens.CellClick += DgvSpecimens_CellClick;
        dgvSpecimens.CellValueChanged += DgvSpecimens_CellValueChanged;
        dgvSpecimens.SelectionChanged += DgvSpecimens_SelectionChanged;
        dgvSpecimens.CurrentCellDirtyStateChanged += (s, e) =>
        {
            if (dgvSpecimens.IsCurrentCellDirty && dgvSpecimens.CurrentCell.ColumnIndex == 0)
                dgvSpecimens.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };

        dgvSpecimens.Columns.Add(new DataGridViewCheckBoxColumn
        {
            Name = "colSelect",
            HeaderText = "",
            Width = 30,
            Resizable = DataGridViewTriState.False,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            TrueValue = true,
            FalseValue = false
        });
        dgvSpecimens.Columns.Add("colSpecimenNo", "样本号");
        dgvSpecimens.Columns.Add("colPatientId", "病案号");
        dgvSpecimens.Columns.Add("colPatientName", "患者姓名");
        dgvSpecimens.Columns.Add("colGender", "性别");
        dgvSpecimens.Columns.Add("colBedNo", "床号");
        dgvSpecimens.Columns.Add("colDepartment", "科室");
        dgvSpecimens.Columns.Add("colStatus", "状态");
        dgvSpecimens.Columns.Add("colTestTime", "检测时间");

        dgvSpecimens.Columns["colSpecimenNo"].Width = 90;
        dgvSpecimens.Columns["colPatientId"].Width = 90;
        dgvSpecimens.Columns["colPatientName"].Width = 80;
        dgvSpecimens.Columns["colGender"].Width = 50;
        dgvSpecimens.Columns["colBedNo"].Width = 60;
        dgvSpecimens.Columns["colDepartment"].Width = 100;
        dgvSpecimens.Columns["colStatus"].Width = 70;
        dgvSpecimens.Columns["colTestTime"].Width = 130;

        dgvSpecimens.Columns["colSelect"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        splitContainer.Panel1.Controls.Add(dgvSpecimens);

        dgvResults = CreateResultGrid();
        dgvResults.Columns.Add("colName", "项目名");
        dgvResults.Columns.Add("colValue", "结果");
        dgvResults.Columns.Add("colUnit", "单位");
        dgvResults.Columns.Add("colRange", "参考范围");
        dgvResults.Columns["colValue"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        dgvResults.Columns["colName"].Width = 130;
        dgvResults.Columns["colValue"].Width = 80;
        dgvResults.Columns["colUnit"].Width = 60;
        dgvResults.Columns["colRange"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        splitContainer.Panel2.Controls.Add(dgvResults);

        Controls.Add(splitContainer);
        splitContainer.BringToFront();
    }

    private static DataGridView CreateSpecimenGrid()
    {
        var dgv = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AllowUserToResizeColumns = false,
            AllowUserToOrderColumns = false,
            ReadOnly = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            GridColor = Color.FromArgb(226, 232, 240),
            Font = new Font("微软雅黑", 9f),
            ColumnHeadersHeight = 32,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            EnableHeadersVisualStyles = false
        };
        dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(71, 85, 105);
        dgv.ColumnHeadersDefaultCellStyle.Font = new Font("微软雅黑", 9f, FontStyle.Bold);
        dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(241, 245, 249);
        dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.FromArgb(71, 85, 105);
        dgv.RowTemplate.Height = 26;
        dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(191, 219, 254);
        dgv.DefaultCellStyle.SelectionForeColor = Color.Black;
        return dgv;
    }

    private static DataGridView CreateResultGrid()
    {
        var dgv = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AllowUserToResizeColumns = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            GridColor = Color.FromArgb(226, 232, 240),
            Font = new Font("微软雅黑", 9f),
            ColumnHeadersHeight = 32,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            EnableHeadersVisualStyles = false
        };
        dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(71, 85, 105);
        dgv.ColumnHeadersDefaultCellStyle.Font = new Font("微软雅黑", 9f, FontStyle.Bold);
        dgv.RowTemplate.Height = 26;
        return dgv;
    }

    private void BuildSearchPanel()
    {
        int row1Y = 8;
        int row2Y = 42;
        var labelFont = new Font("微软雅黑", 10f, FontStyle.Bold);
        var controlFont = new Font("微软雅黑", 10f);

        var lblDateRange = new Label { Text = "限定日期范围:", AutoSize = true, Location = new Point(10, row1Y + 4), Font = labelFont };
        searchPanel.Controls.Add(lblDateRange);

        rdoDateYes = new RadioButton { Text = "是", Location = new Point(110, row1Y + 3), AutoSize = true, Font = controlFont };
        rdoDateYes.CheckedChanged += RdoDateRange_CheckedChanged;
        searchPanel.Controls.Add(rdoDateYes);

        rdoDateNo = new RadioButton { Text = "否", Location = new Point(150, row1Y + 3), AutoSize = true, Checked = true, Font = controlFont };
        rdoDateNo.CheckedChanged += RdoDateRange_CheckedChanged;
        searchPanel.Controls.Add(rdoDateNo);

        var lblStart = new Label { Text = "起始日期:", AutoSize = true, Location = new Point(220, row1Y + 4), Enabled = false, Font = labelFont };
        lblStart.Name = "lblStart";
        searchPanel.Controls.Add(lblStart);
        dtpStartDate = new DateTimePicker { Location = new Point(285, row1Y), Width = 120, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd", Enabled = false, Font = controlFont };
        dtpStartDate.Value = DateTime.Today.AddMonths(-1);
        searchPanel.Controls.Add(dtpStartDate);

        var lblEnd = new Label { Text = "结束日期:", AutoSize = true, Location = new Point(420, row1Y + 4), Enabled = false, Font = labelFont };
        lblEnd.Name = "lblEnd";
        searchPanel.Controls.Add(lblEnd);
        dtpEndDate = new DateTimePicker { Location = new Point(485, row1Y), Width = 120, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd", Enabled = false, Font = controlFont };
        dtpEndDate.Value = DateTime.Today;
        searchPanel.Controls.Add(dtpEndDate);

        var lblStatus = new Label { Text = "状态:", AutoSize = true, Location = new Point(625, row1Y + 4), Font = labelFont };
        searchPanel.Controls.Add(lblStatus);
        cmbAuditStatus = new ComboBox { Location = new Point(665, row1Y), Width = 90, DropDownStyle = ComboBoxStyle.DropDownList, Font = controlFont };
        cmbAuditStatus.Items.AddRange(new object[] { "全部", "待审核", "已审核" });
        cmbAuditStatus.SelectedIndex = 0;
        searchPanel.Controls.Add(cmbAuditStatus);

        var lblSpecimen = new Label { Text = "样本号:", AutoSize = true, Location = new Point(10, row2Y + 4), Font = labelFont };
        searchPanel.Controls.Add(lblSpecimen);
        txtSpecimenNo = new TextBox { Location = new Point(70, row2Y), Width = 100, Font = controlFont };
        searchPanel.Controls.Add(txtSpecimenNo);

        var lblPatient = new Label { Text = "病案号:", AutoSize = true, Location = new Point(185, row2Y + 4), Font = labelFont };
        searchPanel.Controls.Add(lblPatient);
        txtPatientId = new TextBox { Location = new Point(245, row2Y), Width = 100, Font = controlFont };
        searchPanel.Controls.Add(txtPatientId);

        var lblName = new Label { Text = "患者姓名:", AutoSize = true, Location = new Point(360, row2Y + 4), Font = labelFont };
        searchPanel.Controls.Add(lblName);
        txtPatientName = new TextBox { Location = new Point(425, row2Y), Width = 100, Font = controlFont };
        searchPanel.Controls.Add(txtPatientName);

        var lblBed = new Label { Text = "床号:", AutoSize = true, Location = new Point(540, row2Y + 4), Font = labelFont };
        searchPanel.Controls.Add(lblBed);
        txtBedNo = new TextBox { Location = new Point(580, row2Y), Width = 80, Font = controlFont };
        searchPanel.Controls.Add(txtBedNo);

        btnSearch = new Button
        {
            Text = "搜索",
            Width = 70,
            Height = 28,
            Location = new Point(700, row2Y),
            BackColor = Color.FromArgb(59, 130, 246),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = controlFont
        };
        btnSearch.FlatAppearance.BorderSize = 0;
        btnSearch.Click += (s, e) => _ = DoSearchAsync();
        searchPanel.Controls.Add(btnSearch);

        btnReset = new Button { Text = "重置", Width = 70, Height = 28, Location = new Point(780, row2Y), Font = controlFont };
        btnReset.Click += (s, e) => ResetSearch();
        searchPanel.Controls.Add(btnReset);
    }

    private void RdoDateRange_CheckedChanged(object? sender, EventArgs e)
    {
        bool enabled = rdoDateYes.Checked;
        dtpStartDate.Enabled = enabled;
        dtpEndDate.Enabled = enabled;
        if (searchPanel.Controls["lblStart"] is Label lblS) lblS.Enabled = enabled;
        if (searchPanel.Controls["lblEnd"] is Label lblE) lblE.Enabled = enabled;
    }

    private void BuildBottomPanel()
    {
        btnSelectAll = new Button { Text = "全选", Width = 70, Height = 30, Left = 10, Top = 10 };
        btnSelectAll.Click += (s, e) => { SetAllCheckboxes(true); UpdateSelectionCount(); };
        bottomPanel.Controls.Add(btnSelectAll);

        btnInvertSelection = new Button { Text = "反选", Width = 70, Height = 30, Left = 90, Top = 10 };
        btnInvertSelection.Click += (s, e) => { ToggleAllCheckboxes(); UpdateSelectionCount(); };
        bottomPanel.Controls.Add(btnInvertSelection);

        btnPreview = new Button { Text = "打印预览", Width = 85, Height = 30, Left = 200, Top = 10 };
        btnPreview.Click += BtnPreview_Click;
        bottomPanel.Controls.Add(btnPreview);

        btnPrint = new Button { Text = "打印", Width = 70, Height = 30, Left = 295, Top = 10 };
        btnPrint.Click += BtnPrint_Click;
        bottomPanel.Controls.Add(btnPrint);

        lblSelectionCount = new Label { Text = "已选 0 条", AutoSize = true, Left = 400, Top = 17, ForeColor = Color.FromArgb(71, 85, 105) };
        bottomPanel.Controls.Add(lblSelectionCount);
    }

    private void SetAllCheckboxes(bool value)
    {
        dgvSpecimens.ClearSelection();
        for (int i = 0; i < dgvSpecimens.Rows.Count; i++)
            dgvSpecimens.Rows[i].Cells[0].Value = value;
    }

    private void ToggleAllCheckboxes()
    {
        dgvSpecimens.ClearSelection();
        for (int i = 0; i < dgvSpecimens.Rows.Count; i++)
            dgvSpecimens.Rows[i].Cells[0].Value = !(bool?)dgvSpecimens.Rows[i].Cells[0].Value ?? true;
    }

    private void ResetSearch()
    {
        rdoDateNo.Checked = true;
        dtpStartDate.Value = DateTime.Today.AddMonths(-1);
        dtpEndDate.Value = DateTime.Today;
        cmbAuditStatus.SelectedIndex = 0;
        txtSpecimenNo.Text = "";
        txtPatientId.Text = "";
        txtPatientName.Text = "";
        txtBedNo.Text = "";
    }

    private async Task DoSearchAsync()
    {
        try
        {
            SpecimenStatus? statusFilter = null;
            if (cmbAuditStatus.SelectedIndex == 1) statusFilter = SpecimenStatus.Pending;
            else if (cmbAuditStatus.SelectedIndex == 2) statusFilter = SpecimenStatus.Audited;

            string? specimenNo = string.IsNullOrWhiteSpace(txtSpecimenNo.Text) ? null : txtSpecimenNo.Text.Trim();
            string? patientId = string.IsNullOrWhiteSpace(txtPatientId.Text) ? null : txtPatientId.Text.Trim();
            string? patientName = string.IsNullOrWhiteSpace(txtPatientName.Text) ? null : txtPatientName.Text.Trim();
            string? bedNo = string.IsNullOrWhiteSpace(txtBedNo.Text) ? null : txtBedNo.Text.Trim();

            DateTime? fromDate = null;
            DateTime? toDate = null;
            if (rdoDateYes.Checked)
            {
                fromDate = dtpStartDate.Value.Date;
                toDate = dtpEndDate.Value.Date.AddDays(1).AddSeconds(-1);
            }

            var result = await _api.QuerySpecimensAsync(
                page: 1,
                pageSize: 5000,
                specimenNo: specimenNo,
                patientId: patientId,
                patientName: patientName,
                bedNo: bedNo,
                department: null,
                fromDate: fromDate,
                toDate: toDate,
                status: statusFilter,
                sortDesc: false);

            _allSpecimens = result.Items;
            _filteredSpecimens = result.Items
                .OrderBy(s => s.SpecimenNo, StringComparer.Ordinal)
                .ToList();

            BindSpecimenGrid();
            UpdateSelectionCount();
            dgvResults.Rows.Clear();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"查询失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BindSpecimenGrid()
    {
        dgvSpecimens.Rows.Clear();

        foreach (var s in _filteredSpecimens)
        {
            bool isAudited = s.Status == SpecimenStatus.Audited;
            string patientId = isAudited ? s.SnapshotPatientId : s.PatientId;
            string patientName = isAudited ? s.SnapshotPatientName : s.PatientName;
            string gender = isAudited ? s.SnapshotGender : s.Gender;
            string bedNo = isAudited ? s.SnapshotBedNo : s.BedNo;
            string department = isAudited ? s.SnapshotDepartment : s.Department;

            int idx = dgvSpecimens.Rows.Add(
                false,
                s.SpecimenNo,
                patientId,
                patientName,
                gender,
                bedNo,
                department,
                GetStatusText(s.Status),
                s.TestTime?.ToString("yyyy-MM-dd HH:mm") ?? "-"
            );

            var row = dgvSpecimens.Rows[idx];
            row.Tag = s;

            if (s.Status == SpecimenStatus.Pending)
                row.DefaultCellStyle.BackColor = Color.FromArgb(255, 251, 235);
            else if (s.Status == SpecimenStatus.Audited)
                row.DefaultCellStyle.ForeColor = Color.FromArgb(22, 163, 74);
        }
    }

    private void DgvSpecimens_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= dgvSpecimens.Rows.Count) return;

        if (e.ColumnIndex == 0)
        {
            UpdateSelectionCount();
        }
    }

    private void DgvSpecimens_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.ColumnIndex == 0 && e.RowIndex >= 0)
        {
            UpdateSelectionCount();
        }
    }

    private void DgvSpecimens_SelectionChanged(object? sender, EventArgs e)
    {
        if (ModifierKeys.HasFlag(Keys.Control) || ModifierKeys.HasFlag(Keys.Shift))
        {
            SyncSelectionToCheckboxes();
            UpdateSelectionCount();
        }

        if (dgvSpecimens.SelectedRows.Count != 1)
        {
            dgvResults.Rows.Clear();
            return;
        }

        var row = dgvSpecimens.SelectedRows[0];
        if (row.Tag is Specimen s)
            ShowSpecimenResults(s);
    }

    private void SyncSelectionToCheckboxes()
    {
        var selectedIndices = new HashSet<int>();
        foreach (DataGridViewRow row in dgvSpecimens.SelectedRows)
            selectedIndices.Add(row.Index);

        for (int i = 0; i < dgvSpecimens.Rows.Count; i++)
        {
            dgvSpecimens.Rows[i].Cells[0].Value = selectedIndices.Contains(i);
        }
    }

    private void ShowSpecimenResults(Specimen s)
    {
        dgvResults.Rows.Clear();

        var displayConfig = DisplayConfigManager.GetAllItems();
        var resultDict = new Dictionary<string, TestResult>();
        foreach (var r in s.Results)
            if (!string.IsNullOrEmpty(r.TestName))
                resultDict[r.TestName] = r;

        foreach (var configItem in displayConfig)
        {
            if (!resultDict.TryGetValue(configItem.TestName, out var r))
                continue;

            string displayValue = r.Value.HasValue ? r.Value.Value.ToString("G6") : (r.RawValue ?? "-");
            string unit = configItem.Unit ?? "";
            string range = configItem.Reference ?? "-";

            string arrow = "";
            Color valueColor = Color.Black;

            if (r.Flag == ResultFlag.High || r.Flag == ResultFlag.CriticalHigh)
            {
                arrow = "↑";
                valueColor = Color.FromArgb(220, 38, 38);
            }
            else if (r.Flag == ResultFlag.Low || r.Flag == ResultFlag.CriticalLow)
            {
                arrow = "↓";
                valueColor = Color.FromArgb(37, 99, 235);
            }
            else if (r.Value.HasValue && configItem.Low.HasValue && configItem.High.HasValue)
            {
                if (r.Value.Value > configItem.High.Value)
                {
                    arrow = "↑";
                    valueColor = Color.FromArgb(220, 38, 38);
                }
                else if (r.Value.Value < configItem.Low.Value)
                {
                    arrow = "↓";
                    valueColor = Color.FromArgb(37, 99, 235);
                }
            }

            int idx = dgvResults.Rows.Add(
                configItem.ChineseName,
                displayValue + arrow,
                unit,
                range
            );

            var row = dgvResults.Rows[idx];
            row.Cells[0].Style.ForeColor = valueColor;
            row.Cells[1].Style.ForeColor = valueColor;
            row.Cells[2].Style.ForeColor = Color.Black;
            row.Cells[3].Style.ForeColor = Color.Black;
        }
    }

    private void UpdateSelectionCount()
    {
        int count = 0;
        foreach (DataGridViewRow row in dgvSpecimens.Rows)
        {
            if ((bool?)row.Cells[0].Value == true)
                count++;
        }
        lblSelectionCount.Text = $"已选 {count} 条";
    }

    private List<Specimen> GetCheckedSpecimens()
    {
        var list = new List<Specimen>();
        foreach (DataGridViewRow row in dgvSpecimens.Rows)
        {
            if ((bool?)row.Cells[0].Value == true && row.Tag is Specimen s)
                list.Add(s);
        }
        return list;
    }

    private static string GetStatusText(SpecimenStatus status)
    {
        return status switch
        {
            SpecimenStatus.Pending => "待审核",
            SpecimenStatus.Audited => "已审核",
            SpecimenStatus.Archived => "已归档",
            SpecimenStatus.Deleted => "已删除",
            _ => "未知"
        };
    }

    // ==================== 打印功能 ====================

    private void BtnPreview_Click(object? sender, EventArgs e)
    {
        var specimens = GetCheckedSpecimens();
        if (specimens.Count == 0)
        {
            MessageBox.Show("请先选择要打印的标本", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _printSpecimens = specimens.OrderBy(s => s.SpecimenNo, StringComparer.Ordinal).ToList();
        _printIndex = 0;

        var doc = new PrintDocument();
        doc.PrintPage += PrintDocument_PrintPage;
        SetupA4Portrait(doc.DefaultPageSettings);

        var preview = new PrintPreviewDialog
        {
            Document = doc,
            Width = 900,
            Height = 700,
            WindowState = FormWindowState.Normal
        };
        preview.ShowDialog();
    }

    private void BtnPrint_Click(object? sender, EventArgs e)
    {
        var specimens = GetCheckedSpecimens();
        if (specimens.Count == 0)
        {
            MessageBox.Show("请先选择要打印的标本", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _printSpecimens = specimens.OrderBy(s => s.SpecimenNo, StringComparer.Ordinal).ToList();
        _printIndex = 0;

        var doc = new PrintDocument();
        doc.PrintPage += PrintDocument_PrintPage;
        SetupA4Portrait(doc.DefaultPageSettings);

        var dialog = new PrintDialog
        {
            Document = doc,
            UseEXDialog = true
        };

        if (dialog.ShowDialog() == DialogResult.OK)
            doc.Print();
    }

    private static void SetupA4Portrait(PageSettings pageSettings)
    {
        var template = PrintTemplateManager.CurrentTemplate;
        
        PaperSize paperSize = template.PaperSize switch
        {
            "A5" => new PaperSize("A5", 583, 827),
            "Letter" => new PaperSize("Letter", 850, 1100),
            _ => new PaperSize("A4", 827, 1169)
        };
        
        pageSettings.PaperSize = paperSize;
        pageSettings.Landscape = template.Landscape;
        pageSettings.Margins = new Margins(
            (int)template.LeftMargin,
            (int)template.RightMargin,
            (int)template.TopMargin,
            (int)template.BottomMargin
        );
    }

    private void PrintDocument_PrintPage(object? sender, PrintPageEventArgs e)
    {
        if (_printSpecimens == null || _printIndex >= _printSpecimens.Count)
            return;

        var template = PrintTemplateManager.CurrentTemplate;
        int reportsPerPage = template.ReportsPerPage;
        if (reportsPerPage < 1) reportsPerPage = 1;
        if (reportsPerPage > 2) reportsPerPage = 2;

        float availableHeight = e.MarginBounds.Height;
        float reportHeight = availableHeight / reportsPerPage;

        for (int i = 0; i < reportsPerPage && _printIndex < _printSpecimens.Count; i++)
        {
            var specimen = _printSpecimens[_printIndex];
            var reportBounds = new RectangleF(
                e.MarginBounds.Left,
                e.MarginBounds.Top + i * reportHeight,
                e.MarginBounds.Width,
                reportHeight
            );
            PrintSingleReport(e.Graphics, reportBounds, specimen);
            _printIndex++;
        }

        e.HasMorePages = _printIndex < _printSpecimens.Count;
    }

    private static void PrintSingleReport(Graphics g, RectangleF bounds, Specimen specimen)
    {
        var template = PrintTemplateManager.CurrentTemplate;
        
        float left = bounds.Left;
        float top = bounds.Top;
        float width = bounds.Width;
        float y = top;

        // 使用宋体字体
        var titleFont = SimSunFontHelper.CreateFont(template.HeaderFontSize, template.HeaderBold);
        var boldFont = SimSunFontHelper.CreateFont(template.PatientInfoLayout.FontSize, true);
        var normalFont = SimSunFontHelper.CreateFont(template.PatientInfoLayout.FontSize);
        var smallFont = SimSunFontHelper.CreateFont(template.ItemFontSize);

        string title = template.HeaderTitle;
        var titleSize = g.MeasureString(title, titleFont);
        g.DrawString(title, titleFont, Brushes.Black, left + (width - titleSize.Width) / 2, y);
        y += titleSize.Height + template.HeaderSpacing;

        if (template.ShowPatientInfo)
        {
            bool isAudited = specimen.Status == SpecimenStatus.Audited;
            string patientName = isAudited ? specimen.SnapshotPatientName : specimen.PatientName;
            string patientId = isAudited ? specimen.SnapshotPatientId : specimen.PatientId;
            string gender = isAudited ? specimen.SnapshotGender : specimen.Gender;
            string bedNo = isAudited ? specimen.SnapshotBedNo : specimen.BedNo;
            string department = isAudited ? specimen.SnapshotDepartment : specimen.Department;

            float lineHeight = template.PatientInfoLayout.LineSpacing;

            Dictionary<string, string> fieldValues = new()
            {
                { "PatientName", patientName ?? "-" },
                { "Gender", string.IsNullOrEmpty(gender) ? "-" : gender },
                { "BedNo", bedNo ?? "-" },
                { "PatientId", patientId ?? "-" },
                { "Department", department ?? "-" },
                { "SpecimenNo", specimen.SpecimenNo ?? "-" },
                { "SampleType", string.IsNullOrEmpty(specimen.SampleType) ? "未指定" : specimen.SampleType },
                { "TestTime", specimen.TestTime?.ToString("yyyy-MM-dd HH:mm") ?? "-" }
            };

            var fieldsByRow = template.PatientInfoLayout.Fields
                .Where(f => f.Show)
                .GroupBy(f => f.Row)
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var rowGroup in fieldsByRow)
            {
                float x = left;
                foreach (var field in rowGroup.OrderBy(f => f.Order))
                {
                    float offsetX = x + field.XOffset;
                    g.DrawString(field.Label, boldFont, Brushes.Black, offsetX, y);
                    var labelSize = g.MeasureString(field.Label, boldFont);
                    g.DrawString(fieldValues.GetValueOrDefault(field.FieldName, "-"), normalFont, Brushes.Black, offsetX + labelSize.Width, y);
                    x += width / rowGroup.Count();
                }
                y += lineHeight;
            }

            y += template.PatientInfoSpacing;
        }

        g.DrawLine(Pens.Black, left, y, left + width, y);
        y += 8;

        var displayConfig = DisplayConfigManager.GetAllItems();
        var resultDict = new Dictionary<string, TestResult>();
        foreach (var r in specimen.Results)
            if (!string.IsNullOrEmpty(r.TestName))
                resultDict[r.TestName] = r;

        var resultItems = displayConfig
            .Where(c => resultDict.ContainsKey(c.TestName))
            .Select(c => new { Config = c, Result = resultDict[c.TestName] })
            .ToList();

        float rowH = template.RowHeight;

        if (template.UseTwoColumnLayout)
        {
            int total = resultItems.Count;
            int half = (int)Math.Ceiling(total / 2.0);
            float colWidth = (width - template.ColumnSpacing) / 2f;

            void DrawColumn(float cx, int startIdx, int count)
            {
                float cy = y;
                float col1W = colWidth * 0.30f;
                float col2W = colWidth * 0.22f;
                float col3W = colWidth * 0.28f;
                float col4W = colWidth * 0.20f;

                void DrawRow(float ry, string name, string value, string range, string unit, Brush valueBrush)
                {
                    g.DrawString(name, smallFont, Brushes.Black, cx, ry);
                    g.DrawString(value, smallFont, valueBrush, cx + col1W, ry);
                    if (template.ShowReferenceRange)
                        g.DrawString(range, smallFont, Brushes.Black, cx + col1W + col2W, ry);
                    if (template.ShowUnit)
                        g.DrawString(unit, smallFont, Brushes.Black, cx + col1W + col2W + col3W, ry);
                }

                DrawRow(cy, "项目", "结果", template.ShowReferenceRange ? "参考范围" : "", template.ShowUnit ? "单位" : "", Brushes.Black);
                cy += rowH;
                g.DrawLine(Pens.Black, cx, cy, cx + colWidth, cy);
                cy += 2;

                for (int i = startIdx; i < startIdx + count && i < total; i++)
                {
                    var item = resultItems[i];
                    string val = item.Result.Value.HasValue ? item.Result.Value.Value.ToString("G6") : (item.Result.RawValue ?? "-");
                    string unit = item.Config.Unit ?? "";
                    string range = item.Config.Reference ?? "-";

                    string arrow = "";
                    Brush brush = Brushes.Black;
                    if (item.Result.Flag == ResultFlag.High || item.Result.Flag == ResultFlag.CriticalHigh)
                    {
                        brush = Brushes.Red;
                        arrow = "↑";
                    }
                    else if (item.Result.Flag == ResultFlag.Low || item.Result.Flag == ResultFlag.CriticalLow)
                    {
                        brush = Brushes.Blue;
                        arrow = "↓";
                    }
                    else if (item.Result.Value.HasValue && item.Config.Low.HasValue && item.Config.High.HasValue)
                    {
                        if (item.Result.Value.Value > item.Config.High.Value)
                        {
                            brush = Brushes.Red;
                            arrow = "↑";
                        }
                        else if (item.Result.Value.Value < item.Config.Low.Value)
                        {
                            brush = Brushes.Blue;
                            arrow = "↓";
                        }
                    }

                    DrawRow(cy, item.Config.TestName, val + arrow, range, unit, brush);
                    cy += rowH;
                }
            }

            DrawColumn(left, 0, half);
            DrawColumn(left + colWidth + template.ColumnSpacing, half, total - half);
            y += Math.Max(half + 2, total - half + 2) * rowH;
        }
        else
        {
            float col1W = width * 0.30f;
            float col2W = width * 0.22f;
            float col3W = width * 0.28f;
            float col4W = width * 0.20f;

            void DrawRow(float ry, string name, string value, string range, string unit, Brush valueBrush)
            {
                g.DrawString(name, smallFont, Brushes.Black, left, ry);
                g.DrawString(value, smallFont, valueBrush, left + col1W, ry);
                if (template.ShowReferenceRange)
                    g.DrawString(range, smallFont, Brushes.Black, left + col1W + col2W, ry);
                if (template.ShowUnit)
                    g.DrawString(unit, smallFont, Brushes.Black, left + col1W + col2W + col3W, ry);
            }

            DrawRow(y, "项目", "结果", template.ShowReferenceRange ? "参考范围" : "", template.ShowUnit ? "单位" : "", Brushes.Black);
            y += rowH;
            g.DrawLine(Pens.Black, left, y, left + width, y);
            y += 2;

            foreach (var item in resultItems)
            {
                string val = item.Result.Value.HasValue ? item.Result.Value.Value.ToString("G6") : (item.Result.RawValue ?? "-");
                string unit = item.Config.Unit ?? "";
                string range = item.Config.Reference ?? "-";

                string arrow = "";
                Brush brush = Brushes.Black;
                if (item.Result.Flag == ResultFlag.High || item.Result.Flag == ResultFlag.CriticalHigh)
                {
                    brush = Brushes.Red;
                    arrow = "↑";
                }
                else if (item.Result.Flag == ResultFlag.Low || item.Result.Flag == ResultFlag.CriticalLow)
                {
                    brush = Brushes.Blue;
                    arrow = "↓";
                }
                else if (item.Result.Value.HasValue && item.Config.Low.HasValue && item.Config.High.HasValue)
                {
                    if (item.Result.Value.Value > item.Config.High.Value)
                    {
                        brush = Brushes.Red;
                        arrow = "↑";
                    }
                    else if (item.Result.Value.Value < item.Config.Low.Value)
                    {
                        brush = Brushes.Blue;
                        arrow = "↓";
                    }
                }

                DrawRow(y, item.Config.ChineseName, val + arrow, range, unit, brush);
                y += rowH;
            }
        }

        y += 10;
        g.DrawLine(Pens.Black, left, y, left + width, y);
        y += 10;

        if (template.ShowFooter)
        {
            var footerFont = SimSunFontHelper.CreateFont(template.FooterFontSize);
            g.DrawString(template.FooterLeft, footerFont, Brushes.Black, left, y);
            // 右侧内容由标本备注自动填充（已审核取快照，未审核取当前值），不再使用模板预设
            string rightContent = specimen.Status == SpecimenStatus.Audited
                ? (specimen.SnapshotRemark ?? "")
                : (specimen.Remark ?? "");
            if (!string.IsNullOrEmpty(rightContent))
            {
                var rightSize = g.MeasureString(rightContent, footerFont);
                g.DrawString(rightContent, footerFont, Brushes.Black, left + width - rightSize.Width, y);
            }
        }
    }

    public void RefreshData()
    {
        _ = DoSearchAsync();
    }
}
