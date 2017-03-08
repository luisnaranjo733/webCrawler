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
    public class RecentIndexEntity : TableEntity, IIndexEntity
    {
        public const string TABLE_RECENT_INDEX = "recentpageindex";

        public DateTime Date { get; set; }
        public string Title { get; set; }

        public RecentIndexEntity() { }
        public RecentIndexEntity(string url, string title, DateTime date)
        {
            PartitionKey = Base64Encode(url);
            RowKey = WindowsAzure.ChronoTableStorage.RowKey.CreateReverseChronological(DateTime.UtcNow); // external nuget package for generating tick based rowkeys for (ab)using azure table storage lexicographical ordering of entities
            Title = Base64Encode(title);
            Date = date;
        }

        public string GetUrl()
        {
            return Base64Decode(PartitionKey);
        }


        public string GetTitle()
        {
            return Base64Decode(Title);
        }


        public DateTime GetDate()
        {
            return Date;
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        public override string ToString()
        {
            return GetTitle() + " | " + GetUrl();
        }
    }
}
