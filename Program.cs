using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SyncSyntax.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null);
        });
});

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 1;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.AccessDeniedPath = "/Auth/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

var app = builder.Build();

try
{
    using var scope = app.Services.CreateScope();

    var dbContext =
        scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var userManager =
        scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    var roleManager =
        scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    // Create PostgreSQL tables
    await dbContext.Database.MigrateAsync();

    const string adminRole = "Admin";
    const string adminEmail = "admin@gmail.com";
    const string adminPassword = "admin";

    var existingAdminRole =
        await roleManager.FindByNameAsync(adminRole);

    if (existingAdminRole == null)
    {
        await roleManager.CreateAsync(
            new IdentityRole(adminRole));
    }

    var existingAdminUser =
        await userManager.FindByEmailAsync(adminEmail);

    if (existingAdminUser == null)
    {
        var adminUser = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var result =
            await userManager.CreateAsync(
                adminUser,
                adminPassword);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(
                adminUser,
                adminRole);
        }
        else
        {
            foreach (var error in result.Errors)
            {
                Console.WriteLine(error.Description);
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine("Database error:");
    Console.WriteLine(ex.ToString());
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Post}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();