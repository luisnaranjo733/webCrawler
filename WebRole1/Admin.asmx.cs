using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;
using Microsoft.WindowsAzure.Storage.Queue;
using SharedCodeLibrary.models;
using WorkerRole1.helpers;

namespace WebRole1
{
    /// <summary>
    /// Summary description for Admin
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class Admin : WebService
    {
        CloudTable disallowTable;
        CloudTable workerTable;
        CloudTable indexTable;
        CloudQueue urlQueue;
        CloudQueue commandQueue;

        StatsManager statsManager;
        public Admin()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]
            );
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            disallowTable = tableClient.GetTableReference(DisallowEntity.TABLE_DISALLOW);
            disallowTable.CreateIfNotExists();

            workerTable = tableClient.GetTableReference(WorkerRoleInstance.TABLE_WORKER_ROLES);
            workerTable.CreateIfNotExists();

            indexTable = tableClient.GetTableReference(IndexEntity.TABLE_INDEX);
            indexTable.CreateIfNotExists();

            urlQueue = queueClient.GetQueueReference(UrlEntity.QUEUE_URL);
            urlQueue.CreateIfNotExists();

            commandQueue = queueClient.GetQueueReference(Command.QUEUE_COMMAND);
            commandQueue.CreateIfNotExists();

            statsManager = new StatsManager();
        }

        [WebMethod]
        public void QueueSitemap(string robotsURL)
        {

            WebRequest request = WebRequest.Create(robotsURL);
            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            } catch(WebException e)
            {
                Logger.Instance.Log(Logger.LOG_ERROR, "QueueSitemap web request failed");
                return;
            }
            
            Stream dataStream = response.GetResponseStream();

            StreamReader reader = new StreamReader(dataStream);
            string data;
            // Read and display lines from the file until the end of 
            // the file is reached.

            //urlQueue.FetchAttributes();
            //if (urlQueue.ApproximateMessageCount < 15) { return; } // don't add root sitemap urls to the queue if the queue is not empty

            while ((data = reader.ReadLine()) != null)
            {
                string directive = "";
                char letter = '$';
                do
                {
                    letter = data[0];
                    directive = directive + letter;
                    data = data.Substring(1);
                } while (letter != ' ');
                directive = directive.Substring(0, directive.Length - 2);

                if (directive == "Sitemap")
                {
                    UrlEntity urlEntity = new UrlEntity(UrlEntity.URL_TYPE_SITEMAP, data);

                    // Add message
                    CloudQueueMessage message = new CloudQueueMessage(urlEntity.ToString());
                    urlQueue.AddMessage(message);

                }
                else if (directive == "Disallow")
                {
                    // add line to disallow table
                    DisallowEntity disallow = new DisallowEntity(data, new Uri(robotsURL).Host);
                    TableOperation insertOperation = TableOperation.InsertOrReplace(disallow);
                    disallowTable.Execute(insertOperation);
                }   
            }
            dataStream.Close();
            response.Close();
        }

        [WebMethod]
        public void StartLoading()
        {
            CloudQueueMessage startMessage = new CloudQueueMessage(Command.COMMAND_LOAD);
            commandQueue.AddMessage(startMessage);
            //statsManager.updateStat(StatsManager., statsManager.getSizeOfQueue());
        }

        [WebMethod]
        public void StartCrawling()
        {
            CloudQueueMessage stopMessage = new CloudQueueMessage(Command.COMMAND_CRAWL);
            commandQueue.AddMessage(stopMessage);
        }


        [WebMethod]
        public void StartIdling()
        {
            CloudQueueMessage stopMessage = new CloudQueueMessage(Command.COMMAND_IDLE);
            commandQueue.AddMessage(stopMessage);
        }

        /* Retrieve page title for a specific url from index
         * Param: url to use in index search
         * Return page title for url param
         */
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string RetrievePageTitle(string url)
        {
            string title = "index not found :(";

            TableQuery<IndexEntity> query = new TableQuery<IndexEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, IndexEntity.Base64Encode(url)));

            IndexEntity index = null;
            foreach (IndexEntity entity in indexTable.ExecuteQuery(query))
            {
                index = entity;
                break;
            }
            if (index != null)
            {
                title = index.Title;
            } 
            return new JavaScriptSerializer().Serialize(title);
        }



        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string RetrieveStats()
        {
            statsManager.updateStat(StatsManager.SIZE_OF_QUEUE, statsManager.getSizeOfQueue());
            //statsManager.updateStat(StatsManager.SIZE_OF_TABLE, statsManager.getSizeOfTable());
            List<string> stats = StatsManager.GetStats();
            return new JavaScriptSerializer().Serialize(stats);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string RetrieveWorkerStatus()
        {
            List<Dictionary<string, string>> collection = new List<Dictionary<string, string>>();
            if (workerTable.Exists())
            {
                TableQuery<WorkerRoleInstance> query = new TableQuery<WorkerRoleInstance>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, WorkerRoleInstance.TABLE_WORKER_ROLES));

                foreach (WorkerRoleInstance entity in workerTable.ExecuteQuery(query))
                {
                    Dictionary<string, string> workerRoleDict = new Dictionary<string, string>();
                    workerRoleDict.Add("id", entity.RowKey);
                    workerRoleDict.Add("state", entity.State);
                    collection.Add(workerRoleDict);
                }
                
            }
            return new JavaScriptSerializer().Serialize(collection);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string GetErrorLog()
        {
            List<string> errorLog = new List<string>();
            
            if (errorLog.Count == 0)
            {
                errorLog.Add("No errors yet :)");
            }

            Logger.Instance.RetrieveEntries();
            return new JavaScriptSerializer().Serialize(errorLog);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string GetRecentUrlsCrawled(int n)
        {
            List<string> results = new List<string>();

            TableQuery<IndexEntity> query = new TableQuery<IndexEntity>();

            int i = 0;
            foreach (IndexEntity entity in indexTable.ExecuteQuery(query))
            {
                if (i == n) { break; }
                i++;
                results.Add(IndexEntity.Base64Decode(entity.PartitionKey));
            }

            if (results.Count == 0)
            {
                results.Add("Index is empty");
            }

            return new JavaScriptSerializer().Serialize(results);
        }


        [WebMethod]
        public void ClearUrlQueue()
        {
            urlQueue.Clear();
        }

        [WebMethod]
        public void DeleteEverything()
        {
            urlQueue.DeleteIfExists();
            commandQueue.DeleteIfExists();
            disallowTable.DeleteIfExists();
            workerTable.DeleteIfExists();
            indexTable.DeleteIfExists();
        }



        [WebMethod]
        public void TestIndex(string url, string title)
        {
            List<string> indexList = new List<string>();
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]
            );
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable indexTable = tableClient.GetTableReference(IndexEntity.TABLE_INDEX);
            indexTable.CreateIfNotExists();

            IndexEntity entity = new IndexEntity(url, title);
            TableOperation insertOperation = TableOperation.InsertOrReplace(entity);
            indexTable.Execute(insertOperation);
        }

        [WebMethod]
        public List<string> TestIndex2()
        {
            List<string> indexList = new List<string>();
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]
            );
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable indexTable = tableClient.GetTableReference(IndexEntity.TABLE_INDEX);
            indexTable.CreateIfNotExists();

            TableQuery<IndexEntity> query = new TableQuery<IndexEntity>();
            foreach (IndexEntity entity in indexTable.ExecuteQuery(query))
            {
                indexList.Add(entity.ToString());
            }
            return indexList;
        }
    }
}
