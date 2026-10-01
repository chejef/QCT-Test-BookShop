// PORT-TODO
// {"was": "System.Web.Mvc.AreaRegistration", "note": "AdminAreaRegistration is obsolete in ASP.NET Core. Add [Area(\"Admin\")] attribute to each controller in Areas/Admin/Controllers/, and register the area route in Program.cs: app.MapControllerRoute(name: \"Admin_default\", pattern: \"Admin/{controller}/{action}/{id?}\", defaults: new { action = \"Index\" }).RequireAuthorization();  Then delete this file.", "files": ["Program.cs"]}

namespace Bookstore.Web.Areas
{
    // This class is intentionally left empty. ASP.NET Core does not use AreaRegistration.
    // Area routing is configured in Program.cs via MapAreaControllerRoute, and each
    // admin controller uses the [Area("Admin")] attribute.
    //
    // Original MVC 5 route:
    //   name: "Admin_default"
    //   pattern: "Admin/{controller}/{action}/{id}"
    //   defaults: action = "Index", id = optional
    //   namespaces: Bookstore.Web.Areas.Admin.Controllers
    //
    // ASP.NET Core equivalent in Program.cs:
    //   app.MapControllerRoute(
    //       name: "Admin_default",
    //       pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
    //
    // Or more specifically:
    //   app.MapAreaControllerRoute(
    //       name: "Admin_default",
    //       areaName: "Admin",
    //       pattern: "Admin/{controller}/{action=Index}/{id?}");
}