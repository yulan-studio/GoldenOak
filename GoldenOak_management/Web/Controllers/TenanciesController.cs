using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.Data;
using Web.Models;
using Web.Services;

namespace Web.Controllers;

[Authorize]
public sealed class TenanciesController(
    ITenancyService service,
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var items = await service.GetTenanciesAsync(ct);
        if (User.IsInRole("RENTER"))
        {
            var user = await userManager.GetUserAsync(User);
            if (user is null) return Challenge();
            items = items.Where(x => x.Renters.Any(r => r.RenterId == user.Id)).ToList();
        }

        return View(items);
    }

    public async Task<IActionResult> Details(long id, CancellationToken ct)
    {
        var item = await service.GetTenancyAsync(id, ct);
        if (item is null) return NotFound();
        if (!await CanViewAsync(item)) return Forbid();
        return View(item);
    }

    [Authorize(Roles = "ADMIN,PROPERTY_MANAGER")]
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        await LoadCreateOptionsAsync(ct);
        return View(new CreateTenancyViewModel
        {
            LeaseStartDate = DateOnly.FromDateTime(DateTime.Today),
            LeaseEndDate = DateOnly.FromDateTime(DateTime.Today).AddYears(1)
        });
    }

    [Authorize(Roles = "ADMIN,PROPERTY_MANAGER")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTenancyViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) { await LoadCreateOptionsAsync(ct); return View(model); }
        var id = await service.CreateTenancyAsync(new Tenancy
        {
            UnitId = model.UnitId,
            LeaseStartDate = model.LeaseStartDate,
            LeaseEndDate = model.LeaseEndDate,
            MonthlyRent = model.MonthlyRent,
            DepositAmount = model.DepositAmount,
            DepositReceivedDate = model.DepositReceivedDate,
            Status = "PENDING"
        }, ct);
        if (id == 0)
        {
            ModelState.AddModelError(string.Empty, "无法创建租约：请检查日期、金额、房源以及是否已有 ACTIVE 租约。");
            await LoadCreateOptionsAsync(ct); return View(model);
        }

        if (model.PrimaryRenterId > 0 && !await service.AddRenterAsync(id, model.PrimaryRenterId, true, ct))
        {
            ModelState.AddModelError(nameof(model.PrimaryRenterId), "租客不存在，租约已创建但未添加主租客。");
            await LoadCreateOptionsAsync(ct); return View(model);
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "ADMIN,PROPERTY_MANAGER")]
    [HttpGet]
    public async Task<IActionResult> Edit(long id, CancellationToken ct)
    {
        var item = await service.GetTenancyAsync(id, ct);
        return item is null ? NotFound() : View(new EditTenancyViewModel
        {
            TenancyId = item.TenancyId,
            UnitId = item.UnitId,
            LeaseStartDate = item.LeaseStartDate,
            LeaseEndDate = item.LeaseEndDate,
            MonthlyRent = item.MonthlyRent,
            DepositAmount = item.DepositAmount,
            DepositReceivedDate = item.DepositReceivedDate
        });
    }

    [Authorize(Roles = "ADMIN,PROPERTY_MANAGER")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, EditTenancyViewModel model, CancellationToken ct)
    {
        if (id != model.TenancyId) return BadRequest();
        if (!ModelState.IsValid) return View(model);
        if (!await service.UpdateTenancyAsync(new Tenancy
        {
            TenancyId = model.TenancyId,
            UnitId = model.UnitId,
            LeaseStartDate = model.LeaseStartDate,
            LeaseEndDate = model.LeaseEndDate,
            MonthlyRent = model.MonthlyRent,
            DepositAmount = model.DepositAmount,
            DepositReceivedDate = model.DepositReceivedDate
        }, ct))
        {
            ModelState.AddModelError(string.Empty, "无法更新租约：请检查日期、金额、房源以及 ACTIVE 租约冲突。");
            return View(model);
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "ADMIN,PROPERTY_MANAGER")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(long id, string status, CancellationToken ct)
    {
        if (!await service.ChangeStatusAsync(id, status, ct))
            TempData["Error"] = "无法修改状态，请检查租约或 ACTIVE 租约冲突。";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "ADMIN,PROPERTY_MANAGER")]
    [HttpGet]
    public async Task<IActionResult> AddRenter(long id, CancellationToken ct)
    {
        var item = await service.GetTenancyAsync(id, ct);
        return item is null ? NotFound() : View(item);
    }

    [Authorize(Roles = "ADMIN,PROPERTY_MANAGER")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddRenter(long id, long renterId, bool isPrimary, CancellationToken ct)
    {
        if (!await service.AddRenterAsync(id, renterId, isPrimary, ct))
            TempData["Error"] = "无法添加租客，租约或租客不存在，或该租客已经添加。";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "ADMIN,PROPERTY_MANAGER")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveRenter(long id, long renterId, CancellationToken ct)
    {
        await service.RemoveRenterAsync(id, renterId, ct);
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<bool> CanViewAsync(Tenancy tenancy)
    {
        if (!User.IsInRole("RENTER")) return true;
        var user = await userManager.GetUserAsync(User);
        return user is not null && tenancy.Renters.Any(x => x.RenterId == user.Id);
    }

    private async Task LoadCreateOptionsAsync(CancellationToken ct)
    {
        ViewBag.Buildings = await db.Buildings.AsNoTracking().Include(x => x.Units).OrderBy(x => x.BuildingName).ToListAsync(ct);
        ViewBag.Renters = await db.RenterProfiles.AsNoTracking().OrderBy(x => x.FullName).ToListAsync(ct);
    }
}
