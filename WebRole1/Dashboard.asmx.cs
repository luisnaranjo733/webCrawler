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
using CrawlerClassLibrary.components;
using CrawlerClassLibrary.models.TableEntities;
using CrawlerClassLibrary.models.QueueMessages;
using CrawlerClassLibrary.models;
using System.Linq;
using WebRole1.models;

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
        CloudTable recentIndexTable;
        CloudTable logTable;
        CloudQueue urlQueue;
        CloudQueue commandQueue;

        private static StatsManager statsManager = new StatsManager();
        private static SearchResultsCache cache = new SearchResultsCache();

        public Admin()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]
            );
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            disallowTable = tableClient.GetTableReference(DisallowEntity.TABLE_DISALLOW);
            disallowTable.CreateIfNotExists();

            workerTable = tableClient.GetTableReference(WorkerRoleEntity.TABLE_WORKER_ROLES);
            workerTable.CreateIfNotExists();

            indexTable = tableClient.GetTableReference(IndexEntity.TABLE_INDEX);
            indexTable.CreateIfNotExists();

            recentIndexTable = tableClient.GetTableReference(RecentIndexEntity.TABLE_RECENT_INDEX);
            recentIndexTable.CreateIfNotExists();

            logTable = tableClient.GetTableReference(LogEntity.TABLE_LOG);
            logTable.CreateIfNotExists();

            urlQueue = queueClient.GetQueueReference(UrlMessage.QUEUE_URL);
            urlQueue.CreateIfNotExists();

            commandQueue = queueClient.GetQueueReference(CommandMessage.QUEUE_COMMAND);
            commandQueue.CreateIfNotExists();
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
                Logger.Instance.Log(Logger.LOG_ERROR, "QueueSitemap web request failed | " + robotsURL + " " + e.ToString());
                return;
            }
            
            Stream dataStream = response.GetResponseStream();

            StreamReader reader = new StreamReader(dataStream);
            string data;

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
                    UrlMessage urlEntity = new UrlMessage(UrlMessage.URL_TYPE_SITEMAP, data);

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
            StatsManager.Instance.updateStat(StatsManager.SIZE_OF_QUEUE, StatsManager.Instance.getSizeOfQueue());
        }

        [WebMethod]
        public void StartLoading()
        {
            CloudQueueMessage startMessage = new CloudQueueMessage(CommandMessage.COMMAND_LOAD);
            commandQueue.AddMessage(startMessage);
            //statsManager.updateStat(StatsManager., statsManager.getSizeOfQueue());
        }

        [WebMethod]
        public void StartCrawling()
        {
            CloudQueueMessage stopMessage = new CloudQueueMessage(CommandMessage.COMMAND_CRAWL);
            commandQueue.AddMessage(stopMessage);
        }


        [WebMethod]
        public void StartIdling()
        {
            CloudQueueMessage stopMessage = new CloudQueueMessage(CommandMessage.COMMAND_IDLE);
            commandQueue.AddMessage(stopMessage);
        }

        [WebMethod]
        public void ClearCache()
        {
            cache.Clear();
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string RetrieveSearchResults(string searchQuery)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            serializer.MaxJsonLength = Int32.MaxValue;
            if (cache == null)
            {
                cache = new SearchResultsCache();
            }

            IEnumerable<SearchResult> cacheLine = cache.GetLineOrNull(searchQuery);
            if (cacheLine != null)
            {
                try
                {
                    string response = serializer.Serialize(cacheLine);
                    return response;
                } catch (Exception e)
                {
                    Logger.Instance.Log(Logger.LOG_ERROR, "cache retrieval failed: " + e.ToString());
                    return new JavaScriptSerializer().Serialize(false);
                }

            }

            Queue<string> filterConditions = new Queue<string>();
            List<IndexEntity> unrankedSearchResults = new List<IndexEntity>();

            string[] keywords = searchQuery.Split(' ');
            foreach (string keyword in keywords)
            {
                if (keyword == "") { continue; }
                string encodedKeyword = IndexEntity.Base64Encode(keyword.ToLower()); // indexed titles are stored in lowercase, in base64 encoding, 
                string pkFilterCondition = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, encodedKeyword);
                filterConditions.Enqueue(pkFilterCondition);
            }

            if (filterConditions.Count > 1)
            {
                for (int i = 0; i < filterConditions.Count - 1; i++)
                {
                    string currentFilterCondition = filterConditions.Dequeue();
                    string nextFilterCondition = filterConditions.Dequeue();

                    string combinedFilterCondition = TableQuery.CombineFilters(currentFilterCondition, TableOperators.Or, nextFilterCondition);
                    filterConditions.Enqueue(combinedFilterCondition);
                }
            } else if (filterConditions.Count == 0)
            {
                return new JavaScriptSerializer().Serialize(new List<SearchResult>()); // return empty list because no search query was provided
            }

            // at this point, queue should always be of size 1 for any n keywords provided by the user
            string combinedFilterConditions = filterConditions.Dequeue();
            var indexQuery = new TableQuery<IndexEntity>()
                .Where(combinedFilterConditions);

            foreach(IndexEntity entity in indexTable.ExecuteQuery(indexQuery))
            {
                unrankedSearchResults.Add(entity);
            }

            IEnumerable<SearchResult> rankedSearchResults;
            try
            {
                rankedSearchResults = unrankedSearchResults
                    .GroupBy(indexEntity => indexEntity.GetUrl())
                    .Select(grouping => new Tuple<string, int, string, DateTime>(
                        grouping.Key,
                        grouping.ToList().Count,
                        grouping.Select(g => g.GetTitle()).FirstOrDefault(),
                        grouping.Select(g => g.GetDate()).FirstOrDefault()
                        ))
                        .OrderByDescending(g => g.Item2)
                        .ThenBy(g => g.Item4)
                        .Select(x => new SearchResult(x.Item3, x.Item1, x.Item4)).Take(50);
            } catch (Exception e)
            {
                Logger.Instance.Log(Logger.LOG_WARNING, "LINQ result ranking failed: " + e.ToString());
                return new JavaScriptSerializer().Serialize(false);
            }

            try
            {
                cache.Store(searchQuery, rankedSearchResults);
            } catch (Exception e)
            {
                Logger.Instance.Log(Logger.LOG_ERROR, "cache storage failed: " + e.ToString());
                return new JavaScriptSerializer().Serialize(false);
            }

            string results = serializer.Serialize(rankedSearchResults);
            return results;
        }

        [WebMethod]
        public void UpdateStats()
        {
            StatsManager.Instance.UpdateStats();
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string RetrieveStats()
        {
            List<string> stats = StatsManager.Instance.GetStats();
            return new JavaScriptSerializer().Serialize(stats);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string RetrieveWorkerStatus()
        {
            List<Dictionary<string, string>> collection = new List<Dictionary<string, string>>();
            if (workerTable.Exists())
            {
                TableQuery<WorkerRoleEntity> query = new TableQuery<WorkerRoleEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, WorkerRoleEntity.TABLE_WORKER_ROLES));

                foreach (WorkerRoleEntity entity in workerTable.ExecuteQuery(query))
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
            List<string> errorLog = Logger.Instance.RetrieveEntries();

            if (errorLog.Count == 0)
            {
                errorLog.Add("No errors yet :)");
            }

            return new JavaScriptSerializer().Serialize(errorLog);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string GetRecentUrlsCrawled(int n)
        {
            List<string> results = new List<string>();

            TableQuery<RecentIndexEntity> query = new TableQuery<RecentIndexEntity>();

            int i = 0;
            foreach (RecentIndexEntity entity in recentIndexTable.ExecuteQuery(query))
            {
                if (i == n) { break; }
                i++;
                results.Add(entity.GetUrl());
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
            StatsManager.Instance.updateStat(StatsManager.SIZE_OF_QUEUE, StatsManager.Instance.getSizeOfQueue());
        }

        [WebMethod]
        public void DeleteEverything()
        {
            urlQueue.Clear();
            commandQueue.Clear();
            StatsManager.Instance.ResetStats();
            indexTable.DeleteIfExists();
            recentIndexTable.DeleteIfExists();
            logTable.DeleteIfExists();
        }

        [WebMethod]
        public List<string> ShowIndex()
        {
            List<string> indexList = new List<string>();

            TableQuery<IndexEntity> query = new TableQuery<IndexEntity>();
            foreach (IndexEntity entity in indexTable.ExecuteQuery(query))
            {
                indexList.Add(entity.ToString());
            }
            return indexList;
        }

        [WebMethod]
        public List<string> ShowRecentIndex()
        {
            List<string> indexList = new List<string>();

            TableQuery<RecentIndexEntity> query = new TableQuery<RecentIndexEntity>();
            foreach (RecentIndexEntity entity in recentIndexTable.ExecuteQuery(query))
            {
                indexList.Add(entity.ToString());
            }
            return indexList;
        }
    }
}
