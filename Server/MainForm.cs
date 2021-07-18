using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server {
    public partial class MainForm : Form {
        Server server;

        public MainForm() {
            InitializeComponent();
        }

        private void MainForm_Shown(object sender, EventArgs e) {
            server = new Server(this);
            server.Start();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e) {
            server.Stop();
        }

        public void append(string str) {
            this.textBox1.Invoke((Action)delegate {
                this.textBox1.AppendText(str + Environment.NewLine);
                this.textBox1.SelectionStart = this.textBox1.Text.Length;
                this.textBox1.ScrollToCaret();
            });
        }

        public void print(string str) {
            this.textBox1.Invoke((Action)delegate {
                this.textBox1.Text = str;
            });
        }
    }
}
