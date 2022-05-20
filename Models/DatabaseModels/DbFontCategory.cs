using System.ComponentModel.DataAnnotations.Schema;

namespace Dissertation_Thesis_SitesTextCrawler.Models.DatabaseModels
{
    [Table("FontCategory")]
    public class DbFontCategory
    {  
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the font identifier.
        /// </summary>
        public int FontId { get; set; }

        /// <summary>
        /// Gets or sets the category identifier.
        /// </summary>
        public int CategoryId { get; set; }

    }
}