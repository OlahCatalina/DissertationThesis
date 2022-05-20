using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dissertation_Thesis_SitesTextCrawler.Models.DatabaseModels
{
    [Table("Font")]
    public class DbFont
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the font name.
        /// </summary>
        [StringLength(200)]
        public string FontName { get; set; }

    }
}