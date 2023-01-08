using Newtonsoft.Json;
using ResourceMonitorLib.models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ResourceMonitor.Client {
    class Communicator {
        private Thread communicatorThread;
        private bool isRunning;
        private DataCollector dataCollector;
        private bool firstRun = true;
        private string server = "http://localhost:9001";
        //private string server = "http://52.67.242.62:9001";

        public Communicator(DataCollector dataCollector) {
            this.dataCollector = dataCollector;
            this.isRunning = false;
        }

        public Boolean Start() {
            int tryCount = 100;
            bool connected = false;

            /*while (tryCount-- > 0) {
                if (InitCallback()) {
                    connected = true;
                    break;
                }
            }
            if (!connected) {
                MessageBox.Show("Não foi possível se comunicar com o servidor");
                return false;
            }*/

            //MessageBox.Show("INICIAR");

            if (File.Exists(Environment.MachineName + ".config")) {
                string fileName = Environment.MachineName + ".config";
                string fileContent = File.ReadAllText(fileName);
                Computer localhost = JsonConvert.DeserializeObject<Computer>(fileContent);
                if (localhost == null || localhost.name != Environment.MachineName) {
                    File.Delete(Environment.MachineName + ".config");
                    FirstRequestCallback();
                    dataCollector.Initialize();
                }
                else {
                    GetComputerCallback(localhost.uuid);
                }
            }
            else {
                FirstRequestCallback();
            }

            while (!dataCollector.initialized) {
                Console.WriteLine("carregando");
            }

            //MessageBox.Show("CONTINUAR");

            this.dataCollector.Start();

            try {
                this.isRunning = true;
                if (this.communicatorThread == null) {
                    this.communicatorThread = new Thread(CommunicatorCallback);
                    this.communicatorThread.Start();
                }
            }
            catch (Exception ex) {
                return false;
            }
            return true;
        }

        public Boolean Stop() {
            try {
                this.isRunning = false;
                this.communicatorThread.Abort();
                this.communicatorThread = null;
            }
            catch (Exception ex) {
                return false;
            }
            return true;
        }

        private void CommunicatorCallback() {
            while (this.isRunning) {
                IAsyncResult context;
                AsyncCallback asyncCallback = new AsyncCallback(RequestCallback);
                context = asyncCallback.BeginInvoke(null, RequestCallback, null);
                context.AsyncWaitHandle.WaitOne();
                Thread.Sleep(2000);
            }
        }

        private void RequestCallback(IAsyncResult result) {
            WebRequest webRequest = WebRequest.Create($@"{server}/computer");
            string json = dataCollector.JsonData;
            /*Computer computer = JsonConvert.DeserializeObject<Computer>(json);
            if (computer != null) {
                Debug.WriteLine(JsonConvert.SerializeObject(computer.ram.sensors));
            }*/
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            webRequest.Method = "POST";
            webRequest.ContentType = "application/json";
            webRequest.ContentLength = buffer.Length;

            Debug.WriteLine($"{Thread.CurrentThread} - {DateTime.Now.ToString("HH:mm:ss:FF")}");
            try {
                Stream stream = webRequest.GetRequestStream();
                stream.Write(buffer, 0, buffer.Length);
                stream.Close();

                HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();

                if (response.StatusCode == HttpStatusCode.OK) {
                    string data = string.Empty;
                    StreamReader streamReader = new StreamReader(response.GetResponseStream());
                    data = streamReader.ReadToEnd();

                    Config.localhost = JsonConvert.DeserializeObject<Computer>(data);
                    //dataCollector.JsonData = data;
                    File.WriteAllText(Environment.MachineName + ".config", JsonConvert.SerializeObject(Config.localhost));

                    streamReader.Close();
                    streamReader.Dispose();
                }

                webRequest.Abort();
                response.Close();
            }
            catch (Exception ex) {

            }
        }

        private void FirstRequestCallback() {
            WebRequest webRequest = WebRequest.Create($@"{server}/computer");
            string json = dataCollector.JsonData;
            /*Computer computer = JsonConvert.DeserializeObject<Computer>(json);
            if (computer != null) {
                Debug.WriteLine(JsonConvert.SerializeObject(computer.ram.sensors));
            }*/
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            webRequest.Method = "POST";
            webRequest.ContentType = "application/json";
            webRequest.ContentLength = buffer.Length;
            Debug.WriteLine($"{Thread.CurrentThread} - {DateTime.Now.ToString("HH:mm:ss:FF")}");
            try {
                Stream stream = webRequest.GetRequestStream();
                stream.Write(buffer, 0, buffer.Length);
                stream.Close();

                HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();

                if (response.StatusCode == HttpStatusCode.OK) {
                    string data = string.Empty;
                    StreamReader streamReader = new StreamReader(response.GetResponseStream());
                    data = streamReader.ReadToEnd();

                    Config.localhost = JsonConvert.DeserializeObject<Computer>(data);
                    dataCollector.JsonData = data;
                    File.WriteAllText(Environment.MachineName + ".config", JsonConvert.SerializeObject(Config.localhost));

                    streamReader.Close();
                    streamReader.Dispose();
                }

                webRequest.Abort();
                response.Close();
            }
            catch (Exception ex) {

            }
        }

        private bool InitCallback() {
            bool result = false;

            WebRequest webRequest = WebRequest.Create($@"{server}/info");
            byte[] buffer = Encoding.UTF8.GetBytes("{}");
            webRequest.Method = "GET";
            try {

                HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();

                if (response.StatusCode == HttpStatusCode.OK) {
                    string data = string.Empty;
                    StreamReader streamReader = new StreamReader(response.GetResponseStream());
                    data = streamReader.ReadToEnd();

                    streamReader.Close();
                    streamReader.Dispose();

                    result = true;
                }
                else {
                    result = false;
                }

                webRequest.Abort();
                response.Close();
            }
            catch (Exception ex) {
                result = false;
            }
            return result;
        }

        public bool GetComputerCallback(Guid uuid) {
            bool result = false;

            WebRequest webRequest = WebRequest.Create($@"{server}/computer/uuid=" + uuid.ToString());
            byte[] buffer = Encoding.UTF8.GetBytes("");
            webRequest.Method = "GET";
            try {
                HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();

                if (response.StatusCode == HttpStatusCode.OK) {
                    string data = string.Empty;
                    StreamReader streamReader = new StreamReader(response.GetResponseStream());
                    data = streamReader.ReadToEnd();

                    Config.localhost = JsonConvert.DeserializeObject<Computer>(data);
                   // dataCollector.JsonData = data;
                    File.WriteAllText(Environment.MachineName + ".config", JsonConvert.SerializeObject(Config.localhost));

                    streamReader.Close();
                    streamReader.Dispose();

                    result = true;
                }
                else {
                    result = false;
                }

                webRequest.Abort();
                response.Close();
            }
            catch (Exception ex) {
                result = false;
            }
            return result;
        }
    }
}
