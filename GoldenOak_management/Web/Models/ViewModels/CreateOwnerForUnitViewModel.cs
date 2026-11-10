using System.ComponentModel.DataAnnotations;

namespace Web.Models;

public sealed class CreateOwnerForUnitViewModel
{
    public long UnitId { get; init; }
    public string BuildingName { get; init; } = string.Empty;
    public string UnitNumber { get; init; } = string.Empty;

    [Required, Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }

    public string? MailingAddress { get; set; }

    [Range(0.01, 100)]
    public decimal OwnershipPercentage { get; set; } = 100;

    [Required]
    public DateOnly ValidFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public string PayoutPreference { get; set; } = "BANK_TRANSFER";
    public string? AccountHolderName { get; set; }
    public string? BankName { get; set; }
    [StringLength(4, MinimumLength = 4)] public string? AccountLastFour { get; set; }
    public bool IsPrimaryPayoutOwner { get; set; }
}
