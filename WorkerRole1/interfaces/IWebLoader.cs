using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkerRole1.helpers;

namespace WorkerRole1.interfaces
{
    interface IWebLoader
    {
        void parseSitemap(string url);
    }
}
