using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrawlerClassLibrary.components.SwappableStorage
{
    public interface IStorageProvider
    {
        Task indexPage(string url, string title, DateTime date);
    }
}
