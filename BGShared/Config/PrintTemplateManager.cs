using System.Text.Json;
using BGShared.Models;

namespace BGShared.Config;

public static class PrintTemplateManager
{
    private const string ConfigFileName = "print_template.json";
    private static PrintTemplate? _currentTemplate;
    private static string _configPath;

    static PrintTemplateManager()
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

    public static PrintTemplate CurrentTemplate
    {
        get
        {
            if (_currentTemplate == null)
            {
                LoadTemplate();
            }
            return _currentTemplate!;
        }
    }

    private static void LoadTemplate()
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
                var template = JsonSerializer.Deserialize<PrintTemplate>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                if (template != null && (template.PatientInfoLayout == null || template.PatientInfoLayout.Fields.Count == 0))
                {
                    template.PatientInfoLayout = PatientInfoLayout.CreateDefault();
                }
                
                _currentTemplate = template ?? CreateDefaultTemplate();
            }
            catch
            {
                _currentTemplate = CreateDefaultTemplate();
                SaveTemplate(_currentTemplate);
            }
        }
        else
        {
            _currentTemplate = CreateDefaultTemplate();
            SaveTemplate(_currentTemplate);
        }
    }

    private static PrintTemplate CreateDefaultTemplate()
    {
        return new PrintTemplate
        {
            TemplateName = "默认模板",
            
            PaperSize = "A4",
            Landscape = false,
            LeftMargin = 40f,
            TopMargin = 50f,
            RightMargin = 40f,
            BottomMargin = 50f,
            
            ReportsPerPage = 2,
            
            HeaderTitle = "血气分析报告",
            HeaderFontSize = 18f,
            HeaderBold = true,
            HeaderSpacing = 18f,
            
            ShowPatientInfo = true,
            PatientInfoSpacing = 10f,
            PatientInfoLayout = PatientInfoLayout.CreateDefault(),
            
            UseTwoColumnLayout = false,
            ColumnSpacing = 30f,
            ResultTableSpacing = 10f,
            ItemFontSize = 10f,
            ShowReferenceRange = true,
            ShowUnit = true,
            ShowResultFlag = true,
            RowHeight = 18f,

            ShowFooter = true,
            FooterLeft = "说明：本报告结果仅对所测标本负责。",
            FooterRight = "",
            FooterFontSize = 10f,
            FooterSpacing = 10f
        };
    }

    public static void SaveTemplate(PrintTemplate template)
    {
        _currentTemplate = template;
        var json = JsonSerializer.Serialize(template, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(_configPath, json);
    }

    public static void ExportTemplate(string filePath)
    {
        var json = JsonSerializer.Serialize(_currentTemplate ?? CreateDefaultTemplate(), new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(filePath, json);
    }

    public static PrintTemplate? ImportTemplate(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        try
        {
            var json = File.ReadAllText(filePath);
            var template = JsonSerializer.Deserialize<PrintTemplate>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            if (template != null && (template.PatientInfoLayout == null || template.PatientInfoLayout.Fields.Count == 0))
            {
                template.PatientInfoLayout = PatientInfoLayout.CreateDefault();
            }
            
            return template;
        }
        catch
        {
            return null;
        }
    }

    public static void ResetToDefault()
    {
        _currentTemplate = CreateDefaultTemplate();
        SaveTemplate(_currentTemplate);
    }

    public static void ReloadTemplate()
    {
        _currentTemplate = null;
    }
}
