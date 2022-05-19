using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Dissertation_Thesis_SitesTextCrawler.Helpers;
using Dissertation_Thesis_SitesTextCrawler.Models;

namespace Dissertation_Thesis_SitesTextCrawler.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult GetSites()
        {
            var listOfSites = GetListOfSitesFromSitesFile();
            return Json(new {data = listOfSites});
        }

        [HttpPost]
        public ActionResult RemoveSite(int index)
        {
            var listOfSites = GetListOfSitesFromSitesFile();
            listOfSites = listOfSites.Where(s => s.Index != index).ToList();
            var pathToFile = AppDomain.CurrentDomain.BaseDirectory +
                             FileHelper.PATH_TO_FILES +
                             FileHelper.SITES_FILE_NAME;

            FileHelper.WriteSitesInFile(listOfSites, pathToFile);
            return Json(new {data = listOfSites});
        }

        [HttpPost]
        public ActionResult EditSite(Site site)
        {
            var listOfSites = GetListOfSitesFromSitesFile();
            listOfSites = listOfSites.Where(s => s.Index != site.Index).ToList();
            var canEdit = site.Index > 0 && !string.IsNullOrEmpty(site.Name) && !string.IsNullOrEmpty(site.Url) &&
                          site.Categories != null && site.Categories.Count > 0;

            if (canEdit)
            {
                listOfSites.Add(site);
                listOfSites = listOfSites.OrderBy(s => s.Index).ToList();
                var pathToFile = AppDomain.CurrentDomain.BaseDirectory +
                                 FileHelper.PATH_TO_FILES +
                                 FileHelper.SITES_FILE_NAME;

                FileHelper.WriteSitesInFile(listOfSites, pathToFile);
            }

            return Json(new {data = listOfSites});
        }

        [HttpPost]
        public ActionResult AddSite(Site site)
        {
            var listOfSites = GetListOfSitesFromSitesFile();
            var arrayWithIndexes = listOfSites.Select(s => s.Index).ToArray();
            var i = 1;
            while (arrayWithIndexes.Contains(i)) i++;
            site.Index = i;
            listOfSites.Add(site);
            listOfSites = listOfSites.OrderBy(s => s.Index).ToList();
            var pathToFile = AppDomain.CurrentDomain.BaseDirectory +
                             FileHelper.PATH_TO_FILES +
                             FileHelper.SITES_FILE_NAME;

            FileHelper.WriteSitesInFile(listOfSites, pathToFile);
            return Json(new {data = listOfSites});
        }

        [HttpPost]
        public async Task<ActionResult> Train()
        {
            var listOfSites = GetListOfSitesFromSitesFile();
            var pathToFile = AppDomain.CurrentDomain.BaseDirectory +
                             FileHelper.PATH_TO_FILES +
                             FileHelper.CORPUS_FILE_NAME;


            await FeedClassifierCorpusWithSitesTextAndCategory(listOfSites, pathToFile);

            return Json(new { data = listOfSites });
        }

        [HttpPost]
        public async Task<ActionResult> GuessSiteCategory(string siteUrl)
        {
            if (string.IsNullOrEmpty(siteUrl)) return Json(new { });

            var listOfSites = GetListOfSitesFromSitesFile();
            var pathToFile = AppDomain.CurrentDomain.BaseDirectory +
                             FileHelper.PATH_TO_FILES +
                             FileHelper.CORPUS_FILE_NAME;

            // Get site text
            var siteText = await Crawler.GetSiteText(siteUrl);

            // Feed Corpus with existing sites
            await FeedClassifierCorpusWithSitesTextAndCategory(listOfSites, pathToFile);
            var trainCorpus = GetTrainCorpusFromFile(pathToFile);

            var classifier = new Classifier(trainCorpus);

            var categories = trainCorpus.Select(t => t.Class).Distinct();
            var categoryProbabilityDictionary = new Dictionary<string, double>();

            // Calculate probability for each category
            foreach (var category in categories)
            {
                var probability = classifier.IsInClassProbability(category, siteText);
                categoryProbabilityDictionary.Add(category, probability);
            }

            var finalGuess = categoryProbabilityDictionary.OrderByDescending(d => d.Value).First();

            return Json(new {finalGuess});
        }

        private static List<Document> GetTrainCorpusFromFile(string pathToFile)
        {
            var lines = FileHelper.ReadFileLines(pathToFile);
            var documents = new List<Document>();

            foreach (var line in lines)
            {
                var parts = line.Split(new[] {" ||| "}, StringSplitOptions.None);
                var fonts = parts[1].Split(new[] {" [x][x] "}, StringSplitOptions.None).ToList();
                var doc = new Document(parts[0], fonts, parts[2]);
                documents.Add(doc);
            }

            return documents;
        }

        private static async Task FeedClassifierCorpusWithSitesTextAndCategory(IEnumerable<Site> sites, string pathToCorpusFile)
        {
            // Save into text file
            var listOfDocuments = new List<Document>();
   
            foreach (var site in sites)
            {
                var url = site.Url;
                var siteText = await Crawler.GetSiteText(url);
                var fontList = await Crawler.GetSiteFonts(url);

                if (site.Categories != null && site.Categories.Count > 0)
                {
                    foreach (var category in site.Categories)
                    {
                        if (string.IsNullOrEmpty(siteText))
                            continue;
                        var document = new Document(category, fontList, siteText);
                        listOfDocuments.Add(document);
                    }
                }

            }

            FileHelper.WriteDocumentsInCorpusFile(listOfDocuments, pathToCorpusFile);
        }

        private static List<Site> GetListOfSitesFromSitesFile()
        {
            var listOfSites = new List<Site>();
            var pathToFile = AppDomain.CurrentDomain.BaseDirectory +
                             FileHelper.PATH_TO_FILES +
                             FileHelper.SITES_FILE_NAME;
            var lines = FileHelper.ReadFileLines(pathToFile);

            foreach (var line in lines)
            {
                var parts = line.Split(new[] {" ||| "}, StringSplitOptions.None);
                var site = new Site
                {
                    Index = Convert.ToInt32(parts[0]),
                    Name = parts[1],
                    Url = parts[2],
                    Categories = parts[3].Split(new[] {" [x][x] "}, StringSplitOptions.None).ToList()
                };

                listOfSites.Add(site);
            }

            return listOfSites;
        }
    }
}