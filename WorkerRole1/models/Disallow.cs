using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedCode
{
    public class Disallow : TableEntity
    {
        public const string TABLE_DISALLOW = "disallow";

        public Disallow() { }
        public Disallow(string path, string website)
        {
            this.PartitionKey = website;
            this.RowKey = path.Replace("/", "$");
        }
    }
}
