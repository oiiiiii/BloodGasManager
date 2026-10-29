using BGShared.Models;
using BGShared.Utils;

namespace BGService.Parser;

public class ParsedASTMMessage
{
    public string RawMessage { get; set; } = "";
    public string PatientId { get; set; } = "";
    public string PatientName { get; set; } = "";
    public string Gender { get; set; } = "";
    public string SpecimenNo { get; set; } = "";
    public string SampleType { get; set; } = "";
    public string Source { get; set; } = "";
    public string BedNo { get; set; } = "";
    public string Department { get; set; } = "";
    public DateTime? TestTime { get; set; } = null;
    public DateTime? MessageTime { get; set; } = null;
    public List<ParsedResult> Results { get; set; } = new();
    public string SpecimenId => SpecimenNo;
}

public class ParsedResult
{
    public string TestName { get; set; } = "";
    public string TestCode { get; set; } = "";
    public string RawValue { get; set; } = "";
    public double? Value { get; set; }
    public string Unit { get; set; } = "";
    public string Flag { get; set; } = "";
    public string Range { get; set; } = "";
    public string ResultType { get; set; } = "M";
    public DateTime? TestTime { get; set; }
}

public static class ASTMParser
{
    public static ParsedASTMMessage Parse(string message)
    {
        var result = new ParsedASTMMessage { RawMessage = message };

        var records = message.Split('\r', StringSplitOptions.RemoveEmptyEntries);

        foreach (var record in records)
        {
            if (string.IsNullOrWhiteSpace(record)) continue;

            var fields = record.Split('|');
            if (fields.Length == 0) continue;

            string recordType = GetRecordType(fields[0]);

            switch (recordType)
            {
                case "H":
                    ParseHeader(fields, result);
                    break;
                case "P":
                    ParsePatient(fields, result);
                    break;
                case "O":
                    ParseOrder(fields, result);
                    break;
                case "R":
                    var r = ParseResult(fields);
                    if (r != null) result.Results.Add(r);
                    break;
                case "C":
                    break;
                case "L":
                    break;
            }
        }

        var firstResultTime = result.Results.FirstOrDefault(r => r.TestTime.HasValue)?.TestTime;
        if (firstResultTime.HasValue)
            result.TestTime = firstResultTime.Value;

        return result;
    }

    private static string GetRecordType(string firstField)
    {
        if (string.IsNullOrEmpty(firstField)) return "";
        if (firstField.Length >= 2 && char.IsDigit(firstField[0]))
            return firstField.Substring(1, 1);
        return firstField;
    }

    private static void ParseHeader(string[] fields, ParsedASTMMessage result)
    {
        if (fields.Length > 13)
        {
            result.MessageTime = DateTimeExtensions.ParseDateTime(fields[13].SafeTrim());
            if (result.TestTime == null)
                result.TestTime = result.MessageTime;
        }
    }

    private static void ParsePatient(string[] fields, ParsedASTMMessage result)
    {
        if (fields.Length > 3)
            result.PatientId = fields[3].SafeTrim();

        if (fields.Length > 5)
        {
            var nameParts = fields[5].Split('^');
            if (nameParts.Length > 0)
                result.PatientName = nameParts[0].SafeTrim();
        }

        if (fields.Length > 8)
            result.Gender = fields[8].SafeTrim();
    }

    private static void ParseOrder(string[] fields, ParsedASTMMessage result)
    {
        if (fields.Length > 3)
        {
            var sampleIdParts = fields[3].Split('^');
            if (sampleIdParts.Length > 1)
                result.SpecimenNo = sampleIdParts[1].SafeTrim();
            else if (sampleIdParts.Length > 0)
                result.SpecimenNo = sampleIdParts[0].SafeTrim();
        }

        // 解析标本来源 (fields[4] 或 fields[其他])
        if (fields.Length > 4)
        {
            var sourceParts = fields[4].Split('^');
            // 取最后一个非空部分作为来源
            for (int i = sourceParts.Length - 1; i >= 0; i--)
            {
                var part = sourceParts[i].SafeTrim();
                if (!string.IsNullOrEmpty(part))
                {
                    result.Source = part;
                    break;
                }
            }
        }

        if (fields.Length > 13)
        {
            var sampleTypeParts = fields[13].Split('^');
            if (sampleTypeParts.Length > 0)
            {
                string sampleType = sampleTypeParts[0].SafeTrim();
                result.SampleType = sampleType switch
                {
                    "ART" or "A" or "ARTERIAL" or "动脉" => "动脉",
                    "VEN" or "V" or "VENOUS" or "静脉" => "静脉",
                    "MIX" or "混合静脉" => "混合静脉",
                    _ => ""
                };
            }
        }

        if (fields.Length > 15 && string.IsNullOrEmpty(result.SampleType))
        {
            var typeParts = fields[15].Split('^');
            if (typeParts.Length > 0)
            {
                string sampleType = typeParts[0].SafeTrim();
                result.SampleType = sampleType switch
                {
                    "ART" or "A" or "ARTERIAL" or "动脉" => "动脉",
                    "VEN" or "V" or "VENOUS" or "静脉" => "静脉",
                    "MIX" or "混合静脉" => "混合静脉",
                    _ => ""
                };
            }
        }
    }

    private static ParsedResult? ParseResult(string[] fields)
    {
        var r = new ParsedResult();

        if (fields.Length > 2)
        {
            var testParts = fields[2].Split('^');
            if (testParts.Length > 3)
                r.TestName = testParts[3].SafeTrim();
            if (testParts.Length > 4)
                r.ResultType = testParts[4].SafeTrim();
            r.TestCode = r.TestName;
        }

        if (fields.Length > 3)
        {
            r.RawValue = fields[3].SafeTrim();
            r.Value = ValueParser.ParseDouble(r.RawValue);
        }

        if (fields.Length > 4)
            r.Unit = fields[4].SafeTrim();

        if (fields.Length > 5)
            r.Range = fields[5].SafeTrim();

        if (fields.Length > 6)
        {
            var flagStr = fields[6].SafeTrim().ToUpper();
            r.Flag = flagStr;
        }

        if (fields.Length > 11)
        {
            r.TestTime = DateTimeExtensions.ParseDateTime(fields[11].SafeTrim());
        }

        if (string.IsNullOrEmpty(r.TestCode) && string.IsNullOrEmpty(r.TestName))
            return null;

        return r;
    }
}
