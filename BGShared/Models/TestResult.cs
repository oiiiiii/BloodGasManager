namespace BGShared.Models;

public class TestResult
{
    public long Id { get; set; }
    public string SpecimenId { get; set; } = "";
    public string TestName { get; set; } = "";
    public string TestCode { get; set; } = "";
    public double? Value { get; set; }
    public string RawValue { get; set; } = "";
    public string Unit { get; set; } = "";
    public ResultFlag Flag { get; set; } = ResultFlag.Normal;
    public double? MinRange { get; set; }
    public double? MaxRange { get; set; }
    public string ResultType { get; set; } = "M";
    public DateTime CreatedAt { get; set; }
}
