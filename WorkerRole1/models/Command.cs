﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedCodeLibrary.models
{
    public class Command
    {
        public const string QUEUE_COMMAND = "command";
        public const string COMMAND_LOAD = "load";
        public const string COMMAND_CRAWL = "crawl";
        public const string COMMAND_IDLE = "idle";
    }
}
