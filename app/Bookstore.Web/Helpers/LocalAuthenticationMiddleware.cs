using System.Security.Claims;
using Bookstore.Domain.Customers;

namespace Bookstore.Web.Helpers
{
    public class LocalAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private const string UserId = "FB6135C7-1464-4A72-B74E-4B63D343DD09";

        public LocalAuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var customerService = context.RequestServices.GetRequiredService<ICustomerService>();

            if (context.Request.Path.Value?.StartsWith("/Authentication/Login") == true)
            {
                CreateClaimsPrincipal(context);
                await SaveCustomerDetailsAsync(context, customerService);
                context.Response.Cookies.Append("LocalAuthentication", "true", new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(1)
                });
                context.Response.Redirect("/");
            }
            else if (context.Request.Cookies["LocalAuthentication"] != null)
            {
                CreateClaimsPrincipal(context);
                await SaveCustomerDetailsAsync(context, customerService);
                await _next.Invoke(context);
            }
            else
            {
                await _next.Invoke(context);
            }
        }

        private void CreateClaimsPrincipal(HttpContext context)
        {
            var identity = new ClaimsIdentity("Application");
            identity.AddClaim(new Claim(ClaimTypes.Name, "bookstoreuser"));
            identity.AddClaim(new Claim("nameidentifier", UserId));
            identity.AddClaim(new Claim("given_name", "Bookstore"));
            identity.AddClaim(new Claim("family_name", "User"));
            identity.AddClaim(new Claim(ClaimTypes.Role, "Administrators"));
            context.User = new ClaimsPrincipal(identity);
        }

        private async Task SaveCustomerDetailsAsync(HttpContext context, ICustomerService customerService)
        {
            var identity = (ClaimsIdentity)context.User.Identity!;
            var dto = new CreateOrUpdateCustomerDto(identity.FindFirst("nameidentifier")!.Value, identity.Name!, identity.FindFirst("given_name")!.Value, identity.FindFirst("family_name")!.Value);
            await customerService.CreateOrUpdateCustomerAsync(dto);
        }
    }
}