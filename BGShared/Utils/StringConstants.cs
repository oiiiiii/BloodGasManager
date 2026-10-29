namespace BGShared.Utils;

public static class ApiPaths
{
    public const string Specimens = "/specimens";
    public const string SpecimensById = "/specimens/{id}";
    public const string SpecimensLatest = "/specimens/latest/{count}";
    public const string SpecimensQuery = "/specimens/query";
    public const string SpecimensAudit = "/specimens/{id}/audit";
    public const string SpecimensUnarchive = "/specimens/{id}/unarchive";
    
    public const string Patients = "/patients";
    public const string PatientsById = "/patients/{id}";
    public const string PatientsSearch = "/patients/search";
    public const string PatientsFind = "/patients/find";
    
    public const string Status = "/status";
    public const string StatusDailyStats = "/status/stats/daily";
    
    public const string Config = "/config";
    public const string ConfigByKey = "/config/{key}";
    
    public const string Admin = "/admin";
    public const string AdminRestart = "/admin/restart";
    public const string AdminReparse = "/admin/reparse";
    public const string AdminInject = "/admin/inject";
    
    public const string Health = "/health";
}

public static class DbTables
{
    public const string Specimens = "specimens";
    public const string TestResults = "test_results";
    public const string Patients = "patients";
    public const string SystemConfig = "system_config";
    public const string SystemLogs = "system_logs";
    public const string ServiceStatus = "service_status";
    public const string SchemaVersion = "schema_version";
}

public static class DbColumns
{
    public const string Id = "id";
    public const string SpecimenId = "specimen_id";
    public const string PatientId = "patient_id";
    public const string PatientName = "patient_name";
    public const string SpecimenNo = "specimen_no";
    public const string SampleType = "sample_type";
    public const string BedNo = "bed_no";
    public const string Department = "department";
    public const string Gender = "gender";
    public const string TestTime = "test_time";
    public const string ReceiveTime = "receive_time";
    public const string Status = "status";
    public const string AuditTime = "audit_time";
    public const string AuditUser = "audit_user";
    public const string RawMessagePath = "raw_message_path";
    public const string Remark = "remark";
    public const string CreatedAt = "created_at";
    public const string UpdatedAt = "updated_at";
    
    public const string OriginalPatientId = "original_patient_id";
    public const string SnapshotPatientName = "snapshot_patient_name";
    public const string SnapshotBedNo = "snapshot_bed_no";
    public const string SnapshotPatientId = "snapshot_patient_id";
    public const string SnapshotNameAbbreviation = "snapshot_name_abbreviation";
    public const string SnapshotDepartment = "snapshot_department";
    public const string SnapshotGender = "snapshot_gender";
    public const string SnapshotSampleType = "snapshot_sample_type";
    public const string SnapshotRemark = "snapshot_remark";
    
    public const string NameAbbreviation = "name_abbreviation";
    public const string Age = "age";
    public const string Doctor = "doctor";
    public const string Phone = "phone";
    public const string Diagnosis = "diagnosis";
    public const string IsActive = "is_active";
    
    public const string Level = "level";
    public const string Category = "category";
    public const string Message = "message";
    public const string Detail = "detail";
    
    public const string IsRunning = "is_running";
    public const string SerialPort = "serial_port";
    public const string BaudRate = "baud_rate";
    public const string LastHeartbeat = "last_heartbeat";
    public const string TotalSpecimens = "total_specimens";
    public const string TodaySpecimens = "today_specimens";
    public const string LastMessageTime = "last_message_time";
    public const string LastError = "last_error";
    public const string Version = "version";
}

public static class StatusMessages
{
    public const string Pending = "待审核";
    public const string Audited = "已审核";
    public const string Archived = "已归档";
    
    public const string Running = "运行中";
    public const string Stopped = "已停止";
}

public static class LogCategories
{
    public const string Service = "service";
    public const string Serial = "serial";
    public const string Parser = "parser";
    public const string Api = "api";
    public const string Database = "database";
    public const string Archive = "archive";
}

public static class LogLevels
{
    public const string Info = "INFO";
    public const string Warning = "WARNING";
    public const string Error = "ERROR";
    public const string Debug = "DEBUG";
}