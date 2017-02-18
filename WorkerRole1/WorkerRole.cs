using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using System.Configuration;
using Microsoft.WindowsAzure.Storage.Table;
using SharedCode;
using Microsoft.WindowsAzure.Storage.Queue;
using WorkerRole1.helpers;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        private WorkerStateMachine workerStateMachine;

        public override void Run()
        {
            Trace.TraceInformation("WorkerRole1 is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at https://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("WorkerRole1 has been started");
            int instanceID;
            string instanceId = RoleEnvironment.CurrentRoleInstance.Id;
            if (int.TryParse(instanceId.Substring(instanceId.LastIndexOf(".") + 1), out instanceID)) // On cloud.
            {
                int.TryParse(instanceId.Substring(instanceId.LastIndexOf("_") + 1), out instanceID); // On compute emulator.
            }

            workerStateMachine = new WorkerStateMachine(instanceID.ToString());
            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole1 is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            workerStateMachine.Dispose();

            base.OnStop();

            Trace.TraceInformation("WorkerRole1 has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]
            );
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            //CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            CloudQueue commandQueue = queueClient.GetQueueReference(Command.QUEUE_COMMAND);
            commandQueue.CreateIfNotExists();

            CloudQueue urlQueue = queueClient.GetQueueReference(UrlEntity.QUEUE_URL);
            commandQueue.CreateIfNotExists();

            //CloudTable workerRoleTable = tableClient.GetTableReference(WorkerRoleInstance.TABLE_WORKER_ROLES);


            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                //Trace.TraceInformation("Working");


                CloudQueueMessage commandMessage = commandQueue.GetMessage(TimeSpan.FromMinutes(5));
                if (commandMessage != null)
                {
                    if (commandMessage.AsString == Command.COMMAND_START)
                    {
                        workerStateMachine.setState(WorkerStateMachine.STATE_LOADING);
                    } else if (commandMessage.AsString == Command.COMMAND_STOP)
                    {
                        workerStateMachine.setState(WorkerStateMachine.STATE_IDLE);
                    }
                    commandQueue.DeleteMessage(commandMessage);
                }

                if (workerStateMachine.getState() != WorkerStateMachine.STATE_IDLE)
                {
                    CloudQueueMessage urlMessage = urlQueue.GetMessage();
                    if (urlMessage != null)
                    {
                        UrlEntity urlEntity = UrlEntity.Parse(urlMessage.AsString);
                        if (workerStateMachine.getState() == WorkerStateMachine.STATE_LOADING)
                        {
                            if (urlEntity.PartitionKey == UrlEntity.URL_TYPE_SITEMAP)
                            {
                                WebCrawler webCrawler = new WebCrawler(urlEntity.RowKey);
                                webCrawler.parseSitemap(); // crawls xml, adds leaf html urls to queue
                                urlQueue.DeleteMessage(urlMessage);
                            }
                            else if (urlEntity.PartitionKey == UrlEntity.URL_TYPE_HTML)
                            {
                                // if we are in the loading state, and we receive a URL_TYPE_HTML message, that means we've finished
                                // loading and can transition to crawling state
                                workerStateMachine.setState(WorkerStateMachine.STATE_CRAWLING);
                                // intentionally don't delete queue message, so that it gets processed when the state has been set to crawling
                            }

                        }
                        else if(workerStateMachine.getState() == WorkerStateMachine.STATE_CRAWLING)
                        {

                        }
                    }
                }
                
                await Task.Delay(1000);
            }
        }
    }
}
