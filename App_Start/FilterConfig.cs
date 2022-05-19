using System.Web;
using System.Web.Mvc;

namespace Dissertation_Thesis_SitesTextCrawler
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
