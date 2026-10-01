using Bookstore.Data;
using Microsoft.AspNetCore.Mvc;


namespace Bookstore.Web.Controllers
{
    public class AuthenticationController : Controller
    {
        private readonly BookstoreConfiguration _config;

        public AuthenticationController(BookstoreConfiguration config)
        {
            _config = config;
        }

        public ActionResult Login(string? redirectUri = null)
        {
            if(string.IsNullOrWhiteSpace(redirectUri)) return RedirectToAction("Index", "Home");

            return Redirect(redirectUri);
        }

        public ActionResult LogOut()
        {
            return _config.GetSetting("Services:Authentication") == "aws" ? CognitoSignOut() : LocalSignOut();
        }

        private ActionResult LocalSignOut()
        {
            if (HttpContext.Request.Cookies.ContainsKey("LocalAuthentication"))
            {
                HttpContext.Response.Cookies.Delete("LocalAuthentication");
            }

            return RedirectToAction("Index", "Home");
        }

        private ActionResult CognitoSignOut()
        {
            if (Request.Cookies.ContainsKey(".AspNet.Cookies"))
            {
                Response.Cookies.Delete(".AspNet.Cookies");
            }

            var domain = _config.GetSetting("Authentication:Cognito:CognitoDomain");
            var clientId = _config.GetSetting("Authentication:Cognito:LocalClientId");
            var logoutUri = $"{Request.Scheme}://{Request.Host}/";

            return Redirect($"{domain}/logout?client_id={clientId}&logout_uri={logoutUri}");
        }
    }
}
