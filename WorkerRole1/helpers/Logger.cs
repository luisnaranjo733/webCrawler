using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using SharedCodeLibrary.models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerRole1.helpers
{
    public sealed class Logger
    {
        public static string LOG_ERROR = "error";
        public static string LOG_WARNING = "warning";
        public static string LOG_INFO = "info";

        private static Logger instance = null;
        private static readonly object padlock = new object();

        private CloudTable logTable;

        Logger() {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]
            );
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            logTable = tableClient.GetTableReference(LogEntity.TABLE_LOG);
            logTable.CreateIfNotExists();
        }

        public void Log(string type, string message)
        {
            LogEntity logEntity = new LogEntity(type, message);
            TableOperation insertOperation = TableOperation.Insert(logEntity);
            logTable.Execute(insertOperation);
        }

        public List<string> RetrieveEntries(string type="", int n=10)
        {
            List<string> results = new List<string>();

            TableQuery<LogEntity> query = new TableQuery<LogEntity>();
            if (type.Length > 0)
            {
                query = query.Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, type));
            }
            int i = 0;
            foreach (LogEntity entity in logTable.ExecuteQuery(query))
            {
                if (i == n) { break; }
                i++;
                results.Add(entity.ToString());
            }
            return results;
        }

        public static Logger Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new Logger();
                    }
                    return instance;
                }
            }
        }
    }
}
