using System.Net.Http.Json;
using System.Text.Json;
using BGShared.Models;
using BGShared.Utils;

namespace BGManager.Services;

/// <summary>
/// API客户端，负责与后端服务进行通信
/// </summary>
public class ApiClient
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    private const int MaxRetryCount = 2;
    private const int RetryDelayMilliseconds = 500;

    /// <summary>
    /// 服务端基础URL
    /// </summary>
    public string BaseUrl { get; set; }

    /// <summary>
    /// API密钥
    /// </summary>
    public string ApiKey { get; set; }

    /// <summary>
    /// 使用配置文件初始化API客户端
    /// </summary>
    public ApiClient()
    {
        var config = AppConfig.Load();
        BaseUrl = config.ServerUrl;
        ApiKey = config.ApiKey;
        
        _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        
        if (!string.IsNullOrEmpty(ApiKey))
        {
            _http.DefaultRequestHeaders.Add("X-API-Key", ApiKey);
        }
    }

    /// <summary>
    /// 使用指定的基础URL初始化API客户端
    /// </summary>
    /// <param name="baseUrl">服务端基础URL</param>
    public ApiClient(string baseUrl) : this()
    {
        BaseUrl = baseUrl.TrimEnd('/');
    }

    private string Url(string path) => $"{BaseUrl}/api{path}";

    /// <summary>
    /// 执行带重试机制的异步操作（返回泛型结果）
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    /// <param name="operation">异步操作</param>
    /// <param name="methodName">方法名称（用于日志）</param>
    /// <returns>操作结果或默认值</returns>
    private async Task<T?> ExecuteWithRetryAsync<T>(Func<Task<T?>> operation, string methodName)
    {
        int attempts = 0;
        Exception? lastException = null;

        while (attempts <= MaxRetryCount)
        {
            try
            {
                return await operation();
            }
            catch (HttpRequestException ex) when (IsRetryableException(ex))
            {
                lastException = ex;
                attempts++;
                if (attempts <= MaxRetryCount)
                {
                    await Task.Delay(RetryDelayMilliseconds);
                }
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == default)
            {
                lastException = ex;
                attempts++;
                if (attempts <= MaxRetryCount)
                {
                    await Task.Delay(RetryDelayMilliseconds);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        LogError(methodName, lastException!);
        return default;
    }

    /// <summary>
    /// 执行带重试机制的异步操作（返回布尔值）
    /// </summary>
    /// <param name="operation">异步操作</param>
    /// <param name="methodName">方法名称（用于日志）</param>
    /// <returns>操作成功返回true，否则false</returns>
    private async Task<bool> ExecuteWithRetryAsync(Func<Task<bool>> operation, string methodName)
    {
        int attempts = 0;
        Exception? lastException = null;

        while (attempts <= MaxRetryCount)
        {
            try
            {
                return await operation();
            }
            catch (HttpRequestException ex) when (IsRetryableException(ex))
            {
                lastException = ex;
                attempts++;
                if (attempts <= MaxRetryCount)
                {
                    await Task.Delay(RetryDelayMilliseconds);
                }
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == default)
            {
                lastException = ex;
                attempts++;
                if (attempts <= MaxRetryCount)
                {
                    await Task.Delay(RetryDelayMilliseconds);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        LogError(methodName, lastException!);
        return false;
    }

    /// <summary>
    /// 判断HTTP异常是否可重试
    /// </summary>
    /// <param name="ex">HTTP请求异常</param>
    /// <returns>可重试返回true，否则false</returns>
    private bool IsRetryableException(HttpRequestException ex)
    {
        var statusCode = ex.StatusCode;
        return statusCode == null || 
               (int)statusCode >= 500 || 
               statusCode == System.Net.HttpStatusCode.RequestTimeout ||
               statusCode == System.Net.HttpStatusCode.ServiceUnavailable;
    }

    /// <summary>
    /// 检查与服务端的连接状态
    /// </summary>
    /// <returns>连接成功返回true，否则false</returns>
    public async Task<bool> CheckConnectionAsync()
    {
        try
        {
            var resp = await _http.GetAsync($"{BaseUrl}/api{ApiPaths.Health}");
            return resp.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            LogError("CheckConnectionAsync", ex);
            return false;
        }
        catch (Exception ex)
        {
            LogError("CheckConnectionAsync", ex);
            return false;
        }
    }

    /// <summary>
    /// 获取服务端状态信息
    /// </summary>
    /// <returns>服务状态信息或null</returns>
    public async Task<ServiceStatus?> GetStatusAsync()
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var result = await _http.GetFromJsonAsync<ServiceStatus>(Url(ApiPaths.Status), _jsonOptions);
            if (result == null)
            {
                throw new JsonException("Empty response");
            }
            return result;
        }, "GetStatusAsync");
    }

    /// <summary>
    /// 获取服务端全部配置项
    /// </summary>
    /// <returns>配置键值字典，失败返回null</returns>
    public async Task<Dictionary<string, string>?> GetConfigDictAsync()
    {
        var list = await ExecuteWithRetryAsync(async () =>
        {
            return await _http.GetFromJsonAsync<List<SystemConfig>>(Url("/config"), _jsonOptions);
        }, "GetConfigDictAsync");

        return list?.ToDictionary(c => c.Key, c => c.Value, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 设置服务端配置项
    /// </summary>
    /// <param name="key">配置键</param>
    /// <param name="value">配置值</param>
    /// <param name="category">分类</param>
    /// <param name="description">描述</param>
    /// <returns>成功返回true，否则false</returns>
    public async Task<bool> SetConfigAsync(string key, string value, string category = "", string description = "")
    {
        var result = await ExecuteWithRetryAsync(async () =>
        {
            var resp = await _http.PutAsJsonAsync(Url($"/config/{key}"),
                new { Value = value, Category = category, Description = description }, _jsonOptions);
            return resp.IsSuccessStatusCode;
        }, "SetConfigAsync");
        return result == true;
    }

    /// <summary>
    /// 获取最新标本列表
    /// </summary>
    /// <param name="count">获取数量，默认50</param>
    /// <returns>标本列表</returns>
    public async Task<List<Specimen>> GetLatestAsync(int count = 50)
    {
        var result = await ExecuteWithRetryAsync(async () =>
        {
            return await _http.GetFromJsonAsync<List<Specimen>>(Url(ApiPaths.SpecimensLatest.Replace("{count}", count.ToString())), _jsonOptions);
        }, "GetLatestAsync");
        return result ?? new List<Specimen>();
    }

    /// <summary>
    /// 分页查询标本列表
    /// </summary>
    /// <param name="page">页码，从1开始，默认1</param>
    /// <param name="pageSize">每页大小，默认20</param>
    /// <param name="specimenNo">样本号筛选（精确匹配）</param>
    /// <param name="patientId">病案号筛选（精确匹配）</param>
    /// <param name="patientName">患者姓名筛选（精确匹配）</param>
    /// <param name="bedNo">床号筛选（精确匹配）</param>
    /// <param name="department">科室筛选（精确匹配）</param>
    /// <param name="fromDate">开始日期</param>
    /// <param name="toDate">结束日期</param>
    /// <param name="status">状态筛选</param>
    /// <param name="sortDesc">是否降序排列，默认true</param>
    /// <returns>分页结果</returns>
    public async Task<PagedResult<Specimen>> QuerySpecimensAsync(
        int page = 1, int pageSize = 20,
        string? specimenNo = null, string? patientId = null,
        string? patientName = null, string? bedNo = null,
        string? department = null, DateTime? fromDate = null,
        DateTime? toDate = null, SpecimenStatus? status = null, bool sortDesc = true)
    {
        var query = System.Web.HttpUtility.ParseQueryString("");
        query["page"] = page.ToString();
        query["pageSize"] = pageSize.ToString();
        if (specimenNo != null) query["specimenNo"] = specimenNo;
        if (patientId != null) query["patientId"] = patientId;
        if (patientName != null) query["patientName"] = patientName;
        if (bedNo != null) query["bedNo"] = bedNo;
        if (department != null) query["department"] = department;
        if (fromDate.HasValue) query["fromDate"] = fromDate.Value.ToString("yyyy-MM-ddTHH:mm:ss");
        if (toDate.HasValue) query["toDate"] = toDate.Value.ToString("yyyy-MM-ddTHH:mm:ss");
        if (status.HasValue) query["status"] = ((int)status.Value).ToString();
        query["sortDesc"] = sortDesc.ToString().ToLower();

        var result = await ExecuteWithRetryAsync(async () =>
        {
            return await _http.GetFromJsonAsync<PagedResult<Specimen>>(Url($"{ApiPaths.Specimens}?{query}"), _jsonOptions);
        }, "QuerySpecimensAsync");
        return result ?? new PagedResult<Specimen>(new List<Specimen>(), 0, page, pageSize);
    }

    /// <summary>
    /// 审核标本（简单模式）
    /// </summary>
    /// <param name="id">标本ID</param>
    /// <returns>审核成功返回true，否则false</returns>
    public async Task<bool> AuditSpecimenAsync(long id)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var resp = await _http.PostAsync(Url(ApiPaths.SpecimensAudit.Replace("{id}", id.ToString())), null);
            return resp.IsSuccessStatusCode;
        }, "AuditSpecimenAsync");
    }

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
    /// <returns>审核成功返回true，否则false</returns>
    public async Task<bool> AuditSpecimenWithSnapshotAsync(long id,
        string snapshotPatientName,
        string snapshotBedNo,
        string snapshotPatientId,
        string snapshotNameAbbreviation,
        string snapshotDepartment,
        string snapshotGender,
        string snapshotSampleType,
        string snapshotRemark,
        string auditUser = "system")
    {
        var request = new
        {
            snapshotPatientName,
            snapshotBedNo,
            snapshotPatientId,
            snapshotNameAbbreviation,
            snapshotDepartment,
            snapshotGender,
            snapshotSampleType,
            snapshotRemark,
            auditUser
        };

        return await ExecuteWithRetryAsync(async () =>
        {
            var resp = await _http.PostAsJsonAsync(Url(ApiPaths.SpecimensAudit.Replace("{id}", id.ToString())), request);
            return resp.IsSuccessStatusCode;
        }, "AuditSpecimenWithSnapshotAsync");
    }

    /// <summary>
    /// 取消审核标本（恢复为待审核状态）
    /// </summary>
    /// <param name="id">标本ID</param>
    /// <returns>操作成功返回true，否则false</returns>
    public async Task<bool> UnarchiveSpecimenAsync(long id)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var resp = await _http.PostAsync(Url(ApiPaths.SpecimensUnarchive.Replace("{id}", id.ToString())), null);
            return resp.IsSuccessStatusCode;
        }, "UnarchiveSpecimenAsync");
    }

    /// <summary>
    /// 删除标本
    /// </summary>
    /// <param name="id">标本ID</param>
    /// <returns>删除成功返回true，否则false</returns>
    public async Task<bool> DeleteSpecimenAsync(long id)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var resp = await _http.DeleteAsync(Url(ApiPaths.SpecimensById.Replace("{id}", id.ToString())));
            return resp.IsSuccessStatusCode;
        }, "DeleteSpecimenAsync");
    }

    /// <summary>
    /// 获取患者列表
    /// </summary>
    /// <param name="onlyActive">是否只获取活跃患者，默认false</param>
    /// <returns>患者列表</returns>
    public async Task<List<Patient>> GetPatientsAsync(bool onlyActive = false)
    {
        var result = await ExecuteWithRetryAsync(async () =>
        {
            return await _http.GetFromJsonAsync<List<Patient>>(Url($"{ApiPaths.Patients}?onlyActive={onlyActive}"), _jsonOptions);
        }, "GetPatientsAsync");
        return result ?? new List<Patient>();
    }

    /// <summary>
    /// 搜索患者
    /// </summary>
    /// <param name="keyword">搜索关键词（姓名或病案号）</param>
    /// <param name="limit">最大返回数量，默认50</param>
    /// <returns>匹配的患者列表</returns>
    public async Task<List<Patient>> SearchPatientsAsync(string keyword, int limit = 50)
    {
        var result = await ExecuteWithRetryAsync(async () =>
        {
            return await _http.GetFromJsonAsync<List<Patient>>(
                Url($"{ApiPaths.PatientsSearch}?q={Uri.EscapeDataString(keyword)}&limit={limit}"), _jsonOptions);
        }, "SearchPatientsAsync");
        return result ?? new List<Patient>();
    }

    /// <summary>
    /// 查找单个患者（支持床号、姓名缩写、病案号）
    /// </summary>
    /// <param name="keyword">搜索关键词（姓名或病案号）</param>
    /// <returns>匹配的患者或null</returns>
    public async Task<Patient?> FindPatientAsync(string keyword)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            return await _http.GetFromJsonAsync<Patient>(
                Url($"{ApiPaths.PatientsFind}?q={Uri.EscapeDataString(keyword)}"), _jsonOptions);
        }, "FindPatientAsync");
    }

    /// <summary>
    /// 通过病案号精确查找患者
    /// </summary>
    /// <param name="patientId">病案号</param>
    /// <returns>匹配的患者或null</returns>
    public async Task<Patient?> FindPatientByIdAsync(string patientId)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            return await _http.GetFromJsonAsync<Patient>(
                Url($"{ApiPaths.Patients}/findbyid?patientId={Uri.EscapeDataString(patientId)}"), _jsonOptions);
        }, "FindPatientByIdAsync");
    }

    /// <summary>
    /// 保存患者信息（更新或插入）
    /// </summary>
    /// <param name="patient">患者实体</param>
    /// <returns>保存成功返回true，否则false</returns>
    public async Task<bool> SavePatientAsync(Patient patient)
    {
        try
        {
            var resp = await _http.PutAsJsonAsync(Url(ApiPaths.PatientsById.Replace("{id}", patient.PatientId)), patient);
            if (resp.IsSuccessStatusCode) return true;
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var createResp = await _http.PostAsJsonAsync(Url(ApiPaths.Patients), patient);
                return createResp.IsSuccessStatusCode;
            }
            return false;
        }
        catch (HttpRequestException ex)
        {
            LogError("SavePatientAsync", ex);
            return false;
        }
        catch (JsonException ex)
        {
            LogError("SavePatientAsync (JSON)", ex);
            return false;
        }
        catch (Exception ex)
        {
            LogError("SavePatientAsync", ex);
            return false;
        }
    }

    /// <summary>
    /// 删除患者
    /// </summary>
    /// <param name="patientId">患者病案号</param>
    /// <returns>删除成功返回true，否则false</returns>
    public async Task<bool> DeletePatientAsync(string patientId)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var resp = await _http.DeleteAsync(Url(ApiPaths.PatientsById.Replace("{id}", patientId)));
            return resp.IsSuccessStatusCode;
        }, "DeletePatientAsync");
    }

    /// <summary>
    /// 记录错误日志
    /// </summary>
    /// <param name="methodName">方法名称</param>
    /// <param name="ex">异常信息</param>
    private void LogError(string methodName, Exception ex)
    {
        try
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "api_client_errors.log");
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {methodName}: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}\n\n";
            File.AppendAllText(logPath, logEntry);
        }
        catch
        {
        }
    }
}