using Amazon.Rekognition;
using Amazon.S3;
using Bookstore.Data;
using Bookstore.Data.FileServices;
using Bookstore.Data.ImageResizeService;
using Bookstore.Data.ImageValidationServices;
using Bookstore.Data.Repositories;
using Bookstore.Domain;
using Bookstore.Domain.Addresses;
using Bookstore.Domain.Books;
using Bookstore.Domain.Carts;
using Bookstore.Domain.Customers;
using Bookstore.Domain.Offers;
using Bookstore.Domain.Orders;
using Bookstore.Domain.ReferenceData;
using Bookstore.Web.Helpers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Logging — NLog integration (replaces LoggingSetup.ConfigureLogging)
// ---------------------------------------------------------------------------
builder.Logging.ClearProviders();
builder.Host.UseNLog();

ConfigureNLog(builder.Configuration);

// ---------------------------------------------------------------------------
// Configuration — AWS SSM provider (replaces ConfigurationSetup.ConfigureConfiguration)
// The legacy app loaded SSM parameters manually via AmazonSimpleSystemsManagementClient;
// Amazon.Extensions.Configuration.SystemsManager integrates natively with IConfiguration.
// ---------------------------------------------------------------------------
var appName = builder.Configuration["AppName"] ?? "BobsUsedBooksClassic";

if (string.Equals(builder.Configuration["Services:Database"], "aws", StringComparison.OrdinalIgnoreCase)
    || string.Equals(builder.Configuration["Services:Authentication"], "aws", StringComparison.OrdinalIgnoreCase)
    || string.Equals(builder.Configuration["Services:FileService"], "aws", StringComparison.OrdinalIgnoreCase))
{
    builder.Configuration.AddSystemsManager($"/{appName}", optional: true, reloadAfter: TimeSpan.FromMinutes(5));
}

// ---------------------------------------------------------------------------
// Entity Framework Core — ApplicationDbContext (replaces Autofac InstancePerRequest)
// ---------------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("BookstoreDatabaseConnection");
builder.Services.AddDbContext<ApplicationDbContext>(opts => opts.UseSqlServer(connectionString));

// ---------------------------------------------------------------------------
// Domain services (replaces DependencyInjectionSetup.ConfigureDependencyInjection)
// ---------------------------------------------------------------------------
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IReferenceDataService, ReferenceDataService>();
builder.Services.AddScoped<IOfferService, OfferService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IAddressService, AddressService>();
builder.Services.AddScoped<IShoppingCartService, ShoppingCartService>();
builder.Services.AddScoped<IImageResizeService, Bookstore.Data.ImageResizeService.ImageResizeService>();

// Repositories
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IAddressRepository, AddressRepository>();
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IOfferRepository, OfferRepository>();
builder.Services.AddScoped<IShoppingCartRepository, ShoppingCartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IReferenceDataRepository, ReferenceDataRepository>();

// PaginatedList — open generic
builder.Services.AddScoped(typeof(IPaginatedList<>), typeof(PaginatedList<>));

// File service — conditional on configuration (local vs S3)
if (string.Equals(builder.Configuration["Services:FileService"], "aws", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<IAmazonS3>(new AmazonS3Client());
    builder.Services.AddScoped<IFileService, S3FileService>();
}
else
{
    builder.Services.AddSingleton<IFileService>(sp =>
    {
        var env = sp.GetRequiredService<IWebHostEnvironment>();
        return new LocalFileService(Path.Combine(env.WebRootPath, "Content"));
    });
}

// Image validation service — conditional on configuration (local vs Rekognition)
if (string.Equals(builder.Configuration["Services:ImageValidationService"], "aws", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<IAmazonRekognition>(new AmazonRekognitionClient());
    builder.Services.AddScoped<IImageValidationService, RekognitionImageValidationService>();
}
else
{
    builder.Services.AddScoped<IImageValidationService, LocalImageValidationService>();
}

builder.Services.AddHttpContextAccessor();

// ---------------------------------------------------------------------------
// Authentication (replaces AuthenticationSetup.ConfigureAuthentication + OWIN Startup)
//   Dual-path: Cognito OpenID Connect (AWS) or local dev middleware
// ---------------------------------------------------------------------------
if (string.Equals(builder.Configuration["Services:Authentication"], "aws", StringComparison.OrdinalIgnoreCase))
{
    builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie()
        .AddOpenIdConnect(options =>
        {
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.MetadataAddress = builder.Configuration["Authentication:Cognito:MetadataAddress"] ?? string.Empty;
            options.ClientId = builder.Configuration["Authentication:Cognito:LocalClientId"] ?? string.Empty;
            options.SaveTokens = true;
            options.Events = new OpenIdConnectEvents
            {
                OnTokenValidated = context =>
                {
                    return Task.CompletedTask;
                }
            };
        });
}
else
{
    builder.Services
        .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie();
}

// ---------------------------------------------------------------------------
// Authorization (replaces FilterConfig global [Authorize])
// The legacy FilterConfig added a global AuthorizeAttribute. ASP.NET Core
// equivalent is a FallbackPolicy that requires authenticated users.
// ---------------------------------------------------------------------------
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// ---------------------------------------------------------------------------
// MVC
// ---------------------------------------------------------------------------
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ---------------------------------------------------------------------------
// Middleware pipeline — order is load-bearing
// ---------------------------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

// Local authentication middleware (dev-only, replaces OWIN LocalAuthenticationMiddleware)
if (!string.Equals(app.Configuration["Services:Authentication"], "aws", StringComparison.OrdinalIgnoreCase))
{
    app.UseMiddleware<LocalAuthenticationMiddleware>();
}

app.UseAuthorization();

// ---------------------------------------------------------------------------
// Exception logging (replaces Application_Error in Global.asax.cs)
// Note: UseExceptionHandler above handles the user-facing error page;
// this inline middleware ensures the exception is logged via NLog.
// ---------------------------------------------------------------------------
// [PORT-NOTE] Application_Error logged and cleared errors. ASP.NET Core's
// UseExceptionHandler + NLog integration covers this. The inline middleware
// below is an additional safety net for any unhandled exceptions.

// ---------------------------------------------------------------------------
// Routing (replaces RouteConfig.RegisterRoutes + AdminAreaRegistration.RegisterArea)
// ---------------------------------------------------------------------------
app.MapAreaControllerRoute(
    name: "Admin_default",
    areaName: "Admin",
    pattern: "Admin/{controller}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// ---------------------------------------------------------------------------
// NLog configuration (replaces LoggingSetup.ConfigureLogging)
// ---------------------------------------------------------------------------
static void ConfigureNLog(IConfiguration configuration)
{
    var config = new LoggingConfiguration();

    Target loggingTarget;

    if (string.Equals(configuration["Services:LoggingService"], "aws", StringComparison.OrdinalIgnoreCase))
    {
        var logGroupName = configuration["AppName"] ?? "BobsUsedBooksClassic";
        loggingTarget = new NLog.AWS.Logger.AWSTarget { LogGroup = logGroupName };
    }
    else
    {
        loggingTarget = new DebuggerTarget();
    }

    config.AddTarget("aws", loggingTarget);
    config.LoggingRules.Add(new LoggingRule("*", NLog.LogLevel.Info, loggingTarget));

    LogManager.Configuration = config;
}
