using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BGShared.Utils;

public static class DateTimeExtensions
{
    public static string ToStandardFormat(this DateTime dt)
    {
        return dt.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public static string ToFileFormat(this DateTime dt)
    {
        return dt.ToString("yyyyMMdd_HHmmss_fff");
    }

    public static string ToDateOnly(this DateTime dt)
    {
        return dt.ToString("yyyy-MM-dd");
    }

    public static DateTime? ParseDateTime(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;

        string[] formats = {
            "yyyyMMddHHmmss",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy/MM/dd HH:mm:ss",
            "yyyyMMddHHmm",
            "yyyy-MM-dd",
            "yyyy/MM/dd"
        };

        if (DateTime.TryParseExact(s.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt;

        if (DateTime.TryParse(s, out var dt2))
            return dt2;

        return null;
    }
}

public static class StringExtensions
{
    public static string SafeTrim(this string s)
    {
        return string.IsNullOrWhiteSpace(s) ? string.Empty : s.Trim();
    }

    public static string EmptyIfNull(this string s)
    {
        return s ?? string.Empty;
    }

    public static bool IsEmpty(this string s)
    {
        return string.IsNullOrWhiteSpace(s);
    }

    public static bool HasValue(this string s)
    {
        return !string.IsNullOrWhiteSpace(s);
    }

    public static string Truncate(this string s, int maxLen)
    {
        if (string.IsNullOrEmpty(s) || s.Length <= maxLen) return s;
        return s.Substring(0, maxLen);
    }
}

public static class ValueParser
{
    public static double? ParseDouble(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        string cleaned = s.Trim().Trim('?').Trim();
        if (string.IsNullOrEmpty(cleaned)) return null;
        if (double.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
            return result;
        return null;
    }

    public static int? ParseInt(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        string cleaned = s.Trim().Trim('?').Trim();
        if (int.TryParse(cleaned, out var result))
            return result;
        return null;
    }

    public static (double? Min, double? Max) ParseRange(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return (null, null);
        var parts = s.Split('-', '~', '–');
        if (parts.Length == 2)
        {
            var min = ParseDouble(parts[0]);
            var max = ParseDouble(parts[1]);
            return (min, max);
        }
        return (null, null);
    }
}

public static class JsonOptions
{
    public static JsonSerializerOptions Default { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, Default);
    }

    public static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, Default);
    }
}

public static class EncodingHelper
{
    private static readonly Lazy<Encoding> GbkEncoding = new(() =>
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding("GBK");
    });

    public static Encoding GBK => GbkEncoding.Value;
}
