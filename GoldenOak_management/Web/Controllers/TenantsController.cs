using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Web.Models;
using Web.Services;

namespace Web.Controllers;

[Authorize(Roles = "ADMIN,PROPERTY_MANAGER")]
public sealed class TenantsController(IRenterService service, UserManager<ApplicationUser> userManager, IStringLocalizer<SharedResource> localizer) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var renters = await service.GetRentersAsync(ct);
        var contacts = new Dictionary<long, ApplicationUser>();
        foreach (var renter in renters)
        {
            var user = await userManager.FindByIdAsync(renter.UserId.ToString());
            if (user is not null) contacts[renter.UserId] = user;
        }
        ViewBag.RenterContacts = contacts;
        return View("~/Views/Tenants/Index.cshtml", renters);
    }
    [HttpGet] public async Task<IActionResult> Search(string term, CancellationToken ct) => Json(await service.SearchRentersAsync(term, ct));
    [HttpGet("/Renters/Search")] public async Task<IActionResult> LegacySearch(string term, CancellationToken ct) => Json(await service.SearchRentersAsync(term, ct));
    [Authorize(Roles = "ADMIN")]
    [HttpGet] public IActionResult Create() => View("~/Views/Tenants/Create.cshtml", new CreateRenterViewModel());
    [Authorize(Roles = "ADMIN")]
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Create(CreateRenterViewModel model, CancellationToken ct) { if (!ModelState.IsValid) return View("~/Views/Tenants/Create.cshtml", model); if (await service.CreateRenterAsync(model, ct) == 0) { ModelState.AddModelError(string.Empty, "无法创建租客，邮箱可能已经存在。"); return View("~/Views/Tenants/Create.cshtml", model); } return RedirectToAction(nameof(Index)); }
    [Authorize(Roles = "ADMIN")]
    [HttpGet] public async Task<IActionResult> Edit(long id, CancellationToken ct) { var x = await service.GetRenterAsync(id, ct); if (x is null) return NotFound(); var user = await userManager.FindByIdAsync(id.ToString()); if (user is null) return NotFound(); return View("~/Views/Tenants/Edit.cshtml", new EditRenterViewModel { UserId = x.UserId, FullName = x.FullName, Email = user.Email ?? string.Empty, PhoneNumber = user.PhoneNumber, DateOfBirth = x.DateOfBirth, EmergencyContactName = x.EmergencyContactName, EmergencyContactPhone = x.EmergencyContactPhone }); }
    [Authorize(Roles = "ADMIN")]
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Edit(EditRenterViewModel model, CancellationToken ct) { if (!ModelState.IsValid) return View("~/Views/Tenants/Edit.cshtml", model); if (!await service.UpdateRenterAsync(model, ct)) { ModelState.AddModelError(string.Empty, "无法更新租客资料。"); return View("~/Views/Tenants/Edit.cshtml", model); } return RedirectToAction(nameof(Index)); }
    [Authorize(Roles = "ADMIN")]
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Archive(long id, CancellationToken ct) { await service.ArchiveRenterAsync(id, ct); return RedirectToAction(nameof(Index)); }
    [Authorize(Roles = "ADMIN")]
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Delete(long id, CancellationToken ct) { if (!await service.DeleteRenterAsync(id, ct)) TempData["Error"] = localizer["TenantDeleteError"].Value; return RedirectToAction(nameof(Index)); }
}
