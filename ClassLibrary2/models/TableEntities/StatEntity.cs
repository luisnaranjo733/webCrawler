using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedCodeLibrary.models.TableEntities
{
    public class StatEntity : TableEntity
    {
        public const string TABLE_STATS = "stats";

        public string Value { get; set; }

        public StatEntity() { }
        public StatEntity(string statName, string statValue)
        {
            this.PartitionKey = statName;
            this.RowKey = statName;
            this.Value = statValue;
        }
    }
}
