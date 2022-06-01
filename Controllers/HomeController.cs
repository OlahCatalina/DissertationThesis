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
        private static BayesClassifier _bayesNewClassifier;

        public HomeController()
        {
            _sitesManager = new SitesManager(WebApiContext);
            _bayesNewClassifier = new BayesClassifier();
        }

        [HttpGet]
        public ActionResult Index()
        {
            //Classify();
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
                await _sitesManager.AddSiteToDbAsync(site);
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
                _sitesManager.RemoveSiteFromDb(siteId);

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
            var categoryFontFrequencyDict = new List<Tuple<string, string, int>>();

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
                    categoryFontFrequencyDict.Add(new Tuple<string, string, int>(f.FontName, c.CategoryName, categoryFontFreq));
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
                ClassifierAccuracy = _classifier.GetAccuracy(siteTextAndCategory),
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
                FontPerCategoryFrequency = categoryFontFrequencyDict.OrderBy(fpcf => fpcf.Item1).ThenBy(fpcf => fpcf.Item2).ToList()
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

        private void Classify()
        {
            var listOfSites = _sitesManager.GetAllSitesWithTheirCategoriesFromDb();
            var listOfDbSites = _sitesManager.GetAllSitesFromDb();

            foreach (var site in listOfSites)
            {
                foreach (var category in site.Categories)
                {
                    var ss = listOfDbSites.First(s => s.SiteUrl == site.Url);

                    _bayesNewClassifier.Add(category, ss.SiteText);
                }
            }

            var list = new List<string> { "The Best Contouring Tip for Newbies | Into The Gloss Turns out, similarly to painting, contouring is all about location—where you place color is equally as important as which color you’re actually working with. If you’re a talented enough painter (or makeup artist) you know exactly where to place certain shades to accentuate shadows, angles, or certain features of an object, which are important skills for effectively shaping the face. But what if you don’t dabble in one of those two careers and still want to learn how to contour? This was the dilemma I faced last week as I sat in front of my mirror, un-contoured and three minutes away from being late for a nice dinner. While I’d normally use a swipe or two of bronzer where I thought it should go, I had never tried to fully snatch my face… And now wasn’t the time to start. I wanted to level up my makeup for the night, but I clearly didn’t have time to do much. I didn’t need my cheekbones to look like they could cut glass— I just wanted something foolproof and quick. My salvation came, as it often does, in the form of a Tiktok. I looked up ‘contour hack’ and in thirteen seconds flat, makeup theorist Megan Lavallie bestowed upon me such great contouring knowledge that I wanted to scream it from the rooftops. “There’s this one spot on your cheek that if you poked a hole through it, it would touch your teeth. If you softly blend it out [after adding contour], it will create the most soft-looking, high cheekbones for those who want to look good from the side and the front.” And so, with my remaining two and a half minutes, all I had to do was find the spot on my cheek where I could feel my teeth, add a single dot of contour, and blend lightly. That was it! I grabbed a contour brush and my contour-that-isn’t-contour, Glossier’s Cloud Paint in Dusk, and got to work. I applied one dot on each cheek, and as Lavallie instructed, softly blended out. In an instant, my face gained more dimension while still looking incredibly natural. The result was seamless, kind of like I somehow managed to tan just the hollows of my cheeks. Fancy dinner or not, I knew this trick had me in a chokehold from the moment I saw my newly sculpted face. Lavallie’s tip—which has 7.5 million views, by the way—changed the way I think about contouring. I realized that for beauty beginners and lovers of No Makeup Makeup, contour doesn’t have to be an intimidating event filled with multiple creams, powders, and brushes. I used to think it was Kim Kardashian-style or bust, leading to more nightmares about trying the ‘3’ shape on my round face than I would care to admit. Turns out, you can still get a fantastic look from a simplified method. Instead of doing lines or dots of product all over the face à la Instagram tutorials, the bare necessity of contouring is just a single dot! Not so intimidating now, is it? But Hannah, I can hear you saying, I’m still scared I’m going to screw it up. Fair! Contour is easy to screw up when you don’t know what you’re doing. But I have faith that you do know what you’re doing because you’ve gotten this far in the article, and when (not if) you get contouring right, you’ll totally elevate your look in just a few minutes. Besides, this tip makes it almost foolproof, regardless of whether you’re using a cream or powder contour. Might as well try it! It’s not going to get any easier than this. All photos taken by Elliot Duprey Use your Glossier account to save articles on Into The Gloss. More features coming soon! Create new Glossier account Create a Glossier account to build your Into The Gloss profile and save your favorite stories. By signing up, you agree to receive updates and special offers for Into The Gloss&#39;s products and services. You may unsubscribe at any time. I already have an account As in, your inbox. Sign up below and we&#39;ll bring you the top stories from ITG every week. The very best of Into The Gloss, delivered weekly right to your inbox Share Tweet Pin Copy by Hannah Burnstein Share Tweet Pin Copy DON'T MISS Most Popular Interviews Makeup Skincare [email&#160;protected] Invalid Invalid Invalid Invalid Your password needs to be at least 6 characters long System.Linq.Enumerable+WhereSelectListIterator`2[HtmlAgilityPack.HtmlNode,System.String] System.Linq.Enumerable+WhereSelectListIterator`2[HtmlAgilityPack.HtmlNode,System.String] System.Linq.Enumerable+WhereSelectListIterator`2[HtmlAgilityPack.HtmlNode,System.String] System.Linq.Enumerable+WhereSelectListIterator`2[HtmlAgilityPack.HtmlNode,System.String] System.Linq.Enumerable+WhereSelectListIterator`2[HtmlAgilityPack.HtmlNode,System.String]" };
            var pp = _bayesNewClassifier.Classify(list);
            var ppOrd = pp.OrderByDescending(b => b.Value).ToList();
            var a = 1;
        }
    }
}