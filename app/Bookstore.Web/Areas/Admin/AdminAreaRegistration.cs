
namespace Bookstore.Web.Areas
{
    /// <summary>
    /// Legacy MVC5 area registration — no longer used in ASP.NET Core.
    /// Area routing is configured in Program.cs via MapControllerRoute.
    /// Admin controllers should use the [Area("Admin")] attribute.
    /// </summary>
    public static class AdminAreaRegistration
    {
        public const string AreaName = "Admin";
    }
}