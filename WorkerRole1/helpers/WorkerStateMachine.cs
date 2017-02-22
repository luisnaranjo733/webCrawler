using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using SharedCodeLibrary.models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkerRole1.interfaces;

namespace WorkerRole1.helpers
{
    class WorkerStateMachine : IDisposable, IWorkerStateMachine
    {
        public const string STATE_IDLE = "idle";
        public const string STATE_LOADING = "loading";
        public const string STATE_CRAWLING = "crawling";

        private WorkerRoleInstance workerRoleInstance;
        private CloudTable workerRoleTable;
        private int nUrlsCrawled;
        private StatsManager statsManager;

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
            statsManager = new StatsManager();
        }

        public bool Act(UrlEntity urlEntity, IWebLoader webLoader, IWebCrawler webCrawler)
        {
            nUrlsCrawled += 1;

            if (getState() == STATE_LOADING) // loading code
            {
                statsManager.UpdateStats(); // manual update stats on each url during loading phase only
                if (urlEntity.UrlType == UrlEntity.URL_TYPE_SITEMAP)
                {
                    webLoader.parseSitemap(urlEntity.Url); // crawls xml, adds leaf html urls to queue
                }
                else if (urlEntity.UrlType == UrlEntity.URL_TYPE_HTML)
                {
                    // if we are in the loading state, and we receive a URL_TYPE_HTML message, that means we've finished
                    // loading and can transition to crawling state (since we've finished the sitemap queue messages, FIFO)
                    //setState(WorkerStateMachine.STATE_CRAWLING); // transition to next state
                    setState(WorkerStateMachine.STATE_IDLE);
                                                                 // intentionally don't delete queue message, so that it gets processed when the state has been set to crawling
                    return false;
                }
            }
            else if (getState() == STATE_CRAWLING) // crawling code
            {
                if (nUrlsCrawled % StatsManager.UPDATE_STATS_FREQ == 0)
                {
                    statsManager.UpdateStats(); // can be null
                }

                webCrawler.Crawl(urlEntity.Url);
            }
            return true;
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
