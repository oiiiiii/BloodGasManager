using BGManager.Services;
using BGShared.Models;

namespace BGManager.Views;

public class PatientView : UserControl
{
    private readonly ApiClient _api;
    private List<Patient> _patients = new();

    private TableLayoutPanel mainLayout;
    private DataGridView dgvPatients;
    private Button btnAdd;
    private Button btnEdit;
    private Button btnDelete;

    public PatientView(ApiClient api)
    {
        _api = api;
        Dock = DockStyle.Fill;
        BuildUI();
        _ = LoadPatientsAsync();
    }

    private void BuildUI()
    {
        mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Padding = new Padding(8)
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 75f));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        Controls.Add(mainLayout);

        var topPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10), BackColor = Color.FromArgb(241, 245, 249) };
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };

        btnAdd = new Button { Text = "添加", Size = new Size(90, 40), Margin = new Padding(5), Font = new Font("微软雅黑", 10f), BackColor = Color.FromArgb(34, 197, 94), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        btnAdd.FlatAppearance.BorderSize = 0;
        btnAdd.Click += async (s, e) => await AddPatientAsync();
        flow.Controls.Add(btnAdd);

        btnEdit = new Button { Text = "编辑", Size = new Size(90, 40), Margin = new Padding(5), Font = new Font("微软雅黑", 10f) };
        btnEdit.Click += async (s, e) => await EditPatientAsync();
        flow.Controls.Add(btnEdit);

        btnDelete = new Button { Text = "删除", Size = new Size(90, 40), Margin = new Padding(5), Font = new Font("微软雅黑", 10f), ForeColor = Color.FromArgb(220, 38, 38) };
        btnDelete.Click += async (s, e) => await DeletePatientAsync();
        flow.Controls.Add(btnDelete);

        topPanel.Controls.Add(flow);
        mainLayout.Controls.Add(topPanel, 0, 0);

        dgvPatients = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            GridColor = Color.FromArgb(226, 232, 240),
            Font = new Font("微软雅黑", 9f),
            ColumnHeadersHeight = 32,
            EnableHeadersVisualStyles = false
        };
        dgvPatients.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
        dgvPatients.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(71, 85, 105);
        dgvPatients.ColumnHeadersDefaultCellStyle.Font = new Font("微软雅黑", 9f, FontStyle.Bold);
        dgvPatients.Columns.Add("colId", "病案号");
        dgvPatients.Columns.Add("colName", "患者姓名");
        dgvPatients.Columns.Add("colNameAbbr", "姓名缩写");
        dgvPatients.Columns.Add("colBed", "床号");
        dgvPatients.Columns.Add("colDept", "科室");
        dgvPatients.Columns.Add("colGender", "性别");
        dgvPatients.Columns.Add("colDate", "创建日期");
        dgvPatients.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) _ = EditPatientAsync(); };
        mainLayout.Controls.Add(dgvPatients, 0, 1);
    }

    private async Task LoadPatientsAsync()
    {
        try
        {
            var list = await _api.GetPatientsAsync(false);
            _patients = list;
            BindGrid();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BindGrid()
    {
        if (dgvPatients.InvokeRequired)
        {
            dgvPatients.BeginInvoke(() => BindGrid());
            return;
        }

        dgvPatients.Rows.Clear();

        foreach (var p in _patients)
        {
            int idx = dgvPatients.Rows.Add(
                p.PatientId,
                p.PatientName,
                p.NameAbbreviation,
                p.BedNo,
                p.Department,
                p.Gender,
                p.CreatedAt?.ToString("yyyy-MM-dd") ?? ""
            );

            var row = dgvPatients.Rows[idx];
            row.Tag = p;
        }
    }

    private Patient? GetSelectedPatient()
    {
        if (dgvPatients.SelectedRows.Count == 0) return null;
        return dgvPatients.SelectedRows[0].Tag as Patient;
    }

    private async Task AddPatientAsync()
    {
        using var dlg = new PatientEditForm(new Patient { IsActive = true }, _patients);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            bool ok = await _api.SavePatientAsync(dlg.Patient);
            if (ok)
            {
                MessageBox.Show("添加成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadPatientsAsync();
            }
            else
            {
                MessageBox.Show("添加失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private async Task EditPatientAsync()
    {
        var patient = GetSelectedPatient();
        if (patient == null)
        {
            MessageBox.Show("请先选择一个患者", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new PatientEditForm(patient, _patients);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            bool ok = await _api.SavePatientAsync(dlg.Patient);
            if (ok)
            {
                MessageBox.Show("保存成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadPatientsAsync();
            }
            else
            {
                MessageBox.Show("保存失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private async Task DeletePatientAsync()
    {
        var patient = GetSelectedPatient();
        if (patient == null)
        {
            MessageBox.Show("请先选择一个患者", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var result = MessageBox.Show(
            $"确定要删除患者 \"{patient.PatientName} ({patient.PatientId})\" 吗？",
            "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes) return;

        bool ok = await _api.DeletePatientAsync(patient.PatientId);
        if (ok)
        {
            MessageBox.Show("删除成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await LoadPatientsAsync();
        }
        else
        {
            MessageBox.Show("删除失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void RefreshData()
    {
        _ = LoadPatientsAsync();
    }
}
