using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SyncSyntax.Data;
using SyncSyntax.Services;
using SyncSyntax.Settings;

var builder = WebApplication.CreateBuilder(args);

// MVC services
builder.Services.AddControllersWithViews();

// Cloudinary configuration
builder.Services.Configure<CloudinarySettings>(
    builder.Configuration.GetSection("Cloudinary"));

builder.Services.AddScoped<CloudinaryImageService>();

// PostgreSQL database connection
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

// Identity configuration
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

// Login cookie configuration
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.AccessDeniedPath = "/Auth/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

var app = builder.Build();

// Run database migration and create Admin
try
{
    using var scope = app.Services.CreateScope();

    var services = scope.ServiceProvider;

    var dbContext =
        services.GetRequiredService<AppDbContext>();

    var userManager =
        services.GetRequiredService<UserManager<IdentityUser>>();

    var roleManager =
        services.GetRequiredService<RoleManager<IdentityRole>>();

    await dbContext.Database.MigrateAsync();

    const string adminRole = "Admin";
    const string adminEmail = "admin@gmail.com";
    const string adminPassword = "admin";

    var existingAdminRole =
        await roleManager.FindByNameAsync(adminRole);

    if (existingAdminRole == null)
    {
        var roleResult = await roleManager.CreateAsync(
            new IdentityRole(adminRole));

        if (!roleResult.Succeeded)
        {
            foreach (var error in roleResult.Errors)
            {
                Console.WriteLine(
                    $"Role error: {error.Description}");
            }
        }
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

        var userResult =
            await userManager.CreateAsync(
                adminUser,
                adminPassword);

        if (userResult.Succeeded)
        {
            await userManager.AddToRoleAsync(
                adminUser,
                adminRole);

            Console.WriteLine(
                "Admin user created successfully.");
        }
        else
        {
            foreach (var error in userResult.Errors)
            {
                Console.WriteLine(
                    $"Admin error: {error.Description}");
            }
        }
    }
    else
    {
        var isAdmin =
            await userManager.IsInRoleAsync(
                existingAdminUser,
                adminRole);

        if (!isAdmin)
        {
            await userManager.AddToRoleAsync(
                existingAdminUser,
                adminRole);
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

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Post}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();