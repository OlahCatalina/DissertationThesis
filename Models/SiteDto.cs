using System.Collections.Generic;

namespace Dissertation_Thesis_WebsiteScraper.Models
{
    public class SiteDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Url { get; set; }

        public List<string> Categories { get; set; }

    }
    
}