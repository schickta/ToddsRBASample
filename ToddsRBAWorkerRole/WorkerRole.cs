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
            //Trace.TraceInformation("ToddsRBAWorkerRole is running");

            //try
            //{
            //    this.RunAsync(this.cancellationTokenSource.Token).Wait();
            //}
            //finally
            //{
            //    this.runCompleteEvent.Set();
            //}
        }

        public override bool OnStart()
        {
            string connString = RoleEnvironment.GetConfigurationSettingValue("RBAStorage");

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

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            CloudQueueMessage msg = null;

            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
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

                Trace.TraceInformation("Working");
                await Task.Delay(1000);
            }
        }

        private void ProcessQueueMessage (CloudQueueMessage cMsg)
        {
            this.m_messageQueue.DeleteMessage(cMsg);
            return;
        }
    }
}
