using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using TimeTrackingSystem.Data;
using TimeTrackingSystem.Models.Entities;

var builder = WebApplication.CreateBuilder(args);

var databaseProvider = builder.Configuration["DatabaseProvider"];


builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("PostgreSQL")
        ));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(1);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    // options.AccessDeniedPath = "/"; TODO: Add after roles
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.HttpOnly = true;

});


// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    await DbInitializer.InitializeRolesAsync(roleManager);
}

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    var userManager = scope.ServiceProvider
        .GetRequiredService<UserManager<ApplicationUser>>();

    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    await UserSeeder.SeedAsync(userManager);
}

using (var scope = app.Services.CreateScope())
{
    //var userManager = scope.ServiceProvider.GetRequiredService(UserManager<ApplicationUser>);
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var existingUser = await userManager.FindByEmailAsync("test@example.com");
    if(existingUser == null)
    {
        var testUser = new ApplicationUser
        {
            UserName = "test@example.com",
            Email = "test@example.com"
        };

        var result = await userManager.CreateAsync(testUser, "Test123!");

        if (!result.Succeeded)
        {
            throw new Exception(
                string.Join("; ", result.Errors.Select(e => e.Description))
            );
        }

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

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
