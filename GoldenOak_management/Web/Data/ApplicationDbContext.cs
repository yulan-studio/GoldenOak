using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Web.Models;

namespace Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, long>(options)
{
    // 业务实体集合：EF Core 根据这些 DbSet 和实体配置生成对应数据库表。
    public DbSet<OwnerProfile> OwnerProfiles => Set<OwnerProfile>();
    public DbSet<RenterProfile> RenterProfiles => Set<RenterProfile>();
    public DbSet<PropertyManagerProfile> PropertyManagerProfiles => Set<PropertyManagerProfile>();
    public DbSet<ContractorProfile> ContractorProfiles => Set<ContractorProfile>();
    public DbSet<RealtorProfile> RealtorProfiles => Set<RealtorProfile>();
    public DbSet<AdminProfile> AdminProfiles => Set<AdminProfile>();
    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<UnitOwner> UnitOwners => Set<UnitOwner>();
    public DbSet<PropertyManagerAssignment> PropertyManagerAssignments => Set<PropertyManagerAssignment>();
    public DbSet<Tenancy> Tenancies => Set<Tenancy>();
    public DbSet<TenancyRenter> TenancyRenters => Set<TenancyRenter>();
    public DbSet<Lease> Leases => Set<Lease>();

    /// <summary>
    /// 配置 Identity 表名、业务表索引和外键关系。
    /// 所有删除关系默认使用 Restrict，避免删除用户时级联删除财务或历史业务数据。
    /// </summary>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Identity 表映射到数据库设计中的 users、roles 和关联表。
        builder.Entity<ApplicationUser>().ToTable("users");
        builder.Entity<ApplicationRole>().ToTable("roles");
        builder.Entity<IdentityUserRole<long>>().ToTable("user_roles");
        builder.Entity<IdentityUserClaim<long>>().ToTable("user_claims");
        builder.Entity<IdentityUserLogin<long>>().ToTable("user_logins");
        builder.Entity<IdentityRoleClaim<long>>().ToTable("role_claims");
        builder.Entity<IdentityUserToken<long>>().ToTable("user_tokens");

        builder.Entity<ApplicationUser>().Property(x => x.Status).HasMaxLength(30).IsRequired();

        // 一个用户最多对应一个同类型 profile，防止同一用户出现重复 profile。
        foreach (var entity in new[] { typeof(OwnerProfile), typeof(RenterProfile), typeof(PropertyManagerProfile), typeof(ContractorProfile), typeof(RealtorProfile), typeof(AdminProfile) })
            builder.Entity(entity).HasIndex("UserId").IsUnique();

        // Profile 通过 user_id 关联 users。
        // Restrict 表示删除用户前必须先处理 Profile，避免删除用户时级联删除 Profile。
        // 删除 Profile 不会反向删除 users。
        builder.Entity<OwnerProfile>().HasOne(x => x.User).WithOne().HasForeignKey<OwnerProfile>(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<RenterProfile>().HasOne(x => x.User).WithOne().HasForeignKey<RenterProfile>(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PropertyManagerProfile>().HasOne(x => x.User).WithOne().HasForeignKey<PropertyManagerProfile>(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ContractorProfile>().HasOne(x => x.User).WithOne().HasForeignKey<ContractorProfile>(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<RealtorProfile>().HasOne(x => x.User).WithOne().HasForeignKey<RealtorProfile>(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<AdminProfile>().HasOne(x => x.User).WithOne().HasForeignKey<AdminProfile>(x => x.UserId).OnDelete(DeleteBehavior.Restrict);

        // 房产关系：一栋 building 可以有多个 unit；同一栋楼内 unit_number 必须唯一。
        builder.Entity<Building>().HasMany(x => x.Units).WithOne(x => x.Building).HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Unit>().HasIndex(x => new { x.BuildingId, x.UnitNumber }).IsUnique();
        builder.Entity<UnitOwner>().HasOne(x => x.Unit).WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<UnitOwner>().HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PropertyManagerAssignment>().HasOne(x => x.PropertyManager).WithMany().HasForeignKey(x => x.PropertyManagerId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PropertyManagerAssignment>().HasOne(x => x.Building).WithMany().HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PropertyManagerAssignment>().HasOne(x => x.Unit).WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);

        // 租约关系：一个房源可以有多条历史租约，但同一时间的有效租约由业务服务控制。
        builder.Entity<Tenancy>()
            .HasOne(x => x.Unit)
            .WithMany()
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        // 一个租约与租客是多对多关系，使用 tenancy_renters 作为关联表。
        builder.Entity<TenancyRenter>()
            .HasKey(x => new { x.TenancyId, x.RenterId });

        builder.Entity<TenancyRenter>()
            .HasOne(x => x.Tenancy)
            .WithMany(x => x.Renters)
            .HasForeignKey(x => x.TenancyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TenancyRenter>()
            .HasOne(x => x.Renter)
            .WithMany()
            .HasForeignKey(x => x.RenterId)
            .OnDelete(DeleteBehavior.Restrict);

        // 一个 tenancy 最多对应一份正式 lease。
        builder.Entity<Lease>()
            .HasOne(x => x.Tenancy)
            .WithOne(x => x.Lease)
            .HasForeignKey<Lease>(x => x.TenancyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
