using Dissertation_Thesis_SitesTextCrawler.Data;
using Dissertation_Thesis_SitesTextCrawler.Models;
using Dissertation_Thesis_SitesTextCrawler.Models.DatabaseModels;
using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Threading.Tasks;

namespace Dissertation_Thesis_SitesTextCrawler.BLL
{
    public class SitesManager
    {
        private readonly WebAppContext _dbContext;

        public SitesManager(WebAppContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<DbSite> GetAllSitesFromDb()
        {
            return _dbContext.DbSites.ToList();
        }

        public List<SiteDto> GetAllSitesWithTheirCategoriesFromDb()
        {
            var allSitesFromDb = GetAllSitesFromDb();
            var sites = new List<SiteDto>();

            foreach (var dbSite in allSitesFromDb)
            {
                var categoriesIds = _dbContext.DbSiteCategories.Where(sc => sc.SiteId == dbSite.Id).Select(sc=> sc.CategoryId).ToList();
                var dbCategories = _dbContext.DbCategories.Where(c => categoriesIds.Contains(c.Id)).Select(c=> c.CategoryName).ToList();
                var siteDto = new SiteDto
                {
                    Id = dbSite.Id,
                    Name = dbSite.SiteName,
                    Url = dbSite.SiteUrl,
                    Categories = dbCategories
                };

                sites.Add(siteDto);
            }

            return sites;
        }

        public async Task AddSiteToDbAsync(SiteDto site)
        {
            var dbSite = new DbSite { SiteName = site.Name, SiteUrl = site.Url };

            if (string.IsNullOrEmpty(dbSite.SiteName) || string.IsNullOrEmpty(dbSite.SiteUrl) || site.Categories == null || site.Categories.Count == 0)
            {
                throw new Exception("Incomplete site data, insertion failed.");
            }

            dbSite.SiteName = dbSite.SiteName.Trim(' ');
            dbSite.SiteUrl = dbSite.SiteUrl.Trim(' ');

            var siteText = await SiteCrawler.GetSiteText(dbSite.SiteUrl);
            var siteHtml = await SiteCrawler.GetSiteHtml(dbSite.SiteUrl);

            dbSite.SiteText = siteText;
            dbSite.SiteHtml = siteHtml;
            if (string.IsNullOrEmpty(dbSite.SiteText) || string.IsNullOrEmpty(dbSite.SiteHtml))
            {
                throw new Exception("The text or HTML of this site could not be read. Insertion failed.");
            }

            // Add site
            dbSite.SiteText = dbSite.SiteText.Trim(' ');
            dbSite.SiteHtml = dbSite.SiteHtml.Trim(' ');

            _dbContext.DbSites.Add(dbSite);
            await _dbContext.SaveChangesAsync();

            // Add/update categories and site-categories relationships
            foreach (var category in site.Categories)
            {
                if (string.IsNullOrEmpty(category))
                    continue;

                // If category is not already in DB, add it
                var dbCategory = new DbCategory { CategoryName = category.Trim(' ') };
                var foundCategory = _dbContext.DbCategories.FirstOrDefault(c => c.CategoryName == dbCategory.CategoryName);
                if (foundCategory != null)
                {
                    dbCategory = foundCategory;
                }
                else
                {
                    _dbContext.DbCategories.Add(dbCategory);
                    await _dbContext.SaveChangesAsync();
                }

                // Manage relation between site and category
                var siteCatRel = new DbSiteCategory { SiteId = dbSite.Id, CategoryId = dbCategory.Id };
                var foundRelationship = _dbContext.DbSiteCategories
                    .FirstOrDefault(sc => sc.SiteId == siteCatRel.SiteId && sc.CategoryId == siteCatRel.CategoryId);

                if (foundRelationship == null)
                {
                    _dbContext.DbSiteCategories.Add(siteCatRel);
                    await _dbContext.SaveChangesAsync();
                }
            }

            var thisSiteDbCategoriesIds = _dbContext.DbSiteCategories
                .ToList()
                .Where(sc => sc.SiteId == dbSite.Id)
                .Select(sc => sc.CategoryId)
                .ToList();

            // Add/update fonts and site-fonts relationships
            var siteFonts = SiteCrawler.GetSiteFonts(siteHtml);
            foreach (var font in siteFonts)
            {
                if (string.IsNullOrEmpty(font))
                    continue;

                var dbFont = new DbFont { FontName = font.Trim(' ') };

                // If not already in DB, add font
                var foundFont = _dbContext.DbFonts.FirstOrDefault(c => c.FontName == dbFont.FontName);
                if (foundFont != null)
                {
                    dbFont = foundFont;
                }
                else
                {
                    _dbContext.DbFonts.Add(dbFont);
                    await _dbContext.SaveChangesAsync();
                }

                // Manage relation between fonts and sites
                var dbSiteFontRel = new DbSiteFont { FontId = dbFont.Id, SiteId = dbSite.Id };
                var foundRelationship = _dbContext.DbSiteFonts
                    .FirstOrDefault(sf => sf.FontId == dbSiteFontRel.FontId && sf.SiteId == dbSiteFontRel.SiteId);

                // If not already in DB, add site-font relationship
                if (foundRelationship == null)
                {
                    _dbContext.DbSiteFonts.Add(dbSiteFontRel);
                    await _dbContext.SaveChangesAsync();
                }

                // Manage relation between fonts and categories
                foreach (var categoryId in thisSiteDbCategoriesIds)
                {
                    var fontCatRel = new DbFontCategory { FontId = dbFont.Id, CategoryId = categoryId };
                    var foundFontCatRel = _dbContext.DbFontCategories
                        .FirstOrDefault(fc => fc.FontId == fontCatRel.FontId && fc.CategoryId == fontCatRel.CategoryId);

                    // If not already in DB, add font-category relationship
                    if (foundFontCatRel == null)
                    {
                        _dbContext.DbFontCategories.Add(fontCatRel);
                        await _dbContext.SaveChangesAsync();
                    }
                }

            }
        }
        
        public void RemoveSiteFromDb(int siteId)
        {
            var site = _dbContext.DbSites.FirstOrDefault(s => s.Id == siteId);

            if (site == null)
            {
                throw new Exception("Site not found.");
            }

            // Find all attached categories to this site => remove relationship between current site and those categories
            var dbSiteCategories = _dbContext.DbSiteCategories.Where(sc => sc.SiteId == site.Id).ToList();
            _dbContext.DbSiteCategories.RemoveRange(dbSiteCategories);
            _dbContext.SaveChanges();

            // Find all fonts found in this site => remove relationship between current site and those fonts
            var dbSiteFonts = _dbContext.DbSiteFonts.Where(sf => sf.SiteId == site.Id).ToList();
            _dbContext.DbSiteFonts.RemoveRange(dbSiteFonts);
            _dbContext.SaveChanges();

            // Finally, remove the site (The font and category don't get deleted)
            _dbContext.DbSites.Remove(site);
            _dbContext.SaveChanges();
        }

        public async Task UpdateSiteInDb(SiteDto site)
        {
            var dbSite = _dbContext.DbSites.FirstOrDefault(s => s.Id == site.Id);

            if (dbSite == null)
            {
                throw new Exception("Site not found.");
            }

            if (string.IsNullOrEmpty(dbSite.SiteName) || string.IsNullOrEmpty(dbSite.SiteUrl) || site.Categories == null || site.Categories.Count == 0)
            {
                throw new Exception("Incomplete site data, update failed.");
            }

            // Update name
            dbSite.SiteName = site.Name;
            _dbContext.DbSites.AddOrUpdate(dbSite);
            await _dbContext.SaveChangesAsync();
            
            // Recalculate text & html
            dbSite.SiteUrl = site.Url.Trim(' ');
            var siteText = await SiteCrawler.GetSiteText(dbSite.SiteUrl);
            var siteHtml = await SiteCrawler.GetSiteHtml(dbSite.SiteUrl);
            dbSite.SiteText = siteText;
            dbSite.SiteHtml = siteHtml;

            var oldSiteCategories = _dbContext.DbSiteCategories.Where(sc => dbSite.Id == sc.SiteId).ToList();
            var oldSiteFonts = _dbContext.DbSiteFonts.Where(sf => dbSite.Id == sf.SiteId).ToList();

            // Remove old font-category relationships
            foreach (var oldCategory in oldSiteCategories)
            {
                foreach (var oldFont in oldSiteFonts)
                {
                    var dbFontCatRel = _dbContext.DbFontCategories.Where(fc => fc.FontId == oldFont.FontId && fc.CategoryId == oldCategory.CategoryId).ToList();
                    _dbContext.DbFontCategories.RemoveRange(dbFontCatRel);
                    await _dbContext.SaveChangesAsync();
                }
            }

            // Remove old site-font relationships
            foreach (var oldFont in oldSiteFonts)
            {
                var dbSiteFontRel = _dbContext.DbSiteFonts.Where(sf => sf.SiteId == dbSite.Id && sf.FontId == oldFont.FontId).ToList();
                _dbContext.DbSiteFonts.RemoveRange(dbSiteFontRel);
                await _dbContext.SaveChangesAsync();
            }

            // Remove old site-category relationships
            foreach (var oldCategory in oldSiteCategories)
            {
                var dbSiteCatRel = _dbContext.DbSiteCategories.Where(sc => sc.SiteId == dbSite.Id && sc.CategoryId == oldCategory.CategoryId).ToList();
                _dbContext.DbSiteCategories.RemoveRange(dbSiteCatRel);
                await _dbContext.SaveChangesAsync();
            }

            if (!string.IsNullOrEmpty(dbSite.SiteText))
            {
                dbSite.SiteText = dbSite.SiteText.Trim(' ');
            }

            if (!string.IsNullOrEmpty(dbSite.SiteHtml))
            {
                dbSite.SiteHtml = dbSite.SiteHtml.Trim(' ');
            }

            // Find new fonts in site HTML
            var siteNewFonts = SiteCrawler.GetSiteFonts(siteHtml);

            foreach (var font in siteNewFonts)
            {
                if (string.IsNullOrEmpty(font)) continue;

                var dbFont = new DbFont { FontName = font.Trim(' ') };
                var foundDbFont = _dbContext.DbFonts.FirstOrDefault(f => f.FontName == dbFont.FontName);

                // If not in DB already, add font
                if (foundDbFont == null)
                {
                    _dbContext.DbFonts.Add(dbFont);
                    await _dbContext.SaveChangesAsync();
                }
                else
                {
                    dbFont = foundDbFont;
                }

                // Manage relation between fonts and sites
                var dbSiteFontRel = new DbSiteFont { FontId = dbFont.Id, SiteId = dbSite.Id };
                var foundDbSiteFontRel = _dbContext.DbSiteFonts
                    .FirstOrDefault(sf => sf.SiteId == dbSiteFontRel.SiteId && sf.FontId == dbSiteFontRel.FontId);

                // If not in DB already, add site-font relationship
                if (foundDbSiteFontRel == null)
                {
                    _dbContext.DbSiteFonts.Add(dbSiteFontRel);
                    await _dbContext.SaveChangesAsync();
                }
            }

            // Find new categories based on site text
            var siteNewCategories = site.Categories;
            foreach (var category in siteNewCategories)
            {
                if (string.IsNullOrEmpty(category))
                    continue;

                var dbCategory = new DbCategory { CategoryName = category.Trim(' ') };
                var foundDbCategory = _dbContext.DbCategories.FirstOrDefault(c => c.CategoryName == dbCategory.CategoryName);

                // If not in DB already, add new category
                if (foundDbCategory == null)
                {
                    _dbContext.DbCategories.Add(dbCategory);
                    await _dbContext.SaveChangesAsync();
                }
                else
                {
                    dbCategory = foundDbCategory;
                }

                // Manage relationship between categories and sites
                var dbSiteCatRel = new DbSiteCategory { SiteId = dbSite.Id, CategoryId = dbCategory.Id };
                var foundDbSiteCatRel = _dbContext.DbSiteCategories.FirstOrDefault(sc => sc.SiteId == dbSiteCatRel.SiteId && sc.CategoryId == dbSiteCatRel.CategoryId);

                // If not in DB already, add new site-category relationship
                if (foundDbSiteCatRel == null)
                {
                    _dbContext.DbSiteCategories.Add(dbSiteCatRel);
                    await _dbContext.SaveChangesAsync();
                }

            }

            // Manage relationship between fonts and categories
            var dbCategories = _dbContext.DbCategories.Where(c => siteNewCategories.Contains(c.CategoryName)).ToList();
            var dbFonts = _dbContext.DbFonts.Where(f => siteNewFonts.Contains(f.FontName)).ToList();

            foreach (var category in dbCategories)
            {
                foreach (var font in dbFonts)
                {
                    var dbFontCatRel = new DbFontCategory() { CategoryId = category.Id, FontId = font.Id };
                    var foundDbFontCatRel = _dbContext.DbFontCategories.FirstOrDefault(fc => fc.FontId == dbFontCatRel.FontId && fc.CategoryId == dbFontCatRel.CategoryId);


                    // If not in DB already, add new font-category relationship
                    if (foundDbFontCatRel == null)
                    {
                        _dbContext.DbFontCategories.Add(dbFontCatRel);
                        await _dbContext.SaveChangesAsync();
                    }

                }
            }

            _dbContext.DbSites.AddOrUpdate(dbSite);
            await _dbContext.SaveChangesAsync();
        }

        public DbSite FindDbSiteByUrl(string siteUrl)
        {
            var dbSite = _dbContext.DbSites.FirstOrDefault(s => s.SiteUrl == siteUrl);
            
            return dbSite;
        }

        public List<string> GetAllCategoriesNames()
        {
            var dbCategories = _dbContext.DbCategories.Select(c=>c.CategoryName).ToList();

            return dbCategories;
        }
    }
}