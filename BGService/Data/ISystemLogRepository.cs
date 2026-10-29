using BGShared.Models;

namespace BGService.Data;

public interface ISystemLogRepository
{
    long Add(string level, string category, string message, string? detail = null);
    List<SystemLog> GetRecent(int count = 100, string? level = null, string? category = null);
    void CleanOld(int keepDays = 90);
}