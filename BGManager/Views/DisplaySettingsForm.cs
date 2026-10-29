using BGShared.Config;
using BGShared.Models;

namespace BGManager.Views;

public class DisplaySettingsForm : Form
{
    private List<DisplayItem> _items;
    private bool _hasUnsavedChanges;
    private int _dragRowIndex = -1;
    private int _hoverRowIndex = -1;

    private DataGridView dgvItems;
    private Button btnImport;
    private Button btnSave;
    private Button btnExport;
    private Button btnClose;
    private Button btnMoveUp;
    private Button btnMoveDown;
    private Label lblStatus;
    private Panel panelButtons;

    public DisplaySettingsForm()
    {
        _items = new List<DisplayItem>(DisplayConfigManager.Config.Items.OrderBy(i => i.SortOrder).ToList());
        _hasUnsavedChanges = false;

        InitializeComponent();
        LoadItems();
        UpdateUI();
    }

    private void LoadItems()
    {
        dgvItems.Rows.Clear();
        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            var sortDisplay = item.SortOrder > 0 ? item.SortOrder.ToString() : "";
            var lowDisplay = item.Low.HasValue ? item.Low.Value.ToString() : "";
            var highDisplay = item.High.HasValue ? item.High.Value.ToString() : "";
            
            dgvItems.Rows.Add(
                item.IsVisible, 
                sortDisplay,
                item.TestName,
                item.ChineseName,
                item.Unit ?? "",
                lowDisplay,
                highDisplay
            );
        }
    }

    private void UpdateUI()
    {
        btnExport.Enabled = !_hasUnsavedChanges;
        lblStatus.Text = _hasUnsavedChanges ? "有未保存的更改" : "已保存";
        lblStatus.ForeColor = _hasUnsavedChanges ? Color.Red : Color.Green;
    }

    private void MarkUnsaved()
    {
        _hasUnsavedChanges = true;
        UpdateUI();
    }

    private void btnImport_Click(object sender, EventArgs e)
    {
        if (_hasUnsavedChanges)
        {
            var result = MessageBox.Show(
                "有未保存的更改，导入会覆盖当前配置。是否继续？",
                "警告",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
                return;
        }

        using var dialog = new OpenFileDialog();
        dialog.Filter = "JSON 配置文件 (*.json)|*.json|所有文件 (*.*)|*.*";
        dialog.Title = "导入显示配置";

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            try
            {
                DisplayConfigManager.ImportConfig(dialog.FileName);
                _items = new List<DisplayItem>(DisplayConfigManager.Config.Items.OrderBy(i => i.SortOrder).ToList());
                _hasUnsavedChanges = false;
                LoadItems();
                UpdateUI();
                MessageBox.Show("配置导入成功！");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void btnSave_Click(object sender, EventArgs e)
    {
        try
        {
            int visibleIndex = 1;
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].IsVisible)
                {
                    _items[i].SortOrder = visibleIndex++;
                }
                else
                {
                    _items[i].SortOrder = 0;
                }
            }

            DisplayConfigManager.Config.Items = new List<DisplayItem>(_items);
            DisplayConfigManager.SaveConfig();
            _hasUnsavedChanges = false;
            LoadItems();
            UpdateUI();
            MessageBox.Show("配置保存成功！");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btnExport_Click(object sender, EventArgs e)
    {
        if (_hasUnsavedChanges)
        {
            MessageBox.Show("请先保存配置后再导出！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog();
        dialog.Filter = "JSON 配置文件 (*.json)|*.json|所有文件 (*.*)|*.*";
        dialog.Title = "导出显示配置";
        dialog.FileName = "display_config.json";

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            try
            {
                DisplayConfigManager.ExportConfig(dialog.FileName);
                MessageBox.Show("配置导出成功！");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void btnMoveUp_Click(object sender, EventArgs e)
    {
        if (dgvItems.SelectedRows.Count == 0)
            return;

        var rowIndex = dgvItems.SelectedRows[0].Index;
        if (rowIndex <= 0)
            return;

        var item = _items[rowIndex];
        _items.RemoveAt(rowIndex);
        _items.Insert(rowIndex - 1, item);

        LoadItems();
        dgvItems.Rows[rowIndex - 1].Selected = true;
        MarkUnsaved();
    }

    private void btnMoveDown_Click(object sender, EventArgs e)
    {
        if (dgvItems.SelectedRows.Count == 0)
            return;

        var rowIndex = dgvItems.SelectedRows[0].Index;
        if (rowIndex >= _items.Count - 1)
            return;

        var item = _items[rowIndex];
        _items.RemoveAt(rowIndex);
        _items.Insert(rowIndex + 1, item);

        LoadItems();
        dgvItems.Rows[rowIndex + 1].Selected = true;
        MarkUnsaved();
    }

    private void dgvItems_CellContentClick(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _items.Count)
            return;

        if (e.ColumnIndex == 0)
        {
            var currentValue = _items[e.RowIndex].IsVisible;
            var newValue = !currentValue;
            _items[e.RowIndex].IsVisible = newValue;
            
            dgvItems.Rows[e.RowIndex].Cells[0].Value = newValue;
            MarkUnsaved();
        }
    }

    private void dgvItems_CellEndEdit(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _items.Count)
            return;

        var item = _items[e.RowIndex];
        var cellValue = dgvItems.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? "";

        switch (e.ColumnIndex)
        {
            case 2: 
                item.TestName = cellValue;
                break;
            
            case 3: 
                item.ChineseName = string.IsNullOrWhiteSpace(cellValue) ? null : cellValue;
                break;
            
            case 4: 
                item.Unit = string.IsNullOrWhiteSpace(cellValue) ? null : cellValue;
                break;
            
            case 5: 
                if (string.IsNullOrWhiteSpace(cellValue))
                {
                    item.Low = null;
                }
                else if (double.TryParse(cellValue, out var lowNum))
                {
                    item.Low = lowNum;
                }
                AutoFillReference(item);
                break;
            
            case 6: 
                if (string.IsNullOrWhiteSpace(cellValue))
                {
                    item.High = null;
                }
                else if (double.TryParse(cellValue, out var highNum))
                {
                    item.High = highNum;
                }
                AutoFillReference(item);
                break;
        }
        
        MarkUnsaved();
    }

    private void AutoFillReference(DisplayItem item)
    {
        if (item.Low.HasValue && item.High.HasValue)
        {
            item.Reference = $"{item.Low.Value}--{item.High.Value}";
        }
        else if (item.Low.HasValue)
        {
            item.Reference = $"{item.Low.Value}--";
        }
        else if (item.High.HasValue)
        {
            item.Reference = $"--{item.High.Value}";
        }
        else
        {
            item.Reference = null;
        }
    }

    private void dgvItems_CellMouseMove(object sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.ColumnIndex == 2 && e.RowIndex >= 0)
        {
            dgvItems.Cursor = Cursors.Hand;
        }
        else
        {
            dgvItems.Cursor = Cursors.Default;
        }
    }

    private void dgvItems_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.RowIndex >= 0 && e.ColumnIndex == 2)
        {
            _dragRowIndex = e.RowIndex;
            dgvItems.DoDragDrop(dgvItems.Rows[e.RowIndex], DragDropEffects.Move);
        }
    }

    private void dgvItems_DragOver(object sender, DragEventArgs e)
    {
        e.Effect = DragDropEffects.Move;

        Point clientPoint = dgvItems.PointToClient(new Point(e.X, e.Y));
        int hoverIndex = dgvItems.HitTest(clientPoint.X, clientPoint.Y).RowIndex;

        if (hoverIndex != _hoverRowIndex)
        {
            if (_hoverRowIndex >= 0 && _hoverRowIndex < dgvItems.Rows.Count)
            {
                dgvItems.Rows[_hoverRowIndex].DefaultCellStyle.BackColor = Color.White;
            }

            _hoverRowIndex = hoverIndex;

            if (_hoverRowIndex >= 0 && _hoverRowIndex < dgvItems.Rows.Count && _hoverRowIndex != _dragRowIndex)
            {
                dgvItems.Rows[_hoverRowIndex].DefaultCellStyle.BackColor = Color.LightBlue;
            }
        }
    }

    private void dgvItems_DragLeave(object sender, EventArgs e)
    {
        if (_hoverRowIndex >= 0 && _hoverRowIndex < dgvItems.Rows.Count)
        {
            dgvItems.Rows[_hoverRowIndex].DefaultCellStyle.BackColor = Color.White;
        }
        _hoverRowIndex = -1;
    }

    private void dgvItems_DragDrop(object sender, DragEventArgs e)
    {
        Point clientPoint = dgvItems.PointToClient(new Point(e.X, e.Y));
        int dropRowIndex = dgvItems.HitTest(clientPoint.X, clientPoint.Y).RowIndex;

        if (_hoverRowIndex >= 0 && _hoverRowIndex < dgvItems.Rows.Count)
        {
            dgvItems.Rows[_hoverRowIndex].DefaultCellStyle.BackColor = Color.White;
        }
        _hoverRowIndex = -1;

        if (e.Effect == DragDropEffects.Move && _dragRowIndex >= 0 && dropRowIndex >= 0 && _dragRowIndex != dropRowIndex)
        {
            var item = _items[_dragRowIndex];
            _items.RemoveAt(_dragRowIndex);
            _items.Insert(dropRowIndex, item);

            LoadItems();
            dgvItems.Rows[dropRowIndex].Selected = true;
            MarkUnsaved();
        }

        _dragRowIndex = -1;
    }

    private void btnClose_Click(object sender, EventArgs e)
    {
        if (_hasUnsavedChanges)
        {
            var result = MessageBox.Show(
                "有未保存的更改，是否保存？",
                "确认",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                btnSave_Click(sender, e);
                if (_hasUnsavedChanges)
                    return;
            }
            else if (result == DialogResult.Cancel)
            {
                return;
            }
        }
        Close();
    }

    private void InitializeComponent()
    {
        dgvItems = new DataGridView();
        panelButtons = new Panel();
        btnImport = new Button();
        btnExport = new Button();
        btnSave = new Button();
        btnClose = new Button();
        btnMoveUp = new Button();
        btnMoveDown = new Button();
        lblStatus = new Label();
        ((System.ComponentModel.ISupportInitialize)dgvItems).BeginInit();
        panelButtons.SuspendLayout();
        SuspendLayout();

        var colVisible = new DataGridViewCheckBoxColumn();
        colVisible.Name = "colVisible";
        colVisible.HeaderText = "显示";
        colVisible.Width = 50;
        colVisible.Frozen = true;

        var colSortOrder = new DataGridViewTextBoxColumn();
        colSortOrder.Name = "colSortOrder";
        colSortOrder.HeaderText = "排序";
        colSortOrder.ReadOnly = true;
        colSortOrder.Width = 50;

        var colTestName = new DataGridViewTextBoxColumn();
        colTestName.Name = "colTestName";
        colTestName.HeaderText = "项目名称(英文)";
        colTestName.Width = 150;

        var colChineseName = new DataGridViewTextBoxColumn();
        colChineseName.Name = "colChineseName";
        colChineseName.HeaderText = "项目名称(中文)";
        colChineseName.Width = 150;
        colChineseName.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

        var colUnit = new DataGridViewTextBoxColumn();
        colUnit.Name = "colUnit";
        colUnit.HeaderText = "单位";
        colUnit.Width = 70;

        var colLow = new DataGridViewTextBoxColumn();
        colLow.Name = "colLow";
        colLow.HeaderText = "下限";
        colLow.Width = 70;

        var colHigh = new DataGridViewTextBoxColumn();
        colHigh.Name = "colHigh";
        colHigh.HeaderText = "上限";
        colHigh.Width = 70;

        dgvItems.AllowUserToAddRows = false;
        dgvItems.AllowUserToDeleteRows = false;
        dgvItems.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvItems.Columns.AddRange(colVisible, colSortOrder, colTestName, colChineseName, colUnit, colLow, colHigh);
        dgvItems.Dock = DockStyle.Fill;
        dgvItems.Name = "dgvItems";
        dgvItems.RowHeadersVisible = false;
        dgvItems.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvItems.TabIndex = 0;
        dgvItems.AllowDrop = true;
        dgvItems.ScrollBars = ScrollBars.Both;
        dgvItems.CellContentClick += dgvItems_CellContentClick;
        dgvItems.CellEndEdit += dgvItems_CellEndEdit;
        dgvItems.CellMouseMove += dgvItems_CellMouseMove;
        dgvItems.CellMouseDown += dgvItems_CellMouseDown;
        dgvItems.DragOver += dgvItems_DragOver;
        dgvItems.DragLeave += dgvItems_DragLeave;
        dgvItems.DragDrop += dgvItems_DragDrop;

        panelButtons.Controls.Add(lblStatus);
        panelButtons.Controls.Add(btnClose);
        panelButtons.Controls.Add(btnSave);
        panelButtons.Controls.Add(btnExport);
        panelButtons.Controls.Add(btnImport);
        panelButtons.Controls.Add(btnMoveDown);
        panelButtons.Controls.Add(btnMoveUp);
        panelButtons.Dock = DockStyle.Bottom;
        panelButtons.Height = 70;
        panelButtons.Name = "panelButtons";

        btnMoveUp.Location = new Point(12, 12);
        btnMoveUp.Name = "btnMoveUp";
        btnMoveUp.Size = new Size(75, 30);
        btnMoveUp.TabIndex = 0;
        btnMoveUp.Text = "↑ 上移";
        btnMoveUp.UseVisualStyleBackColor = true;
        btnMoveUp.Click += btnMoveUp_Click;

        btnMoveDown.Location = new Point(93, 12);
        btnMoveDown.Name = "btnMoveDown";
        btnMoveDown.Size = new Size(75, 30);
        btnMoveDown.TabIndex = 1;
        btnMoveDown.Text = "↓ 下移";
        btnMoveDown.UseVisualStyleBackColor = true;
        btnMoveDown.Click += btnMoveDown_Click;

        btnImport.Location = new Point(174, 12);
        btnImport.Name = "btnImport";
        btnImport.Size = new Size(75, 30);
        btnImport.TabIndex = 2;
        btnImport.Text = "导入";
        btnImport.UseVisualStyleBackColor = true;
        btnImport.Click += btnImport_Click;

        btnExport.Location = new Point(255, 12);
        btnExport.Name = "btnExport";
        btnExport.Size = new Size(75, 30);
        btnExport.TabIndex = 3;
        btnExport.Text = "导出";
        btnExport.UseVisualStyleBackColor = true;
        btnExport.Click += btnExport_Click;

        btnSave.Location = new Point(336, 12);
        btnSave.Name = "btnSave";
        btnSave.Size = new Size(75, 30);
        btnSave.TabIndex = 4;
        btnSave.Text = "保存";
        btnSave.UseVisualStyleBackColor = true;
        btnSave.Font = new Font(btnSave.Font, FontStyle.Bold);
        btnSave.Click += btnSave_Click;

        btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnClose.Location = new Point(490, 12);
        btnClose.Name = "btnClose";
        btnClose.Size = new Size(75, 30);
        btnClose.TabIndex = 5;
        btnClose.Text = "关闭";
        btnClose.UseVisualStyleBackColor = true;
        btnClose.Click += btnClose_Click;

        lblStatus.AutoSize = true;
        lblStatus.Location = new Point(12, 48);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(56, 17);
        lblStatus.TabIndex = 6;
        lblStatus.Text = "已保存";
        lblStatus.ForeColor = Color.Green;

        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(580, 600);
        MinimumSize = new Size(550, 380);
        Controls.Add(dgvItems);
        Controls.Add(panelButtons);
        Name = "DisplaySettingsForm";
        Text = "显示设置";
        StartPosition = FormStartPosition.CenterParent;
        ((System.ComponentModel.ISupportInitialize)dgvItems).EndInit();
        panelButtons.ResumeLayout(false);
        panelButtons.PerformLayout();
        ResumeLayout(false);
    }
}
