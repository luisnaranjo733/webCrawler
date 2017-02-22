using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerRole1.interfaces
{
    interface IUrlValidator
    {
        bool IsUriValid(string uriString, string loc, bool checkDisallow);
    }
}
