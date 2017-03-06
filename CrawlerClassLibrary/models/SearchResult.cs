using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrawlerClassLibrary.models
{
    public class SearchResult
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public DateTime Date { get; set; }
        public SearchResult(string title, string url, DateTime date)
        {
            Title = title;
            Url = url;
            Date = date;
        }
    }
}
