using Microsoft.EntityFrameworkCore;
using Web.Data;
using Web.Models;

namespace Web.Services;

/// <summary>
/// 物业经理首页服务。
/// 数据访问集中在 Service 中，避免 Controller 直接操作 DbContext。
/// </summary>
public sealed class PropertyManagerDashboardService : IPropertyManagerDashboardService
{
    private readonly ApplicationDbContext _db;

    public PropertyManagerDashboardService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PropertyManagerDashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var buildings = await _db.Buildings
            .AsNoTracking()
            .Include(x => x.Units)
            .OrderBy(x => x.BuildingName)
            .ToListAsync(cancellationToken);

        var units = buildings.SelectMany(x => x.Units).ToList();

        return new PropertyManagerDashboardViewModel
        {
            BuildingCount = buildings.Count,
            UnitCount = units.Count,
            OccupiedUnitCount = units.Count(x => x.Status == "OCCUPIED"),
            AvailableUnitCount = units.Count(x => x.Status == "AVAILABLE"),
            UnavailableUnitCount = units.Count(x => x.Status == "UNAVAILABLE"),
            OwnerCount = await _db.OwnerProfiles.AsNoTracking().CountAsync(cancellationToken),
            RenterCount = await _db.RenterProfiles.AsNoTracking().CountAsync(cancellationToken),
            Buildings = buildings.Select(x => new ManagedBuildingSummary
            {
                Name = x.BuildingName,
                Address = $"{x.AddressLine1}, {x.City}, {x.Province}",
                UnitCount = x.Units.Count,
                OccupiedCount = x.Units.Count(u => u.Status == "OCCUPIED"),
                AvailableCount = x.Units.Count(u => u.Status == "AVAILABLE"),
                Status = x.Status
            }).ToList()
        };
    }
}
