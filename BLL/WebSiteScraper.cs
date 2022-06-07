using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dissertation_Thesis_WebsiteScraper.BLL
{
    public class WebSiteScraper
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

                // Take site title
                var title = htmlDocument.DocumentNode.SelectSingleNode("//head")?.Descendants("title").FirstOrDefault()
                    ?.InnerText;
                totalText += title + " ";

                // Take metadata (site keywords and description)
                var metaDescriptionNode = htmlDocument.DocumentNode.SelectSingleNode("//meta[@name='description']");
                if (metaDescriptionNode != null)
                {
                    var description = metaDescriptionNode.Attributes["content"]?.Value;
                    if (!string.IsNullOrEmpty(description)) totalText += " " + description;
                }

                var metaKeyWordsNode = htmlDocument.DocumentNode.SelectSingleNode("//meta[@name='keywords']");
                if (metaKeyWordsNode != null)
                {
                    var keywords = metaKeyWordsNode.Attributes["content"]?.Value;
                    if (!string.IsNullOrEmpty(keywords)) totalText += " " + keywords;
                }

                // Take body text (<p>, <span>, <h1>, ..., <h5>)
                var body = htmlDocument.DocumentNode.SelectSingleNode("//body");
                if (body != null)
                {
                    var paragraphs = body.Descendants("p").Select(p => p.InnerText).ToList();
                    var spans = body.Descendants("span").Select(p => p.InnerText).ToList();
                    var h1S = body.Descendants("h1").Select(p => p.InnerText).ToList();
                    var h2S = body.Descendants("h2").Select(p => p.InnerText).ToList();
                    var h3S = body.Descendants("h3").Select(p => p.InnerText).ToList();
                    var h4S = body.Descendants("h4").Select(p => p.InnerText).ToList();
                    var h5S = body.Descendants("h5").Select(p => p.InnerText).ToList();

                    if (h1S.Count > 0)
                    {
                        totalText += string.Join(" ", h1S) + " ";
                    }
                    if (h2S.Count > 0)
                    {
                        totalText += string.Join(" ", h2S) + " ";
                    }
                    if (h3S.Count > 0)
                    {
                        totalText += string.Join(" ", h3S) + " ";
                    }
                    if (h4S.Count > 0)
                    {
                        totalText += string.Join(" ", h4S) + " ";
                    }
                    if (h5S.Count > 0)
                    {
                        totalText += string.Join(" ", h5S) + " ";
                    }

                    if (paragraphs.Count > 0)
                    {
                        totalText += string.Join(" ", paragraphs) + " ";
                    }

                    if (spans.Count > 0)
                    {
                        totalText += string.Join(" ", spans) + " ";
                    }
                }

                // take only words
                totalText = Regex.Replace(totalText, @"\n", " ");
                totalText = Regex.Replace(totalText, @"\t", " ");
                totalText = Regex.Replace(totalText, @"»", " ");
                totalText = Regex.Replace(totalText, @"&quot;", " ");
                totalText = Regex.Replace(totalText, @"&amp;", " ");
                totalText = Regex.Replace(totalText, @"&lt;", " ");
                totalText = Regex.Replace(totalText, @"&gt;", " ");
                totalText = Regex.Replace(totalText, @"&nbsp;", " ");
                totalText = Regex.Replace(totalText, @"\d+", " ");
                totalText = Regex.Replace(totalText, @"\W", " ");
                totalText = Regex.Replace(totalText, @"\s+", " ");
                totalText = totalText.ToLowerInvariant();

                if (string.IsNullOrEmpty(totalText) || string.IsNullOrWhiteSpace(totalText))
                {
                    throw new Exception("Site text protected");
                }

                return totalText;
            }
            catch (Exception)
            {
                throw new Exception("Site scraper could not read the site text.");
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

                var head = htmlDocument.DocumentNode.SelectSingleNode("//head")?.InnerHtml ?? "";
                var body = htmlDocument.DocumentNode.SelectSingleNode("//body")?.InnerHtml ?? "";
                var allHtml = head + body;

                return allHtml;
            }
            catch (Exception e)
            {
                throw new Exception("Scraper error at reading site HTML. " + e.Message);
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
            listOfFonts = listOfFonts
                .Where(f => string.Compare(f, "inherit", StringComparison.InvariantCultureIgnoreCase) != 0)
                .ToList();
            return listOfFonts;
        }
    }
}