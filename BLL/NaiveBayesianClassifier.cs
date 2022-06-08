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
            var dictSiteCategories = new Dictionary<string, List<string>>();
            var totalScore = (double)0;

            // Get a dictionary of (actual) sites and their categories list
            foreach (var (siteText, category) in siteTextAndCategory)
            {
                // If already in dictionary, create a list or update value
                if (dictSiteCategories.ContainsKey(siteText))
                {
                    var cat = dictSiteCategories[siteText];
                    
                    if (cat == null || cat.Count == 0)
                    {
                        cat = new List<string>();
                    }

                    if (cat.Contains(category))
                    {
                        continue;
                    }   
                    
                    cat.Add(category);
                    dictSiteCategories[siteText] = cat;
                }
                // Add it to the dictionary
                else
                {
                    dictSiteCategories.Add(siteText, new List<string> { category });
                }

                var ordList = dictSiteCategories[siteText].OrderBy(sc => sc).ToList();
                dictSiteCategories[siteText] = ordList;
            }

            // Calculate the probability of the sites to be in a certain class, then take the classes with the highest probability
            foreach (var site in sites)
            {
                var probabilities = new List<Tuple<string, double>>();

                foreach (var category in categories)
                {
                    var probability = IsInClassProbability(category.CategoryName, site.SiteText);
                    probabilities.Add(new Tuple<string, double>(category.CategoryName, probability));
                }

                var allowedNr = dictSiteCategories[site.SiteText].Count;
                var collectedPoints = 0;

                var predicted = probabilities.OrderByDescending(p => p.Item2)
                    .Take(allowedNr)
                    .OrderBy(p=>p.Item1)
                    .Select(p => p.Item1)
                    .ToList();

                var actualData = dictSiteCategories[site.SiteText].ToArray();
                var predictedData = predicted.ToArray();

                for (var i= 0; i < allowedNr; i++)
                {
                    for (var j = 0; j < allowedNr; j++)
                    {
                        if (actualData[i] == predictedData[j])
                        {
                            collectedPoints++;
                        }
                    }
                }

                var score = (double)collectedPoints / allowedNr;
                totalScore += score;
            }

            var accuracy = (totalScore / sites.Count) * 100;
           
            return (int)accuracy;
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