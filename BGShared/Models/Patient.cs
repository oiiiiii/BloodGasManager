namespace BGShared.Models;

public class Patient
{
    public string PatientId { get; set; } = "";
    public string PatientName { get; set; } = "";
    public string NameAbbreviation { get; set; } = "";
    public string Gender { get; set; } = "";
    public int? Age { get; set; }
    public string BedNo { get; set; } = "";
    public string Department { get; set; } = "";
    public string Doctor { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Diagnosis { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public string Remark { get; set; } = "";
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
