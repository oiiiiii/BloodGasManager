using System.Text.Json;
using BGShared.Models;

namespace BGShared.Config;

public static class DisplayConfigManager
{
    private const string ConfigFileName = "display_config.json";
    private static DisplayConfig? _config;
    private static string _configPath;

    static DisplayConfigManager()
    {
        string appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BGManager");
        
        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }
        
        _configPath = Path.Combine(appDataPath, ConfigFileName);
    }

    public static DisplayConfig Config
    {
        get
        {
            if (_config == null)
            {
                LoadConfig();
            }
            return _config;
        }
    }

    private static void LoadConfig()
    {
        string oldConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
        if (!File.Exists(_configPath) && File.Exists(oldConfigPath))
        {
            try
            {
                File.Copy(oldConfigPath, _configPath);
            }
            catch
            {
            }
        }
        
        if (File.Exists(_configPath))
        {
            try
            {
                var json = File.ReadAllText(_configPath);
                _config = JsonSerializer.Deserialize<DisplayConfig>(json) ?? LoadDefault();
            }
            catch
            {
                _config = LoadDefault();
                SaveConfig();
            }
        }
        else
        {
            _config = LoadDefault();
            SaveConfig();
        }
    }

    public static void SaveConfig()
    {
        if (_config != null)
        {
            var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configPath, json);
        }
    }

    public static void ReloadConfig()
    {
        _config = null;
    }

    public static void ImportConfig(string filePath)
    {
        if (File.Exists(filePath))
        {
            var json = File.ReadAllText(filePath);
            _config = JsonSerializer.Deserialize<DisplayConfig>(json) ?? LoadDefault();
            SaveConfig();
        }
    }

    public static void ExportConfig(string filePath)
    {
        var json = JsonSerializer.Serialize(_config ?? LoadDefault(), new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }

    private static DisplayConfig LoadDefault()
    {
        var config = new DisplayConfig
        {
            Items = new List<DisplayItem>
            {
                new() { TestName = "T", ChineseName = "体温", SortOrder = 1, IsVisible = true, Unit = "℃", Reference = "36--37", Low = 36, High = 37 },
                new() { TestName = "FIO2", ChineseName = "氧气浓度", SortOrder = 2, IsVisible = true, Unit = "%", Reference = "21--100", Low = 21, High = 100 },
                new() { TestName = "pH", ChineseName = "酸碱度", SortOrder = 3, IsVisible = true, Unit = "", Reference = "7.35--7.45", Low = 7.35, High = 7.45 },
                new() { TestName = "pCO2", ChineseName = "二氧化碳分压", SortOrder = 4, IsVisible = true, Unit = "mmHg", Reference = "35--45", Low = 35, High = 45 },
                new() { TestName = "pO2", ChineseName = "氧分压", SortOrder = 5, IsVisible = true, Unit = "mmHg", Reference = "83--108", Low = 83, High = 108 },
                new() { TestName = "tHb", ChineseName = "总血红蛋白", SortOrder = 6, IsVisible = true, Unit = "g/L", Reference = "114--175", Low = 114, High = 175 },
                new() { TestName = "sO2", ChineseName = "氧饱和度", SortOrder = 7, IsVisible = true, Unit = "%", Reference = "94--98", Low = 94, High = 98 },
                new() { TestName = "O2Hb", ChineseName = "氧合血红蛋白分数", SortOrder = 8, IsVisible = true, Unit = "%", Reference = "93--98", Low = 93, High = 98 },
                new() { TestName = "MetHb", ChineseName = "高铁血红蛋白分数", SortOrder = 9, IsVisible = true, Unit = "%", Reference = "0.04--1.52", Low = 0.04, High = 1.52 },
                new() { TestName = "COHb", ChineseName = "碳氧血红蛋白", SortOrder = 10, IsVisible = true, Unit = "%", Reference = "0.5--1.5", Low = 0.5, High = 1.5 },
                new() { TestName = "RHb", ChineseName = "还原血红蛋白分数", SortOrder = 11, IsVisible = true, Unit = "%", Reference = "2--7", Low = 2, High = 7 },
                new() { TestName = "HbF", ChineseName = "胎儿血红蛋白", SortOrder = 12, IsVisible = true, Unit = "%", Reference = "", Low = null, High = null },
                new() { TestName = "Hct", ChineseName = "红细胞压积", SortOrder = 13, IsVisible = true, Unit = "%", Reference = "37--49", Low = 37, High = 49 },
                new() { TestName = "K+", ChineseName = "钾", SortOrder = 14, IsVisible = true, Unit = "mmol/L", Reference = "3.4--4.4", Low = 3.4, High = 4.4 },
                new() { TestName = "Na+", ChineseName = "钠", SortOrder = 15, IsVisible = true, Unit = "mmol/L", Reference = "136--145", Low = 136, High = 145 },
                new() { TestName = "Cl-", ChineseName = "氯", SortOrder = 16, IsVisible = true, Unit = "mmol/L", Reference = "98--107", Low = 98, High = 107 },
                new() { TestName = "Ca++", ChineseName = "钙", SortOrder = 17, IsVisible = true, Unit = "mmol/L", Reference = "1.15--1.33", Low = 1.15, High = 1.33 },
                new() { TestName = "Glu", ChineseName = "血糖", SortOrder = 18, IsVisible = true, Unit = "mmol/L", Reference = "3.6--5.3", Low = 3.6, High = 5.3 },
                new() { TestName = "Lac", ChineseName = "乳酸", SortOrder = 19, IsVisible = true, Unit = "mmol/L", Reference = "0.36--0.75", Low = 0.36, High = 0.75 },
                new() { TestName = "cBase(Ecf,ox)", ChineseName = "标准碱剩余（氧合校正）", SortOrder = 20, IsVisible = true, Unit = "mmol/L", Reference = "-3--3", Low = -3, High = 3 },
                new() { TestName = "SBC", ChineseName = "标准碳酸氢盐浓度", SortOrder = 21, IsVisible = true, Unit = "mmol/L", Reference = "21.2--28.3", Low = 21.2, High = 28.3 },
                new() { TestName = "HCO3-", ChineseName = "血浆碳酸氢盐浓度", SortOrder = 23, IsVisible = true, Unit = "mmol/L", Reference = "21.2--28.3", Low = 21.2, High = 28.3 },
                new() { TestName = "tCO2(B)", ChineseName = "总二氧化碳（全血）", SortOrder = 24, IsVisible = true, Unit = "mmHg", Reference = "24--32", Low = 24, High = 32 },
                new() { TestName = "tCO2(P)", ChineseName = "总二氧化碳（血浆）", SortOrder = 25, IsVisible = true, Unit = "mmHg", Reference = "24--32", Low = 24, High = 32 },
                new() { TestName = "p50(st)", ChineseName = "标准半饱和氧分压", SortOrder = 25, IsVisible = true, Unit = "mmHg", Reference = "24--28", Low = 24, High = 28 },
                new() { TestName = "p50(act)", ChineseName = "实际半饱和氧分压", SortOrder = 26, IsVisible = true, Unit = "mmHg", Reference = "24--28", Low = 24, High = 28 },
                new() { TestName = "AaDpO2", ChineseName = "肺泡-动脉氧分压差", SortOrder = 27, IsVisible = true, Unit = "mmHg", Reference = "5--15", Low = 5, High = 15 },
                new() { TestName = "a/ApO2", ChineseName = "动脉/肺泡氧分压比", SortOrder = 29, IsVisible = true, Unit = "", Reference = "85--95", Low = 85, High = 95 },
                new() { TestName = "SBE", ChineseName = "标准碱剩余", SortOrder = 30, IsVisible = true, Unit = "mmol/L", Reference = "-3--3", Low = -3, High = 3 },
                new() { TestName = "ABE", ChineseName = "实际碱剩余", SortOrder = 33, IsVisible = true, Unit = "mmol/L", Reference = "-3--3", Low = -3, High = 3 },
                new() { TestName = "Anion gap", ChineseName = "阴离子间隙", SortOrder = 34, IsVisible = true, Unit = "mmol/L", Reference = "8--16", Low = 8, High = 16 },
                new() { TestName = "Anion gap (K+)", ChineseName = "含K阴离子间隙", SortOrder = 35, IsVisible = true, Unit = "mmol/L", Reference = "10--20", Low = 10, High = 20 },
                new() { TestName = "mOsm", ChineseName = "渗透压", SortOrder = 36, IsVisible = true, Unit = "mOsm/kg", Reference = "280--320", Low = 280, High = 320 },
                new() { TestName = "tO2", ChineseName = "总氧含量", SortOrder = 37, IsVisible = true, Unit = "%", Reference = "16--22", Low = 16, High = 22 },
                new() { TestName = "pO2(a)/FIO2", ChineseName = "氧合指数", SortOrder = 37, IsVisible = true, Unit = "", Reference = "400--500", Low = 400, High = 500 },
                new() { TestName = "pO2(a,T)/FIO2", ChineseName = "氧合指数（体温校正）", SortOrder = 38, IsVisible = true, Unit = "", Reference = "400--500", Low = 400, High = 500 },
            }
        };
        return config;
    }

    public static DisplayItem? GetItem(string testName)
    {
        return Config.Items.FirstOrDefault(i => i.TestName.Equals(testName, StringComparison.OrdinalIgnoreCase));
    }

    public static List<DisplayItem> GetAllItems()
    {
        return Config.Items
            .OrderBy(i => i.SortOrder)
            .ToList();
    }

    public static List<DisplayItem> GetVisibleItems()
    {
        return Config.Items
            .Where(i => i.IsVisible)
            .OrderBy(i => i.SortOrder)
            .ToList();
    }
}
