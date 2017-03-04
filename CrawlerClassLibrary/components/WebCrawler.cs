using CrawlerClassLibrary.components;
using CrawlerClassLibrary.models.QueueMessages;
using CrawlerClassLibrary.models.TableEntities;
using HtmlAgilityPack;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using SharedCodeLibrary.models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CrawlerClassLibrary.components
{
    public class WebCrawler
    {
        private HtmlWeb web;
        private CloudTable urlTable;
        private ConcurrentDictionary<string, bool> visitedUrls;
        private UrlValidator urlValidator;
        private CloudQueue urlQueue;

        static int nUrlsCrawled;
        static int sizeOfIndex;

        public WebCrawler()
        {
            web = new HtmlWeb();
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]
            );
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            urlTable = tableClient.GetTableReference(IndexEntity.TABLE_INDEX);
            visitedUrls = new ConcurrentDictionary<string, bool>();
            urlValidator = new UrlValidator();

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            urlQueue = queueClient.GetQueueReference(UrlMessage.QUEUE_URL);
        }

        public async void Crawl(Object state)
        {
            try
            {
                UrlMessage urlMessage;
                try
                {
                    urlMessage = (UrlMessage)state;
                }
                catch (InvalidCastException e)
                {
                    e.ToString(); // called this to make annoying compiler warning go away
                    //Logger.Instance.Log(Logger.LOG_ERROR, "Non string passed to crawl method - casting failed: " + url + " | " + e.ToString());
                    return;
                }

                if (!visitedUrls.ContainsKey(urlMessage.Url))
                {
                    Interlocked.Increment(ref WebCrawler.nUrlsCrawled);
                    if (WebCrawler.nUrlsCrawled % StatsManager.UPDATE_STATS_FREQ == 0)
                    {
                        // update stats
                        StatsManager.Instance.updateStat(StatsManager.N_URLS_CRAWLED, nUrlsCrawled.ToString());
                    }

                    HtmlDocument document;
                    try
                    {
                        document = web.Load(urlMessage.Url);
                    }
                    catch { return; }

                    Link parentLink;
                    try
                    {
                        parentLink = new Link(urlMessage.Url);
                    }
                    catch (UriFormatException e)
                    {
                        e.ToString(); // called this to make annoying compiler warning go away
                        //Logger.Instance.Log(Logger.LOG_ERROR, "Invalid url format - Uri() parsing failed: " + url + " | " + e.ToString());
                        return;
                    }


                    string title = fetchTitleFromDocument(document);
                    DateTime date = fetchDateFromDocument(document);

                    await indexPage(urlMessage.Url, title, date);

                    Interlocked.Increment(ref WebCrawler.sizeOfIndex);
                    if (WebCrawler.sizeOfIndex % StatsManager.UPDATE_STATS_FREQ == 0)
                    {
                        // update stats
                        StatsManager.Instance.updateStat(StatsManager.SIZE_OF_TABLE, sizeOfIndex.ToString());
                    }


                    visitedUrls.TryAdd(urlMessage.Url, true); // mark this url as visited in the cache to prevent visiting it again
                    await queueChildLinksForCrawling(parentLink, document); // traverse child <a href=""/> links and add them to queue if valid
                }
            }
            catch (Exception e)
            {
                Logger.Instance.Log(Logger.LOG_ERROR, "General exception: " + state.ToString() + " | " + e.ToString());
            }

        }


        private async Task indexPage(string url, string title, DateTime date)
        {
            string[] keywords = title.Split(' ');
            foreach(string keyword in keywords)
            {
                if (!keyword.All(char.IsLetterOrDigit))
                {
                    continue; // skip keywords with non alphanumberic chars (not valid in azure tables)
                }
                IndexEntity indexEntity = new IndexEntity(url, keyword, date);
                TableOperation insertOperation = TableOperation.InsertOrReplace(indexEntity);
                try
                {
                    await urlTable.ExecuteAsync(insertOperation);
                }
                catch (StorageException e)
                {
                    Logger.Instance.Log(Logger.LOG_ERROR, "Insert to index failed: " + indexEntity.ToString() + " | " + e.ToString());
                    return;
                }
            }
        }

        private async Task queueChildLinksForCrawling(Link parentLink, HtmlDocument document)
        {
            HtmlNodeCollection nodeCollection = document.DocumentNode.SelectNodes("//a");
            if (nodeCollection != null)
            {
                HtmlNode[] nodes = nodeCollection.ToArray();
                foreach (HtmlNode item in nodes)
                {
                    string linkPath = item.GetAttributeValue("href", ""); // could be relative or absolute or external
                    string link;

                    try
                    {
                        link = parentLink.buildUrl(linkPath);
                    }
                    catch (UriFormatException e)
                    {
                        Logger.Instance.Log(Logger.LOG_ERROR, "Invalid url format - Uri() parsing failed: " + e.ToString());
                        return;
                    }

                    if (!visitedUrls.ContainsKey(link) && urlValidator.IsUrlValidCrawling(link))
                    {
                        visitedUrls.TryAdd(link, true);

                        UrlMessage urlEntity = new UrlMessage(UrlMessage.URL_TYPE_HTML, link);

                        // Add message
                        CloudQueueMessage message = new CloudQueueMessage(urlEntity.ToString());
                        await urlQueue.AddMessageAsync(message);

                    }
                }
            }
        }

        private string fetchTitleFromDocument(HtmlDocument document)
        {
            string title = "Page indexed, but no <title> tag found";
            HtmlNodeCollection nodeCollection = document.DocumentNode.SelectNodes("//title");
            if (nodeCollection != null)
            {
                HtmlNode[] nodes = nodeCollection.ToArray();

                foreach (HtmlNode item in nodes)
                {
                    title = item.InnerHtml;
                    break; // there should only be one title, if there are more then pick the 1st one arbitrarily
                }
            }
            return title;
        }

        private DateTime fetchDateFromDocument(HtmlDocument document)
        {
            return DateTime.UtcNow;
        }


    }
}
