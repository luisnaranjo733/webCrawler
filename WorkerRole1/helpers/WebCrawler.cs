

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using SharedCode;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace WorkerRole1.helpers
{
    public class WebCrawler
    {
        private DisallowCache disallowCache;
        public StatsManager statsManager;

        private CloudQueue urlQueue;
        public WebCrawler()
        {
            this.disallowCache = new DisallowCache();
            this.statsManager = new StatsManager();
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]
            );
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            urlQueue = queueClient.GetQueueReference(UrlEntity.QUEUE_URL);
        }

        public void parseHtml(string url)
        {

        }

        public void parseSitemap(string url)
        {
            WebRequest request = WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream dataStream = response.GetResponseStream();

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(dataStream);
            XmlElement root = xmlDoc.DocumentElement;
            if (root.Name == "sitemapindex")
            {
                foreach (XmlNode itemElement in root.ChildNodes)
                {
                    string childUrl = "";
                    string childDate = "";
                    foreach (XmlNode child in itemElement.ChildNodes)
                    {
                        if (child.Name == "loc")
                        {
                            childUrl = child.InnerText;
                        }
                        else if (child.Name == "lastmod")
                        {
                            childDate = child.InnerText;
                        }
                    }
                    if (childUrl.Length > 0)
                    {
                        parseSitemap(childUrl);
                    }
                }
            }
            else if (root.Name == "urlset")
            {
                foreach (XmlNode itemElement in root.ChildNodes)
                {
                    string childUrl = "";
                    string childDate = "";
                    foreach (XmlNode child in itemElement.ChildNodes)
                    {
                        if (child.Name == "loc")
                        {
                            childUrl = child.InnerText;
                        }
                        else if (child.Name == "lastmod")
                        {
                            childDate = child.InnerText;
                        }
                    }
                    if (childUrl.Length > 0)
                    {
                        addUrlToQueue(childUrl);
                    }
                }
            }
        }



        private bool isUrlValid(string url)
        {
            return true;
        }

        private void addUrlToQueue(string url)
        {
            UrlEntity urlEntity = new UrlEntity(UrlEntity.URL_TYPE_HTML, url);

            CloudQueueMessage urlMessage = new CloudQueueMessage(urlEntity.ToString());
            urlQueue.AddMessage(urlMessage);

        }
    }
}
