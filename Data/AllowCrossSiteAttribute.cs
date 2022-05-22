namespace Dissertation_Thesis_SitesTextCrawler.Data
{
    using System.Web.Mvc;

    public class AllowCrossSiteAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            filterContext.RequestContext.HttpContext.Response.AddHeader("Access-Control-Allow-Origin", "*");
            filterContext.RequestContext.HttpContext.Response.AddHeader("Access-Control-Allow-Origin", "null");
            filterContext.RequestContext.HttpContext.Response.AddHeader("Access-Control-Allow-Origin", "chrome-extension://aojklehgimofefpdekhhbagflipncinm");
            filterContext.RequestContext.HttpContext.Response.AddHeader("Access-Control-Allow-Headers", "*");
            filterContext.RequestContext.HttpContext.Response.AddHeader("Access-Control-Allow-Credentials", "true");
            filterContext.RequestContext.HttpContext.Response.AddHeader("Vary", "Origin");
            base.OnActionExecuting(filterContext);
        }
    }
}