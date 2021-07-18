using Newtonsoft.Json;
using ResourceMonitor.utils;
using ResourceMonitorAPI.dao;
using ResourceMonitorLib.models;
using ResourceMonitorLib.utils;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using Timer = System.Windows.Forms.Timer;
using System.ComponentModel;
using System.Web;
using System.Security.Cryptography;

namespace Server {
    class Server {
        private HttpListener listener;
        private Thread listenerThread;
        private MainForm form;
        private Timer statusTimer = new Timer(new Container());
        private Stopwatch stopwatch = new Stopwatch();

        public Server(MainForm form) {
            this.form = form;
            statusTimer.Interval = 60000;
            statusTimer.Tick += StatusTimer_Tick;

            try {
                this.listener = new HttpListener();
                this.listener.IgnoreWriteExceptions = true;
            }
            catch (Exception ex) {
                this.listener = null;
            }
        }

        private void StatusTimer_Tick(object sender, EventArgs e) {
            using (var context = new DatabaseContext()) {
                string query;
                query = "SELECT * FROM resourcemonitor.computer WHERE \"update\" < ";
                query += string.Format("'{0}'", DateTime.Now.AddMinutes(-30).ToString("MM-dd-yyyy HH:mm"));
                var computers = context.computer.SqlQuery(query).ToList();
                foreach (var computer in computers) {
                    context.computer.Where(c => c.uuid == computer.uuid).FirstOrDefault().status = false;
                    context.computer.Where(c => c.uuid == computer.uuid).FirstOrDefault().update = DateTime.Now;
                }

                context.SaveChanges();
            }
        }

        public Boolean Start() {
            if (PlatformNotSupported()) {
                return false;
            }

            try {
                if (this.listener.IsListening) {
                    return true;
                }

                using (var context = new DatabaseContext()) {
                    string query;
                    query = "SELECT * FROM resourcemonitor.computer WHERE \"update\" < ";
                    query += string.Format("'{0}'", DateTime.Now.AddMinutes(-30).ToString("MM-dd-yyyy HH:mm"));
                    var computers = context.computer.SqlQuery(query).ToList();
                    foreach (var computer in computers) {
                        context.computer.Where(c => c.uuid == computer.uuid).FirstOrDefault().status = false;
                        context.computer.Where(c => c.uuid == computer.uuid).FirstOrDefault().update = DateTime.Now;
                    }

                    context.SaveChanges();
                }

                statusTimer.Start();

                string prefix = "http://+:9001/";
                this.listener.Prefixes.Clear();
                this.listener.Prefixes.Add(prefix);
                this.listener.Start();

                if (this.listenerThread == null) {
                    this.listenerThread = new Thread(HandleRequests);
                    this.listenerThread.Start();
                }
            }
            catch (Exception ex) {
                return false;
            }
            return true;
        }

        public Boolean Stop() {
            if (PlatformNotSupported()) {
                return false;
            }

            try {
                this.listenerThread.Abort();
                this.listener.Stop();
                this.listenerThread = null;
            }
            catch (Exception ex) {
                return false;
            }
            return true;
        }

        public Boolean PlatformNotSupported() {
            if (this.listener == null) {
                return true;
            }
            return false;
        }

        private void HandleRequests() {
            while (this.listener.IsListening) {
                this.form.print("Listening");

                IAsyncResult context;
                context = this.listener.BeginGetContext(new AsyncCallback(ListenerCallback), this.listener);
                context.AsyncWaitHandle.WaitOne();
            }
        }

