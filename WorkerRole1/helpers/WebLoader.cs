using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using SharedCodeLibrary.helpers;
using SharedCodeLibrary.models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using WorkerRole1.interfaces;

namespace WorkerRole1.helpers
{
    public class WebLoader : IWebLoader
    {
        public DisallowCache disallowCache;
        private UrlValidator urlValidator;

        private CloudQueue urlQueue;
        public WebLoader()
        {
            this.disallowCache = new DisallowCache();
            this.urlValidator = new UrlValidator(disallowCache);
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]
            );
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            urlQueue = queueClient.GetQueueReference(UrlEntity.QUEUE_URL);
        }

        public void parseSitemap(string url)
        {
            bool isBleacherReport = new Uri(url).Host == "bleacherreport.com";
            WebRequest request = WebRequest.Create(url);
            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException e)
            {
                Logger.Instance.Log(Logger.LOG_ERROR, "parseSitemap web request failed");
                return;
            }
            Stream dataStream = response.GetResponseStream();

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(dataStream);
            XmlElement root = xmlDoc.DocumentElement;
            if (root.Name == "sitemapindex")
            {
                enumerateXml(root, parseSitemap, true, isBleacherReport);
            }
            else if (root.Name == "urlset")
            {
                enumerateXml(root, addUrlToQueue, false, isBleacherReport);
            }
        }

        private void enumerateXml(XmlNode root, Action<string> callback, bool checkDisallow, bool isBleacherReport)
        {
            foreach (XmlNode itemElement in root.ChildNodes)
            {
                string childLoc = "";
                string childLastMod = "";
                foreach (XmlNode child in itemElement.ChildNodes)
                {
                    if (child.Name == "loc")
                    {
                        childLoc = child.InnerText;
                    }
                    else if (child.Name == "lastmod")
                    {
                        childLastMod = child.InnerText;
                    }
                }
                if (childLoc.Length > 0 && urlValidator.IsUriValid(childLoc, childLastMod, checkDisallow))
                {
                    callback(childLoc);
                }
            }
        }

        private void addUrlToQueue(string url)
        {
            UrlEntity urlEntity = new UrlEntity(UrlEntity.URL_TYPE_HTML, url);

            CloudQueueMessage urlMessage = new CloudQueueMessage(urlEntity.ToString());
            urlQueue.AddMessage(urlMessage);

        }

    }
}
