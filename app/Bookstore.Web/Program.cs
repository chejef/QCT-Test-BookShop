using System.Security.Claims;
using BobsBookstoreClassic.Data;
using Bookstore.Common;
using Bookstore.Domain.Customers;
using Bookstore.Web;
using Bookstore.Web.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.AWS.Logger;
using NLog.Config;
using NLog.Targets;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

// --- Phase 3: Configuration migration ---
// Initialize BookstoreConfiguration from appsettings.json (replaces ConfigurationManager)
ConfigurationSetup.ConfigureConfigurationAsync(builder.Configuration).GetAwaiter().GetResult();

// --- Phase 7: Logging migration ---
ConfigureNLog();
builder.Logging.ClearProviders();
builder.Host.UseNLog();

// --- Phase 4: DI migration ---
builder.Services.AddControllersWithViews();
builder.Services.AddBookstoreServices(builder.Configuration);

// --- Phase 6: Filters migration ---
// Global AuthorizeAttribute → FallbackPolicy RequireAuthenticatedUser
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// --- Phase 5: Authentication migration ---
var authMode = BookstoreConfiguration.GetSetting("Services/Authentication");

if (authMode == "aws")
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddOpenIdConnect(options =>
    {
        options.ClientId = BookstoreConfiguration.GetSetting("Authentication/Cognito/LocalClientId");
        options.MetadataAddress = BookstoreConfiguration.GetSetting("Authentication/Cognito/MetadataAddress");
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.SaveTokens = true;
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.UseTokenLifetime = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "cognito:username",
            RoleClaimType = "cognito:groups"
        };
        options.Events = new OpenIdConnectEvents
        {
            OnRedirectToIdentityProvider = context =>
            {
                var returnUrl = context.Request.GetReturnUrl();
                context.ProtocolMessage.RedirectUri = returnUrl;
                return Task.CompletedTask;
            },
            OnAuthorizationCodeReceived = context =>
            {
                var returnUrl = context.Request.GetReturnUrl();
                context.TokenEndpointRequest!.RedirectUri = returnUrl;
                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var customerService = context.HttpContext.RequestServices.GetRequiredService<ICustomerService>();

                var principal = new ClaimsPrincipal(context.Principal!.Identity!);
                var identity = (ClaimsIdentity)principal.Identity!;

                var dto = new CreateOrUpdateCustomerDto(
                    identity.FindFirst(c => c.Type.Contains("nameidentifier"))?.Value
                        ?? identity.FindFirst("sub")?.Value ?? string.Empty,
                    identity.Name ?? string.Empty,
                    identity.FindFirst(y => y.Type.Contains("givenname"))?.Value ?? string.Empty,
                    identity.FindFirst(y => y.Type.Contains("surname"))?.Value ?? string.Empty);

                await customerService.CreateOrUpdateCustomerAsync(dto);
            }
        };
    });
}
else
{
    // Local authentication: no scheme needed — middleware creates ClaimsPrincipal directly
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie();
}

var app = builder.Build();

// --- Middleware pipeline ---
if (!app.Environment.IsDevelopment())
{
    // Global HandleErrorAttribute → UseExceptionHandler
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Local authentication middleware (must be before UseAuthentication so the principal is set)
if (authMode != "aws")
{
    app.UseMiddleware<LocalAuthenticationMiddleware>();
}

app.UseAuthentication();
app.UseAuthorization();

// --- Phase 6: Routing migration ---
app.MapControllerRoute(
    name: "admin",
    pattern: "Admin/{controller=Dashboard}/{action=Index}/{id?}",
    defaults: new { area = "Admin" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// --- NLog configuration (migrated from LoggingSetup.cs) ---
static void ConfigureNLog()
{
    var config = new LoggingConfiguration();

    Target loggingTarget;

    if (BookstoreConfiguration.GetSetting("Services/LoggingService") == "aws")
    {
        loggingTarget = new AWSTarget { LogGroup = Constants.AppName };
    }
    else
    {
        loggingTarget = new DebuggerTarget();
    }

    config.AddTarget("aws", loggingTarget);
    config.LoggingRules.Add(new LoggingRule("*", NLog.LogLevel.Info, loggingTarget));

    LogManager.Configuration = config;
}
