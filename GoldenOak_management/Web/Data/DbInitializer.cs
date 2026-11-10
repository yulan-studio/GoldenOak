using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

    /// <summary>
    /// 创建本地演示用的管理员账号，并确保该账号拥有 ADMIN 角色。
    /// 邮箱通过各自的配置提供，密码统一使用 DemoUser:Password，不写入源代码。
    /// </summary>
    public static async Task SeedDemoAdministratorAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        var email = configuration["DemoAdministrator:Email"];
        var password = configuration["DemoUser:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                Status = "ACTIVE"
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(x => x.Description));
                throw new InvalidOperationException($"Unable to create demo administrator: {errors}");
            }
        }

        await EnsureDemoPasswordAsync(userManager, user, password);

        if (!await userManager.IsInRoleAsync(user, "ADMIN"))
            await userManager.AddToRoleAsync(user, "ADMIN");
    }

    /// <summary>
    /// 创建本地演示用的物业经理账号和 Profile。
    /// 只有在配置了 DemoPropertyManager:Email 和 DemoUser:Password 时才执行，避免把密码写入源代码。
    /// </summary>
    public static async Task SeedDemoPropertyManagerAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        var email = configuration["DemoPropertyManager:Email"];
        var password = configuration["DemoUser:Password"];

        // 没有配置演示账号时跳过，不影响正式环境启动。
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<ApplicationDbContext>();
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                Status = "ACTIVE"
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(x => x.Description));
                throw new InvalidOperationException($"Unable to create demo user: {errors}");
            }
        }

        await EnsureDemoPasswordAsync(userManager, user, password);

        if (!await userManager.IsInRoleAsync(user, "PROPERTY_MANAGER"))
            await userManager.AddToRoleAsync(user, "PROPERTY_MANAGER");

        // 一个用户只建立一个 property_manager_profiles 记录。
        if (!await db.PropertyManagerProfiles.AnyAsync(x => x.UserId == user.Id))
        {
            db.PropertyManagerProfiles.Add(new PropertyManagerProfile
            {
                UserId = user.Id,
                FullName = "Demo Property Manager",
                AverageRating = 0,
                RatingCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// 创建 Phase 1 演示组合：业主、租客、楼宇、单元、产权关系和物业经理分配。
    /// 所有记录都使用稳定的邮箱和楼宇名称作为幂等判断，应用重复启动不会重复插入。
    /// </summary>
    public static async Task SeedDemoPortfolioAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        var managerEmail = configuration["DemoPropertyManager:Email"];
        var demoPassword = configuration["DemoUser:Password"];
        if (string.IsNullOrWhiteSpace(managerEmail) || string.IsNullOrWhiteSpace(demoPassword))
            return;

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<ApplicationDbContext>();
        var manager = await userManager.FindByEmailAsync(managerEmail);
        if (manager is null)
            return;

        var owner = await GetOrCreateUserAsync(userManager, "owner@goldenoak.com", "Demo Owner", demoPassword);
        var renter = await GetOrCreateUserAsync(userManager, "renter@goldenoak.com", "Demo Renter", demoPassword);
        if (!await userManager.IsInRoleAsync(owner, "OWNER"))
            await userManager.AddToRoleAsync(owner, "OWNER");
        if (!await userManager.IsInRoleAsync(renter, "RENTER"))
            await userManager.AddToRoleAsync(renter, "RENTER");

        if (!await db.OwnerProfiles.AnyAsync(x => x.UserId == owner.Id))
        {
            db.OwnerProfiles.Add(new OwnerProfile
            {
                UserId = owner.Id,
                FullName = "Alex Morgan",
                OwnerType = "INDIVIDUAL",
                MailingAddress = "100 King Street West, Toronto, ON",
                PayoutPreference = "BANK_TRANSFER",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        if (!await db.RenterProfiles.AnyAsync(x => x.UserId == renter.Id))
        {
            db.RenterProfiles.Add(new RenterProfile
            {
                UserId = renter.Id,
                FullName = "Jordan Lee",
                EmergencyContactName = "Taylor Lee",
                EmergencyContactPhone = "416-555-0148",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();

        var king = await GetOrCreateBuildingAsync(db, "100 King Street West", "100 King Street West", "Toronto", "ON", "M5X 1A9", 2);
        var oak = await GetOrCreateBuildingAsync(db, "28 Oak Avenue", "28 Oak Avenue", "Toronto", "ON", "M4C 1A1", 2);

        await GetOrCreateUnitAsync(db, king.BuildingId, "1204", 2, 2, 2450, "OCCUPIED");
        await GetOrCreateUnitAsync(db, king.BuildingId, "1502", 1, 1, 2150, "AVAILABLE");
        await GetOrCreateUnitAsync(db, oak.BuildingId, "304", 2, 1, 2180, "OCCUPIED");
        await GetOrCreateUnitAsync(db, oak.BuildingId, "410", 1, 1, 1950, "UNAVAILABLE");
        await db.SaveChangesAsync();

        var units = await db.Units.Where(x => x.BuildingId == king.BuildingId || x.BuildingId == oak.BuildingId).ToListAsync();
        foreach (var unit in units)
        {
            if (!await db.UnitOwners.AnyAsync(x => x.UnitId == unit.UnitId && x.OwnerId == owner.Id))
            {
                db.UnitOwners.Add(new UnitOwner
                {
                    UnitId = unit.UnitId,
                    OwnerId = owner.Id,
                    OwnershipPercentage = 100,
                    ValidFrom = new DateOnly(DateTime.UtcNow.Year, 1, 1),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

        }

        foreach (var buildingId in units.Select(x => x.BuildingId).Distinct())
        {
            if (!await db.PropertyManagerAssignments.AnyAsync(x => x.BuildingId == buildingId && x.PropertyManagerId == manager.Id && x.UnitId == null))
            {
                db.PropertyManagerAssignments.Add(new PropertyManagerAssignment
                {
                    PropertyManagerId = manager.Id,
                    BuildingId = buildingId,
                    AssignedFrom = new DateOnly(DateTime.UtcNow.Year, 1, 1),
                    Status = "ACTIVE",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task<ApplicationUser> GetOrCreateUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string fullName,
        string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is not null)
        {
            await EnsureDemoPasswordAsync(userManager, user, password);
            return user;
        }

        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            Status = "ACTIVE"
        };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Unable to create demo user '{fullName}': {string.Join(", ", result.Errors.Select(x => x.Description))}");
        return user;
    }

    /// <summary>
    /// 仅用于本地演示账号：如果已有账号使用旧密码，则同步为 DemoUser:Password。
    /// 正式环境不应在应用启动时自动重置用户密码。
    /// </summary>
    private static async Task EnsureDemoPasswordAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        string password)
    {
        if (await userManager.CheckPasswordAsync(user, password))
            return;

        if (await userManager.HasPasswordAsync(user))
        {
            var removeResult = await userManager.RemovePasswordAsync(user);
            if (!removeResult.Succeeded)
                throw new InvalidOperationException("Unable to reset demo user password.");
        }

        var addResult = await userManager.AddPasswordAsync(user, password);
        if (!addResult.Succeeded)
        {
            var errors = string.Join(", ", addResult.Errors.Select(x => x.Description));
            throw new InvalidOperationException($"Unable to set demo user password: {errors}");
        }
    }

    private static async Task<Building> GetOrCreateBuildingAsync(ApplicationDbContext db, string name, string address, string city, string province, string postalCode, int unitCount)
    {
        var building = await db.Buildings.FirstOrDefaultAsync(x => x.BuildingName == name);
        if (building is not null)
            return building;

        building = new Building
        {
            BuildingName = name,
            AddressLine1 = address,
            City = city,
            Province = province,
            PostalCode = postalCode,
            BuildingType = "APARTMENT",
            UnitCount = unitCount,
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Buildings.Add(building);
        await db.SaveChangesAsync();
        return building;
    }

    private static async Task<Unit> GetOrCreateUnitAsync(ApplicationDbContext db, long buildingId, string number, decimal bedrooms, decimal bathrooms, decimal rent, string status)
    {
        var unit = await db.Units.FirstOrDefaultAsync(x => x.BuildingId == buildingId && x.UnitNumber == number);
        if (unit is not null)
            return unit;

        unit = new Unit
        {
            BuildingId = buildingId,
            UnitNumber = number,
            Bedrooms = bedrooms,
            Bathrooms = bathrooms,
            AskingRent = rent,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Units.Add(unit);
        return unit;
    }
}
