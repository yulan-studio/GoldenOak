using Microsoft.AspNetCore.Identity;
using Web.Models;

namespace Web.Data;

/// <summary>
/// 初始化系统运行所需的基础 Identity 数据。
/// </summary>
public static class DbInitializer
{
    // 这些是系统第一版使用的基础角色。角色名称应统一使用大写代码，
    // 业务页面通过角色判断用户可以访问哪些功能。
    private static readonly string[] DefaultRoles =
    [
        "ADMIN",
        "PROPERTY_MANAGER",
        "OWNER",
        "RENTER",
        "CONTRACTOR",
        "REALTOR"
    ];

    /// <summary>
    /// 如果角色不存在则创建角色；已存在的角色不会重复创建或覆盖。
    /// </summary>
    public static async Task SeedRolesAsync(IServiceProvider services)
    {
        // RoleManager 由 ASP.NET Core Identity 注册，负责读写 roles 表。
        // 这里使用依赖注入取得服务，不直接创建数据库连接。
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

        foreach (var roleName in DefaultRoles)
        {
            // 先检查角色是否存在，保证应用每次启动都可以安全执行初始化。
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                // NormalizedName 用于 Identity 的大小写不敏感查询。
                var result = await roleManager.CreateAsync(new ApplicationRole
                {
                    Name = roleName,
                    NormalizedName = roleName
                });

                if (!result.Succeeded)
                {
                    // 角色创建失败时立即停止启动，避免应用在权限数据不完整的情况下运行。
                    var errors = string.Join(", ", result.Errors.Select(x => x.Description));
                    throw new InvalidOperationException($"Unable to create role '{roleName}': {errors}");
                }
            }
        }
    }
}
