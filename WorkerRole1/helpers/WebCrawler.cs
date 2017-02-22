using HtmlAgilityPack;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using SharedCodeLibrary.helpers;
using SharedCodeLibrary.models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkerRole1.interfaces;


namespace WorkerRole1.helpers
{
    class WebCrawler : IWebCrawler
    {
        private HtmlWeb web;
        private CloudTable urlTable;
        private Dictionary<string, bool> visitedUrls;
        private UrlValidator urlValidator;
        private CloudQueue urlQueue;

        public WebCrawler(StatsManager statsManager)
        {
            web = new HtmlWeb();
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]
            );
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            urlTable = tableClient.GetTableReference(IndexEntity.TABLE_INDEX);
            visitedUrls = new Dictionary<string, bool>();
            urlValidator = new UrlValidator(new DisallowCache());

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            urlQueue = queueClient.GetQueueReference(UrlEntity.QUEUE_URL);
        }

        public void Crawl(string url)
        {
            if (!visitedUrls.ContainsKey(url)) 
            {
                HtmlDocument document;
                try {
                    document = web.Load(url);
                } catch
                {
                    Logger.Instance.Log(Logger.LOG_ERROR, "html dom parsing failed in WebCrawler.Crawl()");
                    return;
                }
                

                HtmlNode[] nodes = document.DocumentNode.SelectNodes("//title").ToArray();
                string title = "";
                foreach (HtmlNode item in nodes)
                {
                    title = item.InnerHtml;
                    break; // there should only be one title, if there are more then pick the 1st one arbitrarily
                }

                IndexEntity indexEntity = new IndexEntity(url, title);
                TableOperation insertOperation = TableOperation.Insert(indexEntity);
                urlTable.Execute(insertOperation);

                visitedUrls.Add(url, true);

                nodes = document.DocumentNode.SelectNodes("//a").ToArray();
                foreach (HtmlNode item in nodes)
                {
                    string link = item.GetAttributeValue("href", "");
                    if (!visitedUrls.ContainsKey(link) && urlValidator.isUriValidBleacher(link))
                    {
                        visitedUrls.Add(link, true);

                        UrlEntity urlEntity = new UrlEntity(UrlEntity.URL_TYPE_HTML, link);

                        // Add message
                        CloudQueueMessage message = new CloudQueueMessage(urlEntity.ToString());
                        urlQueue.AddMessage(message);

                    }
                }
            }

        }

    }
}
