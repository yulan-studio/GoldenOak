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
    Task<UnitOwnerViewModel?> GetUnitOwnersAsync(long unitId, CancellationToken cancellationToken = default);
    Task<bool> AddUnitOwnerAsync(long unitId, long ownerId, decimal ownershipPercentage, DateOnly validFrom, CancellationToken cancellationToken = default);
    Task<bool> DeleteUnitOwnerAsync(long unitOwnerId, CancellationToken cancellationToken = default);
    Task<CreateOwnerForUnitViewModel?> GetCreateOwnerModelAsync(long unitId, CancellationToken cancellationToken = default);
    Task<bool> CreateOwnerAndAssignAsync(CreateOwnerForUnitViewModel model, CancellationToken cancellationToken = default);
    Task<EditOwnerForUnitViewModel?> GetEditOwnerModelAsync(long unitOwnerId, CancellationToken cancellationToken = default);
    Task<bool> UpdateOwnerForUnitAsync(EditOwnerForUnitViewModel model, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OwnerSearchResult>> SearchOwnersAsync(long unitId, string term, CancellationToken cancellationToken = default);
    Task<bool> AddExistingOwnerToUnitAsync(long unitId, long ownerId, decimal ownershipPercentage, DateOnly validFrom, CancellationToken cancellationToken = default);
}
