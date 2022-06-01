using System.Web.Mvc;
using System.Web.Routing;

namespace Dissertation_Thesis_WebsiteScraper
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "Dissertation_Thesis_WebsiteScraper.Controllers" }
            );
        }
    }
}
