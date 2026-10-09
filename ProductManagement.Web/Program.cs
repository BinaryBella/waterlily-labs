using Microsoft.EntityFrameworkCore;
using ProductManagement.Web.Data;
using ProductManagement.Web.Repositories;
using ProductManagement.Web.Services;
using ProductManagement.Web.Services.Caching;

var builder = WebApplication.CreateBuilder(args);

// The connection string in appsettings.json has no password. In Development the full
// string comes from user secrets, which override appsettings. Failing here at startup
// gives a clear message instead of an obscure error on the first database call.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

// Scoped to match the DbContext: one repository per request.
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// Singleton because it wraps the singleton IMemoryCache and holds no per-request state.
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IReportService, ReportService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    // Logs the unhandled exception, then re-runs the request as /Error to show the shared error page.
    // Development shows the detailed developer exception page instead.
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.MapStaticAssets();

// The Product List page is the home page.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Products}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
