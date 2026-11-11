using System.ComponentModel.DataAnnotations;

namespace Web.Models;

public class CreateRenterViewModel { [Required, MaxLength(150)] public string FullName { get; set; } = string.Empty; [Required, EmailAddress] public string Email { get; set; } = string.Empty; public string? PhoneNumber { get; set; } public DateOnly? DateOfBirth { get; set; } public string? EmergencyContactName { get; set; } public string? EmergencyContactPhone { get; set; } }
public class EditRenterViewModel : CreateRenterViewModel { [Required] public long UserId { get; set; } }
