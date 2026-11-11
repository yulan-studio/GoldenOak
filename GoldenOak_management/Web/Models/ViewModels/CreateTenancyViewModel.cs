using System.ComponentModel.DataAnnotations;

namespace Web.Models;

public class CreateTenancyViewModel
{
    [Range(1, long.MaxValue)]
    public long BuildingId { get; set; }

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

    [Range(1, long.MaxValue)]
    public long PrimaryRenterId { get; set; }
}
