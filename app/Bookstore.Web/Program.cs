using Amazon.Rekognition;
using Amazon.S3;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Autofac;
using Autofac.Extensions.DependencyInjection;
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
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// 1. NLog integration (TR-40) — replaces LoggingSetup.ConfigureLogging()
// ---------------------------------------------------------------------------
builder.Logging.ClearProviders();
builder.Host.UseNLog();

// ---------------------------------------------------------------------------
// 2. Configuration (TR-41) — appsettings.json is loaded by default via
//    WebApplicationBuilder.  AWS SSM parameters are loaded at startup when
//    the "Services/*" keys indicate "aws".
//    Replaces ConfigurationSetup.ConfigureConfiguration().
// ---------------------------------------------------------------------------
var bookstoreConfig = new BookstoreConfiguration(builder.Configuration);
LoadAwsSsmParameters(bookstoreConfig, builder.Configuration);

// ---------------------------------------------------------------------------
// 3. Dependency Injection (TR-40) — Autofac.Extensions.DependencyInjection
//    Replaces DependencyInjectionSetup.ConfigureDependencyInjection().
// ---------------------------------------------------------------------------
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    // Domain services
    containerBuilder.RegisterType<BookService>().As<IBookService>();
    containerBuilder.RegisterType<OrderService>().As<IOrderService>();
    containerBuilder.RegisterType<ReferenceDataService>().As<IReferenceDataService>();
    containerBuilder.RegisterType<OfferService>().As<IOfferService>();
    containerBuilder.RegisterType<CustomerService>().As<ICustomerService>();
    containerBuilder.RegisterType<AddressService>().As<IAddressService>();
    containerBuilder.RegisterType<ShoppingCartService>().As<IShoppingCartService>();
    containerBuilder.RegisterType<ImageResizeService>().As<IImageResizeService>();

    // Repositories
    containerBuilder.RegisterType<CustomerRepository>().As<ICustomerRepository>();
    containerBuilder.RegisterType<AddressRepository>().As<IAddressRepository>();
    containerBuilder.RegisterType<BookRepository>().As<IBookRepository>();
    containerBuilder.RegisterType<OfferRepository>().As<IOfferRepository>();
    containerBuilder.RegisterType<ShoppingCartRepository>().As<IShoppingCartRepository>();
    containerBuilder.RegisterType<OrderRepository>().As<IOrderRepository>();
    containerBuilder.RegisterType<ReferenceDataRepository>().As<IReferenceDataRepository>();

    // PagedResult<> (TR-19) — PaginatedList → PagedResult / IPaginatedList → IPagedResult
    containerBuilder.RegisterGeneric(typeof(PagedResult<>)).As(typeof(IPagedResult<>)).InstancePerLifetimeScope();

    // File service — conditional on configuration
    if (bookstoreConfig.GetSetting("Services:FileService") == "aws")
    {
        containerBuilder.RegisterType<AmazonS3Client>().As<IAmazonS3>();
        containerBuilder.RegisterType<S3FileService>().As<IFileService>();
    }
    else
    {
        // In ASP.NET Core, static files are served from wwwroot.
        // LocalFileService stores cover images under the web root.
        containerBuilder.Register(ctx =>
        {
            var env = ctx.Resolve<IWebHostEnvironment>();
            return new LocalFileService(env.WebRootPath);
        }).As<IFileService>();
    }

    // Image validation service — conditional on configuration
    if (bookstoreConfig.GetSetting("Services:ImageValidationService") == "aws")
    {
        containerBuilder.RegisterType<AmazonRekognitionClient>().As<IAmazonRekognition>();
        containerBuilder.RegisterType<RekognitionImageValidationService>().As<IImageValidationService>();
    }
    else
    {
        containerBuilder.RegisterType<LocalImageValidationService>().As<IImageValidationService>();
    }
});

// Register BookstoreConfiguration as a singleton so controllers/services can inject it.
builder.Services.AddSingleton(bookstoreConfig);

// ApplicationDbContext with DbContextOptions<ApplicationDbContext> (TR-40)
var connectionString = bookstoreConfig.GetConnectionString("BookstoreDatabaseConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// HttpContextAccessor — replaces HttpContext.Current usages
builder.Services.AddHttpContextAccessor();

// ---------------------------------------------------------------------------
// 4. Authentication (TR-40) — replaces AuthenticationConfig.ConfigureAuthentication()
//    OWIN OpenID Connect + Cookies pipeline → ASP.NET Core middleware.
// ---------------------------------------------------------------------------
if (bookstoreConfig.GetSetting("Services:Authentication") == "aws")
{
    ConfigureCognitoAuthentication(builder, bookstoreConfig);
}
else
{
    // Local authentication uses cookie scheme as default, with the
    // LocalAuthenticationMiddleware handling sign-in/sign-out.
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/Authentication/Login";
        });
}

builder.Services.AddAuthorization();

