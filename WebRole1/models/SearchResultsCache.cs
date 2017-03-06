using CrawlerClassLibrary.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1.models
{
    public class SearchResultsCache
    {
        private int MaxSize;
        Dictionary<string, IEnumerable<SearchResult>> Cache;
        public SearchResultsCache(int maxSize = 100)
        {
            MaxSize = maxSize;
            Cache = new Dictionary<string, IEnumerable<SearchResult>>();
        }

        /// <summary>
        /// Get cache line or return null
        /// </summary>
        /// <param name="query"></param>
        /// <returns>cache line or null</returns>
        public IEnumerable<SearchResult> GetLineOrNull(string query)
        {
            if (Cache.ContainsKey(query))
            {
                return Cache[query];
            } else
            {
                return null;
            }
        }

        /// <summary>
        /// Update cache line, reset cache if it grows too big
        /// </summary>
        /// <param name="query"></param>
        /// <param name="searchResult"></param>
        public void Store(string query, IEnumerable<SearchResult> searchResult)
        {
            if (Cache.Count >= MaxSize) { Cache.Clear(); } // clear cache if it gets too big

            Cache.Add(query, searchResult);
        }

        /// <summary>
        /// Clear cache contents
        /// </summary>
        public void Clear()
        {
            Cache.Clear();
        }

    }
}