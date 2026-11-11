using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Web.Data;
using Web.Models;

namespace Web.Services;

public sealed class RenterService(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : IRenterService
{
    public Task<List<RenterProfile>> GetRentersAsync(CancellationToken ct) =>
        db.RenterProfiles.AsNoTracking().OrderBy(x => x.FullName).ToListAsync(ct);

    public Task<RenterProfile?> GetRenterAsync(long userId, CancellationToken ct) =>
        db.RenterProfiles.FirstOrDefaultAsync(x => x.UserId == userId, ct);

    public async Task<List<RenterSearchResult>> SearchRentersAsync(string term, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(term)) return [];
        term = term.Trim();
        return await (from profile in db.RenterProfiles.AsNoTracking()
                      join user in db.Users.AsNoTracking() on profile.UserId equals user.Id
                      where profile.ArchivedAt == null &&
                            (profile.FullName.Contains(term) || (user.Email != null && user.Email.Contains(term)) || (user.PhoneNumber != null && user.PhoneNumber.Contains(term)))
                      orderby profile.FullName
                      select new RenterSearchResult { UserId = profile.UserId, FullName = profile.FullName, Email = user.Email, PhoneNumber = user.PhoneNumber }).Take(20).ToListAsync(ct);
    }

    public async Task<long> CreateRenterAsync(CreateRenterViewModel model, CancellationToken ct)
    {
        if (await userManager.FindByEmailAsync(model.Email) is not null) return 0;
        var user = new ApplicationUser { UserName = model.Email, Email = model.Email, PhoneNumber = model.PhoneNumber, Status = "ACTIVE" };
        var result = await userManager.CreateAsync(user);
        if (!result.Succeeded) return 0;
        await userManager.AddToRoleAsync(user, "RENTER");
        db.RenterProfiles.Add(new RenterProfile { UserId = user.Id, FullName = model.FullName, DateOfBirth = model.DateOfBirth, EmergencyContactName = model.EmergencyContactName, EmergencyContactPhone = model.EmergencyContactPhone, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);
        return user.Id;
    }

    public async Task<bool> UpdateRenterAsync(EditRenterViewModel model, CancellationToken ct)
    {
        var profile = await db.RenterProfiles.FirstOrDefaultAsync(x => x.UserId == model.UserId, ct);
        var user = await userManager.FindByIdAsync(model.UserId.ToString());
        if (profile is null || user is null) return false;
        var duplicate = await userManager.FindByEmailAsync(model.Email);
        if (duplicate is not null && duplicate.Id != user.Id) return false;
        user.Email = model.Email; user.UserName = model.Email; user.PhoneNumber = model.PhoneNumber;
        if (!(await userManager.UpdateAsync(user)).Succeeded) return false;
        profile.FullName = model.FullName; profile.DateOfBirth = model.DateOfBirth; profile.EmergencyContactName = model.EmergencyContactName; profile.EmergencyContactPhone = model.EmergencyContactPhone; profile.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct); return true;
    }

    public async Task<bool> ArchiveRenterAsync(long userId, CancellationToken ct)
    {
        var profile = await db.RenterProfiles.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (profile is null || user is null) return false;
        profile.ArchivedAt = DateTime.UtcNow; profile.UpdatedAt = DateTime.UtcNow; user.Status = "DEACTIVATED";
        await userManager.UpdateAsync(user); await db.SaveChangesAsync(ct); return true;
    }
}
