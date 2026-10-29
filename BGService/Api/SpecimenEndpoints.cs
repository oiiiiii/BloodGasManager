using BGShared.Models;
using BGService.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace BGService.Api;

/// <summary>
/// 标本数据相关API端点
/// </summary>
public static class SpecimenEndpoints
{
    /// <summary>
    /// 注册标本相关API路由
    /// </summary>
    /// <param name="group">路由组构建器</param>
    /// <returns>配置后的路由组</returns>
    public static RouteGroupBuilder MapSpecimenEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetSpecimens);
        group.MapGet("/{id}", GetSpecimenById);
        group.MapPost("/query", QuerySpecimens);
        group.MapPost("/{id}/audit", AuditSpecimen);
        group.MapPost("/{id}/unarchive", UnarchiveSpecimen);
        group.MapDelete("/{id}", DeleteSpecimen);
        group.MapGet("/latest/{count:int}", GetLatest);
        return group;
    }

    /// <summary>
    /// 获取标本列表（支持分页和筛选）
    /// </summary>
    /// <param name="repo">标本仓储</param>
    /// <param name="page">页码，从1开始</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="specimenNo">样本号筛选（精确匹配）</param>
    /// <param name="patientId">病案号筛选（精确匹配）</param>
    /// <param name="patientName">患者姓名筛选（精确匹配）</param>
    /// <param name="bedNo">床号筛选（精确匹配）</param>
    /// <param name="department">科室筛选（精确匹配）</param>
    /// <param name="fromDate">开始日期</param>
    /// <param name="toDate">结束日期</param>
    /// <param name="status">状态筛选</param>
    /// <returns>分页结果</returns>
    private static Ok<PagedResult<Specimen>> GetSpecimens(
        ISpecimenRepository repo,
        int page = 1, int pageSize = 20,
        string? specimenNo = null, string? patientId = null,
        string? patientName = null, string? bedNo = null,
        string? department = null, DateTime? fromDate = null,
        DateTime? toDate = null, SpecimenStatus? status = null)
    {
        var (items, total) = repo.Query(
            fromDate, toDate, specimenNo, patientId, patientName,
            bedNo, department, status, page, pageSize);
        return TypedResults.Ok(new PagedResult<Specimen>(items, total, page, pageSize));
    }

    /// <summary>
    /// 根据ID获取单个标本详情
    /// </summary>
    /// <param name="id">标本ID</param>
    /// <param name="repo">标本仓储</param>
    /// <returns>标本详情或404</returns>
    private static Results<Ok<Specimen>, NotFound> GetSpecimenById(long id, ISpecimenRepository repo)
    {
        var (items, _) = repo.Query(page: 1, pageSize: 1);
        var specimen = items.FirstOrDefault(s => s.Id == id);
        if (specimen == null) return TypedResults.NotFound();
        return TypedResults.Ok(specimen);
    }

    /// <summary>
    /// 通过POST方式查询标本列表
    /// </summary>
    /// <param name="req">查询请求参数</param>
    /// <param name="repo">标本仓储</param>
    /// <returns>分页结果</returns>
    private static Ok<PagedResult<Specimen>> QuerySpecimens([FromBody] SpecimenQueryRequest req, ISpecimenRepository repo)
    {
        var (items, total) = repo.Query(
            req.FromDate, req.ToDate, req.SpecimenNo, req.PatientId,
            req.PatientName, req.BedNo, req.Department, req.Status,
            req.Page, req.PageSize);
        return TypedResults.Ok(new PagedResult<Specimen>(items, total, req.Page, req.PageSize));
    }

    /// <summary>
    /// 审核标本（支持快照保存）
    /// </summary>
    /// <param name="id">标本ID</param>
    /// <param name="req">审核请求（包含快照信息）</param>
    /// <param name="repo">标本仓储</param>
    /// <returns>操作结果或404</returns>
    private static Results<Ok<object>, NotFound> AuditSpecimen(long id, [FromBody] AuditSpecimenRequest? req, ISpecimenRepository repo)
    {
        bool ok;
        
        if (req != null && !string.IsNullOrEmpty(req.SnapshotPatientId))
        {
            ok = repo.AuditWithSnapshot(
                id,
                req.SnapshotPatientName ?? "",
                req.SnapshotBedNo ?? "",
                req.SnapshotPatientId,
                req.SnapshotNameAbbreviation ?? "",
                req.SnapshotDepartment ?? "",
                req.SnapshotGender ?? "",
                req.SnapshotSampleType ?? "",
                req.SnapshotRemark ?? "",
                req.AuditUser ?? "system");
        }
        else
        {
            ok = repo.Audit(id);
        }
        
        if (!ok) return TypedResults.NotFound();
        return TypedResults.Ok<object>(new { success = true, id });
    }

    /// <summary>
    /// 取消审核标本
    /// </summary>
    /// <param name="id">标本ID</param>
    /// <param name="repo">标本仓储</param>
    /// <returns>操作结果或404</returns>
    private static Results<Ok<object>, NotFound> UnarchiveSpecimen(long id, ISpecimenRepository repo)
    {
        bool ok = repo.Unarchive(id);
        if (!ok) return TypedResults.NotFound();
        return TypedResults.Ok<object>(new { success = true, id });
    }

    /// <summary>
    /// 删除标本
    /// </summary>
    /// <param name="id">标本ID</param>
    /// <param name="repo">标本仓储</param>
    /// <returns>操作结果或404</returns>
    private static Results<Ok<object>, NotFound> DeleteSpecimen(long id, ISpecimenRepository repo)
    {
        bool ok = repo.Delete(id);
        if (!ok) return TypedResults.NotFound();
        return TypedResults.Ok<object>(new { success = true, id });
    }

    /// <summary>
    /// 获取最新标本列表
    /// </summary>
    /// <param name="count">获取数量（最大100）</param>
    /// <param name="repo">标本仓储</param>
    /// <returns>最新标本列表</returns>
    private static Ok<List<Specimen>> GetLatest(int count, ISpecimenRepository repo)
    {
        var list = repo.GetLatest(Math.Clamp(count, 1, 100));
        return TypedResults.Ok(list);
    }
}

