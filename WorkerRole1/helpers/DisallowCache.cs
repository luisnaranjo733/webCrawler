using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using SharedCodeLibrary.models;
using SharedCodeLibrary.models.TableEntities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkerRole1.interfaces;

namespace SharedCodeLibrary.helpers
{
    public class DisallowCache : IDisallowCache
    {
        private CloudTable disallowTable;
        private List<string> disallowList;
        private string Domain;
        public DisallowCache(string domain)
        {
            this.Domain = domain;

            disallowList = new List<string>();
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]
            );
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            disallowTable = tableClient.GetTableReference(DisallowEntity.TABLE_DISALLOW);
            refreshCache();
        }

        public void refreshCache()
        {
            disallowList.Clear();
            TableQuery<DisallowEntity> query = new TableQuery<DisallowEntity>();
            query.Where(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, Domain));

            foreach (DisallowEntity entity in disallowTable.ExecuteQuery(query))
            {
                disallowList.Add(entity.ToString());
            }
        }

        public bool isUrlAllowed(Uri uri)
        {
            if (uri.Segments.Length == 0)
            {
                return false;
            }

            foreach(string segment in uri.Segments)
            {
                if (disallowList.Contains(segment.Replace("/", "")))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
