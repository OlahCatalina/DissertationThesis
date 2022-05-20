using Dissertation_Thesis_SitesTextCrawler.Models.DatabaseModels;
using System.Data.Entity;

namespace Dissertation_Thesis_SitesTextCrawler.Data
{
    public class WebAppContext: DbContext
    {
        public WebAppContext() : base("DefaultConnection") {  }

        public virtual DbSet<DbSite> DbSites { get; set; }

        public virtual DbSet<DbFont> DbFonts { get; set; }

        public virtual DbSet<DbCategory> DbCategories { get; set; }

        public virtual DbSet<DbSiteCategory> DbSiteCategories { get; set; }

        public virtual DbSet<DbSiteFont> DbSiteFonts { get; set; }

        public virtual DbSet<DbFontCategory> DbFontCategories { get; set; }

    }
}