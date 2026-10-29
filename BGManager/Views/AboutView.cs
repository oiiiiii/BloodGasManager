using System.Reflection;
using System.IO;

namespace BGManager.Views;

public class AboutView : UserControl
{
    public AboutView()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(249, 250, 251);
        BuildUI();
    }

    private void BuildUI()
    {
        var mainPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(249, 250, 251),
            AutoScroll = true
        };
        Controls.Add(mainPanel);

        var card = new Panel
        {
            Width = 1240,
            Height = 930,
            BackColor = Color.White,
            Padding = new Padding(40),
            BorderStyle = BorderStyle.FixedSingle
        };
        mainPanel.Controls.Add(card);

        // 双栏：严格 50/50 均分，左栏血气解读方法，右栏其余内容
        var columns = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        columns.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        card.Controls.Add(columns);

        var leftCol = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 12, 0) };
        var rightCol = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 0, 0, 0) };
        columns.Controls.Add(leftCol, 0, 0);
        columns.Controls.Add(rightCol, 1, 0);

        var headerPanel = new Panel { Dock = DockStyle.Top, Height = 100 };
        rightCol.Controls.Add(headerPanel);

        var logoPanel = new Panel
        {
            Width = 64,
            Height = 64,
            Left = 0,
            Top = 18,
            BackColor = Color.FromArgb(59, 130, 246)
        };
        headerPanel.Controls.Add(logoPanel);

        var iconLabel = new Label
        {
            Text = "BGS",
            Font = new Font("微软雅黑", 20f, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(14, 15),
            AutoSize = true
        };
        logoPanel.Controls.Add(iconLabel);

        var infoPanel = new Panel { Left = 80, Top = 10, Width = 400 };
        headerPanel.Controls.Add(infoPanel);

        var lblAppName = new Label
        {
            Text = "血气分析管理系统",
            Font = new Font("微软雅黑", 20f, FontStyle.Bold),
            AutoSize = true,
            ForeColor = Color.FromArgb(15, 23, 42)
        };
        infoPanel.Controls.Add(lblAppName);

        var version = GetVersionInfo();
        var lblBuild = new Label
        {
            Text = $"编译日期: {version.BuildDate}",
            Font = new Font("微软雅黑", 11f),
            AutoSize = true,
            Top = 38,
            ForeColor = Color.FromArgb(100, 116, 139)
        };
        infoPanel.Controls.Add(lblBuild);

        var line = new Panel
        {
            Dock = DockStyle.Top,
            Height = 1,
            BackColor = Color.FromArgb(226, 232, 240)
        };
        rightCol.Controls.Add(line);

        var featuresGroup = new GroupBox
        {
            Text = "系统特性",
            Dock = DockStyle.Top,
            Height = 290,
            Font = new Font("微软雅黑", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59)
        };
        rightCol.Controls.Add(featuresGroup);

        var featuresLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(15, 12, 15, 12)
        };
        featuresLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        featuresLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        for (int i = 0; i < 5; i++)
            featuresLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));
        featuresGroup.Controls.Add(featuresLayout);

        AddFeatureItem(featuresLayout, "📡", "ASTM LIS2-A2 协议支持", 0, 0);
        AddFeatureItem(featuresLayout, "🖥️", "服务端/客户端分离架构", 0, 1);
        AddFeatureItem(featuresLayout, "💾", "SQLite 本地数据库", 1, 0);
        AddFeatureItem(featuresLayout, "🔑", "API Key 安全认证", 1, 1);
        AddFeatureItem(featuresLayout, "⚡", "实时数据接收", 2, 0);
        AddFeatureItem(featuresLayout, "📊", "历史数据查询统计", 2, 1);
        AddFeatureItem(featuresLayout, "🖨️", "报告打印预览", 3, 0);
        AddFeatureItem(featuresLayout, "⌨️", "自定义快捷键", 3, 1);
        AddFeatureItem(featuresLayout, "🔄", "数据备份恢复", 4, 0);
        AddFeatureItem(featuresLayout, "📱", "支持局域网访问", 4, 1);

        var usageGroup = new GroupBox
        {
            Text = "使用说明",
            Dock = DockStyle.Top,
            Height = 280,
            Font = new Font("微软雅黑", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59)
        };
        rightCol.Controls.Add(usageGroup);

        var usageBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("微软雅黑", 9.5f),
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(51, 65, 85),
            DetectUrls = false
        };
        usageBox.Text =
            "一、启动与连接\r\n" +
            "  1. 启动 BGService 服务程序（建议设置为开机自启），服务会在后台监听串口与网络端口。\r\n" +
            "  2. 打开 BGManager 管理端，程序会自动连接本地服务；连接成功后状态栏会显示连接状态。\r\n" +
            "  3. 若需在局域网内其他电脑使用，请在“设置”中填写服务端 IP 地址，并保证 API Key 与服务端一致。\r\n" +
            "\r\n" +
            "二、主界面操作\r\n" +
            "  1. 左侧为当日标本列表：红色条目表示待处理，绿色表示已完成，点击条目查看详情。\r\n" +
            "  2. 中间为患者信息区，可修改姓名、床号、住院号、科室等内容，修改后自动同步到服务端。\r\n" +
            "  3. 右侧为检测结果表：勾选需要复制的项目；点击“全选/全不选”“反选”可快速调整选择，\r\n" +
            "     点击“仅勾选异常”只勾选超出参考范围的项目。\r\n" +
            "  4. 点击“复制”把已勾选结果复制到剪贴板，方便粘贴到病历或聊天工具；点击“预览”查看并打印报告。\r\n" +
            "\r\n" +
            "三、查询与统计\r\n" +
            "  1. 进入“查询”页，可按日期范围、患者姓名、住院号、标本号等条件检索历史记录。\r\n" +
            "  2. 查询结果支持分页浏览，并可直接打印报告。\r\n" +
            "\r\n" +
            "四、设置与维护\r\n" +
            "  1. “设置”页可配置串口参数、显示项目、参考范围、打印模板与快捷键。\r\n" +
            "  2. “数据备份”支持手动备份与恢复数据库，系统也会按计划自动备份。\r\n" +
            "  3. 常用快捷键：F5 刷新、F6 审核、Delete 删除，具体可在快捷键设置中调整。\r\n" +
            "\r\n" +
            "五、常见问题\r\n" +
            "  1. 收不到数据：请检查串口是否被其他程序占用，并确认波特率与仪器设置一致。\r\n" +
            "  2. 提示认证失败：请核对服务端与客户端配置的 API Key 是否一致。\r\n" +
            "  3. 无法连接服务：请确认 BGService 正在运行，且防火墙已放行对应端口。";
        usageGroup.Controls.Add(usageBox);

        var abgGroup = new GroupBox
        {
            Text = "血气分析解读方法",
            Dock = DockStyle.Fill,
            Font = new Font("微软雅黑", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59)
        };
        leftCol.Controls.Add(abgGroup);

        var abgBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("微软雅黑", 9.5f),
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(51, 65, 85),
            DetectUrls = false
        };
        abgBox.Text =
            "一、解读流程（六步法）\r\n" +
            "  1. 数据一致性校验：按 Henderson-Hasselbalch 公式 [H+] = 24 × pCO2 ÷ HCO3- 验证三项数据是否匹配，\r\n" +
            "     偏差过大提示数据可疑，建议复查。\r\n" +
            "  2. 酸碱血症判断：pH < 7.35 为酸血症，pH > 7.45 为碱血症。\r\n" +
            "  3. 原发紊乱判断：酸血症时 HCO3- 降低提示代谢性酸中毒、pCO2 升高提示呼吸性酸中毒；\r\n" +
            "     碱血症时 HCO3- 升高提示代谢性碱中毒、pCO2 降低提示呼吸性碱中毒。\r\n" +
            "  4. 代偿评估：将实测代偿值与预期公式比较——\r\n" +
            "       代酸：预期 pCO2 = 1.5×HCO3- + 8 ± 2（Winter 公式）\r\n" +
            "       代碱：预期 pCO2 = 0.7×(HCO3- − 24) + 40 ± 5\r\n" +
            "       呼酸：急性 HCO3- = 24 + (pCO2−40)×0.1；慢性 ×0.35\r\n" +
            "       呼碱：急性 HCO3- = 24 − (40−pCO2)×0.2；慢性 ×0.5\r\n" +
            "     超出预期范围提示存在第二种原发酸碱紊乱。\r\n" +
            "  5. 阴离子间隙（AG = Na+ − Cl− − HCO3-）：AG > 16 为高 AG 型代酸；\r\n" +
            "     计算 Delta Ratio = (AG−12) ÷ (24−HCO3-)：< 0.4 合并高氯性代酸，1–2 典型高 AG 型，\r\n" +
            "     > 2 合并代谢性碱中毒；三者并存为三重酸碱失衡。\r\n" +
            "  6. 氧合评估：氧合指数 P/F = pO2 ÷ (FIO2÷100)，< 300 提示氧合障碍；\r\n" +
            "     pO2 < 60 mmHg 为呼吸衰竭——合并 pCO2 > 50 为 Ⅱ 型，否则为 Ⅰ 型。\r\n" +
            "\r\n" +
            "二、电解质与渗透压解读\r\n" +
            "  1. 血钾（3.4–4.4 mmol/L）：低于下限为低钾血症，高于上限为高钾血症；\r\n" +
            "     酸血症时细胞内钾外移，pH 每下降 0.1 血钾约升高 0.6 mmol/L，纠正酸碱紊乱后应复查。\r\n" +
            "  2. 血钠（136–145 mmol/L）：低于下限为低钠血症，高于上限为高钠血症；\r\n" +
            "     高血糖导致稀释性低钠，血糖每升高 5.5 mmol/L 实测钠约降低 1.6，\r\n" +
            "     需看校正钠 = 实测钠 + 1.6×(血糖−5.5)÷5.5。\r\n" +
            "  3. 血氯（98–107 mmol/L）：低于下限为低氯血症，高于上限为高氯血症；\r\n" +
            "     高氯伴 AG 正常时支持高氯性（正常 AG 型）代谢性酸中毒。\r\n" +
            "  4. 离子钙（1.15–1.33 mmol/L）：低于下限为低钙血症，高于上限为高钙血症；\r\n" +
            "     受 pH 影响，pH 每升高 0.1 离子钙约下降 0.045 mmol/L。\r\n" +
            "  5. 渗透压 mOsm（280–320 mOsm/kg）：低于下限为低渗透压，高于上限为高渗透压；\r\n" +
            "     高渗合并高血糖时提示高渗性高血糖状态可能。\r\n" +
            "\r\n" +
            "三、其他伴随指标\r\n" +
            "  乳酸 > 2.0 为高乳酸血症，≥ 4.0 为重度；COHb > 1.5% 提示一氧化碳暴露；\r\n" +
            "  MetHb > 1.5% 提示高铁血红蛋白血症可能。\r\n" +
            "\r\n" +
            "四、注意事项\r\n" +
            "  1. 以上参考范围以本系统预置值为准，与仪器或检验科标准可能略有差异。\r\n" +
            "  2. 静脉血、混合静脉血的参考范围与动脉血不同，解读时需注意标本类型。\r\n" +
            "  3. 自动解读结果仅供临床参考，最终诊断请结合病史与其他检查综合判断。";
        abgGroup.Controls.Add(abgBox);

        var updateGroup = new GroupBox
        {
            Text = "更新日志",
            Dock = DockStyle.Top,
            Height = 120,
            Font = new Font("微软雅黑", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59)
        };
        rightCol.Controls.Add(updateGroup);

        var updateList = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("微软雅黑", 9f),
            BorderStyle = BorderStyle.None,
            Padding = new Padding(10)
        };
        updateList.Items.Add("• 修复患者匹配逻辑，确保精确匹配病案号");
        updateList.Items.Add("• 添加全局快捷键支持（F5刷新、F6审核、Del删除等）");
        updateList.Items.Add("• 优化API请求重试机制，提升网络稳定性");
        updateList.Items.Add("• 新增数据库索引，提升查询性能");
        updateList.Items.Add("• 支持旧数据库自动迁移升级");
        updateGroup.Controls.Add(updateList);

        var footerPanel = new Panel { Dock = DockStyle.Bottom, Height = 40 };
        rightCol.Controls.Add(footerPanel);

        var copyright = new Label
        {
            Text = "© 2024 血气分析管理系统. All rights reserved.",
            Font = new Font("微软雅黑", 9f),
            AutoSize = true,
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(0, 12)
        };
        footerPanel.Controls.Add(copyright);

        void LayoutCard()
        {
            // 卡片宽度跟随窗口自适应（1000~1600），两栏始终均分
            int avail = mainPanel.ClientSize.Width;
            if (avail > 60)
                card.Width = Math.Max(1000, Math.Min(1600, avail - 40));
            card.Left = Math.Max(10, (avail - card.Width) / 2);
            card.Top = Math.Max(10, (mainPanel.ClientSize.Height - card.Height) / 2);
        }

        mainPanel.SizeChanged += (s, e) => LayoutCard();
        LayoutCard();
    }

    private void AddFeatureItem(TableLayoutPanel layout, string icon, string text, int row, int col)
    {
        var lbl = new Label
        {
            Text = $"{icon} {text}",
            Dock = DockStyle.Fill,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("微软雅黑", 10f),
            ForeColor = Color.FromArgb(51, 65, 85),
            Padding = new Padding(8, 0, 5, 0)
        };
        layout.Controls.Add(lbl, col, row);
    }

    private (string Version, string BuildDate) GetVersionInfo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        var versionStr = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "2.0.0";

        string buildDate = "未知";
        try
        {
            var fileInfo = new FileInfo(assembly.Location);
            buildDate = fileInfo.LastWriteTime.ToString("yyyy-MM-dd");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取编译日期失败: {ex.Message}");
        }

        return (versionStr, buildDate);
    }
}