// ---------------------------------------------------------------------------
// 5. MVC with areas (TR-40) — replaces FilterConfig + RouteConfig
// ---------------------------------------------------------------------------
builder.Services.AddControllersWithViews(options =>
{
    // Global [Authorize] filter — replaces FilterConfig: filters.Add(new AuthorizeAttribute())
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter());
});

// ---------------------------------------------------------------------------
// Build the app
// ---------------------------------------------------------------------------
var app = builder.Build();

// ---------------------------------------------------------------------------
// 6. Exception handling — replaces Application_Error in Global.asax.cs
// ---------------------------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errApp => errApp.Run(async ctx =>
    {
        var ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
        if (ex is not null)
        {
            var logger = ctx.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Unhandled exception");
        }
        ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await ctx.Response.WriteAsync("An unexpected error occurred.");
    }));
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// ---------------------------------------------------------------------------
// 7. Middleware pipeline — order is load-bearing (TR-40)
// ---------------------------------------------------------------------------
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Local authentication middleware — replaces OWIN UseMiddlewareFromContainer.
// Must run after UseRouting and before UseAuthentication so the claims
// principal is established before the auth middleware evaluates it.
if (bookstoreConfig.GetSetting("Services:Authentication") != "aws")
{
    app.UseMiddleware<LocalAuthenticationMiddleware>();
}

app.UseAuthentication();
app.UseAuthorization();

// ---------------------------------------------------------------------------
// 8. Routing — replaces RouteConfig + AreaRegistration (TR-40)
// ---------------------------------------------------------------------------

// Admin area route — replaces AdminAreaRegistration
app.MapAreaControllerRoute(
    name: "Admin_default",
    areaName: "Admin",
    pattern: "Admin/{controller=Dashboard}/{action=Index}/{id?}");

// Default route — replaces RouteConfig.RegisterRoutes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// [PORT-NOTE] Dropped BundleConfig.RegisterBundles — Web Optimization is unsupported
// on ASP.NET Core. Static assets are served directly from wwwroot/. Views must use
// direct <script>/<link> tags instead of @Scripts.Render/@Styles.Render.

app.Run();

// ==========================================================================
// Helper methods
// ==========================================================================

/// <summary>
/// Load AWS SSM parameters into BookstoreConfiguration when the corresponding
/// service is configured as "aws". Replaces ConfigurationSetup.ConfigureConfiguration().
/// </summary>
static void LoadAwsSsmParameters(BookstoreConfiguration config, IConfiguration appConfig)
{
    var rootPath = "/" + Constants.AppName;

    const string databasePath = "/Database";
    const string authenticationPath = "/Authentication";
    const string fileServicePath = "/Files";

    if (appConfig["Services:Database"] == "aws")
    {
        using var client = new AmazonSimpleSystemsManagementClient();
        var request = new GetParameterRequest
        {
            Name = $"{rootPath}{databasePath}/ConnectionStrings/BookstoreDatabaseConnection"
        };
        var response = client.GetParameterAsync(request).GetAwaiter().GetResult();
        config.AddSetting(
            response.Parameter.Name.Replace($"{rootPath}{databasePath}/", string.Empty),
            response.Parameter.Value);
    }

    if (appConfig["Services:Authentication"] == "aws")
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
            config.AddSetting(
                parameter.Name.Replace($"{rootPath}/", string.Empty),
                parameter.Value);
        }
    }

    if (appConfig["Services:FileService"] == "aws")
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
            config.AddSetting(
                parameter.Name.Replace($"{rootPath}/", string.Empty),
                parameter.Value);
        }
    }
}

/// <summary>
/// Configure AWS Cognito OpenID Connect authentication.
/// Replaces AuthenticationConfig.ConfigureCognitoAuthentication().
/// </summary>
static void ConfigureCognitoAuthentication(WebApplicationBuilder builder, BookstoreConfiguration config)
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddOpenIdConnect(options =>
    {
        options.ClientId = config.GetSetting("Authentication:Cognito:LocalClientId");
        options.MetadataAddress = config.GetSetting("Authentication:Cognito:MetadataAddress");
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.SaveTokens = true;
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
                var returnUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}";
                context.ProtocolMessage.RedirectUri = returnUrl;
                return Task.CompletedTask;
            },
            OnAuthorizationCodeReceived = context =>
            {
                var returnUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}";
                context.TokenEndpointRequest!.RedirectUri = returnUrl;
                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                // Resolve ICustomerService via HttpContext.RequestServices (ASP.NET Core DI)
                var customerService = context.HttpContext.RequestServices.GetRequiredService<ICustomerService>();

                var identity = (System.Security.Claims.ClaimsIdentity)context.Principal!.Identity!;

                var dto = new CreateOrUpdateCustomerDto(
                    identity.GetSub(),
                    identity.Name!,
                    identity.FindFirst(y => y.Type.Contains("givenname"))!.Value,
                    identity.FindFirst(y => y.Type.Contains("surname"))!.Value);

                await customerService.CreateOrUpdateCustomerAsync(dto);
            }
        };
    });
}
