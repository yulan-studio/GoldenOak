using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Services;

namespace Web.Controllers;

[Authorize(Roles = "PROPERTY_MANAGER")]
public sealed class PropertyManagerController(IPropertyManagerDashboardService dashboardService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await dashboardService.GetDashboardAsync(ct));
}
