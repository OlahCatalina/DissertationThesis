using Dissertation_Thesis_WebsiteScraper.BLL;
using Dissertation_Thesis_WebsiteScraper.Data;
using Dissertation_Thesis_WebsiteScraper.Models;
using Dissertation_Thesis_WebsiteScraper.Models.ClassifierModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Dissertation_Thesis_WebsiteScraper.Controllers
{
    [AllowCrossSite]
    public class HomeController : BaseController
    {
        private static SitesManager _sitesManager;
        private static Classifier _classifier;

        public HomeController()
        {
            _sitesManager = new SitesManager(WebApiContext);
        }

        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult GetSites()
        {
            var listOfSites = _sitesManager.GetAllSitesWithTheirCategoriesFromDb();

            return Json(new { data = listOfSites });
        }

        [HttpPost]
        public async Task<ActionResult> EditSite(SiteDto site)
        {
            try
            {
                await _sitesManager.UpdateSiteInDb(site);
                _classifier = Train();
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
                await _sitesManager.AddSiteToDbAsync(site);
                _classifier = Train();

                return Json(new { msg = "Site successfully added." });
            }
            catch (Exception e)
            {
                return Json(new { msg = e.Message });
            }
        }

        [HttpPost]
        public ActionResult RemoveSite(int siteId)
        {
            try
            {
                _sitesManager.RemoveSiteFromDb(siteId);
                _classifier = Train();

                return Json(new { msg = "Site successfully deleted." });
            }
            catch (Exception e)
            {
                return Json(new { msg = e.Message });
            }
        }

        [HttpPost]
        public ActionResult GetSiteText(string siteUrl)
        {
            try
            {
                var text =_sitesManager.GetSiteText(siteUrl);
                return Json(new { msg = "Site text successfully retrieved.", text = text });
            }
            catch (Exception e)
            {
                return Json(new { msg = e.Message });
            }
        }

        [HttpPost]
        public ActionResult GetSiteHtml(string siteUrl)
        {
            try
            {
                var html = _sitesManager.GetSiteHtml(siteUrl);
                return Json(new { msg = "Site HTML successfully retrieved.", html = html });
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
                return Json(new { msg = "Site data incomplete, could not perform the classification." });
            }

            if (_classifier == null)
            {
                _classifier = Train();
            }

            try
            {
                var categories = _sitesManager.GetAllCategoriesNames();
                var categoryProbabilityDictionary = new Dictionary<string, double>();

                var siteText = await WebSiteScraper.GetSiteText(siteUrl.Trim(' '));

                // Calculate probability for each category (max 15)
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

                    return Json(new { msg = "Ok", predictions = finalGuess });
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
                return Json(new { msg = "Categories list is empty." });
            }

            try
            {
                var dbCategoriesIds = WebApiContext.DbCategories.Where(c => categories.Contains(c.CategoryName)).Select(c => c.Id).ToList();
                var dbCatFontRelIds = WebApiContext.DbFontCategories.Where(fc => dbCategoriesIds.Contains(fc.CategoryId)).Select(fc => fc.FontId).ToList();
                var dbFonts = WebApiContext.DbFonts.Where(f => dbCatFontRelIds.Contains(f.Id)).Select(f => f.FontName).ToList();

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

            var fonts = WebApiContext.DbFonts.ToList();
            var categories = WebApiContext.DbCategories.ToList();
            var fontFrequencyDict = new List<Tuple<string, int>>();
            var categoryFrequencyDict = new List<Tuple<string, int>>();
            var categoryFontsListDict = new Dictionary<string, List<string>>();
            var fontsCategoriesListDict = new Dictionary<string, List<string>>();

            foreach (var f in fonts)
            {
                var fontFrequency = WebApiContext.DbSiteFonts.Count(sf => sf.FontId == f.Id);
                fontFrequencyDict.Add(new Tuple<string, int>(f.FontName, fontFrequency));
            }

            foreach (var c in categories)
            {
                var categoryFrequency = WebApiContext.DbSiteCategories.Count(sc => sc.CategoryId == c.Id);
                categoryFrequencyDict.Add(new Tuple<string, int>(c.CategoryName, categoryFrequency));
            }

            foreach (var f in fonts)
            {
                foreach (var c in categories)
                {
                    var categoryFontFreq = WebApiContext.DbFontCategories.Count(fc => fc.CategoryId == c.Id && fc.FontId == f.Id);

                    if (categoryFontFreq > 0)
                    {
                        if (!categoryFontsListDict.ContainsKey(c.CategoryName))
                        {
                            categoryFontsListDict.Add(c.CategoryName, new List<string> { f.FontName });
                        }
                        else if (!categoryFontsListDict[c.CategoryName].Contains(f.FontName))
                        {
                            categoryFontsListDict[c.CategoryName].Add(f.FontName);
                        }

                        if (!fontsCategoriesListDict.ContainsKey(f.FontName))
                        {
                            fontsCategoriesListDict.Add(f.FontName, new List<string> { c.CategoryName });
                        }
                        else if (!fontsCategoriesListDict[f.FontName].Contains(c.CategoryName))
                        {
                            fontsCategoriesListDict[f.FontName].Add(c.CategoryName);
                        }
                    }

                }
            }

            var sitesManager = new SitesManager(WebApiContext);
            var sites = WebApiContext.DbSites.ToList();
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
                ClassifierAccuracy = _classifier.GetAccuracy(sites, categories, siteTextAndCategory),
                ClassifierTotalNumberOfClasses = _classifier.GetNumberOfClasses(),
                ClassifierTotalNumberOfWords = _classifier.GetNumberOfAllWords(),
                ClassifierTotalNumberOfUniqueWords = _classifier.GetNumberOfUniqueWords(),
                ClassifierTotalNumberOfSiteCategoryPairs = _classifier.GetNumberOfDocuments(),
                TotalNumberOfCategories = WebApiContext.DbCategories.Count(),
                TotalNumberOfFonts = WebApiContext.DbFonts.Count(),
                TotalNumberOfSites = WebApiContext.DbSites.Count(),
                Categories = WebApiContext.DbCategories.Select(c => c.CategoryName).ToList(),
                Fonts = fonts.Select(c => c.FontName).ToList(),
                FontFrequency = fontFrequencyDict.OrderByDescending(cf => cf.Item2).ToList(),
                CategoryFrequency = categoryFrequencyDict.OrderByDescending(cf => cf.Item2).ToList(),
                CategoryFontsList = categoryFontsListDict,
                FontCategoryList = fontsCategoriesListDict
            };

            return Json(new { data = statistics });
        }

        private static Classifier Train()
        {
            var listOfSites = _sitesManager.GetAllSitesWithTheirCategoriesFromDb();
            var classifierTrainData = new List<Document>();

            foreach (var site in listOfSites)
            {
                var dbSite = _sitesManager.FindDbSiteByUrl(site.Url);

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