using System.Data.Entity;
using Dissertation_Thesis_WebsiteScraper.Models.DatabaseModels;

namespace Dissertation_Thesis_WebsiteScraper.Data
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