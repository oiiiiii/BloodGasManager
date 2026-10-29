using System.Drawing.Printing;
using BGManager.Services;
using BGShared.Config;
using BGShared.Models;

namespace BGManager.Views;

public class MainView : UserControl
{
    private readonly ApiClient _api;
    private System.Windows.Forms.Timer? _refreshTimer;
    private List<Specimen> _specimens = new();
    private Specimen? _currentSpecimen;
    private List<Specimen> _historicalSpecimens = new();
    private DateTime _selectedDate = DateTime.Today;
    private readonly Dictionary<long, Patient> _matchedPatients = new();
    private List<Patient> _patientCache = new();

    private Form? _toastForm;
    private System.Windows.Forms.Timer? _toastTimer;

    private TableLayoutPanel mainLayout;
    private Panel panelCol1;
    private Panel panelCol2;
    private Panel panelCol3;

    private Button btnPrevDay;
    private Button btnNextDay;
    private Button btnToday;
    private DateTimePicker datePicker;
    private Label lblStatus;
    private ListBox lstSpecimens;
    private Button btnRefresh;
    private Button btnDelete;
    private Button btnAudit;

    private TextBox txtPatientName;
    private TextBox txtBedNo;
    private TextBox txtPatientId;
    private TextBox txtOriginalPatientId;
    private ComboBox cboGender;
    private TextBox txtDepartment;
    private TextBox txtReportTime;
    private TextBox txtSpecimenNo;
    private ComboBox cboSampleType;
    private TextBox txtRemark;
    private TextBox txtDiagnosis;

    private DataGridView dgvResults;
    private Button btnSelectAll;
    private Button btnInvertSelect;
    private Button btnAbnormalOnly;
    private Button btnCopy;
    private Button btnPreview;

    public event EventHandler? StatsUpdated;

    public MainView(ApiClient api)
    {
        _api = api;
        Dock = DockStyle.Fill;
        BuildUI();
        SetupRefreshTimer();
        _ = LoadDateDataAsync();
    }

