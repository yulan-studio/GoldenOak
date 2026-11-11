using System.ComponentModel.DataAnnotations;

namespace Web.Models;

public class EditTenancyViewModel
{
    [Required]
    public long TenancyId { get; set; }

    [Range(1, long.MaxValue)]
    public long UnitId { get; set; }

    [Required]
    public DateOnly LeaseStartDate { get; set; }

    [Required]
    public DateOnly LeaseEndDate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal MonthlyRent { get; set; }

    [Range(0, double.MaxValue)]
    public decimal DepositAmount { get; set; }

    public DateOnly? DepositReceivedDate { get; set; }
}
