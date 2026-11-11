using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Web.Models;
using Web.Services;

namespace Web.Controllers;

[Authorize(Roles = "ADMIN,PROPERTY_MANAGER")]
public sealed class RentersController(IRenterService service, UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct) => View(await service.GetRentersAsync(ct));
    [HttpGet] public async Task<IActionResult> Search(string term, CancellationToken ct) => Json(await service.SearchRentersAsync(term, ct));
    [Authorize(Roles = "ADMIN")]
    [HttpGet] public IActionResult Create() => View(new CreateRenterViewModel());
    [Authorize(Roles = "ADMIN")]
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Create(CreateRenterViewModel model, CancellationToken ct) { if (!ModelState.IsValid) return View(model); if (await service.CreateRenterAsync(model, ct) == 0) { ModelState.AddModelError(string.Empty, "无法创建租客，邮箱可能已经存在。"); return View(model); } return RedirectToAction(nameof(Index)); }
    [Authorize(Roles = "ADMIN")]
    [HttpGet] public async Task<IActionResult> Edit(long id, CancellationToken ct) { var x = await service.GetRenterAsync(id, ct); if (x is null) return NotFound(); var user = await userManager.FindByIdAsync(id.ToString()); if (user is null) return NotFound(); return View(new EditRenterViewModel { UserId = x.UserId, FullName = x.FullName, Email = user.Email ?? string.Empty, PhoneNumber = user.PhoneNumber, DateOfBirth = x.DateOfBirth, EmergencyContactName = x.EmergencyContactName, EmergencyContactPhone = x.EmergencyContactPhone }); }
    [Authorize(Roles = "ADMIN")]
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Edit(EditRenterViewModel model, CancellationToken ct) { if (!ModelState.IsValid) return View(model); if (!await service.UpdateRenterAsync(model, ct)) { ModelState.AddModelError(string.Empty, "无法更新租客资料。"); return View(model); } return RedirectToAction(nameof(Index)); }
    [Authorize(Roles = "ADMIN")]
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Archive(long id, CancellationToken ct) { await service.ArchiveRenterAsync(id, ct); return RedirectToAction(nameof(Index)); }
}
