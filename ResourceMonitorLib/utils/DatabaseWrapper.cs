using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceMonitor.utils {
    public class DatabaseWrapper {
        public static class Tables {
            public const string COMPUTER = "computer";
            public const string GPU = "gpu";
            public const string MOTHERBOARD = "motherboard";
            public const string PROCESSOR = "processor";
            public const string RAM = "ram";
            public const string STORAGE = "storage";
            public const string SUPERIO = "superio";
            public const string PHYSICALMEMORY = "physicalmemory";
            public const string READINGS = "readings";
            public const string USER = "user";
        }

        public static class Schemas {
            public const string RESOURCEMONITOR = "resourcemonitor";
        }
    }
}
