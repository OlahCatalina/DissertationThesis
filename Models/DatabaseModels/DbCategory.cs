using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dissertation_Thesis_SitesTextCrawler.Models.DatabaseModels
{
    [Table("Category")]

    public class DbCategory
    { 
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the font name.
        /// </summary>
        [StringLength(250)]
        public string CategoryName { get; set; }
    }
}