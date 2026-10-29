using BGShared.Models;

namespace BGService.Data;

public interface IServiceStatusRepository
{
    ServiceStatus Get();
    void Update(Action<ServiceStatus> updateAction);
    void Save(ServiceStatus status);
    void UpdateHeartbeat();
}