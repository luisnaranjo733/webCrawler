using CrawlerClassLibrary.models.interfaces;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrawlerClassLibrary.models.TableEntities
{
    public class IndexEntity : TableEntity, IIndexEntity
    {
        public const string TABLE_INDEX = "pageindex";

        public string Title { get; set; }
        public DateTime Date { get; set; }

        public IndexEntity() { }
        public IndexEntity(string url, string title, string keyword, DateTime date)
        {
            PartitionKey = Base64Encode(keyword);
            RowKey = Base64Encode(url);
            Title = Base64Encode(title);
            Date = date;
        }

        public string GetTitle() { return Base64Decode(Title); }

        public string GetKeyword()
        {
            return Base64Decode(PartitionKey);
        }

        public string GetUrl()
        {
            return Base64Decode(RowKey);
        }

        public DateTime GetDate()
        {
            return Date;
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }

        public override string ToString()
        {
            return GetKeyword() + " | " + GetUrl();
        }
    }
}
