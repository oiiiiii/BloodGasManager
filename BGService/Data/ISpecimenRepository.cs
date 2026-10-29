using BGShared.Models;

namespace BGService.Data;

/// <summary>
/// 标本数据仓储接口
/// </summary>
public interface ISpecimenRepository
{
    /// <summary>
    /// 根据样本号获取标本
    /// </summary>
    /// <param name="specimenId">样本号</param>
    /// <returns>标本实体或null</returns>
    Specimen? GetBySpecimenId(string specimenId);

    /// <summary>
    /// 获取最新标本列表
    /// </summary>
    /// <param name="count">获取数量，默认50</param>
    /// <param name="status">状态筛选，为空则获取全部</param>
    /// <returns>标本列表</returns>
    List<Specimen> GetLatest(int count = 50, SpecimenStatus? status = null);

    /// <summary>
    /// 分页查询标本
    /// </summary>
    /// <param name="fromDate">开始日期</param>
    /// <param name="toDate">结束日期</param>
    /// <param name="specimenNo">样本号（精确匹配）</param>
    /// <param name="patientId">病案号（精确匹配）</param>
    /// <param name="patientName">患者姓名（精确匹配）</param>
    /// <param name="bedNo">床号（精确匹配）</param>
    /// <param name="department">科室（精确匹配）</param>
    /// <param name="status">状态筛选</param>
    /// <param name="page">页码，从1开始</param>
    /// <param name="pageSize">每页大小</param>
    /// <returns>分页结果（数据列表和总条数）</returns>
    (List<Specimen> Items, long Total) Query(
        DateTime? fromDate = null, DateTime? toDate = null,
        string? specimenNo = null, string? patientId = null,
        string? patientName = null, string? bedNo = null,
        string? department = null, SpecimenStatus? status = null,
        int page = 1, int pageSize = 20);

    /// <summary>
    /// 检查样本号是否存在
    /// </summary>
    /// <param name="specimenId">样本号</param>
    /// <returns>存在返回true，否则false</returns>
    bool Exists(string specimenId);

    /// <summary>
    /// 插入新标本
    /// </summary>
    /// <param name="specimen">标本实体</param>
    /// <returns>新插入记录的ID</returns>
    long Insert(Specimen specimen);

    /// <summary>
    /// 更新标本信息
    /// </summary>
    /// <param name="specimen">标本实体</param>
    /// <returns>更新记录的ID</returns>
    long Update(Specimen specimen);

    /// <summary>
    /// 审核标本
    /// </summary>
    /// <param name="id">标本ID</param>
    /// <param name="auditUser">审核用户，默认system</param>
    /// <returns>成功返回true，否则false</returns>
    bool Audit(long id, string auditUser = "system");

    /// <summary>
    /// 审核标本并保存快照信息
    /// </summary>
    /// <param name="id">标本ID</param>
    /// <param name="snapshotPatientName">快照患者姓名</param>
    /// <param name="snapshotBedNo">快照床号</param>
    /// <param name="snapshotPatientId">快照病案号</param>
    /// <param name="snapshotNameAbbreviation">快照姓名缩写</param>
    /// <param name="snapshotDepartment">快照科室</param>
    /// <param name="snapshotGender">快照性别</param>
    /// <param name="snapshotSampleType">快照样本类型</param>
    /// <param name="snapshotRemark">快照备注</param>
    /// <param name="auditUser">审核用户，默认system</param>
    /// <returns>成功返回true，否则false</returns>
    bool AuditWithSnapshot(long id,
        string snapshotPatientName, string snapshotBedNo, string snapshotPatientId,
        string snapshotNameAbbreviation, string snapshotDepartment, string snapshotGender,
        string snapshotSampleType, string snapshotRemark, string auditUser = "system");

    /// <summary>
    /// 取消审核（恢复为待审核状态）
    /// </summary>
    /// <param name="id">标本ID</param>
    /// <returns>成功返回true，否则false</returns>
    bool Unarchive(long id);

    /// <summary>
    /// 删除标本
    /// </summary>
    /// <param name="id">标本ID</param>
    /// <returns>成功返回true，否则false</returns>
    bool Delete(long id);

    /// <summary>
    /// 删除指定时间之前的所有标本及其结果（数据保留策略）
    /// </summary>
    /// <param name="cutoff">截止时间，早于该时间的标本将被删除</param>
    /// <returns>删除的标本条数</returns>
    int DeleteOlderThan(DateTime cutoff);

    /// <summary>
    /// 获取标本数量
    /// </summary>
    /// <param name="status">状态筛选，为空则获取全部</param>
    /// <returns>标本数量</returns>
    long GetCount(SpecimenStatus? status = null);

    /// <summary>
    /// 获取今日标本数量
    /// </summary>
    /// <returns>今日标本数量</returns>
    int GetTodayCount();
}