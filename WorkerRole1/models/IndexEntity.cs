using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedCodeLibrary.models
{
    public class IndexEntity : TableEntity
    {
        public const string TABLE_INDEX = "pageindex";

        public string Title { get; set; }
        //public DateTime Date { get; set; }

        public IndexEntity() { }
        public IndexEntity(string url, string title)
        {
            PartitionKey = Base64Encode(url);
            RowKey = WindowsAzure.ChronoTableStorage.RowKey.CreateReverseChronological(DateTime.UtcNow);
            Title = title;
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
            return "" + Base64Decode(PartitionKey) + "," + Title;
        }
    }
}
