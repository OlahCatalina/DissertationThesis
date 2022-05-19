using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace Dissertation_Thesis_SitesTextCrawler.Helpers
{
    public class Crawler
    {
        public static async Task<string> GetSiteText(string url)
        {
            try
            {
                var totalText = "";
                var httpClient = new HttpClient();
                var html = await httpClient.GetStringAsync(url);
                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);
                var title = htmlDocument.DocumentNode.SelectSingleNode("//head").Descendants("title").FirstOrDefault()?.InnerText;
                var body = htmlDocument.DocumentNode.SelectSingleNode("//body");
                var paragraphs = body.Descendants("p").ToList().Select(p => p.InnerText);
                var spans = body.Descendants("span").ToList().Select(p => p.InnerText);
                totalText += title + " " + string.Join(" ", paragraphs) + " " + string.Join(" ", spans);
                totalText = Regex.Replace(totalText, @"\s+", " ");
                totalText = Regex.Replace(totalText, @"\n", " ");
                totalText = Regex.Replace(totalText, @"\t", " ");
                return totalText;
            }
            catch (Exception e)
            {
                return "";
            }
          
        }


        public static async Task<List<string>> GetSiteFonts(string url)
        {
            return new List<string> {"Arial", "Verdana"};
        }



    }
}