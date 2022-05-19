using System;
using Dissertation_Thesis_SitesTextCrawler.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dissertation_Thesis_SitesTextCrawler.Helpers
{
    public class FileHelper
    {
        public const string PATH_TO_FILES = "Content\\files\\";
        public const string SITES_FILE_NAME = "sites.txt";
        public const string CORPUS_FILE_NAME = "corpus.txt";
        public const string RESULTS_FILE_NAME = "results.txt";

        public static List<string> ReadFileLines(string pathToFile)
        {
            var lines = File.ReadLines(pathToFile).ToList();
            return lines;
        }

        public static void WriteSitesInFile(List<Site> sites, string pathToFile)
        {
            var lines = sites.Select(site => site.ToString()).ToList();
            WriteLinesInFile(lines, pathToFile);
        }

        public static void WriteDocumentsInCorpusFile(List<Document> documents, string pathToFile)
        {
            var lines = new List<string>();
            foreach (var doc in documents)
            {
                var line = doc.Class + " ||| " + string.Join(" [x][x] ", doc.Fonts) + " ||| " + doc.Text;
                lines.Add(line);
            }

            WriteLinesInFile(lines, pathToFile);
        }

        private static void WriteLinesInFile(IEnumerable<string> lines, string pathToFile)
        {
            File.WriteAllLines(pathToFile, lines);
        }
    }
}