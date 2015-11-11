using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.ServiceRuntime;

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

            string connString = RoleEnvironment.GetConfigurationSettingValue("RBAStorage");

            var storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("RBAStorage"));

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue messageQueue = queueClient.GetQueueReference("messageq");
            messageQueue.CreateIfNotExists();

            messageQueue.AddMessage(new CloudQueueMessage(message));

            return View("Index");
        }
    }
}