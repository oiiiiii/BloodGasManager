namespace BGShared.Models;

public class DisplayItem
{
    public string TestName { get; set; } = "";
    public string ChineseName { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    public string Unit { get; set; } = "";
    public string Reference { get; set; } = "";
    public double? Low { get; set; }
    public double? High { get; set; }
}

public class DisplayConfig
{
    public List<DisplayItem> Items { get; set; } = new();
    public int Version { get; set; } = 1;
}
