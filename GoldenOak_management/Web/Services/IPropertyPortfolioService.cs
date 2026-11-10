using Web.Models;

namespace Web.Services;

public interface IPropertyPortfolioService
{
    Task<IReadOnlyList<Building>> GetBuildingsAsync(CancellationToken cancellationToken = default);
    Task<Building?> GetBuildingAsync(long id, CancellationToken cancellationToken = default);
    Task CreateBuildingAsync(Building building, CancellationToken cancellationToken = default);
    Task<bool> UpdateBuildingAsync(Building building, CancellationToken cancellationToken = default);
    Task<bool> DeleteBuildingAsync(long id, CancellationToken cancellationToken = default);
    Task<Unit?> GetUnitAsync(long id, CancellationToken cancellationToken = default);
    Task CreateUnitAsync(Unit unit, CancellationToken cancellationToken = default);
    Task<bool> UpdateUnitAsync(Unit unit, CancellationToken cancellationToken = default);
    Task<bool> DeleteUnitAsync(long id, CancellationToken cancellationToken = default);
}
