using Amazon.Rekognition;
using Amazon.S3;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Bookstore.Common;
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
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using NLog;
using NLog.AWS.Logger;
using NLog.Config;
using NLog.Targets;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// 1. Configuration — initialize BookstoreConfiguration from appsettings.json
//    This replaces the legacy ConfigurationManager / BookstoreConfiguration
//    static singleton that was set up in Application_Start → ConfigurationSetup.
// ---------------------------------------------------------------------------
BookstoreConfiguration.Initialize(builder.Configuration);

// ConfigurationSetup.ConfigureConfiguration() — fetch AWS SSM parameters at
// startup when running against AWS services.
ConfigureAwsParameters(builder.Configuration);

// ---------------------------------------------------------------------------
// 2. Logging — LoggingSetup.ConfigureLogging()
//    NLog configuration (AWS CloudWatch or local debugger target).
// ---------------------------------------------------------------------------
ConfigureNLogLogging(builder.Configuration);

// ---------------------------------------------------------------------------
// 3. Services — DependencyInjectionSetup.ConfigureDependencyInjection()
//    Migrated from Autofac container to the built-in DI container.
// ---------------------------------------------------------------------------

// EF Core — ApplicationDbContext with SQL Server provider
var connectionString = builder.Configuration.GetConnectionString("BookstoreDatabaseConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Domain services
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IReferenceDataService, ReferenceDataService>();
builder.Services.AddScoped<IOfferService, OfferService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IAddressService, AddressService>();
builder.Services.AddScoped<IShoppingCartService, ShoppingCartService>();
builder.Services.AddScoped<IImageResizeService, ImageResizeService>();

// Repositories
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IAddressRepository, AddressRepository>();
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IOfferRepository, OfferRepository>();
builder.Services.AddScoped<IShoppingCartRepository, ShoppingCartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IReferenceDataRepository, ReferenceDataRepository>();

// Generic paginated list
builder.Services.AddScoped(typeof(IPaginatedList<>), typeof(PaginatedList<>));

// File service — local or S3 depending on configuration
if (builder.Configuration["Services/FileService"] == "aws")
{
    builder.Services.AddSingleton<IAmazonS3, AmazonS3Client>();
    builder.Services.AddScoped<IFileService, S3FileService>();
}
else
{
    var webRootPath = Path.Combine(builder.Environment.WebRootPath ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot"), "Content");
    builder.Services.AddSingleton<IFileService>(new LocalFileService(webRootPath));
}

// Image validation service — local or Rekognition depending on configuration
if (builder.Configuration["Services/ImageValidationService"] == "aws")
{
    builder.Services.AddSingleton<IAmazonRekognition, AmazonRekognitionClient>();
    builder.Services.AddScoped<IImageValidationService, RekognitionImageValidationService>();
}
else
{
    builder.Services.AddScoped<IImageValidationService, LocalImageValidationService>();
}

builder.Services.AddHttpContextAccessor();

// ---------------------------------------------------------------------------
// 4. Authentication — AuthenticationSetup.ConfigureAuthentication()
//    Local dev: custom LocalAuthenticationMiddleware sets a ClaimsPrincipal.
//    AWS: OpenID Connect (Cognito) + cookie authentication.
// ---------------------------------------------------------------------------
var authMode = builder.Configuration["Services/Authentication"];

if (authMode == "aws")
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddOpenIdConnect(options =>
    {
        options.MetadataAddress = BookstoreConfiguration.GetSetting("Authentication/Cognito/MetadataAddress");
        options.ClientId = BookstoreConfiguration.GetSetting("Authentication/Cognito/LocalClientId");
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.CallbackPath = "/signin-oidc";
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("email");
        options.Scope.Add("profile");
        options.Scope.Add("aws.cognito.signin.user.admin");
        options.SaveTokens = true;
    });
}
else
{
    // Local authentication does not use a real auth scheme; instead the
    // LocalAuthenticationMiddleware sets HttpContext.User directly.
    // Register a cookie scheme so [Authorize] and the auth pipeline have a
    // default scheme — the middleware runs before UseAuthentication.
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie();
}

builder.Services.AddAuthorization();

// ---------------------------------------------------------------------------
// 5. MVC — FilterConfig.RegisterGlobalFilters (HandleErrorAttribute → exception
//    handler middleware; AuthorizeAttribute → fallback authorization policy)
//    and route registration.
// ---------------------------------------------------------------------------
builder.Services.AddControllersWithViews(options =>
{
    // FilterConfig had: filters.Add(new AuthorizeAttribute()) — require
    // authenticated users globally. In ASP.NET Core MVC this is done via a
    // fallback policy so all endpoints require auth unless [AllowAnonymous].
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter());
});

