using System.ComponentModel.DataAnnotations.Schema;

namespace Dissertation_Thesis_WebsiteScraper.Models.DatabaseModels
{
    [Table("TrainedData")]

    public class DbTrainedData
    {
        public string CategoryName { get; set; }

        public string Site { get; set; }

    }
}