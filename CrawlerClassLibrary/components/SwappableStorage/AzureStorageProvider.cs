using CrawlerClassLibrary.models.TableEntities;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrawlerClassLibrary.components.SwappableStorage
{
    public sealed class AzureStorageProvider : IStorageProvider
    {
        private CloudTable indexTable;
        private CloudTable recentIndexTable;

        public AzureStorageProvider()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]
            );
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            indexTable = tableClient.GetTableReference(IndexEntity.TABLE_INDEX);
            recentIndexTable = tableClient.GetTableReference(RecentIndexEntity.TABLE_RECENT_INDEX);
            recentIndexTable.CreateIfNotExists();

        }

        public async Task indexPage(string url, string title, DateTime date)
        {
            title = title.ToLower();
            RecentIndexEntity recentIndexEntity = new RecentIndexEntity(url, title, date);
            TableOperation insertOperation = TableOperation.InsertOrReplace(recentIndexEntity);
            try
            {
                await recentIndexTable.ExecuteAsync(insertOperation);
            }
            catch (StorageException e)
            {
                Logger.Instance.Log(Logger.LOG_ERROR, "Insert to recent index failed: " + recentIndexEntity.ToString() + " | " + e.ToString());
                return;
            }


            string[] keywords = title.Split(' ');
            foreach (string keyword in keywords)
            {
                if (!keyword.All(char.IsLetterOrDigit))
                {
                    continue; // skip keywords with non alphanumberic chars (not valid in azure tables)
                }
                IndexEntity indexEntity = new IndexEntity(url, title, keyword, date);
                insertOperation = TableOperation.InsertOrReplace(indexEntity);
                try
                {
                    await indexTable.ExecuteAsync(insertOperation);
                }
                catch (StorageException e)
                {
                    Logger.Instance.Log(Logger.LOG_ERROR, "Insert to index failed: " + indexEntity.ToString() + " | " + e.ToString());
                    return;
                }
            }
        }
    }
}
