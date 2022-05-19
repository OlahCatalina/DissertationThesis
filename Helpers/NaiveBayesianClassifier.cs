using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Dissertation_Thesis_SitesTextCrawler.Helpers
{
    public class Document
    {
        public Document(string @class, List<string> fonts, string text)
        {
            Class = @class;
            Fonts = fonts;
            Text = text;
        }

        public string Class { get; set; }
        public string Text { get; set; }
        public List<string> Fonts { get; set; }

    }

    public static class Helpers
    {
        public static List<string> ExtractFeatures(this string text)
        {
           return Regex
                .Replace(text, "\\p{P}+", "")
                .Split(' ')
                .Where(t => !string.IsNullOrEmpty(t))
                .ToList();
        }
    }

    public class ClassInfo
    {
        public ClassInfo(string name, List<string> trainDocs)
        {
            Name = name;
            var features = trainDocs.SelectMany(x => x.ExtractFeatures());
            WordsCount = features.Count();
            WordCount =
                features.GroupBy(x => x)
                    .ToDictionary(x => x.Key, x => x.Count());
            NumberOfDocs = trainDocs.Count;
        }

        public string Name { get; set; }
        public int WordsCount { get; set; }
        public Dictionary<string, int> WordCount { get; set; }
        public int NumberOfDocs { get; set; }

        public int NumberOfOccurrencesInTrainDocs(string word)
        {
            if (WordCount.Keys.Contains(word)) return WordCount[word];
            return 0;
        }
    }

    public class Classifier
    {
        private readonly List<ClassInfo> _classes;
        private readonly int _countOfDocs;
        private readonly int _uniqWordsCount;

        public Classifier(IReadOnlyCollection<Document> train)
        {
            _classes = train.GroupBy(x => x.Class).Select(g => new ClassInfo(g.Key, g.Select(x => x.Text).ToList()))
                .ToList();
            _countOfDocs = train.Count;
            _uniqWordsCount = train.SelectMany(x => x.Text.Split(' ')).GroupBy(x => x).Count();
        }

        public double IsInClassProbability(string className, string text)
        {
            var words = text.ExtractFeatures();
            var classResults = _classes
                .Select(x => new
                {
                    Result = CalculateResult(x.NumberOfDocs, _countOfDocs, words, x.WordsCount, x, _uniqWordsCount),
                    ClassName = x.Name
                });


            var list = classResults.ToList();
            var result=  list.Single(x => x.ClassName == className).Result / list.Sum(x => x.Result);
           
            return result;
        }

        private static double CalculateResult(double classNumberOfDocs, double allDocsCount, IEnumerable<string> testTextWords, double classWordsCount, ClassInfo @class, double uniqueWordsCount)
        {
            var resCalc = Calc(classNumberOfDocs, allDocsCount, testTextWords, classWordsCount, @class, uniqueWordsCount);
            var result = Math.Pow(Math.E, resCalc);
           
            return result;
        }

        private static double Calc(double classNumberOfDocs, double allDocsCount, IEnumerable<string> testTextWords, double classWordsCount, ClassInfo @class, double uniqueWordsCount)
        {
            var sum = testTextWords.Sum(x => Math.Log( (@class.NumberOfOccurrencesInTrainDocs(x) + 1) / (uniqueWordsCount + classWordsCount)) );
            var result = Math.Log(classNumberOfDocs / allDocsCount) + sum;

            return result;
        }
    }
}