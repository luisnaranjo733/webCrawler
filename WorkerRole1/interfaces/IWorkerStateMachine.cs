using SharedCodeLibrary.models;
using SharedCodeLibrary.models.QueueMessages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerRole1.interfaces
{
    interface IWorkerStateMachine
    {
        bool Act(UrlMessage urlEntity, IWebLoader webLoader,  IWebCrawler webCrawler);
    }
}
