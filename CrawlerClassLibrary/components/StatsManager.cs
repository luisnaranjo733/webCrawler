using CrawlerClassLibrary.models.QueueMessages;
using CrawlerClassLibrary.models.TableEntities;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace CrawlerClassLibrary.components
{
    public class StatsManager
    {
        public const int UPDATE_STATS_FREQ = 3;
        public const string CPU_UTILIZATION = "cpuutilization";
        public const string RAM_AVAILABLE = "ramavailable";
        public const string N_URLS_CRAWLED = "nurlscrawled";
        public const string SIZE_OF_QUEUE = "sizeofqueue";
        public const string SIZE_OF_TABLE = "sizeoftable";

        private CloudTable statsTable;
        private CloudTable indexTable;
        private CloudQueue urlsQueue;

        private PerformanceCounter cpuCounter;
        private PerformanceCounter ramCounter;

        public StatsManager()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]
            );
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            statsTable = tableClient.GetTableReference(StatEntity.TABLE_STATS);
            statsTable.CreateIfNotExists();
            indexTable = tableClient.GetTableReference(IndexEntity.TABLE_INDEX);
            urlsQueue = queueClient.GetQueueReference(UrlMessage.QUEUE_URL);

            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");
        }

        public void UpdateStats()
        {
            // update cpu utilization %
            updateStat(CPU_UTILIZATION, getCurrentCpuUsage());

            // update RAM available
            updateStat(RAM_AVAILABLE, getAvailableRAM());
            // update n URLS crawled
            updateStat(N_URLS_CRAWLED, getUrlsCrawled());
            // update size of queue
            updateStat(SIZE_OF_QUEUE, getSizeOfQueue());
            // update size of table
            updateStat(SIZE_OF_TABLE, getSizeOfTable());
        }

        public void updateStat(string statName, string statValue)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<StatEntity>(statName, statName);
            TableResult retrievedResult = statsTable.Execute(retrieveOperation);

            if (retrievedResult.Result == null)
            {
                StatEntity statEntity = new StatEntity(statName, statValue);
                TableOperation insertOperation = TableOperation.Insert(statEntity);
                statsTable.Execute(insertOperation);
            }
            else
            {
                StatEntity statEntity = (StatEntity)retrievedResult.Result;
                statEntity.Value = statValue;
                TableOperation updateOperation = TableOperation.Replace(statEntity);
                try
                {
                    statsTable.Execute(updateOperation);
                } catch (StorageException e)
                {
                    Logger.Instance.Log(Logger.LOG_ERROR, "Update stats operation failed: " + statName + " | " + e.ToString());
                    return;
                }
                
            }
        }

        private static string retrieveStat(string statName)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]
            );
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            CloudTable statsTable = tableClient.GetTableReference(StatEntity.TABLE_STATS);
            statsTable.CreateIfNotExists();

            TableOperation retrieveOperation = TableOperation.Retrieve<StatEntity>(statName, statName);
            TableResult retrievedResult = statsTable.Execute(retrieveOperation);

            if (retrievedResult.Result != null)
            {
                StatEntity statEntity = (StatEntity)retrievedResult.Result;
                return statEntity.Value;
            } else
            {
                return "";
            }
        }

        public static List<string> GetStats()
        {
            List<string> stats = new List<string>();
            stats.Add(retrieveStat(CPU_UTILIZATION)); // cpu utilization
            stats.Add(retrieveStat(RAM_AVAILABLE)); // ram available
            stats.Add(retrieveStat(N_URLS_CRAWLED)); // n urls crawled
            stats.Add(retrieveStat(SIZE_OF_QUEUE)); // size of queue
            stats.Add(retrieveStat(SIZE_OF_TABLE)); // size of table
            return stats;
        }


        public string getCurrentCpuUsage()
        {
            return cpuCounter.NextValue() + "%";
        }

        public string getAvailableRAM()
        {
            return ramCounter.NextValue() + "MB";
        }

        private string getUrlsCrawled()
        {
            return "?";
        }

        public string getSizeOfQueue()
        {
            urlsQueue.FetchAttributes();
            int? cachedMessageCount = urlsQueue.ApproximateMessageCount;
            if (cachedMessageCount != null)
            {
                return cachedMessageCount.ToString();
            } else
            {
                return "0";
            }
        }

        public string getSizeOfTable()
        {
            return "?";
        }

    }
}
