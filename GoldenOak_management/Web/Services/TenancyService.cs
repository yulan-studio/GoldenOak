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
                .ThenInclude(x => x.Building)
            .Include(x => x.Renters)
                .ThenInclude(x => x.Renter)
            .OrderByDescending(x => x.LeaseStartDate)
            .ThenBy(x => x.Unit.UnitNumber)
            .ToListAsync(ct);
    }

    public Task<Tenancy?> GetTenancyAsync(long id, CancellationToken ct)
    {
        return _db.Tenancies
            .Include(x => x.Unit)
            .Include(x => x.Renters)
                .ThenInclude(x => x.Renter)
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
        await UpdateUnitStatusAsync(model.UnitId, model.LeaseStartDate, ct);
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
        // Status is changed only through ChangeStatusAsync, never by the edit form.
        if (!string.IsNullOrWhiteSpace(model.Status))
            item.Status = model.Status.Trim().ToUpperInvariant();
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// 按业务规则修改租约状态，并同步更新对应房源状态。
    /// 状态流转规则由 IsAllowedTransition 统一判断，避免通过页面或接口绕过规则。
    /// </summary>
    public async Task<bool> ChangeStatusAsync(long id, string status, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(status)) return false;

        var item = await _db.Tenancies.FirstOrDefaultAsync(x => x.TenancyId == id, ct);
        if (item is null) return false;

        status = status.Trim().ToUpperInvariant();
        if (!IsAllowedTransition(item.Status, status)) return false;
        if (status == "ACTIVE" && await HasActiveTenancyAsync(item.UnitId, id, ct))
            return false;

        item.Status = status;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        await UpdateUnitStatusForStatusChangeAsync(item.UnitId, status, id, ct);
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

    private async Task UpdateUnitStatusAsync(long unitId, DateOnly leaseStartDate, CancellationToken ct)
    {
        var unit = await _db.Units.FirstOrDefaultAsync(x => x.UnitId == unitId, ct);
        if (unit is null) return;
        unit.Status = leaseStartDate <= DateOnly.FromDateTime(DateTime.UtcNow) ? "OCCUPIED" : "RESERVED";
        unit.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// 判断租约状态是否允许流转：
    /// PENDING -> ACTIVE/CANCELLED；ACTIVE -> ENDED/TERMINATED。
    /// 未列出的状态变化，包括终止后重新激活，一律拒绝。
    /// </summary>
    private static bool IsAllowedTransition(string currentStatus, string nextStatus) =>
        (currentStatus, nextStatus) switch
        {
            ("PENDING", "ACTIVE") => true,
            ("PENDING", "CANCELLED") => true,
            ("ACTIVE", "ENDED") => true,
            ("ACTIVE", "TERMINATED") => true,
            _ => false
        };

    /// <summary>
    /// 根据租约状态同步房源状态。
    /// ACTIVE 对应 OCCUPIED；租约结束、终止或取消后，只有没有其他 ACTIVE 租约时才恢复 AVAILABLE。
    /// </summary>
    private async Task UpdateUnitStatusForStatusChangeAsync(long unitId, string status, long tenancyId, CancellationToken ct)
    {
        var unit = await _db.Units.FirstOrDefaultAsync(x => x.UnitId == unitId, ct);
        if (unit is null) return;

        if (status == "ACTIVE")
        {
            unit.Status = "OCCUPIED";
        }
        else if (status is "ENDED" or "TERMINATED" or "CANCELLED")
        {
            var hasOtherActiveTenancy = await _db.Tenancies.AnyAsync(
                x => x.UnitId == unitId && x.TenancyId != tenancyId && x.Status == "ACTIVE", ct);
            if (!hasOtherActiveTenancy)
                unit.Status = "AVAILABLE";
        }

        unit.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
