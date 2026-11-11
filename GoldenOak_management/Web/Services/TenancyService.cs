using Microsoft.EntityFrameworkCore;
using Web.Data;
using Web.Models;

namespace Web.Services;

public sealed class TenancyService : ITenancyService
{
    private readonly ApplicationDbContext _db;

    public TenancyService(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<List<Tenancy>> GetTenanciesAsync(CancellationToken ct)
    {
        return _db.Tenancies
            .AsNoTracking()
            .Include(x => x.Unit)
            .Include(x => x.Renters)
            .OrderByDescending(x => x.LeaseStartDate)
            .ThenBy(x => x.Unit.UnitNumber)
            .ToListAsync(ct);
    }

    public Task<Tenancy?> GetTenancyAsync(long id, CancellationToken ct)
    {
        return _db.Tenancies
            .Include(x => x.Unit)
            .Include(x => x.Renters)
            .Include(x => x.Lease)
            .FirstOrDefaultAsync(x => x.TenancyId == id, ct);
    }

    public async Task<long> CreateTenancyAsync(Tenancy model, CancellationToken ct)
    {
        if (!IsValidDatesAndAmounts(model) ||
            !await _db.Units.AnyAsync(x => x.UnitId == model.UnitId, ct) ||
            await HasActiveTenancyAsync(model.UnitId, null, ct))
            return 0;

        var now = DateTime.UtcNow;
        model.CreatedAt = now;
        model.UpdatedAt = now;
        _db.Tenancies.Add(model);
        await _db.SaveChangesAsync(ct);
        return model.TenancyId;
    }

    public async Task<bool> UpdateTenancyAsync(Tenancy model, CancellationToken ct)
    {
        var item = await _db.Tenancies.FirstOrDefaultAsync(x => x.TenancyId == model.TenancyId, ct);
        if (item is null || !IsValidDatesAndAmounts(model)) return false;

        if (item.UnitId != model.UnitId &&
            !await _db.Units.AnyAsync(x => x.UnitId == model.UnitId, ct))
            return false;

        if (string.Equals(model.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase) &&
            await HasActiveTenancyAsync(model.UnitId, model.TenancyId, ct))
            return false;

        item.UnitId = model.UnitId;
        item.LeaseStartDate = model.LeaseStartDate;
        item.LeaseEndDate = model.LeaseEndDate;
        item.MonthlyRent = model.MonthlyRent;
        item.DepositAmount = model.DepositAmount;
        item.DepositReceivedDate = model.DepositReceivedDate;
        item.Status = model.Status;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ChangeStatusAsync(long id, string status, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(status)) return false;

        var item = await _db.Tenancies.FirstOrDefaultAsync(x => x.TenancyId == id, ct);
        if (item is null) return false;

        status = status.Trim().ToUpperInvariant();
        if (status == "ACTIVE" && await HasActiveTenancyAsync(item.UnitId, id, ct))
            return false;

        item.Status = status;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> AddRenterAsync(long tenancyId, long renterId, bool isPrimary, CancellationToken ct)
    {
        var tenancyExists = await _db.Tenancies.AnyAsync(x => x.TenancyId == tenancyId, ct);
        var renterExists = await _db.RenterProfiles.AnyAsync(x => x.UserId == renterId, ct);
        var relationExists = await _db.TenancyRenters.AnyAsync(
            x => x.TenancyId == tenancyId && x.RenterId == renterId, ct);

        if (!tenancyExists || !renterExists || relationExists) return false;

        if (isPrimary)
        {
            var currentPrimary = await _db.TenancyRenters
                .Where(x => x.TenancyId == tenancyId && x.IsPrimary)
                .ToListAsync(ct);
            foreach (var renter in currentPrimary) renter.IsPrimary = false;
        }

        _db.TenancyRenters.Add(new TenancyRenter
        {
            TenancyId = tenancyId,
            RenterId = renterId,
            IsPrimary = isPrimary,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RemoveRenterAsync(long tenancyId, long renterId, CancellationToken ct)
    {
        var relation = await _db.TenancyRenters.FirstOrDefaultAsync(
            x => x.TenancyId == tenancyId && x.RenterId == renterId, ct);
        if (relation is null) return false;

        _db.TenancyRenters.Remove(relation);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static bool IsValidDatesAndAmounts(Tenancy model) =>
        model.LeaseStartDate <= model.LeaseEndDate &&
        model.MonthlyRent >= 0 &&
        model.DepositAmount >= 0;

    private Task<bool> HasActiveTenancyAsync(long unitId, long? excludedTenancyId, CancellationToken ct)
    {
        return _db.Tenancies.AnyAsync(
            x => x.UnitId == unitId &&
                 x.Status == "ACTIVE" &&
                 (!excludedTenancyId.HasValue || x.TenancyId != excludedTenancyId.Value), ct);
    }
}
