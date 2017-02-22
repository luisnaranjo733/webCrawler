using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedCodeLibrary.models
{
    public class DisallowEntity : TableEntity
    {
        public const string TABLE_DISALLOW = "disallow";

        public DisallowEntity() { }
        public DisallowEntity(string path, string website)
        {
            this.PartitionKey = website;
            this.RowKey = Base64Encode(path);
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
            return Base64Decode(RowKey);
        }
    }
}
