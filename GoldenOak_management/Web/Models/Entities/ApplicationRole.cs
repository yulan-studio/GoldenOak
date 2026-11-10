using Microsoft.AspNetCore.Identity;

namespace Web.Models;

/// <summary>
/// 系统角色实体。角色用于权限控制，例如 ADMIN、PROPERTY_MANAGER、OWNER、RENTER 和 CONTRACTOR。
/// </summary>
public class ApplicationRole : IdentityRole<long>
{
}
