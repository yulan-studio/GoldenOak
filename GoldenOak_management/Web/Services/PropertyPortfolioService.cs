using Microsoft.EntityFrameworkCore;
using Web.Data;
using Web.Models;

namespace Web.Services;

/// <summary>Buildings 和 Units 的 CRUD 业务服务。</summary>
public sealed class PropertyPortfolioService : IPropertyPortfolioService
{
    private readonly ApplicationDbContext _db;
    public PropertyPortfolioService(ApplicationDbContext db) => _db = db;

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
}
