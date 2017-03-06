using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrawlerClassLibrary.models.interfaces
{
    interface IIndexEntity
    {
        string GetTitle();
        string GetUrl();
        DateTime GetDate();
    }
}
