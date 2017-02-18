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

        public int WorkerRoleID { get; set; }
        public string State { get; set; }
        public WorkerRoleInstance(string workerRoleID, string state)
        {
            this.PartitionKey = TABLE_WORKER_ROLES;
            this.RowKey = workerRoleID;
            this.State = state;
        }

        public WorkerRoleInstance() { }
    }
}
