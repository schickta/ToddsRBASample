using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

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

        public ActionResult SendForm(string message)
        {
            if (message == null || message.Length == 0)
            {
                message = "no message was entered by the user";
            }

            var storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("RBAStorage"));

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue messageQueue = queueClient.GetQueueReference("messageq");
            messageQueue.CreateIfNotExists();

            messageQueue.AddMessage(new CloudQueueMessage(message));

            return View("Index");
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public JsonResult GetMessages ()
        {
            var storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("RBAStorage"));

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("messages");
            table.CreateIfNotExists();

            // Construct the query operation for all customer entities where PartitionKey="Smith".
            TableQuery<MessageItem> query = new TableQuery<MessageItem>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "1"));

            // Print the fields for each customer.
            foreach (MessageItem msg in table.ExecuteQuery(query))
            {
                int i = 0;
            }

            //var classificationList = TCListItems.DocumentClassificationList(id);
            //return (Json(classificationList, JsonRequestBehavior.AllowGet));
            return (Json(table.ExecuteQuery(query), JsonRequestBehavior.AllowGet));
        }
    }
}