using Dissertation_Thesis_SitesTextCrawler.BLL;
using Dissertation_Thesis_SitesTextCrawler.Data;
using Dissertation_Thesis_SitesTextCrawler.Models;
using Dissertation_Thesis_SitesTextCrawler.Models.ClassifierModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Dissertation_Thesis_SitesTextCrawler.Controllers
{
    [AllowCrossSite]
    public class HomeController : Controller
    {
        private Classifier _classifier;
        
        [HttpGet]
        public ActionResult Index()
        {
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

                await sitesManager.UpdateSiteInDb(site);

                await Task.Run(() => { _classifier = Train(); }).ConfigureAwait(false);
                
                return Json(new { msg = "Site successfully updated." });
            }
            catch (Exception e)
            {
                return Json(new { msg = e.Message });
            }
        }
        
        [HttpPost]
        public async Task<ActionResult> AddSite(SiteDto site)
        {
            try
            {
                var dbContext = new WebAppContext();
                var sitesManager = new SitesManager(dbContext);

                await sitesManager.AddSiteToDbAsync(site);
                await Task.Run(() => { _classifier = Train(); }).ConfigureAwait(false);

                return Json(new { msg = "Site successfully added." });
            }
            catch (Exception e)
            {
                return Json(new { msg = e.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult> RemoveSite(int siteId)
        {
            try
            {
                var dbContext = new WebAppContext();
                var sitesManager = new SitesManager(dbContext);

                sitesManager.RemoveSiteFromDb(siteId);
                await Task.Run(() => { _classifier = Train(); }).ConfigureAwait(false);
                
                return Json(new { msg = "Site successfully deleted." });
            }
            catch (Exception e)
            {
                return Json(new { msg = e.Message });
            }
        }

        [HttpPost]
        [AllowCrossSite]
        public async Task<ActionResult> GuessSiteCategory(string siteUrl)
        {
            if (string.IsNullOrEmpty(siteUrl))
            {
                return Json( new { msg = "Site data incomplete, could not perform the classification." });
            }

            if (_classifier == null)
            {
                _classifier = Train();
            }

            try
            {
                var dbContext = new WebAppContext();
                var sitesManager = new SitesManager(dbContext);

                var categories = sitesManager.GetAllCategoriesNames();
                var categoryProbabilityDictionary = new Dictionary<string, double>();

                var siteText = await SiteCrawler.GetSiteText(siteUrl.Trim(' '));

                // Calculate probability for each category
                foreach (var category in categories)
                {
                    var probability = _classifier.IsInClassProbability(category, siteText);
                    if (!double.IsNaN(probability))
                    {
                        categoryProbabilityDictionary.Add(category, probability);
                    }
                }

                if (categoryProbabilityDictionary.Count > 0)
                {
                    var finalGuess = categoryProbabilityDictionary.OrderByDescending(d => d.Value).Take(3);

                    return Json(new {msg = "Ok", predictions = finalGuess});
                }

                return Json(new { msg = "Could not perform the classification." });

            }
            catch (Exception e)
            {
                return Json(new { msg = e.Message });
            }
            
        }

        [HttpPost]
        [AllowCrossSite]
        public ActionResult SuggestFontsBasedOnCategoriesList(List<string> categories)
        {
            if (categories == null || categories.Count == 0)
            {
                return Json(new { msg = "Categories list is empty."});
            }

            if (_classifier == null)
            {
                _classifier = Train();
            }

            try
            {
                var dbContext = new WebAppContext();
                var dbCategoriesIds = dbContext.DbCategories.Where(c => categories.Contains(c.CategoryName)).Select(c => c.Id);
                var dbCatFontRelIds = dbContext.DbFontCategories.Where(fc => dbCategoriesIds.Contains(fc.CategoryId)).Select(fc => fc.FontId);
                var dbFonts = dbContext.DbFonts.Where(f => dbCatFontRelIds.Contains(f.Id)).Select(f=>f.FontName);

                return Json(new { msg = "Ok", fonts = dbFonts });
            }
            catch (Exception e)
            {
                return Json(new { msg = e.Message });
            }

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
            var fontFrequencyDict = new List<Tuple<string, int>>();
            var categoryFrequencyDict = new List<Tuple<string, int>>();
            var categoryFontFrequencyDict = new List<Tuple<string, string, int>>();

            foreach (var f in fonts)
            {
                var fontFrequency = dbContext.DbSiteFonts.Count(sf => sf.FontId == f.Id);
                fontFrequencyDict.Add(new Tuple<string, int>(f.FontName, fontFrequency));
            }
            foreach (var c in categories)
            {
                var categoryFrequency = dbContext.DbSiteCategories.Count(sc => sc.CategoryId == c.Id);
                categoryFrequencyDict.Add(new Tuple<string, int>(c.CategoryName, categoryFrequency));
            }

            foreach (var f in fonts)
            {
                foreach (var c in categories)
                {
                    var categoryFontFreq = dbContext.DbFontCategories.Count(fc => fc.CategoryId == c.Id && fc.FontId == f.Id);
                    categoryFontFrequencyDict.Add(new Tuple<string,string,int>(f.FontName, c.CategoryName, categoryFontFreq));
                }
            }

            var sitesManager = new SitesManager(dbContext);
            var sites = dbContext.DbSites.ToList();
            var listOfSites = sitesManager.GetAllSitesWithTheirCategoriesFromDb();
            var siteTextAndCategory = new List<Tuple<string, string>>();
            foreach (var s in listOfSites)
            {
                var dbSite = sites.FirstOrDefault(dbS => dbS.SiteUrl == s.Url);
                if (dbSite != null)
                {
                    foreach (var category in s.Categories)
                    {
                        var t = new Tuple<string, string>(dbSite.SiteText, category);
                        siteTextAndCategory.Add(t);
                    }
                }
            }

            var statistics = new Statistics()
            {
                ClassifierAccuracy =  _classifier.GetAccuracy(siteTextAndCategory),
                ClassifierTotalNumberOfClasses = _classifier.GetNumberOfClasses(),
                ClassifierTotalNumberOfWords = _classifier.GetNumberOfAllWords(),
                ClassifierTotalNumberOfUniqueWords= _classifier.GetNumberOfUniqueWords(),
                ClassifierTotalNumberOfSiteCategoryPairs = _classifier.GetNumberOfDocuments(),
                TotalNumberOfCategories = dbContext.DbCategories.Count(),
                TotalNumberOfFonts = dbContext.DbFonts.Count(),
                TotalNumberOfSites = dbContext.DbSites.Count(),
                Categories = dbContext.DbCategories.Select(c=>c.CategoryName).ToList(),
                Fonts = fonts.Select(c => c.FontName).ToList(),
                FontFrequency = fontFrequencyDict.OrderByDescending(cf => cf.Item2).ToList(),
                CategoryFrequency = categoryFrequencyDict.OrderByDescending(cf=>cf.Item2).ToList(),
                FontPerCategoryFrequency = categoryFontFrequencyDict.OrderBy(fpcf => fpcf.Item1).ThenBy(fpcf => fpcf.Item2).ToList()
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