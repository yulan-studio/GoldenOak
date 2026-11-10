using System.ComponentModel.DataAnnotations;

namespace Web.Models;

public sealed class EditOwnerForUnitViewModel
{
    public long UnitOwnerId { get; init; }
    public long UnitId { get; init; }
    public string BuildingName { get; init; } = string.Empty;
    public string UnitNumber { get; init; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }
    public string? MailingAddress { get; set; }

    [Range(0.01, 100)]
    public decimal OwnershipPercentage { get; set; }
    public DateOnly ValidFrom { get; set; }
    public string PayoutPreference { get; set; } = "BANK_TRANSFER";
}