    private void BuildUI()
    {
        mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(5),
            BackColor = Color.FromArgb(249, 250, 251)
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340f));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300f));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        Controls.Add(mainLayout);

        panelCol1 = new Panel { Dock = DockStyle.Fill, Padding = new Padding(3) };
        panelCol2 = new Panel { Dock = DockStyle.Fill, Padding = new Padding(3) };
        panelCol3 = new Panel { Dock = DockStyle.Fill, Padding = new Padding(3) };

        BuildColumn1();
        BuildColumn2();
        BuildColumn3();

        mainLayout.Controls.Add(panelCol1, 0, 0);
        mainLayout.Controls.Add(panelCol2, 1, 0);
        mainLayout.Controls.Add(panelCol3, 2, 0);
    }

    private void BuildColumn1()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55f));
        panelCol1.Controls.Add(layout);

        var datePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            Padding = new Padding(2)
        };
        datePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42f));
        datePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        datePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42f));
        datePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55f));
        datePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        btnPrevDay = new Button
        {
            Text = "◀",
            Dock = DockStyle.Fill,
            Font = new Font("微软雅黑", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 64, 175),
            Margin = new Padding(0, 3, 2, 3)
        };
        btnPrevDay.Click += (s, e) => { datePicker.Value = datePicker.Value.AddDays(-1); };
        datePanel.Controls.Add(btnPrevDay, 0, 0);

        datePicker = new DateTimePicker
        {
            Dock = DockStyle.Fill,
            Format = DateTimePickerFormat.Long,
            Value = _selectedDate,
            Font = new Font("微软雅黑", 14f),
            CalendarFont = new Font("微软雅黑", 14f),
            Margin = new Padding(2, 3, 2, 3)
        };
        datePicker.ValueChanged += (s, e) => { _selectedDate = datePicker.Value; _ = LoadDateDataAsync(); };
        datePanel.Controls.Add(datePicker, 1, 0);

        btnNextDay = new Button
        {
            Text = "▶",
            Dock = DockStyle.Fill,
            Font = new Font("微软雅黑", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 64, 175),
            Margin = new Padding(2, 3, 2, 3)
        };
        btnNextDay.Click += (s, e) => { datePicker.Value = datePicker.Value.AddDays(1); };
        datePanel.Controls.Add(btnNextDay, 2, 0);

        btnToday = new Button
        {
            Text = "今",
            Dock = DockStyle.Fill,
            Font = new Font("微软雅黑", 14f, FontStyle.Bold),
            ForeColor = Color.Red,
            BackColor = Color.White,
            Margin = new Padding(2, 3, 0, 3)
        };
        btnToday.Click += (s, e) => { datePicker.Value = DateTime.Today; };
        datePanel.Controls.Add(btnToday, 3, 0);

        layout.Controls.Add(datePanel, 0, 0);

        var statusPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(2) };
        lblStatus = new Label
        {
            Text = "准备就绪",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.Gray,
            Font = new Font("微软雅黑", 9f)
        };
        statusPanel.Controls.Add(lblStatus);
        layout.Controls.Add(statusPanel, 0, 1);

        var listPanel = new Panel { Dock = DockStyle.Fill };
        var group = new GroupBox { Text = "检验单列表", Dock = DockStyle.Fill, Font = new Font("微软雅黑", 10f) };
        lstSpecimens = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("微软雅黑", 11f),
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 32,
            BorderStyle = BorderStyle.None
        };
        lstSpecimens.SelectedIndexChanged += (s, e) =>
        {
            if (_suppressSelectionChanged) return;
            _ = ShowSelectedDetail();
        };
        lstSpecimens.DrawItem += LstSpecimens_DrawItem;
        group.Controls.Add(lstSpecimens);
        listPanel.Controls.Add(group);
        layout.Controls.Add(listPanel, 0, 2);

        var buttonPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(3) };
        btnRefresh = new Button { Text = "刷新", Dock = DockStyle.Left, Width = 90, Height = 45, Font = new Font("微软雅黑", 11f) };
        btnRefresh.Click += async (s, e) => await LoadDateDataAsync();

        btnDelete = new Button
        {
            Text = "删除",
            Dock = DockStyle.Left,
            Width = 90,
            Height = 45,
            Font = new Font("微软雅黑", 11f, FontStyle.Bold),
            ForeColor = Color.Red,
            Enabled = false
        };
        btnDelete.Click += async (s, e) => await DeleteCurrentAsync();

        btnAudit = new Button
        {
            Text = "审核",
            Dock = DockStyle.Right,
            Width = 90,
            Height = 45,
            Font = new Font("微软雅黑", 11f, FontStyle.Bold),
            Enabled = false
        };
        btnAudit.Click += async (s, e) => await AuditCurrentAsync();

        buttonPanel.Controls.Add(btnRefresh);
        buttonPanel.Controls.Add(btnDelete);
        buttonPanel.Controls.Add(btnAudit);
        layout.Controls.Add(buttonPanel, 0, 3);
    }

    private void BuildColumn2()
    {
        var group = new GroupBox { Text = "患者信息", Dock = DockStyle.Fill, Font = new Font("微软雅黑", 10f), Padding = new Padding(8) };
        panelCol2.Controls.Add(group);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 11,
            ColumnCount = 2,
            Padding = new Padding(5)
        };
        for (int i = 0; i < 9; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));   // 备注
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 75f));   // 考虑诊断
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80f));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        group.Controls.Add(layout);

        int row = 0;
        AddLabeledTextBox(layout, "患者姓名:", out txtPatientName, row++);
        AddLabeledTextBox(layout, "床号:", out txtBedNo, row++);
        AddLabeledTextBox(layout, "病案号:", out txtPatientId, row++);
        AddLabeledTextBox(layout, "原始编号:", out txtOriginalPatientId, row++);
        AddLabeledComboBox(layout, "性别:", out cboGender, row++, new[] { "", "男", "女", "未知" });
        AddLabeledTextBox(layout, "科室:", out txtDepartment, row++);
        AddLabeledTextBox(layout, "报告时间:", out txtReportTime, row++, true);
        AddLabeledTextBox(layout, "样本号:", out txtSpecimenNo, row++, true);
        AddLabeledComboBox(layout, "样本类型:", out cboSampleType, row++, new[] { "未指定", "动脉", "静脉", "混合静脉" });

        var remarkLabel = new Label
        {
            Text = "备注:",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopRight,
            Font = new Font("微软雅黑", 10f)
        };
        layout.Controls.Add(remarkLabel, 0, row);
        txtRemark = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("微软雅黑", 10f),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.White
        };
        layout.Controls.Add(txtRemark, 1, row);
        row++;

        // 考虑诊断：标题与文本框上下布局，占满整行宽度
        var diagnosisPanel = new Panel { Dock = DockStyle.Fill };
        var diagnosisLabel = new Label
        {
            Text = "考虑诊断:",
            Dock = DockStyle.Top,
            Height = 24,
            TextAlign = ContentAlignment.BottomLeft,
            Font = new Font("微软雅黑", 10f, FontStyle.Bold)
        };
        txtDiagnosis = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("微软雅黑", 10f),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true,
            BackColor = Color.FromArgb(255, 250, 240)
        };
        diagnosisPanel.Controls.Add(txtDiagnosis);
        diagnosisPanel.Controls.Add(diagnosisLabel);
        layout.Controls.Add(diagnosisPanel, 0, row);
        layout.SetColumnSpan(diagnosisPanel, 2);
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

    private void BuildColumn3()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55f));
        panelCol3.Controls.Add(layout);

        var group = new GroupBox { Text = "检查结果", Dock = DockStyle.Fill, Font = new Font("微软雅黑", 10f) };
        var resultPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };

        dgvResults = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AllowUserToResizeColumns = false,
            ReadOnly = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.CellSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            GridColor = Color.FromArgb(226, 232, 240),
            Font = new Font("微软雅黑", 10f),
            ColumnHeadersHeight = 35,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { Font = new Font("微软雅黑", 10f, FontStyle.Bold), SelectionBackColor = Color.FromArgb(241, 245, 249), SelectionForeColor = Color.FromArgb(71, 85, 105) },
            RowTemplate = new DataGridViewRow { Height = 28 },
            EnableHeadersVisualStyles = false,
            DefaultCellStyle = new DataGridViewCellStyle { SelectionBackColor = Color.Transparent, SelectionForeColor = Color.Black }
        };
        dgvResults.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
        dgvResults.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(71, 85, 105);
        dgvResults.CellPainting += DgvResults_CellPainting;
        dgvResults.CurrentCellChanged += (s, e) => dgvResults.ClearSelection();

        dgvResults.Columns.Add(new DataGridViewCheckBoxColumn
        {
            Name = "Check",
            HeaderText = "选中",
            Width = 50,
            ReadOnly = false,
            FillWeight = 50
        });
        dgvResults.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ChineseName",
            HeaderText = "中文名称",
            ReadOnly = true,
            FillWeight = 150
        });
        dgvResults.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "TestName",
            HeaderText = "项目名称",
            ReadOnly = true,
            FillWeight = 130
        });
        dgvResults.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Value",
            HeaderText = "当前值",
            ReadOnly = true,
            FillWeight = 100,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("微软雅黑", 10f, FontStyle.Bold) }
        });
        dgvResults.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "History1",
            HeaderText = "历史1",
            ReadOnly = true,
            FillWeight = 100,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
        });
        dgvResults.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "History2",
            HeaderText = "历史2",
            ReadOnly = true,
            FillWeight = 100,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
        });
        dgvResults.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "History3",
            HeaderText = "历史3",
            ReadOnly = true,
            FillWeight = 100,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
        });
        var unitCol = new DataGridViewTextBoxColumn
        {
            Name = "Unit",
            HeaderText = "单位",
            ReadOnly = true,
            FillWeight = 70,
            DefaultCellStyle = new DataGridViewCellStyle { ForeColor = Color.Black },
            HeaderCell = { Style = new DataGridViewCellStyle { ForeColor = Color.Black } }
        };
        dgvResults.Columns.Add(unitCol);
        var refCol = new DataGridViewTextBoxColumn
        {
            Name = "Reference",
            HeaderText = "参考范围",
            ReadOnly = true,
            FillWeight = 120,
            DefaultCellStyle = new DataGridViewCellStyle { ForeColor = Color.Black },
            HeaderCell = { Style = new DataGridViewCellStyle { ForeColor = Color.Black } }
        };
        dgvResults.Columns.Add(refCol);

        resultPanel.Controls.Add(dgvResults);
        group.Controls.Add(resultPanel);
        layout.Controls.Add(group, 0, 0);

        var bottomPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 1,
            Padding = new Padding(5)
        };
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));

        btnSelectAll = new Button { Text = "全选/全不选", Dock = DockStyle.Fill, Margin = new Padding(2), Font = new Font("微软雅黑", 11f) };
        btnSelectAll.Click += BtnSelectAll_Click;

        btnInvertSelect = new Button { Text = "反选", Dock = DockStyle.Fill, Margin = new Padding(2), Font = new Font("微软雅黑", 11f) };
        btnInvertSelect.Click += BtnInvertSelect_Click;

        btnAbnormalOnly = new Button { Text = "仅勾选异常", Dock = DockStyle.Fill, Margin = new Padding(2), Font = new Font("微软雅黑", 11f) };
        btnAbnormalOnly.Click += BtnAbnormalOnly_Click;

        btnCopy = new Button { Text = "复制", Dock = DockStyle.Fill, Margin = new Padding(2), Font = new Font("微软雅黑", 11f), Enabled = false };
        btnCopy.Click += BtnCopy_Click;

        btnPreview = new Button { Text = "预览", Dock = DockStyle.Fill, Margin = new Padding(2), Font = new Font("微软雅黑", 11f) };
        btnPreview.Click += BtnPreview_Click;

        bottomPanel.Controls.Add(btnSelectAll, 0, 0);
        bottomPanel.Controls.Add(btnInvertSelect, 1, 0);
        bottomPanel.Controls.Add(btnAbnormalOnly, 2, 0);
        bottomPanel.Controls.Add(btnCopy, 3, 0);
        bottomPanel.Controls.Add(btnPreview, 4, 0);

        layout.Controls.Add(bottomPanel, 0, 1);
    }

    private void LstSpecimens_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _specimens.Count) return;

        var s = _specimens[e.Index];
        var isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        var isPending = s.Status == SpecimenStatus.Pending;

        using var bgBrush = isSelected
            ? new SolidBrush(Color.FromArgb(219, 234, 254))
            : new SolidBrush(Color.White);
        e.Graphics.FillRectangle(bgBrush, e.Bounds);

        string time = s.TestTime?.ToString("HH:mm") ?? "--:--";
        string patient = string.IsNullOrEmpty(isPending ? s.PatientName : s.SnapshotPatientName)
            ? (!string.IsNullOrEmpty(s.OriginalPatientId) ? s.OriginalPatientId : (!string.IsNullOrEmpty(s.PatientId) ? s.PatientId : "(无姓名)"))
            : (isPending ? s.PatientName : s.SnapshotPatientName);
        string bed = string.IsNullOrEmpty(isPending ? s.BedNo : s.SnapshotBedNo)
            ? ""
            : $"  {(isPending ? s.BedNo : s.SnapshotBedNo)}床";
        string display = $"{time}  {s.SpecimenNo}  {patient}{bed}";

        Color textColor;
        Font font;
        if (isPending)
        {
            textColor = Color.Red;
            font = new Font("微软雅黑", 11f, FontStyle.Bold);
        }
        else
        {
            textColor = Color.FromArgb(22, 163, 74);
            font = new Font("微软雅黑", 11f);
        }

        if (isSelected)
        {
            textColor = Color.FromArgb(30, 64, 175);
        }

        using var textBrush = new SolidBrush(textColor);
        e.Graphics.DrawString(display, font, textBrush, e.Bounds.Left + 5, e.Bounds.Top + 5);

        if (isPending)
            font.Dispose();

        if (isPending)
        {
            using var dotBrush = new SolidBrush(Color.FromArgb(234, 88, 12));
            e.Graphics.FillEllipse(dotBrush, e.Bounds.Right - 18, e.Bounds.Top + 12, 8, 8);
        }
    }

    private void SetupRefreshTimer()
    {
        _refreshTimer = new System.Windows.Forms.Timer { Interval = 5000 };
        _refreshTimer.Tick += async (s, e) => await IncrementalRefreshAsync();
        _refreshTimer.Start();
    }

    public async Task LoadDateDataAsync()
    {
        try
        {
            if (lstSpecimens.InvokeRequired)
            {
                lstSpecimens.BeginInvoke(() => lblStatus.Text = "加载中...");
            }
            else
            {
                lblStatus.Text = "加载中...";
            }

            var fromDate = _selectedDate.Date;
            var toDate = _selectedDate.Date.AddDays(1).AddSeconds(-1);

            var result = await _api.QuerySpecimensAsync(
                page: 1,
                pageSize: 500,
                fromDate: fromDate,
                toDate: toDate,
                sortDesc: true);

            _specimens = result.Items;
            await MatchPatientsForSpecimensAsync();
            BindSpecimenList();
            // 默认选中第一项并自动填充患者信息，无需用户点击
            if (_specimens.Count > 0 && lstSpecimens.SelectedIndex >= 0)
                await ShowSelectedDetail();
            StatsUpdated?.Invoke(this, EventArgs.Empty);

            string status = _selectedDate.Date == DateTime.Today
                ? $"今日共 {_specimens.Count} 条记录"
                : $"{_selectedDate:yyyy-MM-dd} 共 {_specimens.Count} 条记录";
            lblStatus.Text = status;
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"加载失败: {ex.Message}";
        }
    }

    private async Task IncrementalRefreshAsync()
    {
        try
        {
            var fromDate = _selectedDate.Date;
            var toDate = _selectedDate.Date.AddDays(1).AddSeconds(-1);

            var result = await _api.QuerySpecimensAsync(
                page: 1,
                pageSize: 500,
                fromDate: fromDate,
                toDate: toDate,
                sortDesc: true);

            bool hasNew = result.Total > _specimens.Count ||
                          (result.Items.Count > 0 && _specimens.Count > 0 && result.Items[0].Id > _specimens[0].Id);

            _specimens = result.Items;
            await MatchPatientsForSpecimensAsync();
            BindSpecimenList();
            StatsUpdated?.Invoke(this, EventArgs.Empty);

            if (hasNew && _selectedDate.Date == DateTime.Today)
                lblStatus.Text = $"今日共 {_specimens.Count} 条记录 (有新数据)";
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"加载数据失败: {ex.Message}";
        }
    }

    private bool _suppressSelectionChanged = false;

    private async Task MatchPatientsForSpecimensAsync()
    {
        _matchedPatients.Clear();
        if (_specimens.Count == 0) return;

        try
        {
            _patientCache = await _api.GetPatientsAsync(onlyActive: true);
        }
        catch
        {
            _patientCache = new List<Patient>();
            return;
        }

        if (_patientCache.Count == 0) return;

        foreach (var s in _specimens)
        {
            if (s.Status != SpecimenStatus.Pending) continue;

            string matchKey = !string.IsNullOrEmpty(s.OriginalPatientId)
                ? s.OriginalPatientId
                : s.PatientId;
            if (string.IsNullOrWhiteSpace(matchKey)) continue;

            Patient? matched = null;
            var key = matchKey.Trim();

            // 优先匹配床位号
            matched = _patientCache.FirstOrDefault(p =>
                !string.IsNullOrEmpty(p.BedNo) &&
                p.BedNo.Equals(key, StringComparison.OrdinalIgnoreCase));
            // 其次匹配姓名缩写
            matched ??= _patientCache.FirstOrDefault(p =>
                !string.IsNullOrEmpty(p.NameAbbreviation) &&
                p.NameAbbreviation.Equals(key, StringComparison.OrdinalIgnoreCase));
            // 最后匹配病案号
            matched ??= _patientCache.FirstOrDefault(p =>
                p.PatientId.Equals(key, StringComparison.OrdinalIgnoreCase));

            if (matched != null)
            {
                _matchedPatients[s.Id] = matched;
                // 同步更新内存中的展示字段，使列表面板直接显示匹配到的患者信息
                s.PatientName = matched.PatientName;
                if (!string.IsNullOrEmpty(matched.BedNo)) s.BedNo = matched.BedNo;
                s.PatientId = matched.PatientId;
                if (!string.IsNullOrEmpty(matched.Gender)) s.Gender = matched.Gender;
                if (!string.IsNullOrEmpty(matched.Department)) s.Department = matched.Department;
            }
        }
    }

    private void BindSpecimenList()
    {
        if (lstSpecimens.InvokeRequired)
        {
            lstSpecimens.BeginInvoke(BindSpecimenList);
            return;
        }

        long currentId = _currentSpecimen?.Id ?? 0;
        int targetIdx = -1;

        if (currentId > 0)
        {
            for (int i = 0; i < _specimens.Count; i++)
            {
                if (_specimens[i].Id == currentId)
                {
                    targetIdx = i;
                    break;
                }
            }
        }
        if (targetIdx < 0 && _specimens.Count > 0)
            targetIdx = 0;

        _suppressSelectionChanged = true;
        lstSpecimens.BeginUpdate();
        lstSpecimens.Items.Clear();

        foreach (var s in _specimens)
            lstSpecimens.Items.Add(s);

        if (targetIdx >= 0 && targetIdx < _specimens.Count)
            lstSpecimens.SelectedIndex = targetIdx;

        lstSpecimens.EndUpdate();
        _suppressSelectionChanged = false;
    }

    private async Task ShowSelectedDetail()
    {
        if (lstSpecimens.SelectedIndex < 0 || lstSpecimens.SelectedIndex >= _specimens.Count) return;

        var s = _specimens[lstSpecimens.SelectedIndex];
        _currentSpecimen = s;

        string originalId = string.IsNullOrEmpty(s.OriginalPatientId) ? s.PatientId : s.OriginalPatientId;
        bool isAudited = s.Status == SpecimenStatus.Audited;

        if (isAudited)
        {
            txtPatientName.Text = s.SnapshotPatientName;
            txtBedNo.Text = s.SnapshotBedNo;
            txtPatientId.Text = s.SnapshotPatientId;
            txtOriginalPatientId.Text = string.IsNullOrEmpty(s.OriginalPatientId) ? s.PatientId : s.OriginalPatientId;
            cboGender.SelectedItem = string.IsNullOrEmpty(s.SnapshotGender) ? "" : s.SnapshotGender;
            txtDepartment.Text = s.SnapshotDepartment;
            cboSampleType.SelectedItem = s.SnapshotSampleType;
            txtRemark.Text = s.SnapshotRemark ?? "";
        }
        else
        {
            Patient? matchedPatient = null;
            if (_matchedPatients.TryGetValue(s.Id, out var cached))
            {
                matchedPatient = cached;
            }
            else if (!string.IsNullOrEmpty(originalId))
            {
                try
                {
                    matchedPatient = await _api.FindPatientAsync(originalId);
                    if (matchedPatient != null)
                        _matchedPatients[s.Id] = matchedPatient;
                }
                catch
                {
                    matchedPatient = null;
                }
            }

            if (matchedPatient != null)
            {
                txtPatientName.Text = matchedPatient.PatientName;
                txtBedNo.Text = !string.IsNullOrEmpty(matchedPatient.BedNo) ? matchedPatient.BedNo : s.BedNo;
                txtPatientId.Text = matchedPatient.PatientId;
                txtOriginalPatientId.Text = originalId;
                cboGender.SelectedItem = string.IsNullOrEmpty(matchedPatient.Gender) ? "" : matchedPatient.Gender;
                txtDepartment.Text = matchedPatient.Department;
            }
            else
            {
                txtPatientName.Text = s.PatientName;
                txtBedNo.Text = s.BedNo;
                txtPatientId.Text = s.PatientId;
                txtOriginalPatientId.Text = originalId;
                cboGender.SelectedItem = string.IsNullOrEmpty(s.Gender) ? "" : s.Gender;
                txtDepartment.Text = s.Department;
            }
            cboSampleType.SelectedItem = s.SampleType;
            txtRemark.Text = s.Remark ?? "";
        }

        txtReportTime.Text = s.TestTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";
        txtSpecimenNo.Text = s.SpecimenNo;

        SetPatientFieldsEditable(!isAudited);
        UpdateAuditButton();
        UpdateDeleteButton();
        await LoadResultsWithHistoryAsync(s);
        UpdateDiagnosis(s);
    }

    /// <summary>根据当前标本的检验结果生成「考虑诊断」解读文本</summary>
    private void UpdateDiagnosis(Specimen s)
    {
        try
        {
            if (s.Results == null || s.Results.Count == 0)
            {
                txtDiagnosis.Text = "";
                return;
            }
            txtDiagnosis.Text = BGShared.Analysis.AbgInterpreter.Interpret(s.Results, s.SampleType);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"生成考虑诊断失败: {ex.Message}");
            txtDiagnosis.Text = "";
        }
    }

    private void SetPatientFieldsEditable(bool editable)
    {
        txtPatientName.ReadOnly = !editable;
        txtPatientName.BackColor = editable ? Color.White : Color.FromArgb(245, 245, 245);
        txtBedNo.ReadOnly = !editable;
        txtBedNo.BackColor = editable ? Color.White : Color.FromArgb(245, 245, 245);
        txtPatientId.ReadOnly = !editable;
        txtPatientId.BackColor = editable ? Color.White : Color.FromArgb(245, 245, 245);
        txtOriginalPatientId.ReadOnly = true;
        txtOriginalPatientId.BackColor = Color.FromArgb(245, 245, 245);
        cboGender.Enabled = editable;
        txtDepartment.ReadOnly = !editable;
        txtDepartment.BackColor = editable ? Color.White : Color.FromArgb(245, 245, 245);
        cboGender.Enabled = editable;
        cboSampleType.Enabled = editable;
        txtRemark.ReadOnly = !editable;
        txtRemark.BackColor = editable ? Color.White : Color.FromArgb(245, 245, 245);
    }

    private void UpdateAuditButton()
    {
        if (_currentSpecimen == null)
        {
            btnAudit.Enabled = false;
            btnAudit.Text = "审核";
            btnAudit.ForeColor = SystemColors.ControlText;
            return;
        }

        if (_currentSpecimen.Status == SpecimenStatus.Audited)
        {
            btnAudit.Enabled = true;
            btnAudit.Text = "取消审核";
            btnAudit.ForeColor = Color.FromArgb(75, 85, 99);
        }
        else
        {
            btnAudit.Enabled = true;
            btnAudit.Text = "审核";
            btnAudit.ForeColor = Color.Black;
        }
    }

    private void UpdateDeleteButton()
    {
        if (_currentSpecimen == null)
        {
            btnDelete.Enabled = false;
            return;
        }
        btnDelete.Enabled = _currentSpecimen.Status == SpecimenStatus.Pending;
    }

    private async Task LoadResultsWithHistoryAsync(Specimen s)
    {
        dgvResults.Rows.Clear();
        btnCopy.Enabled = false;

        var visibleItems = DisplayConfigManager.GetVisibleItems();

        foreach (var configItem in visibleItems)
        {
            var result = s.Results.FirstOrDefault(r =>
                r.TestName.Equals(configItem.TestName, StringComparison.OrdinalIgnoreCase));

            string displayValue = "-";
            Color valueColor = Color.Black;
            string arrow = "";

            if (result != null && result.Value.HasValue)
            {
                displayValue = result.Value.Value.ToString("G6");
                if (configItem.Low.HasValue && configItem.High.HasValue)
                {
                    if (result.Value.Value > configItem.High.Value)
                    {
                        valueColor = Color.Red;
                        arrow = " ↑";
                    }
                    else if (result.Value.Value < configItem.Low.Value)
                    {
                        valueColor = Color.Blue;
                        arrow = " ↓";
                    }
                }
            }

            int idx = dgvResults.Rows.Add(
                result != null && result.Value.HasValue,
                configItem.ChineseName,
                configItem.TestName,
                displayValue + arrow,
                "",
                "",
                "",
                configItem.Unit,
                configItem.Reference
            );

            var row = dgvResults.Rows[idx];
            row.Cells["ChineseName"].Style.ForeColor = valueColor;
            row.Cells["TestName"].Style.ForeColor = valueColor;
            row.Cells["Value"].Style.ForeColor = valueColor;

            row.Cells["Unit"].Style.ForeColor = Color.Black;
            row.Cells["Reference"].Style.ForeColor = Color.Black;
        }

        btnCopy.Enabled = true;
        await LoadHistoricalResultsAsync(s);
    }

    private async Task LoadHistoricalResultsAsync(Specimen current)
    {
        _historicalSpecimens = await GetHistoricalSpecimensAsync(current);

        for (int i = 0; i < 3; i++)
        {
            var colName = $"History{i + 1}";
            if (i < _historicalSpecimens.Count)
            {
                var hist = _historicalSpecimens[i];
                var headerTime = hist.TestTime?.ToString("MM-dd HH:mm") ?? $"历史{i + 1}";
                dgvResults.Columns[colName].HeaderText = headerTime;
            }
            else
            {
                dgvResults.Columns[colName].HeaderText = $"历史{i + 1}";
            }
        }

        var visibleItems = DisplayConfigManager.GetVisibleItems();

        for (int rowIdx = 0; rowIdx < dgvResults.Rows.Count; rowIdx++)
        {
            var testName = dgvResults.Rows[rowIdx].Cells["TestName"].Value?.ToString();
            if (string.IsNullOrEmpty(testName)) continue;

            var configItem = visibleItems.FirstOrDefault(i =>
                i.TestName.Equals(testName, StringComparison.OrdinalIgnoreCase));

            for (int histIdx = 0; histIdx < 3; histIdx++)
            {
                var colName = $"History{histIdx + 1}";
                if (histIdx < _historicalSpecimens.Count)
                {
                    var histSpec = _historicalSpecimens[histIdx];
                    var histResult = histSpec.Results.FirstOrDefault(r =>
                        r.TestName.Equals(testName, StringComparison.OrdinalIgnoreCase));

                    if (histResult?.Value.HasValue == true)
                    {
                        var val = histResult.Value.Value;
                        var displayVal = val.ToString("G6");
                        Color valColor = Color.Black;

                        if (configItem != null && configItem.Low.HasValue && configItem.High.HasValue)
                        {
                            if (val > configItem.High.Value)
                                valColor = Color.Red;
                            else if (val < configItem.Low.Value)
                                valColor = Color.Blue;
                        }

                        dgvResults.Rows[rowIdx].Cells[colName].Value = displayVal;
                        dgvResults.Rows[rowIdx].Cells[colName].Style.ForeColor = valColor;
                    }
                }
            }
        }
    }

    private async Task<List<Specimen>> GetHistoricalSpecimensAsync(Specimen current)
    {
        try
        {
            string? matchId = null;
            if (current.Status == SpecimenStatus.Audited && !string.IsNullOrEmpty(current.SnapshotPatientId))
                matchId = current.SnapshotPatientId;
            else if (!string.IsNullOrEmpty(current.OriginalPatientId))
                matchId = current.OriginalPatientId;
            else if (!string.IsNullOrEmpty(current.PatientId))
                matchId = current.PatientId;

            if (string.IsNullOrEmpty(matchId))
                return new List<Specimen>();

            var result = await _api.QuerySpecimensAsync(
                page: 1,
                pageSize: 100,
                patientId: matchId,
                sortDesc: true);

            var historical = result.Items
                .Where(s => s.Id != current.Id)
                .Where(s => s.TestTime < current.TestTime)
                .OrderByDescending(s => s.TestTime)
                .Take(3)
                .ToList();

            return historical;
        }
        catch
        {
            return new List<Specimen>();
        }
    }

    private void BtnSelectAll_Click(object? sender, EventArgs e)
    {
        if (dgvResults.Rows.Count == 0) return;

        bool allChecked = true;
        foreach (DataGridViewRow row in dgvResults.Rows)
        {
            if (!Convert.ToBoolean(row.Cells["Check"].Value))
            {
                allChecked = false;
                break;
            }
        }

        foreach (DataGridViewRow row in dgvResults.Rows)
        {
            row.Cells["Check"].Value = !allChecked;
        }

        dgvResults.RefreshEdit();
    }

    private void BtnInvertSelect_Click(object? sender, EventArgs e)
    {
        foreach (DataGridViewRow row in dgvResults.Rows)
        {
            var checkCell = row.Cells["Check"];
            checkCell.Value = !Convert.ToBoolean(checkCell.Value);
        }

        dgvResults.RefreshEdit();
    }

    private void BtnAbnormalOnly_Click(object? sender, EventArgs e)
    {
        if (_currentSpecimen == null) return;

        var visibleItems = DisplayConfigManager.GetVisibleItems();

        foreach (DataGridViewRow row in dgvResults.Rows)
        {
            var checkCell = row.Cells["Check"];
            var testName = row.Cells["TestName"].Value?.ToString();

            if (string.IsNullOrEmpty(testName))
            {
                checkCell.Value = false;
                continue;
            }

            var configItem = visibleItems.FirstOrDefault(i =>
                i.TestName.Equals(testName, StringComparison.OrdinalIgnoreCase));
            var result = _currentSpecimen.Results.FirstOrDefault(r =>
                r.TestName.Equals(testName, StringComparison.OrdinalIgnoreCase));

            if (configItem == null || result == null || !result.Value.HasValue)
            {
                checkCell.Value = false;
                continue;
            }

            bool isAbnormal = false;
            if (configItem.Low.HasValue && configItem.High.HasValue)
            {
                if (result.Value.Value > configItem.High.Value ||
                    result.Value.Value < configItem.Low.Value)
                {
                    isAbnormal = true;
                }
            }

            checkCell.Value = isAbnormal;
        }

        dgvResults.RefreshEdit();
    }

    private void BtnCopy_Click(object? sender, EventArgs e)
    {
        if (_currentSpecimen == null) return;

        var items = new List<string>();
        foreach (DataGridViewRow row in dgvResults.Rows)
        {
            if (Convert.ToBoolean(row.Cells["Check"].Value))
            {
                var testName = row.Cells["TestName"].Value?.ToString() ?? "";
                var value = row.Cells["Value"].Value?.ToString() ?? "";
                var unit = row.Cells["Unit"].Value?.ToString() ?? "";
                items.Add($"{testName}：{value}{unit}");
            }
        }

        if (items.Count > 0)
        {
            var text = "血气分析：" + string.Join("，", items) + "。";
            Clipboard.SetText(text);
            ShowToast("已复制到剪贴板");
        }
        else
        {
            ShowToast("没有选中的项目");
        }
    }

    private void ShowToast(string message)
    {
        _toastForm?.Close();
        _toastTimer?.Stop();
        _toastTimer?.Dispose();

        _toastForm = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            ShowInTaskbar = false,
            TopMost = true,
            BackColor = Color.FromArgb(50, 50, 50),
            Opacity = 0.9,
            Size = new Size(200, 50)
        };

        var lbl = new Label
        {
            Text = message,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.White,
            Font = new Font("微软雅黑", 11f, FontStyle.Bold)
        };
        _toastForm.Controls.Add(lbl);

        var parent = FindForm();
        if (parent != null)
        {
            int x = parent.Left + (parent.Width - _toastForm.Width) / 2;
            int y = parent.Top + (parent.Height - _toastForm.Height) / 2;
            _toastForm.Location = new Point(x, y);
        }

        _toastTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _toastTimer.Tick += (s, e) =>
        {
            _toastTimer?.Stop();
            _toastTimer?.Dispose();
            _toastTimer = null;
            if (_toastForm != null && !_toastForm.IsDisposed)
            {
                _toastForm.Close();
                _toastForm.Dispose();
                _toastForm = null;
            }
        };
        _toastTimer.Start();

        _toastForm.Show();
    }

    private void DgvResults_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
        {
            if (e.State.HasFlag(DataGridViewElementStates.Selected))
            {
                var cell = dgvResults.Rows[e.RowIndex].Cells[e.ColumnIndex];
                Color backColor = cell.Style.BackColor;
                if (backColor.IsEmpty) backColor = Color.White;
                Color foreColor = cell.Style.ForeColor;
                if (foreColor.IsEmpty) foreColor = Color.Black;

                e.Graphics.FillRectangle(new SolidBrush(backColor), e.CellBounds);
                e.PaintContent(e.CellBounds);
                e.Handled = true;
            }
        }
    }

    public async Task AuditCurrentAsync()
    {
        if (_currentSpecimen == null)
        {
            MessageBox.Show("请先选择一条记录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_currentSpecimen.Status == SpecimenStatus.Audited)
        {
            bool ok = await _api.UnarchiveSpecimenAsync(_currentSpecimen.Id);
            if (ok)
            {
                _currentSpecimen.Status = SpecimenStatus.Pending;
                UpdateAuditButton();
                await LoadDateDataAsync();
            }
            else
            {
                MessageBox.Show("取消审核失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        else
        {
            string snapshotPatientName = txtPatientName.Text.Trim();
            string snapshotBedNo = txtBedNo.Text.Trim();
            string snapshotPatientId = txtPatientId.Text.Trim();
            string snapshotNameAbbr = "";
            string snapshotDepartment = txtDepartment.Text.Trim();
            string snapshotGender = cboGender.SelectedItem?.ToString() ?? "";
            string snapshotSampleType = cboSampleType.SelectedItem?.ToString() ?? "未指定";
            string snapshotRemark = txtRemark.Text.Trim();

            bool ok = await _api.AuditSpecimenWithSnapshotAsync(
                _currentSpecimen.Id,
                snapshotPatientName,
                snapshotBedNo,
                snapshotPatientId,
                snapshotNameAbbr,
                snapshotDepartment,
                snapshotGender,
                snapshotSampleType,
                snapshotRemark);

            if (ok)
            {
                _currentSpecimen.Status = SpecimenStatus.Audited;
                UpdateAuditButton();
                await LoadDateDataAsync();
            }
            else
            {
                MessageBox.Show("审核失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public async Task DeleteCurrentAsync()
    {
        if (_currentSpecimen == null)
        {
            MessageBox.Show("请先选择一条记录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_currentSpecimen.Status == SpecimenStatus.Audited)
        {
            MessageBox.Show("已审核的标本不能删除", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var result = MessageBox.Show(
            $"确定要删除样本 {_currentSpecimen.SpecimenNo} 吗？此操作不可恢复！",
            "删除确认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes) return;

        bool ok = await _api.DeleteSpecimenAsync(_currentSpecimen.Id);
        if (ok)
        {
            MessageBox.Show("删除成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _currentSpecimen = null;
            await LoadDateDataAsync();
        }
        else
        {
            MessageBox.Show("删除失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public (long Total, int Pending) GetStats()
    {
        int pending = _specimens.Count(s => s.Status == SpecimenStatus.Pending);
        return (_specimens.Count, pending);
    }

    private void BtnPreview_Click(object? sender, EventArgs e)
    {
        PrintReport();
    }

    public void PrintReport()
    {
        if (_currentSpecimen == null)
        {
            MessageBox.Show("请先选择一条记录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var doc = new PrintDocument();
        doc.PrintPage += PrintDocument_PrintPage;
        SetupA5Landscape(doc.DefaultPageSettings);

        var preview = new PrintPreviewDialog
        {
            Document = doc,
            Width = 900,
            Height = 700,
            WindowState = FormWindowState.Normal
        };
        preview.ShowDialog();
    }

    

    private static void SetupA5Landscape(PageSettings pageSettings)
    {
        var template = PrintTemplateManager.CurrentTemplate;

        PaperSize paperSize = new PaperSize("A5", 583, 827);

        pageSettings.PaperSize = paperSize;
        pageSettings.Landscape = true;
        pageSettings.Margins = new Margins(
            (int)template.LeftMargin,
            (int)template.RightMargin,
            (int)template.TopMargin,
            (int)template.BottomMargin
        );
    }

    private void PrintDocument_PrintPage(object? sender, PrintPageEventArgs e)
    {
        if (_currentSpecimen == null)
            return;

        PrintSingleReport(e.Graphics, e.MarginBounds, _currentSpecimen);
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

                DrawRow(y, item.Config.TestName, val + arrow, range, unit, brush);
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

    protected override void OnHandleDestroyed(EventArgs e)
    {
        _refreshTimer?.Stop();
        base.OnHandleDestroyed(e);
    }
}
