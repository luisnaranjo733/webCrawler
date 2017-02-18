

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
        public string Url { get; set; }

        private CloudQueue urlQueue;
        public WebCrawler(string url)
        {
            this.Url = url;

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]
            );
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            urlQueue = queueClient.GetQueueReference(UrlEntity.QUEUE_URL);
        }

        public void parseHtml()
        {

        }

        public void parseSitemap()
        {
            this.parseSitemap(Url);
        }

        private void parseSitemap(string url)
        {
            WebRequest request = WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream dataStream = response.GetResponseStream();

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(dataStream);
            XmlElement root = xmlDoc.DocumentElement;
            if (root.Name == "sitemapindex")
            {
                enumerateXmlDoc(root, parseSitemap);
            }
            else if (root.Name == "urlset")
            {
                enumerateXmlDoc(root, addUrlToQueue);
            }
        }

        private void enumerateXmlDoc(XmlElement root, Action<string> callback)
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
                    callback(childUrl);
                }
            }
        }

        private bool isUrlValid(string url)
        {
            return true;
        }

        private void addUrlToQueue(string url)
        {
            if (isUrlValid(url))
            {

                UrlEntity urlEntity = new UrlEntity(UrlEntity.URL_TYPE_HTML, url);

                CloudQueueMessage urlMessage = new CloudQueueMessage(urlEntity.ToString());
                urlQueue.AddMessage(urlMessage);
            }

        }
    }
}
