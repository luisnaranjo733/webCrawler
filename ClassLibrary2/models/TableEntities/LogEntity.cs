using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedCodeLibrary.models.TableEntities
{
    public class LogEntity : TableEntity
    {
        public const string TABLE_LOG = "errorlog";

        public string Message { get; set; }

        public LogEntity() { }
        public LogEntity(string type, string message)
        {
            PartitionKey = type;
            RowKey = WindowsAzure.ChronoTableStorage.RowKey.CreateReverseChronological(DateTime.UtcNow);
            Message = message;
        }

        public override string ToString()
        {
            return PartitionKey + ':' + Message;
        }
    }
}