// [PORT-NOTE] BundleConfig.RegisterBundles dropped — System.Web.Optimization
// bundling is not supported on ASP.NET Core. Static assets are served directly
// from wwwroot/ (CSS, JS referenced via <link>/<script> in _Layout.cshtml).

var app = builder.Build();

// ---------------------------------------------------------------------------
// 6. Middleware pipeline
// ---------------------------------------------------------------------------

// Application_Error → centralised exception handler
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errApp => errApp.Run(async ctx =>
    {
        var ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
        if (ex is not null)
        {
            var logger = NLog.LogManager.GetCurrentClassLogger();
            logger.Error(ex, "Unhandled exception");
        }
        ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
        ctx.Response.ContentType = "text/html";
        await ctx.Response.WriteAsync("<h1>An error occurred.</h1><p>Please try again later.</p>");
    }));
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Local authentication middleware — runs BEFORE UseAuthentication so it can
// set HttpContext.User for the cookie-less local dev flow.
if (authMode != "aws")
{
    app.UseMiddleware<LocalAuthenticationMiddleware>();
}

app.UseAuthentication();
app.UseAuthorization();

// ---------------------------------------------------------------------------
// 7. Endpoint routing — RouteConfig.RegisterRoutes + AdminAreaRegistration
// ---------------------------------------------------------------------------

// Admin area route (from AdminAreaRegistration.cs)
app.MapControllerRoute(
    name: "Admin_default",
    pattern: "Admin/{controller}/{action=Index}/{id?}",
    defaults: new { area = "Admin" });

// Default route (from RouteConfig.cs)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// ===========================================================================
// Helper methods — ported from the legacy App_Start classes
// ===========================================================================

/// <summary>
/// Ported from ConfigurationSetup.ConfigureConfiguration().
/// Loads AWS SSM parameters into BookstoreConfiguration when running against
/// AWS services (Database, Authentication, FileService).
/// </summary>
static void ConfigureAwsParameters(IConfiguration configuration)
{
    var rootPath = "/" + Constants.AppName;

    const string databasePath = "/Database";
    const string authenticationPath = "/Authentication";
    const string fileServicePath = "/Files";

    if (configuration["Services/Database"] == "aws")
    {
        using var client = new AmazonSimpleSystemsManagementClient();
        var request = new GetParameterRequest
        {
            Name = $"{rootPath}{databasePath}/ConnectionStrings/BookstoreDatabaseConnection"
        };
        var response = client.GetParameterAsync(request).GetAwaiter().GetResult();
        BookstoreConfiguration.AddSetting(
            response.Parameter.Name.Replace($"{rootPath}{databasePath}/", string.Empty),
            response.Parameter.Value);
    }

    if (configuration["Services/Authentication"] == "aws")
    {
        using var client = new AmazonSimpleSystemsManagementClient();
        var request = new GetParametersByPathRequest
        {
            Path = $"{rootPath}{authenticationPath}/",
            Recursive = true
        };
        var response = client.GetParametersByPathAsync(request).GetAwaiter().GetResult();
        foreach (var parameter in response.Parameters)
        {
            BookstoreConfiguration.AddSetting(
                parameter.Name.Replace($"{rootPath}/", string.Empty),
                parameter.Value);
        }
    }

    if (configuration["Services/FileService"] == "aws")
    {
        using var client = new AmazonSimpleSystemsManagementClient();
        var request = new GetParametersByPathRequest
        {
            Path = $"{rootPath}{fileServicePath}/",
            Recursive = true
        };
        var response = client.GetParametersByPathAsync(request).GetAwaiter().GetResult();
        foreach (var parameter in response.Parameters)
        {
            BookstoreConfiguration.AddSetting(
                parameter.Name.Replace($"{rootPath}/", string.Empty),
                parameter.Value);
        }
    }
}

/// <summary>
/// Ported from LoggingSetup.ConfigureLogging().
/// Configures NLog with either an AWS CloudWatch target or a local debugger target.
/// </summary>
static void ConfigureNLogLogging(IConfiguration configuration)
{
    var config = new LoggingConfiguration();

    NLog.Targets.Target loggingTarget;

    if (configuration["Services/LoggingService"] == "aws")
    {
        loggingTarget = new AWSTarget { LogGroup = Constants.AppName };
    }
    else
    {
        loggingTarget = new DebuggerTarget();
    }

    config.AddTarget("aws", loggingTarget);
    config.LoggingRules.Add(new LoggingRule("*", NLog.LogLevel.Info, loggingTarget));

    NLog.LogManager.Configuration = config;
}
