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
using Microsoft.WindowsAzure.Storage.Queue;
using CrawlerClassLibrary.components;
using CrawlerClassLibrary.models.QueueMessages;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        private CloudQueue commandQueue;
        private CloudQueue urlQueue;

        private WorkerStateMachine workerStateMachine;
        private WebLoader webLoader;
        private WebCrawler webCrawler;

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

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]
            );
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            commandQueue = queueClient.GetQueueReference(CommandMessage.QUEUE_COMMAND);
            commandQueue.CreateIfNotExists();

            urlQueue = queueClient.GetQueueReference(UrlMessage.QUEUE_URL);
            urlQueue.CreateIfNotExists();

            webCrawler = new WebCrawler();
            workerStateMachine = new WorkerStateMachine(instanceID.ToString(), urlQueue);
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



            while (!cancellationToken.IsCancellationRequested)
            {
                CloudQueueMessage commandMessage = commandQueue.GetMessage(TimeSpan.FromMinutes(5));
                if (commandMessage != null)
                {
                    if (commandMessage.AsString == CommandMessage.COMMAND_LOAD)
                    {
                        workerStateMachine.setState(WorkerStateMachine.STATE_LOADING);
                        webLoader = new WebLoader();
                    } else if (commandMessage.AsString == CommandMessage.COMMAND_IDLE)
                    {
                        workerStateMachine.setState(WorkerStateMachine.STATE_IDLE);
                    } else if (commandMessage.AsString == CommandMessage.COMMAND_CRAWL)
                    {
                        workerStateMachine.setState(WorkerStateMachine.STATE_CRAWLING);
                        webCrawler = new WebCrawler();
                    }
                    commandQueue.DeleteMessage(commandMessage);
                }

                if (workerStateMachine.getState() != WorkerStateMachine.STATE_IDLE) // in a loading or crawling state
                {
                    CloudQueueMessage urlMessage = urlQueue.GetMessage();
                    workerStateMachine.Act(urlMessage);
                }
                
                await Task.Delay(100);
            }
        }

    }
}
