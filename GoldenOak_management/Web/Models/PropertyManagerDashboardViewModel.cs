namespace Web.Models;

/// <summary>
/// 物业经理首页展示所需的汇总数据。
/// 页面只读取这个 ViewModel，不直接在 Razor 页面里访问数据库。
/// </summary>
public sealed class PropertyManagerDashboardViewModel
{
    public int BuildingCount { get; init; }
    public int UnitCount { get; init; }
    public int OccupiedUnitCount { get; init; }
    public int AvailableUnitCount { get; init; }
    public int UnavailableUnitCount { get; init; }
    public int OwnerCount { get; init; }
    public int RenterCount { get; init; }
    public IReadOnlyList<ManagedBuildingSummary> Buildings { get; init; } = [];
}

public sealed class ManagedBuildingSummary
{
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public int UnitCount { get; init; }
    public int OccupiedCount { get; init; }
    public int AvailableCount { get; init; }
    public string Status { get; init; } = string.Empty;
}
