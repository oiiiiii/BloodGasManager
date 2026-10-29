namespace BGShared.Models;

public enum SpecimenStatus
{
    Pending = 0,
    Audited = 1,
    Archived = 2,
    Deleted = 3
}

public enum ResultFlag
{
    Normal = 0,
    Low = 1,
    High = 2,
    CriticalLow = 3,
    CriticalHigh = 4
}
