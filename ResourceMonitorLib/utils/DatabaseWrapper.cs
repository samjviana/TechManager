using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceMonitorLib.utils {
    public class DatabaseWrapper {
        public static class Tables {
            public const string COMPUTER = "computer";
            public const string GPU = "gpu";
            public const string MOTHERBOARD = "motherboard";
            public const string PROCESSOR = "processor";
            public const string SMARTPHONEPROCESSOR = "smartphoneprocessor";
            public const string SMARTPHONERAM = "smartphoneram";
            public const string SMARTPHONEOS = "smartphoneos";
            public const string SMARTPHONEREADINGS = "smartphonereadings";
            public const string RAM = "ram";
            public const string STORAGE = "storage";
            public const string SUPERIO = "superio";
            public const string PHYSICALMEMORY = "physicalmemory";
            public const string READINGS = "readings";
            public const string USER = "user";
            public const string OPERATINGSYSTEM = "operatingsystem";
            public const string INSTALLEDSOFTWARE = "installedsoftware";
            public const string SMARTPHONE = "smartphone";
        }

        public static class Schemas {
            public const string RESOURCEMONITOR = "resourcemonitor";
        }
    }
}
