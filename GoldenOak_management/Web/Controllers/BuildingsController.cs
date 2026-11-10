using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Web.Models;
using Web.Services;

namespace Web.Controllers;

[Authorize]
public sealed class BuildingsController(IPropertyPortfolioService service) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct) => View(await service.GetBuildingsAsync(ct));
    [Authorize(Roles = "ADMIN")]
    public IActionResult Create() => View(new Building { BuildingType = "APARTMENT", Status = "ACTIVE" });

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Create(Building model, CancellationToken ct) { if (!ModelState.IsValid) return View(model); await service.CreateBuildingAsync(model, ct); return RedirectToAction(nameof(Index)); }

    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Edit(long id, CancellationToken ct) { var item = await service.GetBuildingAsync(id, ct); return item is null ? NotFound() : View(item); }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Edit(long id, Building model, CancellationToken ct) { if (id != model.BuildingId) return BadRequest(); if (!ModelState.IsValid) return View(model); if (!await service.UpdateBuildingAsync(model, ct)) return NotFound(); return RedirectToAction(nameof(Index)); }

    public async Task<IActionResult> Units(long id, CancellationToken ct) { var item = await service.GetBuildingAsync(id, ct); return item is null ? NotFound() : View(item); }
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct) { var item = await service.GetBuildingAsync(id, ct); return item is null ? NotFound() : View(item); }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> DeleteConfirmed(long id, CancellationToken ct) { if (!await service.DeleteBuildingAsync(id, ct)) TempData["Error"] = "楼宇不存在，或仍然包含单元，无法删除。"; return RedirectToAction(nameof(Index)); }
}
