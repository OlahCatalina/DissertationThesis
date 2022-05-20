using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dissertation_Thesis_SitesTextCrawler.Helpers
{
    public class SiteCrawler
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
                // TAKE META WITH CONTENT NAME=KEYWORDS & NAME=DESCRIPTION
                var title = htmlDocument.DocumentNode.SelectSingleNode("//head").Descendants("title").FirstOrDefault()?.InnerText;
                var body = htmlDocument.DocumentNode.SelectSingleNode("//body");
                var paragraphs = body.Descendants("p").ToList().Select(p => p.InnerText);
                var spans = body.Descendants("span").ToList().Select(p => p.InnerText);
                var h1S = body.Descendants("h1").ToList().Select(p => p.InnerText);
                var h2S = body.Descendants("h2").ToList().Select(p => p.InnerText);
                var h3S = body.Descendants("h3").ToList().Select(p => p.InnerText);
                var h4S = body.Descendants("h4").ToList().Select(p => p.InnerText);
                var h5S = body.Descendants("h5").ToList().Select(p => p.InnerText);
                var hs = h1S + " " + h2S + " " + h3S + " " + h4S + " " + h5S + " ";
                totalText += title + " " + string.Join(" ", paragraphs) + " " + string.Join(" ", spans) + " " + hs;
                totalText = Regex.Replace(totalText, @"\s+", " ");
                totalText = Regex.Replace(totalText, @"\n", " ");
                totalText = Regex.Replace(totalText, @"\t", " ");
                totalText = Regex.Replace(totalText, @"»", " ");
                totalText= Regex.Replace(totalText, @"&amp;", " ");

                return totalText;
            }
            catch (Exception e)
            {
                return "";
            }
          
        }

        public static async Task<string> GetSiteHtml(string url)
        {
            try
            {
                var httpClient = new HttpClient();
                var html = await httpClient.GetStringAsync(url);
                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);

                var head = htmlDocument.DocumentNode.SelectSingleNode("//head").InnerHtml;
                var body = htmlDocument.DocumentNode.SelectSingleNode("//body").InnerHtml;
                var allHtml = head + body;

                return allHtml;
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static List<string> GetSiteFonts(string siteHtml)
        {
            var listOfFonts = new List<string>();
            const string pattern = "font-family:\\s?(['|\"]?(\\w* *)+['|\"]?)";

            foreach (Match match in Regex.Matches(siteHtml, pattern))
            {
                if (!match.Success || match.Groups.Count <= 0) continue;

                var fontValue = match.Value;
                listOfFonts.Add(fontValue);
            }

            listOfFonts = listOfFonts.Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList();
            listOfFonts = listOfFonts.Select(f => f.Split(':')[1].Trim(' ').Trim('\"').Trim('\'')).ToList();

            return listOfFonts;
        }

    }
}