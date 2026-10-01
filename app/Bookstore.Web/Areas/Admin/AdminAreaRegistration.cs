
namespace Bookstore.Web.Areas
{

    /// <summary>
    /// Replaces the MVC 5 AreaRegistration for the Admin area.
    /// Call <see cref="RegisterArea"/> from Program.cs endpoint configuration.
    /// </summary>
    public static class AdminAreaRegistration
    {
        public static string AreaName => "Admin";

        /// <summary>
        /// Registers the Admin area route using ASP.NET Core endpoint routing.
        /// </summary>
        public static void RegisterArea(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapAreaControllerRoute(
                name: "Admin_default",
                areaName: "Admin",
                pattern: "Admin/{controller}/{action=Index}/{id?}");
        }
    }
}