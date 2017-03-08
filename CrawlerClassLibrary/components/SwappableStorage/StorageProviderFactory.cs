using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrawlerClassLibrary.components.SwappableStorage
{
    public class StorageProviderFactory
    {
        public enum Provider { Azure };

        private static StorageProviderFactory instance = null;
        private static readonly object padlock = new object();

        public IStorageProvider CreateStorageProvider(Provider storageProviderType)
        {
            if (storageProviderType == Provider.Azure)
            {
                return new AzureStorageProvider();
            } else
            {
                return null;
            }
        }


        public static StorageProviderFactory Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new StorageProviderFactory();
                    }
                    return instance;
                }
            }
        }
    }
}
