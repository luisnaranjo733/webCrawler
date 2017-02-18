using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace SharedCode
{
    public class UrlEntity : TableEntity
    {
        public const string TABLE_URL = "urlstable";
        public const string QUEUE_URL = "urlsqueue";

        public const string URL_TYPE_SITEMAP = "sitemap";
        public const string URL_TYPE_HTML = "html";

        private const char DELIMITER = ',';

        public UrlEntity() { }
        public UrlEntity(string urlType, string url)
        {
            this.PartitionKey = urlType;
            this.RowKey = url;
        }

        public override string ToString()
        {
            return this.PartitionKey + DELIMITER + this.RowKey;
        }

        public static UrlEntity Parse(string toString)
        {
            List<string> data = toString.Split(DELIMITER).ToList<string>();
            return new UrlEntity(data[0], data[1]);
        }
    }
}
