using Microsoft.EntityFrameworkCore;
using AdminService.Repositories.Context;
using AdminService.Repositories.Entities;
using AdminService.Repositories.Interfaces;

namespace AdminService.Repositories.Implementations;

public class SystemSettingRepository : ISystemSettingRepository
{
    private readonly AdminServiceDbContext _dbContext;

    public SystemSettingRepository(AdminServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SystemSettings
            .FirstOrDefaultAsync(x => x.Key == key, cancellationToken);
    }

    public async Task<IReadOnlyList<SystemSetting>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SystemSettings
            .AsNoTracking()
            .OrderBy(x => x.Key)
            .ToListAsync(cancellationToken);
    }

    public async Task<SystemSetting> SetAsync(
        string key,
        string value,
        string? description = null,
        string? category = null,
        Guid? adminUserId = null,
        CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.SystemSettings
            .FirstOrDefaultAsync(x => x.Key == key, cancellationToken);

        if (existing is null)
        {
            var newSetting = new SystemSetting
            {
                Key = key,
                Value = value,
                Description = description,
                Category = category,
                UpdatedByAdminId = adminUserId,
                UpdatedAtUtc = DateTime.UtcNow
            };
            await _dbContext.SystemSettings.AddAsync(newSetting, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return newSetting;
        }

        existing.Value = value;
        if (!string.IsNullOrWhiteSpace(description))
        {
            existing.Description = description;
        }
        if (!string.IsNullOrWhiteSpace(category))
        {
            existing.Category = category;
        }
        existing.UpdatedByAdminId = adminUserId;
        existing.UpdatedAtUtc = DateTime.UtcNow;

        _dbContext.SystemSettings.Update(existing);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }
}
