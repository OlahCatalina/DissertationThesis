using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dissertation_Thesis_WebsiteScraper.Models.DatabaseModels
{
    [Table("Site")]
    public class DbSite
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the site name.
        /// </summary>
        [StringLength(1000)]
        public string SiteName { get; set; }

        /// <summary>
        /// Gets or sets the site URL.
        /// </summary>
        [StringLength(1000)]
        public string SiteUrl{ get; set; }

        /// <summary>
        /// Gets or sets the site (inner) text.
        /// </summary>
        public string SiteText { get; set; }

        /// <summary>
        /// Gets or sets the site HTML.
        /// </summary>
        public string SiteHtml { get; set; }
    }
}