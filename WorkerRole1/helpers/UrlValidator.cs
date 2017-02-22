using SharedCodeLibrary.helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkerRole1.interfaces;

namespace WorkerRole1.helpers
{
    public class UrlValidator : IUrlValidator
    {
        private static DateTime cutoff = new DateTime(2016, 12, 1);

        private DisallowCache disallowCache;
        public UrlValidator(DisallowCache disallowCache)
        {
            this.disallowCache = disallowCache;
        }

        public bool IsUriValid(string loc, string lastMod, bool checkDisallow)
        {
            if (checkDisallow)
            {
                return isUriRecent(loc, lastMod) && isUriValid(loc);
            } else
            {
                return isUriRecent(loc, lastMod);
            }
        }

        public bool isUriValidBleacher(string url)
        {
            if (url.Contains("nba"))
            {
                return isUriValid(url);
            }
            return false;
        }

        public bool isUriValid(string loc) {
            // loading phase
            // get domain
            // check  that domain is bleacher report or cnn
            // if domain is bleacher report, check that url is nba related

            /* in the crawling phase, if a url ends in a directory, like "www.cnn.com/politics/" 
             * it's easier to assume it's an html page. I'm not sure if this is 100% true, there 
             * are probably edge cases where this is not true and I'll spend some time (~5min)
             *  finding cases where this isn't true but I suspect that would be hard and prob 
             *  not worth your time now unless you're 100% done w/ everything else and just 
             *  focusing on edge cases now. 
             * 
             * */
            Uri uri = new Uri(loc);
            if (uri.Host != "www.cnn.com" && uri.Host != "bleacherreport.com")
            {
                return false;
            }
            string[] segments = uri.Segments;
            return disallowCache.isUrlAllowed(uri) && uri.Segments.Length > 1; // check disallow and check if it has a folder by checking segment length
        }

        private static bool isUriRecent(string loc, string lastMod)
        {
            Uri uri = new Uri(loc);

            int lastModRecent = 0;
            int urlPathRecent = 0;

            if (lastMod.Length > 0) // lastmod is present
            {
                DateTime lastModDate;
                try {
                    lastModDate = DateTime.Parse(lastMod);
                } catch
                {
                    lastModDate = cutoff;
                    Logger.Instance.Log(Logger.LOG_ERROR, "parsing <lastmod> to datetime failed");
                }
                
                if (lastModDate > cutoff) {
                    lastModRecent = 1;
                } else
                {
                    lastModRecent = 2;
                }
            }

            int year = 0;
            int month = 0;
            int day = 0;
            for (int i = 0; i < uri.Segments.Length; i++)
            {
                string segment = uri.Segments[i];
                segment = segment.Replace("/", "");
                int intValue = 0;
                int.TryParse(segment, out intValue);

                if (intValue != 0) // segment is an int value (possible date)
                {
                    if (year == 0)
                    {
                        if (segment.Length == 4)
                        {
                            year = intValue;
                        }
                    }
                    else if (year != 0 && month == 0)
                    {
                        if (segment.Length == 2)
                        {
                            month = intValue;
                        }

                    }
                    else if (year != 0 && month != 0 && day == 0)
                    {
                        if (segment.Length == 2)
                        {
                            day = intValue;
                        }
                    }
                }

                if (segment.Contains(".xml"))
                {
                    string[] splitUrl = segment.Split('-');
                    if (splitUrl.Length == 4) //[" sitemap", "show", "2017", "02.xml"] not root sitemaps with .xml (length == 2)
                    {
                        string parsedYearString;
                        string parsedMonthString;
                        try
                        {
                            parsedYearString = splitUrl[2];
                            parsedMonthString = splitUrl[3].Substring(0, 2);
                        } catch
                        {
                            parsedYearString = "";
                            parsedMonthString = "";
                            Logger.Instance.Log(Logger.LOG_ERROR, "parsing root *.xml sitemap for date failed");
                        }


                        Int32.TryParse(parsedYearString, out year);
                        Int32.TryParse(parsedMonthString, out month);
                    }


                }

            }

            
            if (year != 0)
            {
                if (month == 0)
                {
                    month = 12; // assume the worst if the url doesn't contain a month
                }
                if (day == 0)
                {
                    day = 28; // assume the worst if the url doesn't contain a day
                }
                DateTime date = new DateTime(year, month, day);
                if (date >= cutoff) {
                    urlPathRecent = 1;
                } else
                {
                    urlPathRecent = 2;
                }

            }

            if (lastModRecent == 0 && urlPathRecent == 0) // neither has date
            {
                return true; // if no date available, add it to the queue
            } else if (lastModRecent != 0 && urlPathRecent == 0) // lastmod only has date
            {
                return lastModRecent == 1;
            } else if (lastModRecent == 0 && urlPathRecent != 0) // url path only has date
            {
                return urlPathRecent == 1;
            } else if (lastModRecent != 0 && urlPathRecent != 0) // both have date
            {
                return urlPathRecent == 1;
            }

            return false;
        }
    }
}
