namespace BGShared.Models;

public class PatientInfoField
{
    public string FieldName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Label { get; set; } = "";
    public bool Show { get; set; } = true;
    public int Row { get; set; }
    public int Order { get; set; }
    public float XOffset { get; set; }
}

public class PatientInfoLayout
{
    public float FontSize { get; set; } = 10f;
    public float LineSpacing { get; set; } = 20f;
    public List<PatientInfoField> Fields { get; set; } = new();

    public static PatientInfoLayout CreateDefault()
    {
        return new PatientInfoLayout
        {
            FontSize = 10f,
            LineSpacing = 22f,
            Fields = new List<PatientInfoField>
            {
                new() { FieldName = "PatientName", DisplayName = "患者姓名", Label = "患者：", Show = true, Row = 1, Order = 1, XOffset = 0 },
                new() { FieldName = "Gender", DisplayName = "性别", Label = "性别：", Show = true, Row = 1, Order = 2, XOffset = 0 },
                new() { FieldName = "BedNo", DisplayName = "床号", Label = "床号：", Show = true, Row = 1, Order = 3, XOffset = 0 },
                new() { FieldName = "PatientId", DisplayName = "病案号", Label = "病案号：", Show = true, Row = 2, Order = 1, XOffset = 0 },
                new() { FieldName = "Department", DisplayName = "科室", Label = "科室：", Show = true, Row = 2, Order = 2, XOffset = 0 },
                new() { FieldName = "SpecimenNo", DisplayName = "样本编号", Label = "编号：", Show = true, Row = 2, Order = 3, XOffset = 0 },
                new() { FieldName = "SampleType", DisplayName = "样本类型", Label = "样本：", Show = true, Row = 2, Order = 4, XOffset = 0 },
                new() { FieldName = "TestTime", DisplayName = "报告时间", Label = "报告时间：", Show = true, Row = 3, Order = 1, XOffset = 0 },
            }
        };
    }
}

public class PrintTemplate
{
    public string TemplateName { get; set; } = "默认模板";
    
    public string PaperSize { get; set; } = "A4";
    public bool Landscape { get; set; } = false;
    public float LeftMargin { get; set; } = 40f;
    public float TopMargin { get; set; } = 50f;
    public float RightMargin { get; set; } = 40f;
    public float BottomMargin { get; set; } = 50f;
    
    public int ReportsPerPage { get; set; } = 2;
    
    public string HeaderTitle { get; set; } = "血气分析报告";
    public float HeaderFontSize { get; set; } = 18f;
    public bool HeaderBold { get; set; } = true;
    public float HeaderSpacing { get; set; } = 18f;
    
    public bool ShowPatientInfo { get; set; } = true;
    public float PatientInfoSpacing { get; set; } = 10f;
    public PatientInfoLayout PatientInfoLayout { get; set; } = PatientInfoLayout.CreateDefault();
    
    public bool UseTwoColumnLayout { get; set; } = false;
    public float ColumnSpacing { get; set; } = 30f;
    public float ResultTableSpacing { get; set; } = 10f;
    public float ItemFontSize { get; set; } = 10f;
    public bool ShowReferenceRange { get; set; } = true;
    public bool ShowUnit { get; set; } = true;
    public bool ShowResultFlag { get; set; } = true;
    public float RowHeight { get; set; } = 18f;

    public bool ShowFooter { get; set; } = true;
    public string FooterLeft { get; set; } = "说明：本报告结果仅对所测标本负责。";
    public string FooterRight { get; set; } = "";
    public float FooterFontSize { get; set; } = 10f;
    public float FooterSpacing { get; set; } = 10f;
}
