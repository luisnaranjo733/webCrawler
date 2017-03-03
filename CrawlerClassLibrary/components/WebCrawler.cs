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


namespace CrawlerClassLibrary.components
{
    public class WebCrawler
    {
        private HtmlWeb web;
        private CloudTable urlTable;
        private ConcurrentDictionary<string, bool> visitedUrls;
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
            visitedUrls = new ConcurrentDictionary<string, bool>();
            urlValidator = new UrlValidator();

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            urlQueue = queueClient.GetQueueReference(UrlMessage.QUEUE_URL);
        }

        public void Crawl(Object state)
        {
            try
            {
                InnerCrawl(state);
            } catch (Exception e)
            {
                Logger.Instance.Log(Logger.LOG_ERROR, "General exception: " + state.ToString() + " | " + e.ToString());
            }

        }

        private void InnerCrawl(Object state)
        {
            string url = "";
            try
            {
                url = (string)state;
            }
            catch (InvalidCastException e)
            {
                Logger.Instance.Log(Logger.LOG_ERROR, "Non string passed to crawl method - casting failed: " + url + " | " + e.ToString());
                return;
            }

            if (!visitedUrls.ContainsKey(url))
            {
                HtmlDocument document;
                try
                {
                    document = web.Load(url);
                }
                catch
                {
                    

                    return;
                }

                Link parentLink;
                try
                {
                    parentLink = new Link(url);
                } catch (UriFormatException e)
                {
                    Logger.Instance.Log(Logger.LOG_ERROR, "Invalid url format - Uri() parsing failed: " + url + " | " + e.ToString());
                    return;
                }
                    

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


                IndexEntity indexEntity = new IndexEntity(url, title); // also add page date
                TableOperation insertOperation = TableOperation.Insert(indexEntity);
                try
                {
                    urlTable.ExecuteAsync(insertOperation);
                }
                catch (StorageException e)
                {
                    Logger.Instance.Log(Logger.LOG_ERROR, "Insert to index failed: " + indexEntity.ToString() + " | " + e.ToString());
                    return;
                }


                visitedUrls.TryAdd(url, true);

                nodeCollection = document.DocumentNode.SelectNodes("//a");
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
                            Logger.Instance.Log(Logger.LOG_ERROR, "Invalid url format - Uri() parsing failed: " + url + " | " + e.ToString());
                            return;
                        }

                        if (!visitedUrls.ContainsKey(link) && urlValidator.IsUrlValidCrawling(link))
                        {
                            visitedUrls.TryAdd(link, true);

                            UrlMessage urlEntity = new UrlMessage(UrlMessage.URL_TYPE_HTML, link);

                            // Add message
                            CloudQueueMessage message = new CloudQueueMessage(urlEntity.ToString());
                            urlQueue.AddMessageAsync(message);

                        }
                    }
                }


            }
        }

    }
}
