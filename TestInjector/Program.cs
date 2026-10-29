using System.Text.Json;

var apiUrl = "http://localhost:5000/api/specimens/latest/1";

using var client = new HttpClient();
var json = await client.GetStringAsync(apiUrl);
using var doc = JsonDocument.Parse(json);
var specimens = doc.RootElement;

if (specimens.GetArrayLength() > 0)
{
    var s = specimens[0];
    var results = s.GetProperty("results");
    Console.WriteLine($"标本: {s.GetProperty("specimenNo").GetString()}, 结果数: {results.GetArrayLength()}");
    Console.WriteLine();
    
    foreach (var r in results.EnumerateArray())
    {
        var name = r.GetProperty("testName").GetString();
        var val = r.GetProperty("value").ValueKind == JsonValueKind.Number 
            ? r.GetProperty("value").GetDouble().ToString() 
            : r.GetProperty("value").GetString();
        var unit = r.GetProperty("unit").GetString();
        Console.WriteLine($"{name,-25} {val,-15} {unit}");
    }
}
