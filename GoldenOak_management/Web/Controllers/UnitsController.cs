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

    public async Task<IActionResult> Delete(long id, CancellationToken ct) { var item = await service.GetUnitAsync(id, ct); return item is null ? NotFound() : View(item); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, Unit model, CancellationToken ct) { if (id != model.UnitId) return BadRequest(); if (!ModelState.IsValid) return View(model); if (!await service.UpdateUnitAsync(model, ct)) return NotFound(); return RedirectToAction("Units", "Buildings", new { id = model.BuildingId }); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id, long buildingId, CancellationToken ct) { await service.DeleteUnitAsync(id, ct); return RedirectToAction("Units", "Buildings", new { id = buildingId }); }
}
