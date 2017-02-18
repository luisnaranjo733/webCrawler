using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedCode
{
    public class WorkerRoleInstance : TableEntity
    {
        public const string TABLE_WORKER_ROLES= "workerroles";

        public const string STATE_IDLE = "idle";
        public const string STATE_LOADING = "loading";
        public const string STATE_CRAWLING = "crawling";

        public int WorkerRoleID { get; set; }
        public string State { get; set; }
        public WorkerRoleInstance(int workerRoleID)
        {
            this.PartitionKey = TABLE_WORKER_ROLES;
            this.RowKey = workerRoleID.ToString();
        }

        public WorkerRoleInstance() { }
    }
}
