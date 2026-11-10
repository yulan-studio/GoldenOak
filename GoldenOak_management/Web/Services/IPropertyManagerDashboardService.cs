using Web.Models;

namespace Web.Services;

/// <summary>
/// 物业经理首页的数据查询和业务汇总服务。
/// Controller 不直接访问数据库，而是通过这个接口取得页面模型。
/// </summary>
public interface IPropertyManagerDashboardService
{
    Task<PropertyManagerDashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default);
}
