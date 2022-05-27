using System.ComponentModel.DataAnnotations.Schema;

namespace Dissertation_Thesis_WebsiteScraper.Models.DatabaseModels
{
    [Table("SiteCategory")]

    public class DbSiteCategory
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
        /// Gets or sets the category identifier.
        /// </summary>
        public int CategoryId { get; set; }

    }
}