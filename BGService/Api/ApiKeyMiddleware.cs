using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace BGService.Api;

public class ApiKeyMiddleware
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private const string ApiKeyConfigKey = "Api:Key";
    private readonly RequestDelegate _next;
    private static readonly HashSet<string> ExcludedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/health"
    };
    private static int _noKeyWarned;

    public ApiKeyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (ExcludedPaths.Contains(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
        var validApiKey = configuration[ApiKeyConfigKey];

        if (string.IsNullOrEmpty(validApiKey))
        {
            // 未配置 Key 时保持放行（兼容单机部署），但只在启动后警告一次
            if (Interlocked.Exchange(ref _noKeyWarned, 1) == 0)
            {
                Program.SafeLog("警告：未配置 Api:Key，API 接口当前无鉴权保护。局域网部署请在 appsettings.json 中配置。");
            }
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out StringValues apiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "API Key is required" });
            return;
        }

        // 固定时间比较，区分大小写，避免时序侧信道
        var expectedBytes = Encoding.UTF8.GetBytes(validApiKey);
        var actualBytes = Encoding.UTF8.GetBytes(apiKey.ToString());
        if (expectedBytes.Length != actualBytes.Length ||
            !CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API Key" });
            return;
        }

        await _next(context);
    }
}