/// <summary>
/// 标本查询请求参数
/// </summary>
public class SpecimenQueryRequest
{
    /// <summary>
    /// 页码，默认1
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// 每页大小，默认20
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// 样本号（精确匹配）
    /// </summary>
    public string? SpecimenNo { get; set; }

    /// <summary>
    /// 病案号（精确匹配）
    /// </summary>
    public string? PatientId { get; set; }

    /// <summary>
    /// 患者姓名（精确匹配）
    /// </summary>
    public string? PatientName { get; set; }

    /// <summary>
    /// 床号（精确匹配）
    /// </summary>
    public string? BedNo { get; set; }

    /// <summary>
    /// 科室（精确匹配）
    /// </summary>
    public string? Department { get; set; }

    /// <summary>
    /// 开始日期
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// 结束日期
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// 状态筛选
    /// </summary>
    public SpecimenStatus? Status { get; set; }
}

/// <summary>
/// 审核标本请求参数（含快照信息）
/// </summary>
public class AuditSpecimenRequest
{
    /// <summary>
    /// 快照患者姓名
    /// </summary>
    public string? SnapshotPatientName { get; set; }

    /// <summary>
    /// 快照床号
    /// </summary>
    public string? SnapshotBedNo { get; set; }

    /// <summary>
    /// 快照病案号
    /// </summary>
    public string? SnapshotPatientId { get; set; }

    /// <summary>
    /// 快照姓名缩写
    /// </summary>
    public string? SnapshotNameAbbreviation { get; set; }

    /// <summary>
    /// 快照科室
    /// </summary>
    public string? SnapshotDepartment { get; set; }

    /// <summary>
    /// 快照性别
    /// </summary>
    public string? SnapshotGender { get; set; }

    /// <summary>
    /// 快照样本类型
    /// </summary>
    public string? SnapshotSampleType { get; set; }

    /// <summary>
    /// 快照备注（审核时保存，不可再修改）
    /// </summary>
    public string? SnapshotRemark { get; set; }

    /// <summary>
    /// 审核用户，默认system
    /// </summary>
    public string? AuditUser { get; set; }
}