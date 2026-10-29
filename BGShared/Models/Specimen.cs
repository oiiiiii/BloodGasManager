namespace BGShared.Models;

public class Specimen
{
    public long Id { get; set; }
    public string SpecimenId { get; set; } = "";
    public string PatientId { get; set; } = "";
    public string PatientName { get; set; } = "";
    public string SpecimenNo { get; set; } = "";
    public string SampleType { get; set; } = "";
    public string Source { get; set; } = "";
    public string BedNo { get; set; } = "";
    public string Department { get; set; } = "";
    public string Gender { get; set; } = "";
    public string OriginalPatientId { get; set; } = "";
    public DateTime? TestTime { get; set; }
    public DateTime ReceiveTime { get; set; }
    public SpecimenStatus Status { get; set; } = SpecimenStatus.Pending;
    public DateTime? AuditTime { get; set; }
    public string AuditUser { get; set; } = "";
    public string RawMessagePath { get; set; } = "";
    public string Remark { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public string SnapshotPatientName { get; set; } = "";
    public string SnapshotBedNo { get; set; } = "";
    public string SnapshotPatientId { get; set; } = "";
    public string SnapshotNameAbbreviation { get; set; } = "";
    public string SnapshotDepartment { get; set; } = "";
    public string SnapshotGender { get; set; } = "";
    public string SnapshotSampleType { get; set; } = "";
    public string SnapshotRemark { get; set; } = "";

    public List<TestResult> Results { get; set; } = new();
}
