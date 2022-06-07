using System.Collections.Generic;
using System.Linq;
using Dissertation_Thesis_WebsiteScraper.BLL;

namespace Dissertation_Thesis_WebsiteScraper.Models.ClassifierModels
{
    public class ClassInfo
    {
        public ClassInfo(string name, IReadOnlyCollection<string> trainDocs)
        {
            var features = trainDocs.SelectMany(x => x.ExtractFeatures()).ToList();
            Name = name;
            WordsCount = features.Count();
            WordWithFrequency = features.GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());
            NumberOfDocs = trainDocs.Count;
        }

        public string Name { get; set; }

        public int WordsCount { get; set; }

        public Dictionary<string, int> WordWithFrequency { get; set; }

        public int NumberOfDocs { get; set; }

        public int NumberOfOccurrencesInTrainDocs(string word)
        {
            if (WordWithFrequency.Keys.Contains(word)) 
                return WordWithFrequency[word];
            
            return 0;
        }
    }
}