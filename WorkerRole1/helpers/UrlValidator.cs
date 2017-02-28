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

        private DisallowCache bleacherDisallowCache;
        private DisallowCache cnnDisallowCache;
        public UrlValidator()
        {
            bleacherDisallowCache = new DisallowCache("bleacherreport.com");
            cnnDisallowCache = new DisallowCache("www.cnn.com");
        }

        public bool IsUrlValidLoading(string loc, string lastMod, bool checkDisallow)
        {
            bool isUrlValid = false;
            Uri uri = new Uri(loc);

            bool isUriAllowed = true;
            if (uriIsBleacher(uri))
            {
                isUrlValid = loc.Contains("nba");
                if (checkDisallow) { isUriAllowed = bleacherDisallowCache.isUrlAllowed(uri); }
                
            } else if (uriIsCnn(uri))
            {
                isUrlValid = isUriRecent(loc, lastMod);
                if (checkDisallow) { isUriAllowed = cnnDisallowCache.isUrlAllowed(uri); }
            }
            return isUrlValid && isUriAllowed;
        }

        public bool IsUrlValidCrawling(string url)
        {
            // skip relative urls
            if (!url.Contains("http")) // skip relative urls like "/page.html" 
            {
                return false;
            }
            Uri uri = new Uri(url);

            // check domain, then disallow cache
            if (uriIsBleacher(uri))
            {
                if (!bleacherDisallowCache.isUrlAllowed(uri)) { return false; }
            } else if (uriIsCnn(uri))
            {
                if (!cnnDisallowCache.isUrlAllowed(uri)) { return false; }
            } else { return false; }

            // check if is html
            string[] segments = uri.Segments;
            return uri.Segments.Length > 1; // check disallow and check if it has a folder by checking segment length
        }

        private static bool uriIsBleacher(Uri uri) { return uri.Host.Contains("bleacherreport"); }
        private static bool uriIsCnn(Uri uri) { return uri.Host.Contains("cnn"); }

        private bool checkUriDomain(Uri uri)
        {
            return uri.Host.Contains("cnn") || uri.Host.Contains("bleacherreport");
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
