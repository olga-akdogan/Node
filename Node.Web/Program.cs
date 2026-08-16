using System.Text;
using Anthropic;
using GetStream;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Node.Data.Data;
using Node.Data.Models;
using Node.Data.Services;
using Node.Web.Middleware;
using Node.Web.Resources;
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
    .AddDefaultTokenProviders() // Nodig voor e-mailbevestigingstokens.
    .AddErrorDescriber<LokaleIdentityErrorDescriber>(); // Meertalige Identity-foutmeldingen.


builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});


builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.FromMinutes(5));

// JWT bearer scheme for the REST API (used by the MAUI companion app), next
// to the cookie scheme AddIdentity() above already set up for the MVC site.
// AddJwtBearer only registers a second handler here; it does not change the
// default scheme, so web pages keep using the cookie unless a controller
// explicitly opts into "Bearer" via [Authorize(AuthenticationSchemes = ...)].
var jwtSleutel = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Configuratie 'Jwt:Key' ontbreekt.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Configuratie 'Jwt:Issuer' ontbreekt.");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Configuratie 'Jwt:Audience' ontbreekt.");

builder.Services
    .AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSleutel)),
        };
    });

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// GetStream Chat
var streamApiKey = builder.Configuration["Stream:ApiKey"]
    ?? throw new InvalidOperationException("Configuratie 'Stream:ApiKey' ontbreekt.");
var streamApiSecret = builder.Configuration["Stream:ApiSecret"]
    ?? throw new InvalidOperationException("Configuratie 'Stream:ApiSecret' ontbreekt.");

builder.Services.AddSingleton(new StreamClient(streamApiKey, streamApiSecret));
// ChatClient expects generic GetStream.IClient-interface
builder.Services.AddSingleton<IClient>(sp => sp.GetRequiredService<StreamClient>());
builder.Services.AddSingleton<ChatClient>();

// Claude API (Anthropic): matchinterpretaties op basis van beide horoscopen.
var anthropicApiKey = builder.Configuration["Anthropic:ApiKey"]
    ?? throw new InvalidOperationException("Configuratie 'Anthropic:ApiKey' ontbreekt.");
builder.Services.AddSingleton(new AnthropicClient { ApiKey = anthropicApiKey });


builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<ISwipeService, SwipeService>();
builder.Services.AddScoped<IMatchService, MatchService>();
builder.Services.AddScoped<IChartService, ChartService>();
builder.Services.AddScoped<IStreamChatService, StreamChatService>();
builder.Services.AddScoped<INatalChartCalculator, NatalChartCalculator>();
builder.Services.AddScoped<IMatchInterpretationService, MatchInterpretationService>();
builder.Services.AddScoped<ISwipeTeaserService, SwipeTeaserService>();
builder.Services.AddScoped<IChartInterpretationService, ChartInterpretationService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IProfilePictureService, ProfilePictureService>();

// Nominatim (OpenStreetMap) vereist een identificerende User-Agent per gebruiksvoorwaarden.
builder.Services.AddHttpClient<IGeocodingService, GeocodingService>(client =>
{
    client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Node-astrologie-datingapp/1.0 (examenproject EhB)");
});

// Meertaligheid: nl, en en fr zijn alle drie volledig vertaald; nl is de standaardtaal.
// Geen ResourcesPath instellen: SharedResource.cs staat zelf al in de map
// Resources/, en de .resx-bestanden staan ernaast in diezelfde map — dat is
// exact de standaardconventie (resx-pad volgt de naamruimte van de klasse).
// ResourcesPath = "Resources" zou hier een dubbele Resources/Resources/-map
// verwachten en de vertalingen dus niet meer vinden.
builder.Services.AddLocalization();

builder.Services
    .AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        // Alle validatieattributen ([Required], [Display], ...) halen hun
        // vertaalde tekst uit dezelfde gedeelde resource-bestanden, in plaats
        // van een apart resx-bestand per ViewModel te vereisen.
        options.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(SharedResource));
    });

var ondersteundeTalen = new[] { "nl", "en", "fr" };
var localisatieOpties = new RequestLocalizationOptions()
    .SetDefaultCulture("nl")
    .AddSupportedCultures(ondersteundeTalen)
    .AddSupportedUICultures(ondersteundeTalen);

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

// Cultuur van het verzoek: eerst het cookie dat de taalkiezer zet, anders
// (bij een eerste bezoek) de taalvoorkeur van de browser, met nl als terugval.
app.UseRequestLocalization(localisatieOpties);

app.UseRouting();


app.UseAuthentication();
app.UseAuthorization();

// Eigen middleware: houdt de cookietoestemming bij (zie CookieConsentLog) en
// verwerkt de banner-keuze rechtstreeks, zonder controller. Staat na
// UseAuthentication zodat context.User al ingevuld is voor de log.
app.UseCookieConsent();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

await app.RunAsync();
