﻿namespace Dissertation_Thesis_WebsiteScraper.Models.ClassifierModels
{
    public class Document
    {
        public Document(string @class, string text)
        {
            Class = @class;
            Text = text;
        }

        public string Class { get; set; }

        public string Text { get; set; }

    }
}