using SharedCodeLibrary.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerRole1.interfaces
{
    interface IWorkerStateMachine
    {
        bool Act(UrlEntity urlEntity, IWebLoader webLoader,  IWebCrawler webCrawler);
    }
}
