using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

// Include Azure Storage assemblies

using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage.Table;

using ToddsRBASampleCommon;

namespace ToddsRBAWebRole.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        /// -------------------------------------------------------------------------------------
        /// <summary>
        /// SendForm: This action is meant to be triggered when the "Send" button is clicked on
        /// the UI. It's purpose is to queue the entered message so that the Worker Role can
        /// pick it up.
        /// </summary>
        /// <param name="message">The message entered via the UI that will be queued</param>
        /// ------------------------------------------------------------------------------------
        /// 
        public ActionResult SendForm(string message)
        {
            // If there is a blank message, we'll use an arbitrary default value.

            if (message == null || message.Length == 0)
            {
                message = "no message was entered by the user";
            }

            // Grab the storage account from the configuration file. Proceed with boilerplate code
            // to obtain a reference to the Azure Storage queue. Create it if it doesn't exist.

            var storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("RBAStorage"));

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue messageQueue = queueClient.GetQueueReference("messageq");
            messageQueue.CreateIfNotExists();

            // Wrap the message into a CloudQueueMessage and enqueue it.

            messageQueue.AddMessage(new CloudQueueMessage(message));

            return View("Index");
        }

        /// ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// GetMessages: This action will retreive all messages in Azure Table Storage and return
        /// them as jSon. It is meant to be called by jquery on the UI side to refresh the 
        /// list of messages. 
        /// </summary>
        /// <returns></returns>
        /// -----------------------------------------------------------------------------------------------------
        /// 
        [AcceptVerbs(HttpVerbs.Get)]
        public JsonResult GetMessages ()
        {
            // Boilerplate code to retrieve the Azure storage string from the configuration, then to retrieve a reference to
            // the Table Store.
            
            var storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("RBAStorage"));

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("messages");
            table.CreateIfNotExists();

            // Construct the query operation to retrieve all messages (all have PartitionKey = 1).

            TableQuery<MessageItem> query = new TableQuery<MessageItem>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "1"));

            // Return the result as json.

            return (Json(table.ExecuteQuery(query), JsonRequestBehavior.AllowGet));
        }
    }
}