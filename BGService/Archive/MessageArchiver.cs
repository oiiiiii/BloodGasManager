using BGShared.Utils;
using BGService.Parser;

namespace BGService.Archive;

public class MessageArchiver
{
    public string ArchivePath { get; set; } = "archive";
    public bool SaveHl7 { get; set; } = true;
    public bool SaveJson { get; set; } = true;

    public MessageArchiver()
    {
    }

    public MessageArchiver(string archivePath, bool saveHl7 = true, bool saveJson = true)
    {
        ArchivePath = archivePath;
        SaveHl7 = saveHl7;
        SaveJson = saveJson;
    }

    public string SaveMessage(ParsedASTMMessage parsed, string rawMessage, byte[] rawBytes)
    {
        var receiveTime = DateTime.Now;
        var specimenId = string.IsNullOrEmpty(parsed.SpecimenNo)
            ? $"unknown_{receiveTime.Ticks}"
            : parsed.SpecimenNo;

        string dateDir = Path.Combine(ArchivePath, receiveTime.ToString("yyyyMM"));
        if (!Directory.Exists(dateDir))
            Directory.CreateDirectory(dateDir);

        string fileBase = $"{receiveTime:yyyyMMdd_HHmmss_fff}_{SanitizeFileName(specimenId)}";

        if (SaveHl7)
        {
            string hl7Path = Path.Combine(dateDir, $"{fileBase}.hl7");
            File.WriteAllBytes(hl7Path, rawBytes);
        }

        if (SaveJson)
        {
            string jsonPath = Path.Combine(dateDir, $"{fileBase}.json");
            var jsonObj = new
            {
                receiveTime = receiveTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                specimenId = parsed.SpecimenNo,
                patientId = parsed.PatientId,
                patientName = parsed.PatientName,
                sampleType = parsed.SampleType,
                bedNo = parsed.BedNo,
                department = parsed.Department,
                testTime = parsed.TestTime?.ToString("yyyy-MM-dd HH:mm:ss"),
                results = parsed.Results.Select(r => new
                {
                    testCode = r.TestCode,
                    testName = r.TestName,
                    value = r.Value,
                    rawValue = r.RawValue,
                    unit = r.Unit,
                    flag = r.Flag,
                    range = r.Range,
                    resultType = r.ResultType
                }).ToList()
            };
            File.WriteAllText(jsonPath, JsonOptions.Serialize(jsonObj));
        }

        return Path.Combine(dateDir, fileBase);
    }

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "unknown";
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }
}
