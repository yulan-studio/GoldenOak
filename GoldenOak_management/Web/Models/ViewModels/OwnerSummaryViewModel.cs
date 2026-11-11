namespace Web.Models;

public class OwnerSummaryViewModel
{
    public long OwnerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public decimal OwnershipPercentage { get; set; }
    public bool IsPrimaryPayoutOwner { get; set; }
}
