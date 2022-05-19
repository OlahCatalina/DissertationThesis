using System.Web.Optimization;

namespace Dissertation_Thesis_SitesTextCrawler
{
    public class BundleConfig
    {
        // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/allScripts").Include(
                        "~/Scripts/jquery/jquery.min.js",
                        "~/Scripts/bootstrap/bootstrap.min.js",
                        "~/Scripts/dataTables/dataTables.min.js",
                        "~/Scripts/jqueryConfirm/jquery-confirm.min.js",
                        "~/Scripts/select2/select2.min.js"));
            
            bundles.Add(new StyleBundle("~/Content/allStyles").Include(
                      "~/Content/bootstrap/bootstrap.min.css",
                      "~/Content/dataTables/dataTables.min.css",
                      "~/Content/jqueryConfirm/jquery-confirm.min.css",
                      "~/Content/select2/select2.min.css",
                      "~/Content/Site.css"));
        }
    }
}
