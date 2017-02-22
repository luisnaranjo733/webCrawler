using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerRole1.interfaces
{
    interface IWebCrawler
    {
        void Crawl(string url);
    }
}
