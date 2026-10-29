using BGShared.Models;
using BGService.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace BGService.Api;

public static class StatusEndpoints
{
    public static RouteGroupBuilder MapStatusEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetStatus);
        group.MapGet("/stats/daily", GetDailyStats);
        return group;
    }

    private static Ok<ServiceStatus> GetStatus(IServiceStatusRepository statusRepo, ISpecimenRepository specRepo)
    {
        var status = statusRepo.Get();
        status.TotalSpecimens = specRepo.GetCount();
        status.TodaySpecimens = specRepo.GetTodayCount();
        return TypedResults.Ok(status);
    }

    private static Ok<DailyStats> GetDailyStats(ISpecimenRepository repo, int days = 30)
    {
        var stats = new DailyStats
        {
            Total = repo.GetCount(),
            Today = repo.GetTodayCount(),
            Pending = (int)repo.GetCount(SpecimenStatus.Pending),
            Audited = (int)repo.GetCount(SpecimenStatus.Audited)
        };
        return TypedResults.Ok(stats);
    }
}

public class DailyStats
{
    public long Total { get; set; }
    public int Today { get; set; }
    public int Pending { get; set; }
    public int Audited { get; set; }
}

public static class ConfigEndpoints
{
    public static RouteGroupBuilder MapConfigEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetAll);
        group.MapGet("/{key}", GetByKey);
        group.MapPut("/{key}", SetValue);
        return group;
    }

    private static Ok<List<SystemConfig>> GetAll(ConfigRepository repo, string? category = null)
    {
        return TypedResults.Ok(repo.GetAll(category));
    }

    private static Results<Ok<SystemConfig>, NotFound> GetByKey(ConfigRepository repo, string key)
    {
        var val = repo.GetValue(key);
        if (val == null) return TypedResults.NotFound();
        return TypedResults.Ok(new SystemConfig { Key = key, Value = val });
    }

    private static Ok<object> SetValue(string key, [FromBody] ConfigSetRequest req, ConfigRepository repo)
    {
        repo.SetValue(key, req.Value, req.Category ?? "", req.Description ?? "");
        return TypedResults.Ok<object>(new { success = true, key, req.Value });
    }
}

public class ConfigSetRequest
{
    public string Value { get; set; } = "";
    public string? Category { get; set; }
    public string? Description { get; set; }
}

public static class AdminEndpoints
{
    public static RouteGroupBuilder MapAdminEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/restart", Restart);
        group.MapPost("/reparse", Reparse);
        group.MapPost("/inject", InjectMessage);
        return group;
    }

    private static Ok<object> Restart()
    {
        return TypedResults.Ok<object>(new { success = true, message = "重启指令已发送" });
    }

    private static Ok<object> Reparse()
    {
        return TypedResults.Ok<object>(new { success = true, message = "重解析指令已发送" });
    }

    private static IResult InjectMessage([FromBody] InjectRequest req, BGProcessor processor)
    {
        if (string.IsNullOrEmpty(req.Message))
        {
            return Results.BadRequest(new { error = "message is required" });
        }

        var specimen = processor.InjectMessage(req.Message);
        if (specimen == null)
        {
            return Results.BadRequest(new { error = "failed to parse or specimen already exists" });
        }

        return Results.Ok(new { success = true, specimenId = specimen.SpecimenId, resultCount = specimen.Results.Count });
    }
}

public class InjectRequest
{
    public string Message { get; set; } = "";
}