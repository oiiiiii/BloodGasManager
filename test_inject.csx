using System.Text;
using System.Text.Json;

var testDir = @"D:\Code\PCtools\ZZ.ABE\HL7SerialListener\ceshi\received_messages";
var apiUrl = "http://localhost:5000/api/admin/inject";

Console.WriteLine("=== Test Data Injector ===");
Console.WriteLine();

if (!Directory.Exists(testDir))
{
    Console.WriteLine($"ERROR: Test directory not found: {testDir}");
    return;
}

var files = Directory.GetFiles(testDir, "*.hl7").OrderBy(f => f).ToArray();
Console.WriteLine($"Found {files.Length} test files");
Console.WriteLine();

int success = 0, fail = 0;

using var http = new HttpClient();

foreach (var file in files)
{
    var fileName = Path.GetFileName(file);
    Console.WriteLine($"Processing: {fileName}");

    var content = File.ReadAllText(file, Encoding.UTF8);

    var startMarker = "========== 原始报文 ==========";
    var endMarker = "========== 报文结束 ==========";

    int startIdx = content.IndexOf(startMarker);
    int endIdx = content.IndexOf(endMarker);

    if (startIdx < 0 || endIdx <= startIdx)
    {
        Console.WriteLine("  WARN: no message body");
        fail++;
        continue;
    }

    var pureMessage = content.Substring(
        startIdx + startMarker.Length,
        endIdx - startIdx - startMarker.Length).Trim();

    var requestBody = JsonSerializer.Serialize(new { message = pureMessage });
    var httpContent = new StringContent(requestBody, Encoding.UTF8, "application/json");

    try
    {
        var resp = await http.PostAsync(apiUrl, httpContent);
        var respBody = await resp.Content.ReadAsStringAsync();

        if (resp.IsSuccessStatusCode)
        {
            var json = JsonSerializer.Deserialize<JsonElement>(respBody);
            var specId = json.GetProperty("specimenId").GetString();
            var resultCount = json.GetProperty("resultCount").GetInt32();
            Console.WriteLine($"  OK: specimen={specId}, results={resultCount}");
            success++;
        }
        else
        {
            Console.WriteLine($"  FAIL: {resp.StatusCode} - {respBody}");
            fail++;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ERROR: {ex.Message}");
        fail++;
    }

    Console.WriteLine();
}

Console.WriteLine("=========================");
Console.WriteLine($"Done. Success: {success}, Fail: {fail}");
Console.WriteLine();

try
{
    var listResp = await http.GetAsync("http://localhost:5000/api/specimens/latest/10");
    var listBody = await listResp.Content.ReadAsStringAsync();
    var list = JsonSerializer.Deserialize<List<JsonElement>>(listBody);
    Console.WriteLine($"Total specimens in DB: {list?.Count ?? 0}");
    foreach (var s in list ?? new())
    {
        var specNo = s.GetProperty("specimenNo").GetString();
        var patName = s.GetProperty("patientName").GetString();
        var status = s.GetProperty("status").GetInt32();
        var results = s.GetProperty("results").GetArrayLength();
        Console.WriteLine($"  {specNo} | {patName} | {results} results | status={status}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to fetch list: {ex.Message}");
}
