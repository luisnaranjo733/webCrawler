using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using SharedCode;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerRole1.helpers
{
    class WorkerStateMachine : IDisposable
    {
        public const string STATE_IDLE = "idle";
        public const string STATE_LOADING = "loading";
        public const string STATE_CRAWLING = "crawling";

        private WorkerRoleInstance workerRoleInstance;
        private CloudTable workerRoleTable;
        private int nUrlsCrawled;

        public WorkerStateMachine(string workerID)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]
            );
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            workerRoleTable = tableClient.GetTableReference(WorkerRoleInstance.TABLE_WORKER_ROLES);

            TableOperation retrieveOperation = TableOperation.Retrieve<WorkerRoleInstance>(WorkerRoleInstance.TABLE_WORKER_ROLES, workerID);
            TableResult retrievedResult = workerRoleTable.Execute(retrieveOperation);
            if (retrievedResult.Result == null) 
            {
                workerRoleInstance = new WorkerRoleInstance(workerID, STATE_IDLE);
                TableOperation insertOperation = TableOperation.Insert(workerRoleInstance);
                workerRoleTable.Execute(insertOperation);
            } else
            {
                workerRoleInstance = (WorkerRoleInstance)retrievedResult.Result;
                this.setState(STATE_IDLE);
            }
        }

        public bool Act(UrlEntity urlEntity, WebCrawler webCrawler)
        {
            nUrlsCrawled += 1;

            if (nUrlsCrawled % StatsManager.UPDATE_STATS_FREQ == 0)
            {
                webCrawler.statsManager.UpdateStats();
            }

            if (getState() == STATE_LOADING) // loading code
            {
                webCrawler.statsManager.UpdateStats(); // manual update stats on each url during loading phase only
                if (urlEntity.PartitionKey == UrlEntity.URL_TYPE_SITEMAP)
                {
                    webCrawler.parseSitemap(urlEntity.RowKey); // crawls xml, adds leaf html urls to queue
                    return true;
                }
                else if (urlEntity.PartitionKey == UrlEntity.URL_TYPE_HTML)
                {
                    // if we are in the loading state, and we receive a URL_TYPE_HTML message, that means we've finished
                    // loading and can transition to crawling state (since we've finished the sitemap queue messages, FIFO)
                    setState(WorkerStateMachine.STATE_CRAWLING); // transition to next state
                                                                 // intentionally don't delete queue message, so that it gets processed when the state has been set to crawling
                    return false;
                }
                return false; // should never happen
            }
            else if (getState() == STATE_CRAWLING) // crawling code
            {
                if (urlEntity.PartitionKey == UrlEntity.URL_TYPE_HTML)
                {
                    webCrawler.parseHtml(urlEntity.RowKey);
                }
                return true;
            }
            return false;
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
