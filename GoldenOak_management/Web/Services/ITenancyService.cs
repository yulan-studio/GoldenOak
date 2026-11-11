using Web.Models;

namespace Web.Services;

public interface ITenancyService
{
    Task<List<Tenancy>> GetTenanciesAsync(CancellationToken ct);
    Task<Tenancy?> GetTenancyAsync(long id, CancellationToken ct);
    Task<long> CreateTenancyAsync(Tenancy model, CancellationToken ct);
    Task<bool> UpdateTenancyAsync(Tenancy model, CancellationToken ct);
    Task<bool> ChangeStatusAsync(long id, string status, CancellationToken ct);
    Task<bool> AddRenterAsync(long tenancyId, long renterId, bool isPrimary, CancellationToken ct);
    Task<bool> RemoveRenterAsync(long tenancyId, long renterId, CancellationToken ct);
}
