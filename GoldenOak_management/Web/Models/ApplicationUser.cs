using Microsoft.AspNetCore.Identity;

namespace Web.Models;

/// <summary>
/// GoldenOak 的统一登录用户实体。登录、密码、邮箱、手机号和账号安全信息由 ASP.NET Core Identity 管理。
/// 业务资料应放在对应的 profile 表中，而不是全部放在 users 表中。
/// </summary>
public class ApplicationUser : IdentityUser<long>
{
    /// <summary>
    /// 用户账号状态。建议使用 ACTIVE、SUSPENDED、INVITED 或 DEACTIVATED。
    /// DEACTIVATED 表示账号不能登录或参与新的业务流程，但不代表删除历史资料。
    /// </summary>
    public string Status { get; set; } = "ACTIVE";

    /// <summary>
    /// 用户资料被归档的时间。为空表示当前资料未归档；有值表示资料已归档。
    /// 归档不会自动删除 users，也不会删除财务、租约或审计历史记录。
    /// </summary>
    public DateTime? ArchivedAt { get; set; }
}
