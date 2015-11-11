using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;

using ToddsRBASampleCommon;

namespace ToddsRBAWorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        private CloudQueue m_messageQueue = null;

        public override void Run()
        {
            CloudQueueMessage msg = null;

            while (true)
            {
                try
                {
                    msg = this.m_messageQueue.GetMessage();

                    if (msg != null)
                    {
                        ProcessQueueMessage(msg);
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(1000);
                    }
                }
                catch (StorageException e)
                {
                    if (msg != null && msg.DequeueCount > 5)
                    {
                        this.m_messageQueue.DeleteMessage(msg);
                    }

                    System.Threading.Thread.Sleep(5000);
                }
            }
        }

        public override bool OnStart()
        {
            var storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("RBAStorage"));

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            m_messageQueue = queueClient.GetQueueReference("messageq");
            m_messageQueue.CreateIfNotExists();

            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("ToddsRBAWorkerRole has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("ToddsRBAWorkerRole is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("ToddsRBAWorkerRole has stopped");
        }

        private void ProcessQueueMessage(CloudQueueMessage cMsg)
        {
            var storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("RBAStorage"));

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("messages");
            table.CreateIfNotExists();

            // Create the TableOperation object that inserts the customer entity.

            MessageItem msg = new MessageItem(cMsg.AsString);

            TableOperation insertOperation = TableOperation.Insert(msg);

            // Execute the insert operation.
            table.Execute(insertOperation);

            this.m_messageQueue.DeleteMessage(cMsg);
            return;
        }
    }
}

