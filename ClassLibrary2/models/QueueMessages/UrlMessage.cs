using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace SharedCodeLibrary.models.QueueMessages
{
    public class UrlMessage
    {
        public const string QUEUE_URL = "urlsqueue";

        public const string URL_TYPE_SITEMAP = "sitemap";
        public const string URL_TYPE_HTML = "html";

        private const char DELIMITER = ',';

        public string UrlType { get; set; }
        public string Url { get; set; }
        public UrlMessage(string urlType, string url)
        {
            UrlType = urlType;
            Url = url;
        }

        public override string ToString()
        {
            return this.UrlType + DELIMITER + this.Url;
        }

        public static UrlMessage Parse(string toString)
        {
            List<string> data = toString.Split(DELIMITER).ToList<string>();
            return new UrlMessage(data[0], data[1]);
        }
    }
}
