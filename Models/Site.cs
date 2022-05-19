using System.Collections.Generic;

namespace Dissertation_Thesis_SitesTextCrawler.Models
{
    public class Site
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public List<string> Categories{ get; set; }

        public override string ToString()
        {
            var str = Index + " ||| " + Name + " ||| " + Url + " ||| ";
            str += string.Join(" [x][x] ", Categories);
            return str;
        }
    }
    
}