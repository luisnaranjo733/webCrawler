using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using SharedCode;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerRole1.helpers
{
    class DisallowCache
    {
        private CloudTable disallowTable;
        private List<string> disallowList;
        public DisallowCache()
        {
            disallowList = new List<string>();
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]
            );
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            disallowTable = tableClient.GetTableReference(DisallowEntity.TABLE_DISALLOW);

            TableQuery<DisallowEntity> query = new TableQuery<DisallowEntity>();

            foreach (DisallowEntity entity in disallowTable.ExecuteQuery(query))
            {
                disallowList.Add(entity.ToString());
            }
        }

        public bool isUrlAllowed(string path)
        {
            return disallowList.Contains(path);
        }
    }
}
