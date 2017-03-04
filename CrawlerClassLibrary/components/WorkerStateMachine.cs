using CrawlerClassLibrary.models.QueueMessages;
using CrawlerClassLibrary.models.TableEntities;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CrawlerClassLibrary.components
{
    public class WorkerStateMachine : IDisposable
    {
        public const string STATE_IDLE = "idle";
        public const string STATE_LOADING = "loading";
        public const string STATE_CRAWLING = "crawling";

        private WorkerRoleEntity workerRoleInstance;
        private CloudTable workerRoleTable;

        private CloudQueue urlQueue;

        private StatsManager statsManager;
        private int crawlCount = 0;
        private WebLoader webLoader;
        private WebCrawler webCrawler;

        public WorkerStateMachine(string workerID, CloudQueue urlQueue)
        {
            this.urlQueue = urlQueue;
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]
            );
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            workerRoleTable = tableClient.GetTableReference(WorkerRoleEntity.TABLE_WORKER_ROLES);

            TableOperation retrieveOperation = TableOperation.Retrieve<WorkerRoleEntity>(WorkerRoleEntity.TABLE_WORKER_ROLES, workerID);
            TableResult retrievedResult = workerRoleTable.Execute(retrieveOperation);
            if (retrievedResult.Result == null) 
            {
                workerRoleInstance = new WorkerRoleEntity(workerID, STATE_IDLE);
                TableOperation insertOperation = TableOperation.Insert(workerRoleInstance);
                workerRoleTable.Execute(insertOperation);
            } else
            {
                workerRoleInstance = (WorkerRoleEntity)retrievedResult.Result;
                this.setState(workerRoleInstance.State);
            }
            statsManager = new StatsManager();
            webLoader = new WebLoader();
            webCrawler = new WebCrawler();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queueMessage">queueMessage may be null if queue is empty</param>
        public async void Act(CloudQueueMessage queueMessage)
        {
            bool deleteMessage = true;  

            if (queueMessage == null) // if url queue is empty
            {
                if (getState() == STATE_CRAWLING) // and we are in the crawling state, we must have finished crawling
                {
                    setState(WorkerStateMachine.STATE_IDLE); // so go to idle
                }
                return; // exit since there is nothing to act on
            }
            UrlMessage urlMessage = UrlMessage.Parse(queueMessage.AsString);


            if (getState() == STATE_LOADING) // loading code
            {
                statsManager.UpdateStats(); // manual update stats on each url during loading phase only
                if (urlMessage.UrlType == UrlMessage.URL_TYPE_SITEMAP)
                {
                    webLoader.parseSitemap(urlMessage.Url); // crawls xml, adds leaf html urls to queue
                }
                else if (urlMessage.UrlType == UrlMessage.URL_TYPE_HTML)
                {
                    // if we are in the loading state, and we receive a URL_TYPE_HTML message, that means we've finished
                    // loading and can transition to crawling state (since we've finished the sitemap queue messages, FIFO)
                    //setState(WorkerStateMachine.STATE_CRAWLING); // transition to next state
                    setState(WorkerStateMachine.STATE_IDLE);                          
                    deleteMessage = false; // intentionally don't delete queue message, so that it gets processed when the state has been set to crawling
                }
            }
            else if (getState() == STATE_CRAWLING) // crawling state
            {
                crawlCount += 1;
                if (crawlCount % StatsManager.UPDATE_STATS_FREQ == 0)
                {
                    StatsManager.Instance.updateStat(StatsManager.SIZE_OF_QUEUE, StatsManager.Instance.getSizeOfQueue());
                }

                // multi threading with thread pool gets queued up here
                WaitCallback callback = new WaitCallback(webCrawler.Crawl);
                ThreadPool.QueueUserWorkItem(callback, urlMessage);
            }

            if (deleteMessage) { await urlQueue.DeleteMessageAsync(queueMessage); }
        }

        public bool setState(string state)
        {
            if (state == STATE_IDLE)
            {
                workerRoleInstance.State = STATE_IDLE;
            } else if (state == STATE_LOADING)
            {
                workerRoleInstance.State = STATE_LOADING;
            } else if (state == STATE_CRAWLING)
            {
                workerRoleInstance.State = STATE_CRAWLING;
            } else
            {
                return false;
            }
            TableOperation updateOperation = TableOperation.Replace(workerRoleInstance);
            workerRoleTable.Execute(updateOperation);

            // refresh references to components
            statsManager = new StatsManager();
            webLoader = new WebLoader();
            webCrawler = new WebCrawler();
            return true;
        }

        public string getState()
        {
            return this.workerRoleInstance.State;
        }

        public void Dispose()
        {
            TableOperation deleteOperation = TableOperation.Delete(workerRoleInstance);
            workerRoleTable.Execute(deleteOperation);
        }
    }
}
