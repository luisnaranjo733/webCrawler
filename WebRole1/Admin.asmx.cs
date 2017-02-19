using SharedCode;
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

        [WebMethod]
        public void StartCrawling(string robotsURL)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]
            );
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            CloudTable disallowTable = tableClient.GetTableReference(DisallowEntity.TABLE_DISALLOW);
            disallowTable.CreateIfNotExists();

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue urlQueue = queueClient.GetQueueReference(UrlEntity.QUEUE_URL);
            urlQueue.CreateIfNotExists();

            // send start command so that workers transition to "loading"
            CloudQueue commandQueue = queueClient.GetQueueReference(Command.QUEUE_COMMAND);
            commandQueue.CreateIfNotExists();

            WebRequest request = WebRequest.Create(robotsURL);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream dataStream = response.GetResponseStream();

            StreamReader reader = new StreamReader(dataStream);
            string data;
            // Read and display lines from the file until the end of 
            // the file is reached.

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

            CloudQueueMessage startMessage = new CloudQueueMessage(Command.COMMAND_START);
            commandQueue.AddMessage(startMessage);
        }


        /* Retrieve page title for a specific url from index
         * Param: url to use in index search
         * Return page title for url param
         */
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string RetrievePageTitle(string url)
        {
            string title = "test title";
            return new JavaScriptSerializer().Serialize(title);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string RetrieveStats()
        {
            List<string> stats = StatsManager.GetStats();
            return new JavaScriptSerializer().Serialize(stats);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string RetrieveWorkerStatus()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]
            );

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            
            CloudTable table = tableClient.GetTableReference(WorkerRoleInstance.TABLE_WORKER_ROLES);

            List<Dictionary<string, string>> collection = new List<Dictionary<string, string>>();
            if (table.Exists())
            {
                TableQuery<WorkerRoleInstance> query = new TableQuery<WorkerRoleInstance>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, WorkerRoleInstance.TABLE_WORKER_ROLES));

                foreach (WorkerRoleInstance entity in table.ExecuteQuery(query))
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
        public void StopWorkers()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]
            );
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue commandQueue = queueClient.GetQueueReference(Command.QUEUE_COMMAND);
 


            CloudQueueMessage stopMessage = new CloudQueueMessage(Command.COMMAND_STOP);
            commandQueue.AddMessage(stopMessage);
        }

        [WebMethod]
        public void ClearEverything()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]
            );
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            CloudQueue urlQueue = queueClient.GetQueueReference(UrlEntity.QUEUE_URL);
            CloudQueue commandQueue = queueClient.GetQueueReference(Command.QUEUE_COMMAND);
            CloudTable disallowTable = tableClient.GetTableReference(DisallowEntity.TABLE_DISALLOW);
            CloudTable workerRoleTable = tableClient.GetTableReference(WorkerRoleInstance.TABLE_WORKER_ROLES);
            CloudTable urlEntityTable = tableClient.GetTableReference(UrlEntity.TABLE_URL);


            CloudQueueMessage stopMessage = new CloudQueueMessage(Command.COMMAND_STOP);
            commandQueue.AddMessage(stopMessage);

            urlQueue.DeleteIfExists();
            commandQueue.DeleteIfExists();
            disallowTable.DeleteIfExists();
            //workerRoleTable.DeleteIfExists();
            urlEntityTable.DeleteIfExists();
            
        }
    }
}
