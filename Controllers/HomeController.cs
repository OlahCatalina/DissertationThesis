using System;
using Dissertation_Thesis_SitesTextCrawler.BLL;
using Dissertation_Thesis_SitesTextCrawler.Data;
using Dissertation_Thesis_SitesTextCrawler.Helpers;
using Dissertation_Thesis_SitesTextCrawler.Models;
using Dissertation_Thesis_SitesTextCrawler.Models.ClassifierModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Dissertation_Thesis_SitesTextCrawler.Models.DatabaseModels;

namespace Dissertation_Thesis_SitesTextCrawler.Controllers
{
    public class HomeController : Controller
    {
        private Classifier _classifier;

        [HttpGet]
        public ActionResult Index()
        {
            _classifier = Train();
            return View();
        }

        [HttpPost]
        public ActionResult GetSites()
        {
            var dbContext = new WebAppContext();
            var sitesManager = new SitesManager(dbContext);
            var listOfSites = sitesManager.GetAllSitesWithTheirCategoriesFromDb();

            return Json(new {data = listOfSites});
        }

        [HttpPost]
        public async Task<ActionResult> EditSite(SiteDto site)
        {
            try
            {
                var dbContext = new WebAppContext();
                var sitesManager = new SitesManager(dbContext);

                var editedSite = await sitesManager.UpdateSiteInDb(site);

                await Task.Run(() => { _classifier = Train();}).ConfigureAwait(false);

                return Json(new { data = editedSite });
            }
            catch (Exception)
            {
                return Json(null);
            }
        }
        
        [HttpPost]
        public async Task<ActionResult> AddSite(SiteDto site)
        {
            try
            {
                var dbContext = new WebAppContext();
                var sitesManager = new SitesManager(dbContext);
                var addedSite = await sitesManager.AddSiteToDbAsync(site);

                await Task.Run(() => { _classifier = Train(); }).ConfigureAwait(false);

                return Json(new { data = addedSite });
            }
            catch (Exception)
            {
                return Json(null );
            }
        }

        [HttpPost]
        public async Task<ActionResult> RemoveSite(int siteId)
        {
            try
            {
                var dbContext = new WebAppContext();
                var sitesManager = new SitesManager(dbContext);

                var removedSite = sitesManager.RemoveSiteFromDb(siteId);

                await Task.Run(() => { _classifier = Train(); }).ConfigureAwait(false);

                return Json(new { data = removedSite });
            }
            catch (Exception)
            {
                return Json(null);
            }
        }

        [HttpPost]
        public async Task<ActionResult> GuessSiteCategory(string siteUrl)
        {
            var finalGuessUnknown = new KeyValuePair<string, double>("Unknown", 1);

            if (string.IsNullOrEmpty(siteUrl))
            {
                return Json( new { finalGuessUnknown });
            }

            if (_classifier == null)
            {
                _classifier = Train();
            }

            var dbContext = new WebAppContext();
            var sitesManager = new SitesManager(dbContext);

            var categories = sitesManager.GetAllCategoriesNames();
            var categoryProbabilityDictionary = new Dictionary<string, double>();

            var siteText = await SiteCrawler.GetSiteText(siteUrl.Trim(' '));

            // Calculate probability for each category
            foreach (var category in categories)
            {
                var probability = _classifier.IsInClassProbability(category, siteText);
                if(!double.IsNaN(probability))
                {
                    categoryProbabilityDictionary.Add(category, probability);
                }
            }

            if (categoryProbabilityDictionary.Count > 0)
            {
                var finalGuess = categoryProbabilityDictionary.OrderByDescending(d => d.Value).Take(3).ToList();

                return Json(new { predictions = finalGuess });
            }

            return Json(new { finalGuessUnknown });
        }

        [HttpPost]
        public ActionResult GetStatistics()
        {
            if (_classifier == null)
            {
                _classifier = Train();
            }

            var dbContext = new WebAppContext();
            var fonts = dbContext.DbFonts.ToList();
            var categories = dbContext.DbCategories.ToList();
            var fontFrequencyDict = new Dictionary<string, int>();
            var categoryFrequencyDict = new Dictionary<string, int>();
            var categoryFontFrequencyDict = new List<Tuple<string, string, int>>();

            foreach (var f in fonts)
            {
                var fontFrequency = dbContext.DbSiteFonts.Count(sf => sf.FontId == f.Id);
                fontFrequencyDict.Add(f.FontName, fontFrequency);
            }
            foreach (var c in categories)
            {
                var categoryFrequency = dbContext.DbSiteCategories.Count(sc => sc.CategoryId == c.Id);
                categoryFrequencyDict.Add(c.CategoryName, categoryFrequency);
            }

            foreach (var f in fonts)
            {
                foreach (var c in categories)
                {
                    var categoryFontFreq = dbContext.DbFontCategories.Count(fc => fc.CategoryId == c.Id && fc.FontId == f.Id);
                    categoryFontFrequencyDict.Add(new Tuple<string,string,int>(f.FontName, c.CategoryName, categoryFontFreq));
                }
            }

            var statistics = new Statistics()
            {
                ClassifierAccuracy =  _classifier.GetAccuracy(),
                ClassifierTotalNumberOfClasses = _classifier.GetNumberOfClasses(),
                ClassifierTotalNumberOfWords = _classifier.GetNumberOfAllWords(),
                ClassifierTotalNumberOfUniqueWords= _classifier.GetNumberOfUniqueWords(),
                ClassifierTotalNumberOfSiteCategoryPairs = _classifier.GetNumberOfDocuments(),
                TotalNumberOfCategories = dbContext.DbCategories.Count(),
                TotalNumberOfFonts = dbContext.DbFonts.Count(),
                TotalNumberOfSites = dbContext.DbSites.Count(),
                Categories = dbContext.DbCategories.Select(c=>c.CategoryName).ToList(),
                Fonts = fonts.Select(c => c.FontName).ToList(),
                FontFrequency = fontFrequencyDict,
                CategoryFrequency = categoryFrequencyDict,
                FontPerCategoryFrequency = categoryFontFrequencyDict
            };

            return Json(new { data = statistics });
        }

        private static Classifier Train()
        {
            var dbContext = new WebAppContext();
            var sitesManager = new SitesManager(dbContext);
            var listOfSites = sitesManager.GetAllSitesWithTheirCategoriesFromDb();
            var classifierTrainData = new List<Document>();

            foreach (var site in listOfSites)
            {
                var dbSite = sitesManager.FindDbSiteByUrl(site.Url);
                
                if (dbSite == null) 
                    continue;

                var siteText = dbSite.SiteText;

                foreach (var categoryName in site.Categories)
                {
                    var document = new Document(categoryName, siteText);
                    classifierTrainData.Add(document);
                }
            }

            return new Classifier(classifierTrainData);
        }

    }
}