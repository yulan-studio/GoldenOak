namespace Web.Models;

public sealed class OwnerSearchResult
{
    public long OwnerId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public int UnitCount { get; init; }
    public bool IsAlreadyLinked { get; init; }
}
