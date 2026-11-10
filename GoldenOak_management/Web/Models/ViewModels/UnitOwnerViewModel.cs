namespace Web.Models;

public sealed class UnitOwnerViewModel
{
    public long UnitId { get; init; }
    public long BuildingId { get; init; }
    public string UnitNumber { get; init; } = string.Empty;
    public string BuildingName { get; init; } = string.Empty;
    public IReadOnlyList<UnitOwnerSummary> Owners { get; init; } = [];
}

public sealed class UnitOwnerSummary
{
    public long UnitOwnerId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public decimal OwnershipPercentage { get; init; }
    public bool IsPrimaryPayoutOwner { get; init; }
    public DateOnly ValidFrom { get; init; }
    public DateOnly? ValidTo { get; init; }
}

