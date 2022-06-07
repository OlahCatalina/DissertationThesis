using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dissertation_Thesis_WebsiteScraper.Models.ClassifierModels;

namespace Dissertation_Thesis_WebsiteScraper.BLL
{
    public class Classifier
    {
        private readonly List<ClassInfo> _classes;
        private readonly int _numberOfDocuments;
        private readonly int _numberOfUniqueWords;
        private readonly int _numberOfTotalWords;

        public Classifier(IReadOnlyCollection<Document> train)
        {
            _classes = train
                .GroupBy(x => x.Class)
                .Select(g => new ClassInfo(g.Key, g.Select(x => x.Text).ToList()))
                .ToList();
            _numberOfDocuments = train.Count;
            _numberOfUniqueWords = train.SelectMany(x => x.Text.Split(' ')).GroupBy(x => x).Count();
            _numberOfTotalWords = train.SelectMany(x => x.Text.Split(' ')).Count();
        }

        public int GetNumberOfDocuments()
        {
            return _numberOfDocuments;
        }

        public int GetNumberOfUniqueWords()
        {
            return _numberOfUniqueWords;
        }

        public int GetNumberOfAllWords()
        {
            return _numberOfTotalWords;
        }

        public int GetNumberOfClasses()
        {
            return _classes.Count;
        }

        public int GetAccuracy(List<Models.DatabaseModels.DbSite> sites, List<Models.DatabaseModels.DbCategory> categories, List<Tuple<string, string>> siteTextAndCategory)
        {
            var pointsForAlgorithm = 0;
            var totalPoints = 0;
            var ce = new List<Tuple<double, bool>>();

            foreach (var site in sites)
            {
                foreach (var category in categories)
                {
                    totalPoints += 1;

                    var probability = IsInClassProbability(category.CategoryName, site.SiteText);
                    var isSiteActuallyInCategory = siteTextAndCategory
                        .FirstOrDefault(sc => sc.Item1 == site.SiteText && sc.Item2 == category.CategoryName) != null;
                   
                    ce.Add(new Tuple<double, bool>(probability, isSiteActuallyInCategory));

                    //if (isSiteActuallyInCategory)
                    //{
                    //    // Site in category, so the probability should be high
                    //    if (probability >= 0.1)
                    //    {
                    //        // It's a guess
                    //        pointsForAlgorithm += 1;
                    //    }
                    //    else
                    //    {
                    //        // Not a guess
                    //    }
                    //}
                    //else
                    //{
                    //    // Site NOT in category, so the probability should be low
                    //    if (probability <= 0.1)
                    //    {
                    //        // It's a guess
                    //        pointsForAlgorithm += 1;
                    //    }
                    //    else
                    //    {
                    //        // Not a guess
                    //    }
                    //}
                }

            }

            var averageYes = ce.Where(s => s.Item2).Select(s => s.Item1).Average();
            var averageNope = ce.Where(s => s.Item2 == false).Select(s => s.Item1).Average();

            var threshold = (double)1/categories.Count; // ~0.66

            foreach (var c in ce)
            {
                if (c.Item2)
                {
                    // If it was in category
                    if (averageYes - threshold <= c.Item1 && averageYes + threshold >= c.Item1)
                    {
                        pointsForAlgorithm += 1;
                    }
                }
                else
                {
                    // If it was NOT in category
                    if (averageNope - threshold <= c.Item1 && averageNope + threshold >= c.Item1)
                    {
                        pointsForAlgorithm += 1;
                    }
                }
            }
           

            if (totalPoints != 0)
            {
                var accuracy = ((double)pointsForAlgorithm / totalPoints) * 100;
                var acc = Convert.ToInt32(accuracy);
                return acc;
            }

            return 0;
        }

        public double IsInClassProbability(string className, string text)
        {
            var words = text.ExtractFeatures();
            var classResults = _classes
                .Select(x => new
                {
                    Result = CalculateResult(x.NumberOfDocs, _numberOfDocuments, words, x.WordsCount, x, _numberOfUniqueWords),
                    ClassName = x.Name
                })
                .ToList();

            var resPerCurrentClass = classResults.Single(x => x.ClassName == className).Result;
            var resPerAllClasses = classResults.Sum(x => x.Result);
            var finalResult = resPerCurrentClass / resPerAllClasses;

            return finalResult;
        }

        private static double CalculateResult(double classNumberOfDocs, double allDocsCount, IEnumerable<string> testTextWords, double classWordsCount, ClassInfo @class, double uniqueWordsCount)
        {
            var words = testTextWords.ToList();
            double sum = 0;

            foreach (var x in words)
            {
                var numberOfOccurrencesInTrainDocs = @class.NumberOfOccurrencesInTrainDocs(x);
                var log = Math.Log(numberOfOccurrencesInTrainDocs + 1);
                sum += log;
            }

            sum /= (uniqueWordsCount + classWordsCount);

            var result = Math.Log(classNumberOfDocs / allDocsCount) + sum;
            result = Math.Pow(Math.E, result);

            return result;
        }

    }
    
    public static class Helpers
    {
        public static List<string> ExtractFeatures(this string text)
        {
            text = Regex.Replace(text, "[^a-zA-Z0-9_]+", " ");

            var listOfWords = Regex
                .Replace(text, "\\p{P}+", "")
                .Split(' ')
                .Where(t => !string.IsNullOrEmpty(t))
                .ToList();

           return listOfWords;
        }
    }

}