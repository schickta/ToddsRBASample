using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;

namespace ToddsRBASampleCommon
{
    /// ----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// MessageItem is a simple class which specializes TableEntity and is used to store/retreive string messages
    /// from Azure Table storage.
    /// </summary>
    /// ---------------------------------------------------------------------------------------------------------
    /// 
    public class MessageItem : TableEntity
    {
        public MessageItem()
        {
        }

        public MessageItem(string msg)
        {
            this.PartitionKey = "1";
            this.RowKey = msg;
        }
    }
}
