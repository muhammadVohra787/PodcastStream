using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Extensions.NETCore.Setup;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Podcast_MVC.Data;
using Podcast_MVC.Models;
using Podcast_MVC.Scripts;
using Podcast_MVC.Services;

var builder = WebApplication.CreateBuilder(args);

var awsService = AwsService.GetInstance(builder.Configuration);

string connectionString;

if (builder.Environment.IsDevelopment())
{
    // Production: use local/fallback connection string
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection not found.");
    Console.WriteLine("Using DefaultConnection for Dev.");
}
else
{
    // Development: use RDS via SSM
    var dbEndpoint = await awsService.GetParameterAsync("/podcast/database/endpoint");
    var dbUser = await awsService.GetParameterAsync("/podcast/database/username", true);
    var dbPassword = await awsService.GetParameterAsync("/podcast/database/password", true);

    connectionString =
        $"Server={dbEndpoint};Database=podcastdb;User Id={dbUser};Password={dbPassword};Encrypt=True;TrustServerCertificate=True;";
    Console.WriteLine("Using AWS parameter store for Prod.");

}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configure Identity
builder.Services.AddDefaultIdentity<User>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>() 
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();


builder.Services.AddSingleton<AwsService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return AwsService.GetInstance(config);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DatabaseInitializer.Initialize(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during database seeding");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
app.Run();
