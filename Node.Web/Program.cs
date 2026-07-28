using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Node.Data.Data;
using Node.Data.Models;
using Node.Web.Services;
using Node.Web.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Databankcontext: connection string komt uit appsettings (lokaal) of
// user-secrets/omgevingsvariabelen (publieke databank, geen geheimen in de repo).
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' ontbreekt.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Identity met rollen en de eigen ApplicationUser.
// De standaard Identity-UI wordt niet gebruikt: de registratie-, login- en
// beheerpagina's zijn eigen MVC-controllers en views.
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Inloggen kan pas nadat het e-mailadres bevestigd is.
        options.SignIn.RequireConfirmedEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders(); // Nodig voor e-mailbevestigingstokens.

// Paden van de eigen login- en geen-toegang-pagina's.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Cookie van een geblokkeerde/gewijzigde gebruiker wordt binnen de vijf
// minuten ongeldig (de beheerder vernieuwt bij blokkeren de security stamp).
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.FromMinutes(5));

// Eigen services.
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<ISwipeService, SwipeService>();
builder.Services.AddScoped<IMatchService, MatchService>();
builder.Services.AddScoped<IChartService, ChartService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Uitgebreide seeding bij het opstarten van een (gedeeltelijk) lege databank:
// migraties toepassen en demogegevens aanvullen.
using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

// HTTP-pijplijn configureren.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Eerst vaststellen wie de gebruiker is, daarna pas wat die mag.
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

await app.RunAsync();
