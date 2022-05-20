using System.ComponentModel.DataAnnotations.Schema;

namespace Dissertation_Thesis_SitesTextCrawler.Models.DatabaseModels
{
    [Table("SiteFont")]

    public class DbSiteFont
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the site identifier.
        /// </summary>
        public int SiteId { get; set; }

        /// <summary>
        /// Gets or sets the font identifier.
        /// </summary>
        public int FontId { get; set; }
    }
}