        private void ListenerCallback(IAsyncResult result) {
            HttpListener httpListener = (HttpListener)result.AsyncState;
            if (httpListener == null || !httpListener.IsListening) {
                return;
            }

            HttpListenerContext httpContext;
            try {
                httpContext = httpListener.EndGetContext(result);
            }
            catch (Exception ex) {
                return;
            }

            HttpListenerRequest httpRequest = httpContext.Request;
            string requestString = httpRequest.RawUrl.Substring(1);

            //Debug.WriteLine(requestString);
            //this.form.append(requestString + Environment.NewLine);

            if (requestString.Contains(DatabaseWrapper.Tables.COMPUTER)) {
                if (httpRequest.HttpMethod == "POST") {
                    Debug.WriteLine($"{requestString} - {DateTime.Now.ToString("HH:mm:ss:FF")}");
                }
            }

            string body = "";
            using (Stream bodyStream = httpRequest.InputStream) {
                using (StreamReader reader = new StreamReader(bodyStream, Encoding.UTF8)) {
                    body = reader.ReadToEnd();
                }
            }
            //Debug.WriteLine(body);
            /*if (requestString.StartsWith("computer") && httpRequest.HttpMethod == "POST") {
                Computer computer = JsonConvert.DeserializeObject<Computer>(body);
                Debug.WriteLine(computer);
            }*/

            string[] keys;
            try {
                keys = requestString.Split('/');
            }
            catch (Exception ex) {
                keys = new string[1];
                keys[0] = requestString;
            }

            /*using (var stringReader = new StringReader(body)) {
                using (var stringWriter = new StringWriter()) {
                    var jsonReader = new JsonTextReader(stringReader);
                    var jsonWriter = new JsonTextWriter(stringWriter) { Formatting = Formatting.Indented };
                    jsonWriter.WriteToken(jsonReader);
                    //this.form.print(stringWriter.ToString());
                }
            }*/


            string json = "{}";
            try {
                if (requestString.StartsWith(DatabaseWrapper.Tables.COMPUTER)) {
                    json = processComputerRequest(keys, body, httpRequest.HttpMethod);
                }
                if (requestString.StartsWith(DatabaseWrapper.Tables.READINGS)) {
                    json = processRequest(new ReadingsDAO(), keys, body, httpRequest.HttpMethod);
                }
                if (requestString.StartsWith(DatabaseWrapper.Tables.USER)) {
                    json = processUserRequest(keys, body, httpRequest.HttpMethod);
                }
                if (requestString.StartsWith("login")) {
                    json = processLoginRequest(keys, body, httpRequest.HttpMethod);
                }
                else if (requestString.StartsWith("register")) {
                    json = Guid.NewGuid().ToString();
                }
                else if(requestString.StartsWith("info")) {
                    json = JsonConvert.SerializeObject(new {
                        info = new {
                            name = "resource monitor server",
                            version = "0.0.1",
                        }
                    });
                }
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
                httpContext.Response.StatusCode = 500;
                json = "{\"status\":\"error\"}";
            }

            // Console.WriteLine(computer);


            SendJson(httpContext.Response, json);
        }

        private void SendJson(HttpListenerResponse response, string content) {
            if (content == null || content == "{\"status\":\"error\"}") {
                content = "{}";
                response.StatusCode = 500;
            }
            byte[] contentBytes = Encoding.UTF8.GetBytes(content);

            response.AddHeader("Cache-Control", "no-cache");
            response.AddHeader("Access-Control-Allow-Origin", "*");
            response.ContentLength64 = contentBytes.Length;
            response.ContentType = "application/json";

            try {
                Stream outputStream = response.OutputStream;
                outputStream.Write(contentBytes, 0, contentBytes.Length);
                outputStream.Close();
            }
            catch (Exception ex) {
                Console.WriteLine(ex.ToString());
                var st = new StackTrace(ex, true);
                var frame = st.GetFrame(0);
                var line = frame.GetFileLineNumber();
                Console.WriteLine(String.Format("Exception on line: {0}", line));
            }

            response.Close();
        }

