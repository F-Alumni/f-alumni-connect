using AdminService.Repositories.Entities;

namespace AdminService.Repositories.Interfaces;

public interface ISystemSettingRepository
{
    Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SystemSetting>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SystemSetting> SetAsync(string key, string value, string? description = null, string? category = null, Guid? adminUserId = null, CancellationToken cancellationToken = default);
}
