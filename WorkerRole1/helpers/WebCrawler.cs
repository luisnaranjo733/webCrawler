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
using WorkerRole1.models;

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
            urlValidator = new UrlValidator();

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
                    Logger.Instance.Log(Logger.LOG_ERROR, "html dom parsing failed in WebCrawler.Crawl() for " + url);
                    return;
                }

                Link parentLink = new Link(url);

                HtmlNode[] nodes = document.DocumentNode.SelectNodes("//title").ToArray();
                string title = "Page indexed, but no <title> tag found";
                foreach (HtmlNode item in nodes)
                {
                    title = item.InnerHtml;
                    break; // there should only be one title, if there are more then pick the 1st one arbitrarily
                }

                IndexEntity indexEntity = new IndexEntity(url, title); // also add page date
                TableOperation insertOperation = TableOperation.Insert(indexEntity);
                urlTable.Execute(insertOperation);

                visitedUrls.Add(url, true);

                nodes = document.DocumentNode.SelectNodes("//a").ToArray();
                foreach (HtmlNode item in nodes)
                {
                    string linkPath = item.GetAttributeValue("href", ""); // could be relative or absolute or external
                    string link = parentLink.buildUrl(linkPath);
                    if (!visitedUrls.ContainsKey(link) && urlValidator.IsUrlValidCrawling(link))
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