        private string processLoginRequest(string[] keys, string body, string method) {
            string json = null;

            if (method == "GET") {
                List<KeyValuePair<string, string>> parameters = new List<KeyValuePair<string, string>>();
                var rawParameters = keys[0].Substring(keys[0].IndexOf('?') + 1).Split('&');
                foreach (string rawParameter in rawParameters) {
                    var splits = rawParameter.Split('=');
                    parameters.Add(new KeyValuePair<string, string>(
                        HttpUtility.UrlDecode(splits[0]),
                        HttpUtility.UrlDecode(splits[1])
                    ));
                }

                User user = new User() {
                    username = parameters.Where(p => p.Key == "username").FirstOrDefault().Value,
                    password = parameters.Where(p => p.Key == "password").FirstOrDefault().Value,
                };

                SHA256 sha256Encoder = SHA256.Create();
                byte[] bytes = sha256Encoder.ComputeHash(Encoding.UTF8.GetBytes(user.password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++) {
                    builder.Append(bytes[i].ToString("x2"));
                }
                user.password = builder.ToString();

                using (var context = new DatabaseContext()) {
                    User dbUser = context.user.Where(u => u.username == user.username).FirstOrDefault();

                    if (dbUser == null) {
                        json = "{ \"error\": 2, \"message\": \"O nome de usuário não existe\" }";
                    }
                    else if (dbUser.password != user.password) {
                        json = "{ \"error\": 3, \"message\": \"A senha está incorreta\" }";
                    }
                    else {
                        json = "{ \"error\": 0, \"message\": \"Sucesso\" }";
                    }
                }
            }

            return json;
        }

        private string processUserRequest(string[] keys, string body, string method) {
            string json = null;

            if (method == "POST") {
                User user = JsonConvert.DeserializeObject<User>(body);

                SHA256 sha256Encoder = SHA256.Create();
                byte[] bytes = sha256Encoder.ComputeHash(Encoding.UTF8.GetBytes(user.password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++) {
                    builder.Append(bytes[i].ToString("x2"));
                }
                user.password = builder.ToString();

                using (var context = new DatabaseContext()) {
                    context.user.Add(user);

                    try {
                        context.SaveChanges();

                        json = "{ \"error\": 0, \"message\": \"Sucesso\" }";
                    }
                    catch {
                        json = "{ \"error\": 1, \"message\": \"Erro Desconhecido\" }";
                    }
                }
            }

            return json;
        }

        private string processRequest<T>(DAO<T> dao, string[] keys, string body, string method) where T : class {
            string json = null;

            switch (method) {
                case "GET":
                    if (true) {
                        List<KeyValuePair<string, string>> parameters = new List<KeyValuePair<string, string>>();
                        var rawParameters = keys[1].Split('&');
                        foreach (string rawParameter in rawParameters) {
                            var splits = rawParameter.Split('=');
                            parameters.Add(new KeyValuePair<string, string>(HttpUtility.UrlDecode(splits[0]), HttpUtility.UrlDecode(splits[1])));
                        }

                        if (parameters[0].Key == "uuid") {
                            ComputerDAO computerDAO = new ComputerDAO();
                            Computer computer = computerDAO.getByUUID(Guid.Parse(parameters[0].Value), true);

                            if (parameters.Count > 1 && parameters[1].Key == "start") {
                                List<Readings> readingsList = new List<Readings>();
                                using (var context = new DatabaseContext()) {
                                    var query = string.Format(
                                        "SELECT * FROM resourcemonitor.readings WHERE computer_id = {0} AND date BETWEEN {1} AND {2}",
                                        computer.id,
                                        string.Format("'{0}'", DateTime.Now.AddMinutes(-1).ToString("MM-dd-yyyy HH:mm:ss.FF")),
                                        string.Format("'{0}'", DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss.FF"))
                                    );
                                    readingsList = context.readings.SqlQuery(query).ToList();
                                    json = JsonConvert.SerializeObject(readingsList);
                                }
                            }
                            else {
                                Readings readings;
                                using (var context = new DatabaseContext()) {
                                    var query = string.Format(
                                        "SELECT * FROM resourcemonitor.readings WHERE computer_id = {0} ORDER BY readings.date DESC LIMIT 1",
                                        computer.id
                                    );
                                    readings = context.readings.SqlQuery(query).FirstOrDefault();
                                }
                                if (readings != null) {
                                    if (keys[2] == "ram") {
                                        json = JsonConvert.SerializeObject(readings.ram);
                                    }
                                    else if (keys[2] == "cpu") {
                                        var processorsReadings = JsonConvert.DeserializeObject<List<Object>>(readings.processors);
                                        json = JsonConvert.SerializeObject(processorsReadings[Int32.Parse(keys[3])]);
                                    }
                                    else if (keys[2] == "gpu") {
                                        var gpusReadings = JsonConvert.DeserializeObject<List<Object>>(readings.gpus);
                                        json = JsonConvert.SerializeObject(gpusReadings[Int32.Parse(keys[3])]);
                                    }
                                    else if (keys[2] == "hdd") {
                                        json = JsonConvert.SerializeObject(readings.storages);
                                    }
                                    else {
                                        json = "null";
                                    }
                                }
                                else {
                                    json = "null";
                                }
                            }
                        }
                    }
                    break;
                case "POST":
                    break;
                default:
                    break;
            }

            return json;
        }

        private string processComputerRequest(string[] keys, string body, string method) {

            ComputerDAO computerDAO = new ComputerDAO();
            string json = null;

            if (method == "GET") {
                List<KeyValuePair<string, string>> parameters = new List<KeyValuePair<string, string>>();
                if (keys.Count() == 1) {
                    using (var context = new DatabaseContext()) {
                        var computers = context.computer.Include(
                            c => c.gpus
                        ).Include(
                            c => c.processors
                        ).Include(
                            c => c.ram
                        ).Include(
                            c => c.motherboard
                        ).Include(
                            c => c.storages
                        ).ToList();

                        foreach (var computer in computers) {
                            var ram = context.ram.Where(
                                r => r.uuid == computer.ram.uuid
                            ).Include(r => r.physicalMemories).FirstOrDefault();
                            var motherboard = context.motherboard.Where(
                                m => m.uuid == computer.motherboard.uuid
                            ).Include(m => m.superIO).FirstOrDefault();
                            computer.ram = ram;
                            computer.motherboard = motherboard;
                        }

                        json = JsonConvert.SerializeObject(computers);
                    }
                }
                else {
                    var rawParameters = keys[1].Split('&');
                    foreach (string rawParameter in rawParameters) {
                        var splits = rawParameter.Split('=');
                        parameters.Add(new KeyValuePair<string, string>(splits[0], splits[1]));
                    }

                    if (parameters[0].Key == "uuid") {
                        Computer computer = computerDAO.getByUUID(Guid.Parse(parameters[0].Value), true);
                        if (computer == null) {
                            json = "null";
                        }
                        else {
                            json = JsonConvert.SerializeObject(computer);
                        }
                    }
                }
            }
            else if (method == "POST") {
                Computer newComputer = JsonConvert.DeserializeObject<Computer>(body);
                if (newComputer == null) {
                    throw new Exception("Corpo da requisição nulo");
                }
                Computer computer = computerDAO.getByUUID(newComputer.uuid, true);
                if (computer != null) {
                    using (var context = new DatabaseContext()) {
                        /*context.computer.Where(
                            c => c.uuid == computer.uuid
                        ).FirstOrDefault().name = newComputer.name;
                        context.computer.Where(
                            c => c.uuid == computer.uuid
                        ).FirstOrDefault().status = newComputer.status;
                        context.computer.Where(
                            c => c.uuid == computer.uuid
                        ).FirstOrDefault().update = DateTime.Now;*/

                        // context.SaveChanges();
                        Computer __computer = context.computer.Where(
                            c => c.uuid == computer.uuid
                        ).FirstOrDefault();

                        foreach (var propertyInfo in __computer.GetType().GetProperties()) {
                            if (propertyInfo.Name != "id" && propertyInfo.Name != "uuid" && !propertyInfo.GetGetMethod().IsVirtual) {
                                var newValue = newComputer.GetType().GetProperty(propertyInfo.Name).GetValue(newComputer);
                                propertyInfo.SetValue(__computer, newValue);
                            }
                        }

                        context.computer.Where(
                            c => c.uuid == computer.uuid
                        ).FirstOrDefault().ram.total = newComputer.ram.total;
                        context.computer.Where(
                            c => c.uuid == computer.uuid
                        ).FirstOrDefault().ram.update = DateTime.Now;
                        foreach (var newPhysicalMemory in newComputer.ram.physicalMemories) {
                            var physicalMemory = context.physicalmemory.Where(s => s.uuid == newPhysicalMemory.uuid).FirstOrDefault();
                            if (physicalMemory == null) {
                                newPhysicalMemory.uuid = Guid.NewGuid();
                                context.computer.Where(
                                    c => c.uuid == computer.uuid
                                ).FirstOrDefault().ram.physicalMemories.Add(newPhysicalMemory);
                                context.Entry(newPhysicalMemory).State = EntityState.Added;
                            }
                            else {
                                physicalMemory.capacity = newPhysicalMemory.capacity;
                                physicalMemory.update = DateTime.Now;
                            }
                        }
                        if (computer.ram.physicalMemories.Count > newComputer.ram.physicalMemories.Count) {
                            List<PhysicalMemory> toRemove = new List<PhysicalMemory>(computer.ram.physicalMemories);
                            foreach (var newPhysicalMemory in newComputer.ram.physicalMemories) {
                                var physicalMemory = computer.ram.physicalMemories.Where(p => p.uuid == newPhysicalMemory.uuid).FirstOrDefault();
                                context.computer.Where(
                                    c => c.uuid == computer.uuid
                                ).FirstOrDefault().ram.physicalMemories.Remove(physicalMemory);
                                context.Entry(physicalMemory).State = EntityState.Deleted;
                            }
                        }

                        //context.SaveChanges();

                        context.computer.Where(
                            c => c.uuid == computer.uuid
                        ).FirstOrDefault().motherboard.name = newComputer.motherboard.name;
                        context.superio.Where(
                            s => s.uuid == computer.motherboard.superIO.uuid
                        ).FirstOrDefault().name = newComputer.motherboard.superIO.name;
                        context.superio.Where(
                            s => s.uuid == computer.motherboard.superIO.uuid
                        ).FirstOrDefault().update = DateTime.Now;

                        //context.SaveChanges();

                        foreach (var newGpu in newComputer.gpus) {
                            var gpu = context.gpu.Where(g => g.uuid == newGpu.uuid).FirstOrDefault();
                            if (gpu == null) {
                                newGpu.uuid = Guid.NewGuid();
                                context.computer.Where(
                                    c => c.uuid == computer.uuid
                                ).FirstOrDefault().gpus.Add(newGpu);
                                context.Entry(newGpu).State = EntityState.Added;
                            }
                            else {
                                gpu.coreClock = newGpu.coreClock;
                                gpu.memoryClock = newGpu.memoryClock;
                                gpu.name = newGpu.name;
                                gpu.number = newGpu.number;
                                gpu.power = newGpu.power;
                                gpu.temperature = newGpu.temperature;
                                gpu.update = DateTime.Now;
                            }
                        }
                        if (computer.gpus.Count > newComputer.gpus.Count) {
                            List<GPU> toRemove = new List<GPU>(computer.gpus);
                            foreach (var newGpu in newComputer.gpus) {
                                var gpu = computer.gpus.Where(g => g.uuid == newGpu.uuid).FirstOrDefault();
                                if (gpu != null) {
                                    toRemove.Remove(gpu);
                                }
                            }
                            foreach (var gpuToRemove in toRemove) {
                                var gpu = context.gpu.Where(g => g.uuid == gpuToRemove.uuid).FirstOrDefault();
                                context.computer.Where(
                                    c => c.uuid == computer.uuid
                                ).FirstOrDefault().gpus.Remove(gpu);
                                context.Entry(gpu).State = EntityState.Deleted;
                            }
                        }

                        //context.SaveChanges();

                        foreach (var newProcessor in newComputer.processors) {
                            var processor = context.processor.Where(p => p.uuid == newProcessor.uuid).FirstOrDefault();
                            if (newProcessor == null) {
                                processor.uuid = Guid.NewGuid();
                                context.computer.Where(
                                    c => c.uuid == computer.uuid
                                ).FirstOrDefault().processors.Add(newProcessor);
                                context.Entry(newProcessor).State = EntityState.Added;
                            }
                            else {
                                processor.clock = newProcessor.clock;
                                processor.cores = newProcessor.cores;
                                processor.name = newProcessor.name;
                                processor.number = newProcessor.number;
                                processor.power = newProcessor.power;
                                processor.temperature = newProcessor.temperature;
                                processor.update = DateTime.Now;
                            }
                        }
                        if (computer.processors.Count > newComputer.processors.Count) {
                            List<Processor> toRemove = new List<Processor>(computer.processors);
                            foreach (var newProcessor in newComputer.processors) {
                                var processor = computer.processors.Where(p => p.uuid == newProcessor.uuid).FirstOrDefault();
                                context.computer.Where(
                                    c => c.uuid == computer.uuid
                                ).FirstOrDefault().processors.Remove(processor);
                                context.Entry(processor).State = EntityState.Deleted;
                            }
                        }

                        //context.SaveChanges();

                        foreach (var newStorage in newComputer.storages) {
                            var storage = context.storage.Where(s => s.uuid == newStorage.uuid).FirstOrDefault();
                            if (storage == null) {
                                newStorage.uuid = Guid.NewGuid();
                                context.computer.Where(
                                    c => c.uuid == computer.uuid
                                ).FirstOrDefault().storages.Add(newStorage);
                                context.Entry(newStorage).State = EntityState.Added;
                            }
                            else {
                                storage.disks = newStorage.disks;
                                storage.index = newStorage.index;
                                storage.name = newStorage.name;
                                storage.size = newStorage.size;
                                storage.update = DateTime.Now;
                                storage.read = newStorage.read;
                                storage.write = newStorage.write;
                            }
                        }

                        if (computer.storages.Count > newComputer.storages.Count) {
                            List<Storage> toRemove = new List<Storage>(computer.storages);
                            foreach (var newStorage in newComputer.storages) {
                                var storage = computer.storages.Where(p => p.uuid == newStorage.uuid).FirstOrDefault();
                                if (storage != null) {
                                    toRemove.Remove(storage);
                                }
                            }
                            foreach (var storageToRemove in toRemove) {
                                var storage = context.storage.Where(
                                    s => s.uuid == storageToRemove.uuid
                                ).FirstOrDefault();
                                context.Entry(storage).State = EntityState.Deleted;
                            }
                        }

                        //context.SaveChanges();

                        List<object> gpuReadings = new List<object>();
                        foreach (var gpu in newComputer.gpus) {
                            gpuReadings.Add(new {
                                uuid = gpu.uuid,
                                readings = gpu.sensors
                            });
                        }
                        List<object> processorReadings = new List<object>();
                        foreach (var processor in newComputer.processors) {
                            processorReadings.Add(new {
                                uuid = processor.uuid,
                                readings = processor.sensors
                            });
                        }
                        List<object> storageReadings = new List<object>();
                        foreach (var storage in newComputer.storages) {
                            storageReadings.Add(new {
                                uuid = storage.uuid,
                                readings = storage.sensors
                            });
                        }
                        object ramReadings = new {
                            uuid = newComputer.ram.uuid,
                            readings = newComputer.ram.sensors
                        };
                        stopwatch.Start();
                        if (((dynamic)ramReadings).readings != null) {
                            Readings readings = new Readings() {
                                computer = context.computer.Where(c => c.uuid == computer.uuid).FirstOrDefault(),
                                gpus = JsonConvert.SerializeObject(gpuReadings),
                                date = DateTime.Now,
                                processors = JsonConvert.SerializeObject(processorReadings),
                                ram = JsonConvert.SerializeObject(ramReadings),
                                storages = JsonConvert.SerializeObject(storageReadings),
                            };

                            context.readings.Add(readings);

                            context.SaveChanges();
                        }
                        stopwatch.Stop();
                        //Debug.WriteLine(((double)stopwatch.ElapsedTicks).ToString() + " ticks");
                        stopwatch.Reset();
                    }

                    Computer _computer = computerDAO.getByUUID(computer.uuid, true);

                    using (var context = new DatabaseContext()) {
                        var motherboard = context.motherboard.Include("superIO").Where(
                            m => m.uuid == computer.motherboard.uuid
                        ).FirstOrDefault();
                        _computer.motherboard = motherboard;
                        var ram = context.ram.Include("physicalMemories").Where(
                            r => r.uuid == computer.ram.uuid
                        ).FirstOrDefault();
                        _computer.ram = ram;
                    }

                    json = JsonConvert.SerializeObject(_computer);
                }
                else {
                    Computer _computer = newComputer;
                    _computer.uuid = Guid.NewGuid();
                    foreach (var _gpu in _computer.gpus) {
                        _gpu.uuid = Guid.NewGuid();
                    }
                    foreach (var _storage in _computer.storages) {
                        _storage.uuid = Guid.NewGuid();
                    }
                    foreach (var _processor in _computer.processors) {
                        _processor.uuid = Guid.NewGuid();
                    }
                    _computer.motherboard.uuid = Guid.NewGuid();
                    _computer.motherboard.superIO.uuid = Guid.NewGuid();
                    _computer.ram.uuid = Guid.NewGuid();
                    foreach (var _physicalmemory in _computer.ram.physicalMemories) {
                        _physicalmemory.uuid = Guid.NewGuid();
                    }
                    Computer response = computerDAO.add((dynamic)_computer);
                    json = JsonConvert.SerializeObject(response);
                }
            }

            return json;
        }
    }
}
