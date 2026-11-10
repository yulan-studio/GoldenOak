using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Web.Data;
using Web.Models;

namespace Web.Services;

/// <summary>Buildings 和 Units 的 CRUD 业务服务。</summary>
public sealed class PropertyPortfolioService : IPropertyPortfolioService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    public PropertyPortfolioService(ApplicationDbContext db, UserManager<ApplicationUser> userManager) { _db = db; _userManager = userManager; }

    public async Task<IReadOnlyList<Building>> GetBuildingsAsync(CancellationToken ct = default) => await _db.Buildings.AsNoTracking().Include(x => x.Units).OrderBy(x => x.BuildingName).ToListAsync(ct);
    public Task<Building?> GetBuildingAsync(long id, CancellationToken ct = default) => _db.Buildings.Include(x => x.Units).FirstOrDefaultAsync(x => x.BuildingId == id, ct);

    public async Task CreateBuildingAsync(Building building, CancellationToken ct = default) { building.CreatedAt = building.UpdatedAt = DateTime.UtcNow; _db.Buildings.Add(building); await _db.SaveChangesAsync(ct); }

    public async Task<bool> UpdateBuildingAsync(Building model, CancellationToken ct = default)
    {
        var item = await _db.Buildings.FindAsync([model.BuildingId], ct); if (item is null) return false;
        item.BuildingName = model.BuildingName; item.AddressLine1 = model.AddressLine1; item.AddressLine2 = model.AddressLine2;
        item.City = model.City; item.Province = model.Province; item.PostalCode = model.PostalCode; item.BuildingType = model.BuildingType;
        item.UnitCount = model.UnitCount; item.Status = model.Status; item.Notes = model.Notes; item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct); return true;
    }

    public async Task<bool> DeleteBuildingAsync(long id, CancellationToken ct = default)
    {
        var item = await _db.Buildings.Include(x => x.Units).FirstOrDefaultAsync(x => x.BuildingId == id, ct);
        if (item is null || item.Units.Count > 0) return false;
        _db.Buildings.Remove(item); await _db.SaveChangesAsync(ct); return true;
    }

    public Task<Unit?> GetUnitAsync(long id, CancellationToken ct = default) => _db.Units.Include(x => x.Building).FirstOrDefaultAsync(x => x.UnitId == id, ct);
    public async Task CreateUnitAsync(Unit unit, CancellationToken ct = default) { unit.CreatedAt = unit.UpdatedAt = DateTime.UtcNow; _db.Units.Add(unit); await _db.SaveChangesAsync(ct); }

    public async Task<bool> UpdateUnitAsync(Unit model, CancellationToken ct = default)
    {
        var item = await _db.Units.FindAsync([model.UnitId], ct); if (item is null) return false;
        item.UnitNumber = model.UnitNumber; item.Bedrooms = model.Bedrooms; item.Bathrooms = model.Bathrooms; item.SizeSqft = model.SizeSqft;
        item.AskingRent = model.AskingRent; item.Status = model.Status; item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct); return true;
    }

    public async Task<bool> DeleteUnitAsync(long id, CancellationToken ct = default)
    {
        var item = await _db.Units.FindAsync([id], ct); if (item is null) return false;
        _db.Units.Remove(item); await _db.SaveChangesAsync(ct); return true;
    }

    public async Task<UnitOwnerViewModel?> GetUnitOwnersAsync(long unitId, CancellationToken ct = default)
    {
        var unit = await _db.Units.AsNoTracking().FirstOrDefaultAsync(x => x.UnitId == unitId, ct);
        if (unit is null) return null;
        var building = await _db.Buildings.AsNoTracking().FirstAsync(x => x.BuildingId == unit.BuildingId, ct);
        var owners = await (from relation in _db.UnitOwners.AsNoTracking()
                            join profile in _db.OwnerProfiles.AsNoTracking() on relation.OwnerId equals profile.UserId
                            join user in _db.Users.AsNoTracking() on profile.UserId equals user.Id
                            where relation.UnitId == unitId
                            select new UnitOwnerSummary { UnitOwnerId = relation.UnitOwnerId, FullName = profile.FullName, Email = user.Email, OwnershipPercentage = relation.OwnershipPercentage, ValidFrom = relation.ValidFrom, ValidTo = relation.ValidTo }).ToListAsync(ct);
        return new UnitOwnerViewModel { UnitId = unitId, BuildingId = unit.BuildingId, UnitNumber = unit.UnitNumber, BuildingName = building.BuildingName, Owners = owners };
    }

    public async Task<bool> AddUnitOwnerAsync(long unitId, long ownerId, decimal ownershipPercentage, DateOnly validFrom, CancellationToken ct = default)
    {
        var unitExists = await _db.Units.AnyAsync(x => x.UnitId == unitId, ct);
        var ownerExists = await _db.OwnerProfiles.AnyAsync(x => x.UserId == ownerId, ct);
        var relationExists = await _db.UnitOwners.AnyAsync(x => x.UnitId == unitId && x.OwnerId == ownerId, ct);
        if (!unitExists || !ownerExists || relationExists || ownershipPercentage <= 0 || ownershipPercentage > 100) return false;
        _db.UnitOwners.Add(new UnitOwner { UnitId = unitId, OwnerId = ownerId, OwnershipPercentage = ownershipPercentage, ValidFrom = validFrom, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync(ct); return true;
    }

    public async Task<bool> DeleteUnitOwnerAsync(long unitOwnerId, CancellationToken ct = default)
    {
        var relation = await _db.UnitOwners.FindAsync([unitOwnerId], ct); if (relation is null) return false;
        _db.UnitOwners.Remove(relation); await _db.SaveChangesAsync(ct); return true;
    }

    public async Task<CreateOwnerForUnitViewModel?> GetCreateOwnerModelAsync(long unitId, CancellationToken ct = default)
    {
        var unit = await _db.Units.AsNoTracking().Include(x => x.Building).FirstOrDefaultAsync(x => x.UnitId == unitId, ct);
        return unit is null ? null : new CreateOwnerForUnitViewModel { UnitId = unitId, UnitNumber = unit.UnitNumber, BuildingName = unit.Building.BuildingName };
    }

    public async Task<bool> CreateOwnerAndAssignAsync(CreateOwnerForUnitViewModel model, CancellationToken ct = default)
    {
        var unitExists = await _db.Units.AnyAsync(x => x.UnitId == model.UnitId, ct);
        if (!unitExists || await _userManager.FindByEmailAsync(model.Email) is not null) return false;

        var user = new ApplicationUser { UserName = model.Email, Email = model.Email, PhoneNumber = model.PhoneNumber, Status = "INVITED", EmailConfirmed = false };
        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded) return false;

        _db.OwnerProfiles.Add(new OwnerProfile { UserId = user.Id, FullName = model.FullName, MailingAddress = model.MailingAddress, PayoutPreference = model.PayoutPreference, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        _db.UnitOwners.Add(new UnitOwner { UnitId = model.UnitId, OwnerId = user.Id, OwnershipPercentage = model.OwnershipPercentage, ValidFrom = model.ValidFrom, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<EditOwnerForUnitViewModel?> GetEditOwnerModelAsync(long unitOwnerId, CancellationToken ct = default)
    {
        var relation = await _db.UnitOwners.AsNoTracking().FirstOrDefaultAsync(x => x.UnitOwnerId == unitOwnerId, ct);
        if (relation is null) return null;
        var unit = await _db.Units.AsNoTracking().Include(x => x.Building).FirstOrDefaultAsync(x => x.UnitId == relation.UnitId, ct);
        var profile = await _db.OwnerProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == relation.OwnerId, ct);
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == relation.OwnerId, ct);
        if (unit is null || profile is null || user is null) return null;
        return new EditOwnerForUnitViewModel { UnitOwnerId = unitOwnerId, UnitId = unit.UnitId, BuildingName = unit.Building.BuildingName, UnitNumber = unit.UnitNumber, FullName = profile.FullName, Email = user.Email ?? string.Empty, PhoneNumber = user.PhoneNumber, MailingAddress = profile.MailingAddress, OwnershipPercentage = relation.OwnershipPercentage, ValidFrom = relation.ValidFrom, PayoutPreference = profile.PayoutPreference };
    }

    public async Task<bool> UpdateOwnerForUnitAsync(EditOwnerForUnitViewModel model, CancellationToken ct = default)
    {
        var relation = await _db.UnitOwners.FirstOrDefaultAsync(x => x.UnitOwnerId == model.UnitOwnerId, ct);
        if (relation is null) return false;
        var user = await _userManager.FindByIdAsync(relation.OwnerId.ToString());
        var profile = await _db.OwnerProfiles.FirstOrDefaultAsync(x => x.UserId == relation.OwnerId, ct);
        if (user is null || profile is null) return false;
        var duplicate = await _userManager.FindByEmailAsync(model.Email);
        if (duplicate is not null && duplicate.Id != user.Id) return false;

        user.Email = model.Email; user.UserName = model.Email; user.PhoneNumber = model.PhoneNumber;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded) return false;
        profile.FullName = model.FullName; profile.MailingAddress = model.MailingAddress; profile.PayoutPreference = model.PayoutPreference; profile.UpdatedAt = DateTime.UtcNow;
        relation.OwnershipPercentage = model.OwnershipPercentage; relation.ValidFrom = model.ValidFrom; relation.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct); return true;
    }

    public async Task<IReadOnlyList<OwnerSearchResult>> SearchOwnersAsync(long unitId, string term, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(term)) return [];
        term = term.Trim();
        return await (from profile in _db.OwnerProfiles.AsNoTracking()
                      join user in _db.Users.AsNoTracking() on profile.UserId equals user.Id
                      where profile.FullName.Contains(term) || (user.Email != null && user.Email.Contains(term)) || (user.PhoneNumber != null && user.PhoneNumber.Contains(term))
                      select new OwnerSearchResult { OwnerId = profile.UserId, FullName = profile.FullName, Email = user.Email, PhoneNumber = user.PhoneNumber, UnitCount = _db.UnitOwners.Count(r => r.OwnerId == profile.UserId), IsAlreadyLinked = _db.UnitOwners.Any(r => r.UnitId == unitId && r.OwnerId == profile.UserId) })
            .OrderBy(x => x.FullName).Take(20).ToListAsync(ct);
    }

    public async Task<bool> AddExistingOwnerToUnitAsync(long unitId, long ownerId, decimal ownershipPercentage, DateOnly validFrom, CancellationToken ct = default)
    {
        if (!await _db.Units.AnyAsync(x => x.UnitId == unitId, ct) || !await _db.OwnerProfiles.AnyAsync(x => x.UserId == ownerId, ct) || await _db.UnitOwners.AnyAsync(x => x.UnitId == unitId && x.OwnerId == ownerId, ct) || ownershipPercentage <= 0 || ownershipPercentage > 100) return false;
        _db.UnitOwners.Add(new UnitOwner { UnitId = unitId, OwnerId = ownerId, OwnershipPercentage = ownershipPercentage, ValidFrom = validFrom, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync(ct); return true;
    }
}
