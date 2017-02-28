using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedCodeLibrary.models
{
    public class Link
    {
        private string parentUrl;
        private Uri parentUri;
        public Link(string parentUrl)
        {
            this.parentUrl = parentUrl;
            this.parentUri = new Uri(parentUrl);
        }

        public string buildUrl(string href)
        {
            return new Uri(parentUri, href).ToString();
        }
    }
}
