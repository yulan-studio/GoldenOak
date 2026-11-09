using Microsoft.AspNetCore.Identity;

namespace Web.Models;

public class ApplicationUser : IdentityUser<long>
{
    public string Status { get; set; } = "ACTIVE";
    public DateTime? ArchivedAt { get; set; }
}
