using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using ResourceMonitorServer.instance;

namespace TMServerService.core {
    public partial class TMServerService : ServiceBase {
        Server server;
        public TMServerService() {
            InitializeComponent();
            eventLog = new EventLog();
            if (!EventLog.SourceExists("ServerSource")) {
                EventLog.CreateEventSource("ServerSource", "Log");
            }
            eventLog.Source = "ServerSource";
            eventLog.Log = "Log";
        }

        protected override void OnStart(string[] args) {
            eventLog.WriteEntry("TM Server Service Started");

            server = new Server();
            Console.WriteLine(server);
            server.Start();
        }

        protected override void OnStop() {
            eventLog.WriteEntry("TM Server Service Stopped");
            server.Stop();
        }
    }
}
