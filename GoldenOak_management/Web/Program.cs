using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Web.Data;
using Web.Models;
using Web.Services;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Enable shared English/Chinese UI localization.
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseMySql(
	connectionString,
	new MySqlServerVersion(new Version(8, 4, 0))));


builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
	.AddRoles<ApplicationRole>()
	.AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews()
	.AddViewLocalization()
	.AddDataAnnotationsLocalization();

// 注册业务服务。Controller 通过接口调用业务逻辑，不直接访问数据库。
builder.Services.AddScoped<IPropertyManagerDashboardService, PropertyManagerDashboardService>();
builder.Services.AddScoped<IPropertyPortfolioService, PropertyPortfolioService>();

var app = builder.Build();

var supportedCultures = new[] { new CultureInfo("zh-CN"), new CultureInfo("en-US") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
	DefaultRequestCulture = new RequestCulture("zh-CN"),
	SupportedCultures = supportedCultures,
	SupportedUICultures = supportedCultures,
	RequestCultureProviders = new RequestCultureProvider[]
	{
		new CookieRequestCultureProvider(),
		new AcceptLanguageHeaderRequestCultureProvider()
	}
});

// 创建一个服务作用域来取得 RoleManager 等 scoped 服务。
// 应用启动时初始化系统基础角色，确保登录和授权页面可以立即使用这些角色。
// DbInitializer 会先检查角色是否存在，因此重复启动不会创建重复数据，也不会覆盖已有角色。
using (var scope = app.Services.CreateScope())
{
	await DbInitializer.SeedRolesAsync(scope.ServiceProvider);
	await DbInitializer.SeedDemoPropertyManagerAsync(scope.ServiceProvider, app.Configuration);
	await DbInitializer.SeedDemoPortfolioAsync(scope.ServiceProvider, app.Configuration);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseMigrationsEndPoint();
}
else
{
	app.UseExceptionHandler("/Home/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();
// 启用 wwwroot 下的 CSS、JavaScript 和图片等静态资源。
// 如果缺少这一行，页面 HTML 可以正常显示，但 site.css 不会被浏览器加载。
app.UseStaticFiles();
app.UseRouting();

// Identity 登录状态必须在授权检查之前恢复。
app.UseAuthentication();
app.UseAuthorization();



app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");


app.MapRazorPages();
   

app.Run();
