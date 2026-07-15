using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SyncSyntax.Data;

var builder = WebApplication.CreateBuilder(args);

// MVC services
builder.Services.AddControllersWithViews();

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

// Database migration आणि Admin तयार करणे
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

    // Database migration run करा
    await dbContext.Database.MigrateAsync();

    const string adminRole = "Admin";
    const string adminEmail = "admin@gmail.com";
    const string adminPassword = "admin";

    // Admin role तयार करा
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

    // Admin user शोधा
    var existingAdminUser =
        await userManager.FindByEmailAsync(adminEmail);

    // Admin user नसेल तर तयार करा
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
        // Admin user आहे पण role नसेल तर role add करा
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

// Production error handling
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// wwwroot मधील images, CSS आणि JS serve करण्यासाठी
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// .NET static assets mapping
app.MapStaticAssets();

// Default route
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Post}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();