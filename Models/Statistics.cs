using System;
using System.Collections.Generic;

namespace Dissertation_Thesis_WebsiteScraper.Models
{
    public class Statistics
    {
        public int ClassifierAccuracy { get; set; }

        public int ClassifierTotalNumberOfWords { get; set; }

        public int ClassifierTotalNumberOfUniqueWords { get; set; }

        public int ClassifierTotalNumberOfSiteCategoryPairs { get; set; }

        public int ClassifierTotalNumberOfClasses { get; set; }
        
        public int TotalNumberOfSites { get; set; }

        public int TotalNumberOfCategories { get; set; }

        public List<string> Categories { get; set; }

        public int TotalNumberOfFonts { get; set; }

        public List<string> Fonts{ get; set; }

        public List<Tuple<string, int>> FontFrequency { get; set; }

        public List<Tuple<string, int>> CategoryFrequency { get; set; }

        public List<Tuple<string, string, int>> FontPerCategoryFrequency { get; set; }

    }
}