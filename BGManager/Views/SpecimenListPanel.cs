using BGShared.Models;

namespace BGManager.Views;

public class SpecimenListPanel : Panel
{
    public DateTimePicker DatePicker { get; private set; }
    public Button BtnPrevDay { get; private set; }
    public Button BtnNextDay { get; private set; }
    public Button BtnToday { get; private set; }
    public Label LblStatus { get; private set; }
    public ListBox LstSpecimens { get; private set; }
    public Button BtnRefresh { get; private set; }
    public Button BtnDelete { get; private set; }
    public Button BtnAudit { get; private set; }

    public event EventHandler? DateChanged;
    public event EventHandler? RefreshClicked;
    public event EventHandler? DeleteClicked;
    public event EventHandler? AuditClicked;
    public event EventHandler? SelectionChanged;

    private bool _suppressSelectionChanged;

    public bool SuppressSelectionChanged => _suppressSelectionChanged;

    internal void SetSuppressSelectionChanged(bool value)
    {
        _suppressSelectionChanged = value;
    }

    public SpecimenListPanel()
    {
        Dock = DockStyle.Fill;
        BuildUI();
    }

    private void BuildUI()
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
        Controls.Add(layout);

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

        BtnPrevDay = new Button
        {
            Text = "◀",
            Dock = DockStyle.Fill,
            Font = new Font("微软雅黑", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 64, 175),
            Margin = new Padding(0, 3, 2, 3)
        };
        BtnPrevDay.Click += (s, e) => { DatePicker.Value = DatePicker.Value.AddDays(-1); };
        datePanel.Controls.Add(BtnPrevDay, 0, 0);

        DatePicker = new DateTimePicker
        {
            Dock = DockStyle.Fill,
            Format = DateTimePickerFormat.Long,
            Value = DateTime.Today,
            Font = new Font("微软雅黑", 14f),
            CalendarFont = new Font("微软雅黑", 14f),
            Margin = new Padding(2, 3, 2, 3)
        };
        DatePicker.ValueChanged += (s, e) => DateChanged?.Invoke(this, EventArgs.Empty);
        datePanel.Controls.Add(DatePicker, 1, 0);

        BtnNextDay = new Button
        {
            Text = "▶",
            Dock = DockStyle.Fill,
            Font = new Font("微软雅黑", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 64, 175),
            Margin = new Padding(2, 3, 2, 3)
        };
        BtnNextDay.Click += (s, e) => { DatePicker.Value = DatePicker.Value.AddDays(1); };
        datePanel.Controls.Add(BtnNextDay, 2, 0);

        BtnToday = new Button
        {
            Text = "今",
            Dock = DockStyle.Fill,
            Font = new Font("微软雅黑", 14f, FontStyle.Bold),
            ForeColor = Color.Red,
            BackColor = Color.White,
            Margin = new Padding(2, 3, 0, 3)
        };
        BtnToday.Click += (s, e) => { DatePicker.Value = DateTime.Today; };
        datePanel.Controls.Add(BtnToday, 3, 0);

        layout.Controls.Add(datePanel, 0, 0);

        var statusPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(2) };
        LblStatus = new Label
        {
            Text = "准备就绪",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.Gray,
            Font = new Font("微软雅黑", 9f)
        };
        statusPanel.Controls.Add(LblStatus);
        layout.Controls.Add(statusPanel, 0, 1);

        var listPanel = new Panel { Dock = DockStyle.Fill };
        var group = new GroupBox { Text = "检验单列表", Dock = DockStyle.Fill, Font = new Font("微软雅黑", 10f) };
        LstSpecimens = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("微软雅黑", 11f),
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 32,
            BorderStyle = BorderStyle.None
        };
        LstSpecimens.SelectedIndexChanged += (s, e) =>
        {
            if (!_suppressSelectionChanged)
                SelectionChanged?.Invoke(this, EventArgs.Empty);
        };
        LstSpecimens.DrawItem += LstSpecimens_DrawItem;
        group.Controls.Add(LstSpecimens);
        listPanel.Controls.Add(group);
        layout.Controls.Add(listPanel, 0, 2);

        var buttonPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(3) };
        BtnRefresh = new Button { Text = "刷新", Dock = DockStyle.Left, Width = 90, Height = 45, Font = new Font("微软雅黑", 11f) };
        BtnRefresh.Click += (s, e) => RefreshClicked?.Invoke(this, EventArgs.Empty);

        BtnDelete = new Button
        {
            Text = "删除",
            Dock = DockStyle.Left,
            Width = 90,
            Height = 45,
            Font = new Font("微软雅黑", 11f, FontStyle.Bold),
            ForeColor = Color.Red,
            Enabled = false
        };
        BtnDelete.Click += (s, e) => DeleteClicked?.Invoke(this, EventArgs.Empty);

        BtnAudit = new Button
        {
            Text = "审核",
            Dock = DockStyle.Right,
            Width = 90,
            Height = 45,
            Font = new Font("微软雅黑", 11f, FontStyle.Bold),
            Enabled = false
        };
        BtnAudit.Click += (s, e) => AuditClicked?.Invoke(this, EventArgs.Empty);

        buttonPanel.Controls.Add(BtnRefresh);
        buttonPanel.Controls.Add(BtnDelete);
        buttonPanel.Controls.Add(BtnAudit);
        layout.Controls.Add(buttonPanel, 0, 3);
    }

    public void UpdateList(List<Specimen> specimens)
    {
        SetSuppressSelectionChanged(true);
        LstSpecimens.Items.Clear();
        foreach (var s in specimens)
            LstSpecimens.Items.Add(s);
        SetSuppressSelectionChanged(false);
    }

    private void LstSpecimens_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= LstSpecimens.Items.Count) return;

        var s = (Specimen)LstSpecimens.Items[e.Index];
        var isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        var isPending = s.Status == SpecimenStatus.Pending;

        using var bgBrush = isSelected
            ? new SolidBrush(Color.FromArgb(219, 234, 254))
            : new SolidBrush(Color.White);
        e.Graphics.FillRectangle(bgBrush, e.Bounds);

        string time = s.TestTime?.ToString("HH:mm") ?? "--:--";
        string patient = string.IsNullOrEmpty(isPending ? s.PatientName : s.SnapshotPatientName)
            ? "(无姓名)"
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
}