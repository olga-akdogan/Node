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
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));


builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Logging in is only possible after the email address is confirmed.
        options.SignIn.RequireConfirmedEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders() // Needed for email confirmation tokens.
    .AddErrorDescriber<LocalizedIdentityErrorDescriber>(); // Multilingual Identity error messages.


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
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Configuration 'Jwt:Key' is missing.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Configuration 'Jwt:Issuer' is missing.");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Configuration 'Jwt:Audience' is missing.");

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        };
    });

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// GetStream Chat
var streamApiKey = builder.Configuration["Stream:ApiKey"]
    ?? throw new InvalidOperationException("Configuration 'Stream:ApiKey' is missing.");
var streamApiSecret = builder.Configuration["Stream:ApiSecret"]
    ?? throw new InvalidOperationException("Configuration 'Stream:ApiSecret' is missing.");

builder.Services.AddSingleton(new StreamClient(streamApiKey, streamApiSecret));
// ChatClient expects generic GetStream.IClient-interface
builder.Services.AddSingleton<IClient>(sp => sp.GetRequiredService<StreamClient>());
builder.Services.AddSingleton<ChatClient>();

// Claude API (Anthropic): match interpretations based on both natal charts.
var anthropicApiKey = builder.Configuration["Anthropic:ApiKey"]
    ?? throw new InvalidOperationException("Configuration 'Anthropic:ApiKey' is missing.");
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

// Nominatim (OpenStreetMap) requires an identifying User-Agent per its terms of use.
builder.Services.AddHttpClient<IGeocodingService, GeocodingService>(client =>
{
    client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Node-astrologie-datingapp/1.0 (examenproject EhB)");
});

// Multilingualism: nl, en and fr are all three fully translated; nl is the default language.
// No ResourcesPath set: SharedResource.cs itself already lives in the
// Resources/ folder, and the .resx files sit right next to it in that same
// folder — that's exactly the default convention (resx path follows the
// class's namespace). ResourcesPath = "Resources" would expect a nested
// Resources/Resources/ folder here and would no longer find the translations.
builder.Services.AddLocalization();

builder.Services
    .AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        // All validation attributes ([Required], [Display], ...) get their
        // translated text from the same shared resource files, instead of
        // requiring a separate resx file per view model.
        options.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(SharedResource));
    });

var supportedLanguages = new[] { "nl", "en", "fr" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("nl")
    .AddSupportedCultures(supportedLanguages)
    .AddSupportedUICultures(supportedLanguages);

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

// HTTP pipeline config
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Culture of the request: first the cookie the language picker sets,
// otherwise (on a first visit) the browser's language preference, falling back to nl.
app.UseRequestLocalization(localizationOptions);

app.UseRouting();


app.UseAuthentication();
app.UseAuthorization();

// Custom middleware: tracks cookie consent (see CookieConsentLog) and
// processes the banner choice directly, without a controller. Placed after
// UseAuthentication so context.User is already populated for the log.
app.UseCookieConsent();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

await app.RunAsync();
