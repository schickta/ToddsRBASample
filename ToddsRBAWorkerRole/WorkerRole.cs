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

        /// <summary>
        /// m_messageQueue is a handle to the queue. A reference is obtained upon startup of the Role, then
        /// is polled and processed continually during the Run operation.
        /// </summary>
        /// 
        private CloudQueue m_messageQueue = null;

        /// =-----------------------------------------------------------------------------------------------
        /// <summary>
        /// Run: This method continually polls the queue, waiting on an incoming message. When obtained, the
        /// message is saved to Table Storage, then is dequeued.
        /// </summary>
        /// ------------------------------------------------------------------------------------------------
        /// 
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

        /// ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// OnStart: Sets m_messageQueue which is a handle to the Azure Storage Queue. The rest is boilerplate.
        /// </summary>
        /// <returns></returns>
        /// ---------------------------------------------------------------------------------------------------
        /// 
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

        /// ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// OnStop: Boilerplate code, executed when the role is halting.
        /// </summary>
        /// ----------------------------------------------------------------------------------------------------
        /// 
        public override void OnStop()
        {
            Trace.TraceInformation("ToddsRBAWorkerRole is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("ToddsRBAWorkerRole has stopped");
        }

        /// -------------------------------------------------------------------------------------------------------
        /// <summary>
        /// ProcesQueueMessage: Processes messages by storing them into Azure Table storage, then de-queueing the message.
        /// </summary>
        /// <param name="cMsg">The CloudQueueMessage object which contains the queue'd message</param>
        /// -------------------------------------------------------------------------------------------------------
        /// 
        private void ProcessQueueMessage(CloudQueueMessage cMsg)
        {
            // Obtain a reference to the message queue.

            var storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("RBAStorage"));

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("messages");
            table.CreateIfNotExists();

            // Create a new MessageItem object, passing in our message.

            MessageItem msg = new MessageItem(cMsg.AsString);

            // Create and execute a table operation to insert the message.

            TableOperation insertOperation = TableOperation.Insert(msg);

            table.Execute(insertOperation);

            // Dequeue the message.

            this.m_messageQueue.DeleteMessage(cMsg);

            return;
        }
    }
}

