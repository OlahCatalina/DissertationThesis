using Dissertation_Thesis_SitesTextCrawler.Models.ClassifierModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Dissertation_Thesis_SitesTextCrawler.BLL
{
    public class Classifier
    {
        private readonly List<ClassInfo> _classes;
        private readonly int _countOfDocs;
        private readonly int _uniqWordsCount;
        private readonly int _allWordsCount;

        public Classifier(IReadOnlyCollection<Document> train)
        {
            _classes = train
                .GroupBy(x => x.Class)
                .Select(g => new ClassInfo(g.Key, g.Select(x => x.Text).ToList()))
                .ToList();
            _countOfDocs = train.Count;
            _uniqWordsCount = train.SelectMany(x => x.Text.Split(' ')).GroupBy(x => x).Count();
            _allWordsCount = train.SelectMany(x => x.Text.Split(' ')).Count();
        }

        public int GetNumberOfDocuments()
        {
            return _countOfDocs;
        }

        public int GetNumberOfUniqueWords()
        {
            return _uniqWordsCount;
        }

        public int GetNumberOfAllWords()
        {
            return _allWordsCount;
        }

        public int GetNumberOfClasses()
        {
            return _classes.Count;
        }

        public int GetAccuracy()
        {
            return 100;
        }

        public double IsInClassProbability(string className, string text)
        {
            var words = text.ExtractFeatures();
            var classResults = _classes
                .Select(x => new
                {
                    Result = CalculateResult(x.NumberOfDocs, _countOfDocs, words, x.WordsCount, x, _uniqWordsCount),
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
            var chars = new [] { ",", ".", "/", "!", "@", "#", "$", "%", "^", "&", "*", "'", "\"", ";", "_", "(", ")", ":", "|", "[", "]" };
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