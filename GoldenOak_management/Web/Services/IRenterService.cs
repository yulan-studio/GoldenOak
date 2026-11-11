using Web.Models;

namespace Web.Services;

public interface IRenterService
{
    Task<List<RenterProfile>> GetRentersAsync(CancellationToken ct);
    Task<RenterProfile?> GetRenterAsync(long userId, CancellationToken ct);
    Task<long> CreateRenterAsync(CreateRenterViewModel model, CancellationToken ct);
    Task<bool> UpdateRenterAsync(EditRenterViewModel model, CancellationToken ct);
    Task<bool> ArchiveRenterAsync(long userId, CancellationToken ct);
}
