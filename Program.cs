using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SyncSyntax.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 1;
}).AddEntityFrameworkStores<AppDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.AccessDeniedPath = "/Auth/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;


});


// Add services to the container.

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var _userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var _roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();


    string adminEmail = "admin@gmail.com";
    string adminPassword = "admin";

    var existingAdminRole = await _roleManager.FindByIdAsync("Admin");

    if (existingAdminRole == null)
    {
        await _roleManager.CreateAsync(new IdentityRole("Admin"));
    }

    var existingAdminUser = await _userManager.FindByEmailAsync(adminEmail);

    if (existingAdminUser == null)
    {
        var adminUser = new IdentityUser { UserName = adminEmail, Email = adminEmail };
        await _userManager.CreateAsync(adminUser, adminPassword);
        await _userManager.AddToRoleAsync(adminUser, "Admin");
    }

}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Post}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();