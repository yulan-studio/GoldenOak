using Microsoft.AspNetCore.Mvc;
using Web.Models;
using Web.Services;

namespace Web.Controllers;

public sealed class UnitsController(IPropertyPortfolioService service) : Controller
{
    public async Task<IActionResult> Create(long buildingId, CancellationToken ct) { var building = await service.GetBuildingAsync(buildingId, ct); if (building is null) return NotFound(); ViewBag.Building = building; return View(new Unit { BuildingId = buildingId, Status = "AVAILABLE" }); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Unit model, CancellationToken ct) { var building = await service.GetBuildingAsync(model.BuildingId, ct); if (building is null) return NotFound(); if (!ModelState.IsValid) { ViewBag.Building = building; return View(model); } await service.CreateUnitAsync(model, ct); return RedirectToAction("Units", "Buildings", new { id = model.BuildingId }); }

    public async Task<IActionResult> Edit(long id, CancellationToken ct) { var item = await service.GetUnitAsync(id, ct); return item is null ? NotFound() : View(item); }

    public async Task<IActionResult> Owners(long id, CancellationToken ct) { var model = await service.GetUnitOwnersAsync(id, ct); return model is null ? NotFound() : View(model); }

    [HttpGet]
    public async Task<IActionResult> CreateOwner(long unitId, CancellationToken ct) { var model = await service.GetCreateOwnerModelAsync(unitId, ct); return model is null ? NotFound() : View(model); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOwner(CreateOwnerForUnitViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        if (!await service.CreateOwnerAndAssignAsync(model, ct)) { ModelState.AddModelError(string.Empty, "无法创建业主，Email 可能已经存在。"); return View(model); }
        return RedirectToAction(nameof(Owners), new { id = model.UnitId });
    }

    [HttpGet]
    public async Task<IActionResult> SearchOwners(long unitId, string term, CancellationToken ct) => Json(await service.SearchOwnersAsync(unitId, term, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddExistingOwner(long unitId, long ownerId, decimal ownershipPercentage, DateOnly validFrom, CancellationToken ct)
    {
        if (!await service.AddExistingOwnerToUnitAsync(unitId, ownerId, ownershipPercentage, validFrom, ct))
            TempData["Error"] = "无法关联业主，请检查业主、产权比例或重复关联。";
        return RedirectToAction(nameof(Owners), new { id = unitId });
    }

    [HttpGet]
    public async Task<IActionResult> EditOwner(long id, CancellationToken ct) { var model = await service.GetEditOwnerModelAsync(id, ct); return model is null ? NotFound() : View(model); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditOwner(EditOwnerForUnitViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        if (!await service.UpdateOwnerForUnitAsync(model, ct)) { ModelState.AddModelError(string.Empty, "无法更新业主资料，Email 可能已经存在。"); return View(model); }
        return RedirectToAction(nameof(Owners), new { id = model.UnitId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddOwner(long unitId, long ownerId, decimal ownershipPercentage, DateOnly validFrom, CancellationToken ct)
    {
        await service.AddUnitOwnerAsync(unitId, ownerId, ownershipPercentage, validFrom, ct);
        return RedirectToAction(nameof(Owners), new { id = unitId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteOwner(long id, long unitId, CancellationToken ct)
    {
        await service.DeleteUnitOwnerAsync(id, ct);
        return RedirectToAction(nameof(Owners), new { id = unitId });
    }

    public async Task<IActionResult> Delete(long id, CancellationToken ct) { var item = await service.GetUnitAsync(id, ct); return item is null ? NotFound() : View(item); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, Unit model, CancellationToken ct) { if (id != model.UnitId) return BadRequest(); if (!ModelState.IsValid) return View(model); if (!await service.UpdateUnitAsync(model, ct)) return NotFound(); return RedirectToAction("Units", "Buildings", new { id = model.BuildingId }); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id, long buildingId, CancellationToken ct) { await service.DeleteUnitAsync(id, ct); return RedirectToAction("Units", "Buildings", new { id = buildingId }); }
}
