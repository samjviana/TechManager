using ResourceMonitor.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ResourceMonitor.Client {
    public partial class MainForm : Form {
        private Communicator communicator;
        private DataCollector dataCollector;

        public MainForm() {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e) {
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e) {
            communicator.Stop();
            dataCollector.Stop();
        }

        private void MainForm_Shown(object sender, EventArgs e) {
            dataCollector = new DataCollector();
            dataCollector.Initialize();

            communicator = new Communicator(dataCollector);
            communicator.Start();
        }
    }
}
