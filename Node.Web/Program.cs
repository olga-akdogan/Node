using GetStream;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Node.Data.Data;
using Node.Data.Models;
using Node.Data.Services;
using Node.Web.Services;
using Node.Web.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' ontbreekt.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));


builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Inloggen kan pas nadat het e-mailadres bevestigd is.
        options.SignIn.RequireConfirmedEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders(); // Nodig voor e-mailbevestigingstokens.


builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});


builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.FromMinutes(5));

// GetStream Chat
var streamApiKey = builder.Configuration["Stream:ApiKey"]
    ?? throw new InvalidOperationException("Configuratie 'Stream:ApiKey' ontbreekt.");
var streamApiSecret = builder.Configuration["Stream:ApiSecret"]
    ?? throw new InvalidOperationException("Configuratie 'Stream:ApiSecret' ontbreekt.");

builder.Services.AddSingleton(new StreamClient(streamApiKey, streamApiSecret));
// ChatClient expects generic GetStream.IClient-interface
builder.Services.AddSingleton<IClient>(sp => sp.GetRequiredService<StreamClient>());
builder.Services.AddSingleton<ChatClient>();


builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<ISwipeService, SwipeService>();
builder.Services.AddScoped<IMatchService, MatchService>();
builder.Services.AddScoped<IChartService, ChartService>();
builder.Services.AddScoped<IStreamChatService, StreamChatService>();
builder.Services.AddScoped<INatalChartCalculator, NatalChartCalculator>();

// Nominatim (OpenStreetMap) vereist een identificerende User-Agent per gebruiksvoorwaarden.
builder.Services.AddHttpClient<IGeocodingService, GeocodingService>(client =>
{
    client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Node-astrologie-datingapp/1.0 (examenproject EhB)");
});

builder.Services.AddControllersWithViews();

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

// HTTP-pipeline config
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
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

await app.RunAsync();
