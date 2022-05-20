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