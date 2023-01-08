using Microsoft.Win32;
using Newtonsoft.Json;
using NvAPIWrapper.GPU;
using NvAPIWrapper.Native.GPU;
using ResourceMonitorLib.models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using OHMHardware = OpenHardwareMonitor.Hardware;

namespace ResourceMonitor.Client {
    class DataCollector {
        private Thread dataCollectorThread;
        private bool isRunning;
        private Computer computer;
        private PhysicalGPU[] nvidiaGpus;
        private OHMHardware.Computer ohmComputer;
        private ManagementScope scope;
        private List<object> readWrites = new List<object>();
        private string jsonData;
        private bool nvmlV2Error = false;
        private bool nvmlV1Error = false;
        private bool nvApiError = false;
        private Stopwatch stopwatch = new Stopwatch();
        private List<PerformanceCounter> readPerfCounters = new List<PerformanceCounter>();
        private List<PerformanceCounter> writePerfCounters = new List<PerformanceCounter>();
        private Dictionary<string, Thread> threads = new Dictionary<string, Thread>();
        public bool initialized = false;

        public DataCollector() {
            this.isRunning = false;
            this.jsonData = string.Empty;

            this.ohmComputer = new OHMHardware.Computer() {
                CPUEnabled = true,
                FanControllerEnabled = true,
                GPUEnabled = true,
                HDDEnabled = true,
                MainboardEnabled = true,
                RAMEnabled = true
            };

            try {
                this.ohmComputer.Open();
            }
            catch (EntryPointNotFoundException ex) {
                this.ohmComputer.GPUEnabled = false;
                this.ohmComputer.Open();
            }


            this.computer = new Computer();
            this.computer.operatingsystem = new ResourceMonitorLib.models.OperatingSystem();

            try {
                this.nvidiaGpus = PhysicalGPU.GetPhysicalGPUs();
            }
            catch (Exception ex) {
                Debug.WriteLine(ex);
                this.nvidiaGpus = null;
                nvApiError = true;
            }

            initNvmlv2();
            if (nvmlV2Error) {
                initNvmlv1();
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public void initNvmlv2() {
            try {
                NvmlWrapper.Nvml.nvmlInit_v2();
            }
            catch (Exception ex) {
                Debug.WriteLine("Erro ao inicializar NVML API");
                this.nvmlV2Error = true;
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public void initNvmlv1() {
            try {
                NvmlWrapper.Nvml.nvmlInit();
            }
            catch (Exception ex) {
                Debug.WriteLine("Erro ao inicializar NVML API");
                this.nvmlV1Error = true;
            }
        }

        public void Initialize() {
            threads.Add("ComputerThread", new Thread(new ThreadStart(ComputerThreadCallback)));
            threads.Add("OperatingSystemThread", new Thread(new ThreadStart(OperatingSystemThreadCallback)));

            initialized = true;
        }

        public Boolean Start() {
            /*var files = "";
            foreach (var logicDisk in DriveInfo.GetDrives()) {
                if (File.Exists(logicDisk.Name + "file16m.dat")) {
                    continue;
                }
                files += logicDisk.Name + "file16m.dat ";
            }

            Console.WriteLine("Criando arquivo para teste dos dispositivos de armazenamento");*/

            foreach (var thread in threads) {
                thread.Value.Start();
            }

            foreach (var thread in threads) {
                thread.Value.Join();
            }

            Process diskSpd = new Process();
            diskSpd.StartInfo.UseShellExecute = false;
            diskSpd.StartInfo.FileName = ".\\diskspd.exe";
            diskSpd.StartInfo.CreateNoWindow = true;
            //diskSpd.StartInfo.Arguments = "-c16M " + files;
            //diskSpd.Start();
            //diskSpd.WaitForExit();

            //Console.WriteLine("Arquivo criado");
            Console.WriteLine("Testando Leitura e Escrita");

            diskSpd.StartInfo.RedirectStandardOutput = true;
            foreach (var logicDisk in DriveInfo.GetDrives()) {
                if (!logicDisk.IsReady) {
                    continue;
                }
                diskSpd.StartInfo.Arguments = "-b1M -d2 -o8 -t1 -W0 -S -w0 -c16M " + logicDisk.Name + "file16m.dat";
                diskSpd.Start();
                string line;
                string readInfo = "";
                bool flag = false;
                while ((line = diskSpd.StandardOutput.ReadLine()) != null) {
                    if (line.StartsWith("Read IO")) {
                        flag = true;
                        continue;
                    }
                    if (line.StartsWith("Write IO")) {
                        flag = true;
                        break;
                    }
                    if (flag) {
                        if (line.StartsWith("thread") || line.StartsWith("-") || line.StartsWith("total") || line == "") {
                            continue;
                        }
                        readInfo = line.Replace(" ", ""); ;
                    }
                }
                diskSpd.WaitForExit();

                diskSpd.StartInfo.Arguments = "-b1M -d2 -o8 -t1 -W0 -S -w100 -c16M " + logicDisk.Name + "file16m.dat";
                diskSpd.Start();
                string writeInfo = "";
                flag = false;
                while ((line = diskSpd.StandardOutput.ReadLine()) != null) {
                    if (line.StartsWith("Write IO")) {
                        flag = true;
                        continue;
                    }
                    if (flag) {
                        if (line.StartsWith("thread") || line.StartsWith("-") || line.StartsWith("total") || line == "") {
                            continue;
                        }
                        writeInfo = line.Replace(" ", "");
                    }
                }
                diskSpd.WaitForExit();

                double read = 0; 
                double write = 0;  

                if (readInfo == "") {
                    read = -1;
                }
                else {
                    read = Double.Parse(readInfo.Split('|')[3], CultureInfo.CreateSpecificCulture("en-US")) * 1.048576;
                }
                if (writeInfo == "") {
                    read = -1;
                }
                else {
                    write = Double.Parse(writeInfo.Split('|')[3], CultureInfo.CreateSpecificCulture("en-US")) * 1.048576;
                }

                readWrites.Add(new {
                    disk = logicDisk.Name,
                    read = read,
                    write = write
                });
            }

            foreach (var logicDisk in DriveInfo.GetDrives()) {
                if (File.Exists(logicDisk.Name + "file16m.dat")) {
                    File.Delete(logicDisk.Name + "file16m.dat");
                }
            }

            this.computer = Config.localhost;

            try {
                scope = new ManagementScope("\\\\localhost\\root\\cimv2");

                this.isRunning = true;
                if (this.dataCollectorThread == null) {
                    this.dataCollectorThread = new Thread(DataCollectorCallback);
                    this.dataCollectorThread.Start();
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
                this.dataCollectorThread.Abort();
                this.dataCollectorThread = null;
            }
            catch (Exception ex) {
                return false;
            }

            NvmlWrapper.Nvml.nvmlShutdown();

            return true;
        }

        private string GetStringOfWmi(string wmiClass, string property, int value) {
            ManagementPath managementPath = new ManagementPath(wmiClass);
            ObjectGetOptions options = new ObjectGetOptions(null, System.TimeSpan.MaxValue, true);
            ManagementClass managementClass = new ManagementClass(managementPath, options);
            var propertyData = managementClass.Properties[property];
            var values = propertyData.Qualifiers["Values"].Value as string[];

            string wmiString = values[value];
            /*Encoding unicode = Encoding.Unicode;
            Encoding utf8 = Encoding.UTF8;
            byte[] unicodeBytes = unicode.GetBytes(values[value]);
            byte[] utf8Bytes = Encoding.Convert(unicode, utf8, unicodeBytes);
            wmiString = utf8.GetString(utf8Bytes);*/

            /*var encondings = Encoding.GetEncodings();
            foreach (var encoding in encondings) {
                Encoding utf8 = Encoding.UTF8;
                byte[] encodingBytes = encoding.GetEncoding().GetBytes(wmiString);
                byte[] utf8Bytes = Encoding.Convert(encoding.GetEncoding(), utf8, encodingBytes);
                string msg = utf8.GetString(utf8Bytes);
                File.AppendAllText("encodings.txt", encoding.Name + " || " + msg + "\r\n");
            }
            File.AppendAllText("encodings.txt", "----------------------------------------------------------");*/

            return wmiString;
        }

        private void ComputerThreadCallback() {
            var searcher = new ManagementObjectSearcher("SELECT " +
                "DomainRole," +
                "Domain," +
                "DNSHostName," +
                "Workgroup," +
                "PartOfDomain," +
                "UserName, " +
                "PCSystemType," +
                "Manufacturer," +
                "Model," +
                "PowerState," +
                "PrimaryOwnerContact," +
                "PrimaryOwnerName," +
                "SupportContactDescription," +
                "SystemType," +
                "ThermalState " +
                "FROM Win32_ComputerSystem");
            var managementObjects = searcher.Get();

            foreach (var computerSystem in managementObjects) {
                computer.partOfDomain = Convert.ToBoolean(computerSystem.Properties["PartOfDomain"].Value);
                if (computer.partOfDomain) {
                    computer.domain = computerSystem.Properties["Domain"].Value.ToString();
                }
                else {
                    computer.workGroup = computerSystem.Properties["Workgroup"].Value.ToString();
                }
                int domainRole = Convert.ToInt32(computerSystem.Properties["DomainRole"].Value);
                computer.domainRole = GetStringOfWmi("Win32_ComputerSystem", "DomainRole", domainRole);
                computer.dnsName = Convert.ToString(computerSystem.Properties["DNSHostName"].Value);
                computer.currentUser = Convert.ToString(computerSystem.Properties["UserName"].Value);
                int pcSystemType = Convert.ToInt32(computerSystem.Properties["PCSystemType"].Value);
                computer.computerType = GetStringOfWmi("Win32_ComputerSystem", "PCSystemType", pcSystemType);
                computer.manufacturer = Convert.ToString(computerSystem.Properties["Manufacturer"].Value);
                computer.model = Convert.ToString(computerSystem.Properties["Model"].Value);
                int powerState = Convert.ToInt32(computerSystem.Properties["PowerState"].Value);
                computer.powerState = GetStringOfWmi("Win32_ComputerSystem", "PowerState", powerState);
                computer.ownerContact = Convert.ToString(computerSystem.Properties["PrimaryOwnerContact"].Value);
                computer.ownerName = Convert.ToString(computerSystem.Properties["PrimaryOwnerName"].Value);
                computer.supportContact = Convert.ToString(computerSystem.Properties["SupportContactDescription"].Value);
                computer.systemType = Convert.ToString(computerSystem.Properties["SystemType"].Value);
                int thermalState = Convert.ToInt32(computerSystem.Properties["ThermalState"].Value);
                computer.thermalState = GetStringOfWmi("Win32_ComputerSystem", "ThermalState", thermalState);
            }

            /*ManagementPath a = new ManagementPath("Win32_ComputerSystem");
            ObjectGetOptions b= new ObjectGetOptions(null, System.TimeSpan.MaxValue, true);
            ManagementClass c = new ManagementClass(a, b);
            var d = c.Properties["DomainRole"];
            var e = d.Value;*/

            /*var b = a.Properties;
            var c = b.Cast<PropertyData>().ToList();
            var d = c.Where(e => e.Name == "DomainRole");
            var f = d.FirstOrDefault().Qualifiers;
            var g = f.Cast<QualifierData>().ToList();
            var h = g.Where(i => i.Name == "MappingStrings");*/

            //Console.WriteLine(computer);
            /*ObjectQuery query = new ObjectQuery("SELECT DomainRole FROM Win32_ComputerSystem");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(this.scope, query);
            ManagementObjectCollection collection = searcher.Get();

            foreach (ManagementObject wmiObject in collection) {
                var a = wmiObject["DomainRole"];
                Console.WriteLine(a);
            }*/
        }

        private void DataCollectorCallback() {
            while (this.isRunning) {
                /*IAsyncResult context;
                AsyncCallback asyncCallback = new AsyncCallback(ComputerDataLoader);
                context = asyncCallback.BeginInvoke(null, ComputerDataLoader, null);
                context.AsyncWaitHandle.WaitOne();*/
                if (computer == null) {
                    computer = new Computer();
                    computer.operatingsystem = new ResourceMonitorLib.models.OperatingSystem();
                }
                computer.name = Environment.MachineName;
                computer.status = true;

                Thread computerThread = new Thread(new ThreadStart(ComputerThreadCallback));
                Thread operatingSystemThread = new Thread(new ThreadStart(OperatingSystemThreadCallback));
                Thread ohmThread = new Thread(new ThreadStart(OhmThreadCallback));
                Thread cpuThread = new Thread(new ThreadStart(CpuThreadCallback));
                Thread ramThread = new Thread(new ThreadStart(RamThreadCallback));
                Thread gpuThread = new Thread(new ThreadStart(GpuThreadCallback));
                Thread storageThread = new Thread(new ThreadStart(StorageThreadCallback));

                computerThread.Start();
                operatingSystemThread.Start();
                ohmThread.Start();
                cpuThread.Start();
                ramThread.Start();
                gpuThread.Start();
                storageThread.Start();

                computerThread.Join();
                operatingSystemThread.Join();
                ohmThread.Join();
                cpuThread.Join();
                ramThread.Join();
                gpuThread.Join();
                storageThread.Join();

                Config.computer = computer;

                this.jsonData = JsonConvert.SerializeObject(Config.computer);

                var pcCategories = PerformanceCounterCategory.GetCategories();
                // implement solution to invalid index exception
                // LODCTR /r
                var physicalDiskPCC = new PerformanceCounterCategory("PhysicalDisk");
                var instanceNames = new List<string>();
                foreach (var instanceName in physicalDiskPCC.GetInstanceNames()) {
                    if (instanceName != "_Total") {
                        instanceNames.Add(instanceName);
                    }
                }
                readPerfCounters = new List<PerformanceCounter>();
                writePerfCounters = new List<PerformanceCounter>();
                foreach (var instanceName in instanceNames) {
                    readPerfCounters.Add(new PerformanceCounter() {
                        CategoryName = "PhysicalDisk",
                        InstanceName = instanceName,
                        CounterName = "Disk Read Bytes/sec"
                    });
                    writePerfCounters.Add(new PerformanceCounter() {
                        CategoryName = "PhysicalDisk",
                        InstanceName = instanceName,
                        CounterName = "Disk Write Bytes/sec"
                    });
                }
                stopwatch.Start();
                foreach (PerformanceCounter performanceCounter in readPerfCounters) {
                    performanceCounter.NextValue();
                }
                foreach (PerformanceCounter performanceCounter in writePerfCounters) {
                    performanceCounter.NextValue();
                }

                Thread.Sleep(500);
            }
        }

        private void OperatingSystemThreadCallback() {
            var searcher = new ManagementObjectSearcher("SELECT " +
                "Caption," +
                "Version," +
                "BuildNumber," +
                "Manufacturer," +
                "OSArchitecture," +
                "SerialNumber, " +
                "Status," +
                "InstallDate," +
                "MUILanguages," +
                "CountryCode," +
                "CodeSet," +
                "BootDevice," +
                "SystemDrive," +
                "WindowsDirectory " +
                "FROM Win32_OperatingSystem");
            var managementObjects = searcher.Get();

            computer.operatingsystem = new ResourceMonitorLib.models.OperatingSystem();
            foreach (var operatingSystem in managementObjects) {
                computer.operatingsystem.name = operatingSystem.Properties["Caption"].Value.ToString();
                computer.operatingsystem.version = operatingSystem.Properties["Version"].Value.ToString();
                computer.operatingsystem.build = operatingSystem.Properties["BuildNumber"].Value.ToString();
                computer.operatingsystem.manufacturer = operatingSystem.Properties["Manufacturer"].Value.ToString();
                computer.operatingsystem.architecture = operatingSystem.Properties["OSArchitecture"].Value.ToString();
                computer.operatingsystem.serialNumber = operatingSystem.Properties["SerialNumber"].Value.ToString();
                computer.operatingsystem.serialKey = getSerialKey();
                computer.operatingsystem.status = operatingSystem.Properties["Status"].Value.ToString();
                computer.operatingsystem.installDate = ManagementDateTimeConverter.ToDateTime(operatingSystem.Properties["InstallDate"].Value.ToString());
                computer.operatingsystem.language = operatingSystem.Properties["MUILanguages"].Value.ToString();
                computer.operatingsystem.language = operatingSystem.Properties["CountryCode"].Value.ToString();
                computer.operatingsystem.codePage = Convert.ToInt32(operatingSystem.Properties["CodeSet"].Value.ToString());
                computer.operatingsystem.systemPartition = operatingSystem.Properties["SystemDrive"].Value.ToString();
                computer.operatingsystem.bootDevice = operatingSystem.Properties["BootDevice"].Value.ToString();
                computer.operatingsystem.installPath = operatingSystem.Properties["WindowsDirectory"].Value.ToString();
            }
        }

        private string getSerialKey() {
            byte[] id = null;
            var regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion");

            if (regKey != null) id = regKey.GetValue("DigitalProductId") as byte[];
            if (id == null) {
                return "";
            }

            //first byte offset
            const int start = 52;
            //last byte offset
            const int end = start + 15;
            //decoded key length
            const int length = 29;
            //decoded key in byte form
            const int decoded = 15;
            //char[] for holding the decoded product key
            var decodedKey = new char[length];
            //List<byte> to hold the key bytes
            var keyHex = new List<byte>();
            //list to hold possible alpha-numeric characters
            //that may be in the product key
            var charsInKey = new List<char>() {
                'B', 'C', 'D', 'F', 'G', 'H',
                'J', 'K', 'M', 'P', 'Q', 'R',
                'T', 'V', 'W', 'X', 'Y', '2',
                '3', '4', '6', '7', '8', '9'
            };

            //add all bytes to our list
            for (var i = start; i <= end; i++) {
                keyHex.Add(id[i]);
            }

            //now the decoding starts
            for (var i = length - 1; i >= 0; i--) {
                switch ((i + 1) % 6) {
                    //if the calculation is 0 (zero) then add the seperator
                    case 0:
                        decodedKey[i] = '-';
                        break;
                    default:
                        var idx = 0;
                        for (var j = decoded - 1; j >= 0; j--) {
                            var @value = (idx << 8) | keyHex[j];
                            keyHex[j] = (byte)(@value / 24);
                            idx = @value % 24;
                            decodedKey[i] = charsInKey[idx];
                        }
                        break;
                }
            }

            return new string(decodedKey);
        }

        private void OhmThreadCallback() {
            foreach (var hardware in ohmComputer.Hardware) {
                hardware.Update();
                if (hardware.HardwareType == OHMHardware.HardwareType.CPU) {
                    int cpuIdx = GetProcessorNumber(hardware);
                    try {
                        var trash = computer.processors.ElementAt(cpuIdx);
                    }
                    catch (Exception ex) {
                        computer.processors.Add(new Processor());
                    }
                    computer.processors.ElementAt(cpuIdx).name = hardware.Name;
                    computer.processors.ElementAt(cpuIdx).cores = GetProcessorCoreCount(hardware);
                    computer.processors.ElementAt(cpuIdx).clock = GetProcessorClock();
                    computer.processors.ElementAt(cpuIdx).number = GetProcessorNumber(hardware);
                    computer.processors.ElementAt(cpuIdx).power = GetProcessorPower();
                    computer.processors.ElementAt(cpuIdx).temperature = GetProcessorTemperature();

                    if (hardware.Sensors.Length > 0) {
                        if (computer.processors.ElementAt(cpuIdx).sensors == null) {
                            computer.processors.ElementAt(cpuIdx).sensors = new Processor.Sensors();
                        }
                        computer.processors.ElementAt(cpuIdx).sensors.clock = 0;
                        foreach (var sensor in hardware.Sensors) {
                            if (sensor.SensorType == OHMHardware.SensorType.Temperature && sensor.Name == "CPU Package") {
                                computer.processors.ElementAt(cpuIdx).sensors.temperature = sensor.Value.GetValueOrDefault();
                            }
                            else if (sensor.SensorType == OHMHardware.SensorType.Clock && sensor.Name.StartsWith("CPU Core #")) {
                                computer.processors.ElementAt(cpuIdx).sensors.clock += sensor.Value.GetValueOrDefault();
                            }
                            else if (sensor.SensorType == OHMHardware.SensorType.Power && sensor.Name == "CPU Package") {
                                computer.processors.ElementAt(cpuIdx).sensors.power = sensor.Value.GetValueOrDefault();
                            }
                            else if (sensor.SensorType == OHMHardware.SensorType.Load && sensor.Name == "CPU Total") {
                                computer.processors.ElementAt(cpuIdx).sensors.load = sensor.Value.GetValueOrDefault();
                            }
                        }
                        computer.processors.ElementAt(cpuIdx).sensors.clock /= computer.processors.ElementAt(cpuIdx).cores;
                    }
                }
                else if (hardware.HardwareType == OHMHardware.HardwareType.RAM) {
                    if (hardware.Sensors.Length > 0) {
                        if (computer.ram.sensors == null) {
                            computer.ram.sensors = new RAM.Sensors();
                        }
                        foreach (var sensor in hardware.Sensors) {
                            if (sensor.SensorType == OHMHardware.SensorType.Load) {
                                computer.ram.sensors.load = sensor.Value.GetValueOrDefault();
                            }
                            else if (sensor.SensorType == OHMHardware.SensorType.Data && sensor.Name == "Used Memory") {
                                computer.ram.sensors.used = sensor.Value.GetValueOrDefault();
                            }
                            else if (sensor.SensorType == OHMHardware.SensorType.Data && sensor.Name == "Available Memory") {
                                computer.ram.sensors.free = sensor.Value.GetValueOrDefault();
                            }
                        }
                    }
                }
                else if (hardware.HardwareType == OHMHardware.HardwareType.Mainboard) {
                    computer.motherboard.name = hardware.Name;

                    if (hardware.SubHardware.Length > 0) {
                        foreach (var subHardware in hardware.SubHardware) {
                            subHardware.Update();
                            if (subHardware.HardwareType == OHMHardware.HardwareType.SuperIO) {
                                computer.motherboard.superIO.name = subHardware.Name;
                            }
                        }
                    }
                }
            }
        }

        private void CpuThreadCallback() {

        }

        private void RamThreadCallback() {
            int ramCount = GetRAMCount();
            double totalPhysicalRam = 0;
            for (int i = 0; i < ramCount; i++) {
                int ramIdx = i;
                try {
                    var trash = computer.ram.physicalMemories.ElementAt(ramIdx);
                }
                catch (Exception ex) {
                    computer.ram.physicalMemories.Add(new PhysicalMemory());
                }
                computer.ram.physicalMemories.ElementAt(ramIdx).capacity = GetPhysicalRAMCapacity(i);
                totalPhysicalRam += computer.ram.physicalMemories.ElementAt(ramIdx).capacity;
            }
            computer.ram.total = totalPhysicalRam / 1024;
        }

        private void GpuThreadCallback() {
            if (!nvmlV2Error) {
                uint nvidiaGpuCount;
                NvmlWrapper.Nvml.nvmlDeviceGetCount_v2(out nvidiaGpuCount);
                for (uint i = 0; i < nvidiaGpuCount; i++) {
                    IntPtr nvmlDevice;
                    StringBuilder nvmlName = new StringBuilder();
                    uint nvmlMaxTemperature;
                    uint nvmlMaxCoreClock;
                    uint nvmlMaxMemoryClock;
                    uint nvmlMinPower;
                    uint nvmlMaxPower;
                    uint nvmlCurrentTemperature;
                    uint nvmlCurrentCoreClock;
                    uint nvmlCurrentMemoryClock;
                    uint nvmlCurrentPower;
                    NvmlWrapper.nvmlUtilization_t nvmlCurrentUtilization;
                    uint nvmlCurrentLoad;
                    int gpuIdx = (int)i;

                    NvmlWrapper.nvmlReturn_t deviceReturn = NvmlWrapper.Nvml.nvmlDeviceGetHandleByIndex_v2(i, out nvmlDevice);
                    NvmlWrapper.nvmlReturn_t nameReturn = NvmlWrapper.Nvml.nvmlDeviceGetName(nvmlDevice, nvmlName, 64);
                    NvmlWrapper.nvmlReturn_t maxTemperatureReturn = NvmlWrapper.Nvml.nvmlDeviceGetTemperatureThreshold(nvmlDevice, NvmlWrapper.nvmlTemperatureThresholds_t.NVML_TEMPERATURE_THRESHOLD_GPU_MAX, out nvmlMaxTemperature);
                    NvmlWrapper.nvmlReturn_t maxCoreClockReturn = NvmlWrapper.Nvml.nvmlDeviceGetMaxClockInfo(nvmlDevice, NvmlWrapper.nvmlClockType_t.NVML_CLOCK_GRAPHICS, out nvmlMaxCoreClock);
                    NvmlWrapper.nvmlReturn_t maxMemoryClockReturn = NvmlWrapper.Nvml.nvmlDeviceGetMaxClockInfo(nvmlDevice, NvmlWrapper.nvmlClockType_t.NVML_CLOCK_MEM, out nvmlMaxMemoryClock);
                    NvmlWrapper.nvmlReturn_t maxPowerReturn = NvmlWrapper.Nvml.nvmlDeviceGetPowerManagementLimitConstraints(nvmlDevice, out nvmlMinPower, out nvmlMaxPower);
                    NvmlWrapper.nvmlReturn_t temperatureReturn = NvmlWrapper.Nvml.nvmlDeviceGetTemperature(nvmlDevice, NvmlWrapper.nvmlTemperatureSensors_t.NVML_TEMPERATURE_GPU, out nvmlCurrentTemperature);
                    NvmlWrapper.nvmlReturn_t loadReturn = NvmlWrapper.Nvml.nvmlDeviceGetUtilizationRates(nvmlDevice, out nvmlCurrentUtilization);
                    NvmlWrapper.nvmlReturn_t coreClockReturn = NvmlWrapper.Nvml.nvmlDeviceGetClockInfo(nvmlDevice, NvmlWrapper.nvmlClockType_t.NVML_CLOCK_GRAPHICS, out nvmlCurrentCoreClock);
                    NvmlWrapper.nvmlReturn_t memoryClockReturn = NvmlWrapper.Nvml.nvmlDeviceGetClockInfo(nvmlDevice, NvmlWrapper.nvmlClockType_t.NVML_CLOCK_MEM, out nvmlCurrentMemoryClock);
                    NvmlWrapper.nvmlReturn_t powerReturn = NvmlWrapper.Nvml.nvmlDeviceGetPowerUsage(nvmlDevice, out nvmlCurrentPower);

                    nvmlCurrentLoad = nvmlCurrentUtilization.gpu;

                    if (!nvApiError) {
                        if (maxTemperatureReturn == NvmlWrapper.nvmlReturn_t.NVML_ERROR_NOT_SUPPORTED) {
                            var thermalSensors = nvidiaGpus[i].ThermalInformation.ThermalSensors.ToArray();
                            nvmlMaxTemperature = (uint)thermalSensors.Where(t => t.Controller == ThermalController.GPU).FirstOrDefault().DefaultMaximumTemperature;
                        }
                        if (maxCoreClockReturn == NvmlWrapper.nvmlReturn_t.NVML_ERROR_NOT_SUPPORTED) {
                            try {
                                var clockFrequencies = nvidiaGpus[i].BoostClockFrequencies;
                                nvmlMaxCoreClock = clockFrequencies.GraphicsClock.Frequency / 1000;
                            }
                            catch {
                                try {
                                    var clockFrequencies = nvidiaGpus[i].BaseClockFrequencies;
                                    nvmlMaxCoreClock = clockFrequencies.GraphicsClock.Frequency / 1000;
                                }
                                catch (Exception ex) {
                                    nvmlMaxCoreClock = 0;
                                }
                            }
                        }
                        if (maxMemoryClockReturn == NvmlWrapper.nvmlReturn_t.NVML_ERROR_NOT_SUPPORTED) {
                            try {
                                var clockFrequencies = nvidiaGpus[i].BoostClockFrequencies;
                                nvmlMaxMemoryClock = clockFrequencies.MemoryClock.Frequency / 1000;
                            }
                            catch {
                                try {
                                    var clockFrequencies = nvidiaGpus[i].BaseClockFrequencies;
                                    nvmlMaxMemoryClock = clockFrequencies.MemoryClock.Frequency / 1000;
                                }
                                catch (Exception ex) {
                                    nvmlMaxMemoryClock = 0;
                                }
                            }
                        }
                        if (maxPowerReturn == NvmlWrapper.nvmlReturn_t.NVML_ERROR_NOT_SUPPORTED) {

                        }
                        if (temperatureReturn == NvmlWrapper.nvmlReturn_t.NVML_ERROR_NOT_SUPPORTED) {
                            var thermalSensors = nvidiaGpus[i].ThermalInformation.ThermalSensors.ToArray();
                            var currentTemperature = thermalSensors.Where(t => t.Controller == ThermalController.GPU).FirstOrDefault().CurrentTemperature;

                            nvmlCurrentTemperature = (uint)currentTemperature;
                        }
                        if (loadReturn == NvmlWrapper.nvmlReturn_t.NVML_ERROR_NOT_SUPPORTED) {
                            nvmlCurrentLoad = (uint)nvidiaGpus[i].UsageInformation.GPU.Percentage;
                        }
                        if (coreClockReturn == NvmlWrapper.nvmlReturn_t.NVML_ERROR_NOT_SUPPORTED) {
                            nvmlCurrentCoreClock = nvidiaGpus[i].CurrentClockFrequencies.GraphicsClock.Frequency / 1000;
                        }
                        if (memoryClockReturn == NvmlWrapper.nvmlReturn_t.NVML_ERROR_NOT_SUPPORTED) {
                            nvmlCurrentMemoryClock = nvidiaGpus[i].CurrentClockFrequencies.MemoryClock.Frequency / 1000;
                        }
                        if (powerReturn == NvmlWrapper.nvmlReturn_t.NVML_ERROR_NOT_SUPPORTED) {

                        }
                    }

                    try {
                        var trash = computer.gpus.ElementAt(gpuIdx);
                    }
                    catch (Exception ex) {
                        computer.gpus.Add(new GPU());
                    }
                    computer.gpus.ElementAt(gpuIdx).number = gpuIdx;
                    computer.gpus.ElementAt(gpuIdx).name = nvmlName.ToString();
                    computer.gpus.ElementAt(gpuIdx).temperature = nvmlMaxTemperature;
                    computer.gpus.ElementAt(gpuIdx).coreClock = nvmlMaxCoreClock;
                    computer.gpus.ElementAt(gpuIdx).memoryClock = nvmlMaxMemoryClock;
                    computer.gpus.ElementAt(gpuIdx).power = (double)nvmlMaxPower / 1000;

                    if (computer.gpus.ElementAt(gpuIdx).sensors == null) {
                        computer.gpus.ElementAt(gpuIdx).sensors = new GPU.Sensors();
                    }
                    computer.gpus.ElementAt(gpuIdx).sensors.coreClock = nvmlCurrentCoreClock;
                    computer.gpus.ElementAt(gpuIdx).sensors.memoryClock = nvmlCurrentMemoryClock;
                    computer.gpus.ElementAt(gpuIdx).sensors.load = nvmlCurrentLoad;
                    computer.gpus.ElementAt(gpuIdx).sensors.temperature = nvmlCurrentTemperature;
                    computer.gpus.ElementAt(gpuIdx).sensors.power = (double)nvmlCurrentPower / 1000;

                }
            }
            else if (!nvApiError) {
                int nvidiaGpuCount = nvidiaGpus.Count();
                for (int i = 0; i < nvidiaGpuCount; i++) {
                    int gpuIdx = i;
                    try {
                        var trash = computer.gpus.ElementAt(gpuIdx);
                    }
                    catch (Exception ex) {
                        computer.gpus.Add(new GPU());
                    }
                    computer.gpus.ElementAt(gpuIdx).number = gpuIdx;
                    computer.gpus.ElementAt(gpuIdx).name = "NVIDIA " + nvidiaGpus[i].FullName;

                    var thermalSensors = nvidiaGpus[i].ThermalInformation.ThermalSensors.ToArray();
                    var nvApiMaxTemperature = thermalSensors.Where(t => t.Controller == ThermalController.GPU).FirstOrDefault().DefaultMaximumTemperature;

                    computer.gpus.ElementAt(gpuIdx).temperature = nvApiMaxTemperature;
                    try {
                        var clockFrequencies = nvidiaGpus[i].BoostClockFrequencies;
                        computer.gpus.ElementAt(gpuIdx).coreClock = clockFrequencies.GraphicsClock.Frequency / 1000;
                        computer.gpus.ElementAt(gpuIdx).memoryClock = clockFrequencies.MemoryClock.Frequency / 1000;
                    }
                    catch {
                        try {
                            var clockFrequencies = nvidiaGpus[i].BaseClockFrequencies;
                            computer.gpus.ElementAt(gpuIdx).coreClock = clockFrequencies.GraphicsClock.Frequency / 1000;
                            computer.gpus.ElementAt(gpuIdx).memoryClock = clockFrequencies.MemoryClock.Frequency / 1000;
                        }
                        catch (Exception ex) {
                            computer.gpus.ElementAt(gpuIdx).coreClock = 0;
                            computer.gpus.ElementAt(gpuIdx).memoryClock = 0;
                        }
                    }
                    computer.gpus.ElementAt(gpuIdx).power = 0;

                    if (computer.gpus.ElementAt(gpuIdx).sensors == null) {
                        computer.gpus.ElementAt(gpuIdx).sensors = new GPU.Sensors();
                    }
                    computer.gpus.ElementAt(gpuIdx).sensors.coreClock = nvidiaGpus[i].CurrentClockFrequencies.GraphicsClock.Frequency / 1000;
                    computer.gpus.ElementAt(gpuIdx).sensors.memoryClock = nvidiaGpus[i].CurrentClockFrequencies.MemoryClock.Frequency / 1000;
                    computer.gpus.ElementAt(gpuIdx).sensors.load = nvidiaGpus[i].UsageInformation.GPU.Percentage;

                    var currentTemperature = thermalSensors.Where(t => t.Controller == ThermalController.GPU).FirstOrDefault().CurrentTemperature;

                    computer.gpus.ElementAt(gpuIdx).sensors.temperature = currentTemperature;
                    computer.gpus.ElementAt(gpuIdx).sensors.power = 0;
                }
            }
        }

        private void StorageThreadCallback() {
            ICollection<Storage> OHMStorages = new List<Storage>();
            foreach (var hardware in ohmComputer.Hardware) {
                hardware.Update();
                if (hardware.HardwareType == OHMHardware.HardwareType.HDD) {
                    Storage storage = new Storage();
                    storage.name = hardware.Name;

                    if (hardware.Sensors.Length > 0) {
                        storage.sensors = new Storage.Sensors();
                        foreach (var sensor in hardware.Sensors) {
                            if (sensor.SensorType == OHMHardware.SensorType.Temperature) {
                                storage.sensors.temperature = sensor.Value.GetValueOrDefault();
                            }
                            else if (sensor.SensorType == OHMHardware.SensorType.Load) {
                                storage.sensors.load = sensor.Value.GetValueOrDefault();
                            }
                        }
                    }

                    OHMStorages.Add(storage);
                }
            }

            var readValues = new List<object>();
            var writeValues = new List<object>();
            foreach (PerformanceCounter performanceCounter in readPerfCounters) {
                try {
                    readValues.Add(new {
                        name = performanceCounter.InstanceName,
                        value = performanceCounter.NextValue() / 1000000
                    });
                }
                catch {

                }
            }
            foreach (PerformanceCounter performanceCounter in writePerfCounters) {
                try {
                    writeValues.Add(new {
                        name = performanceCounter.InstanceName,
                        value = performanceCounter.NextValue() / 1000000
                    });
                }
                catch {

                }
            }

            List<int> storageIndexes = GetStorageIndexes();
            int iteration = 0;
            foreach (var index in storageIndexes) {
                int storageIdx = index;
                try {
                    var trash = computer.storages.ElementAt(iteration);
                }
                catch (Exception ex) {
                    computer.storages.Add(new Storage());
                }

                computer.storages.ElementAt(iteration).index = storageIdx;
                computer.storages.ElementAt(iteration).name = GetStorageName(index);
                computer.storages.ElementAt(iteration).disks = GetStorageDisks(index);
                computer.storages.ElementAt(iteration).size = GetStorageSize(index);

                int countRead = 0;
                int countWrite = 0;
                double readSum = 0;
                double writeSum = 0;
                foreach (dynamic readWrite in readWrites) {
                    if (computer.storages.ElementAt(iteration).disks.Contains(readWrite.disk.Replace("\\", ""))) {
                        if (readWrite.read >= 0) {
                            readSum += readWrite.read;
                            countRead++;
                        }
                        if (readWrite.write >= 0) {
                            writeSum += readWrite.write;
                            countWrite++;
                        }
                    }
                }
                computer.storages.ElementAt(iteration).read = readSum / countRead;
                computer.storages.ElementAt(iteration).write = writeSum / countWrite;

                computer.storages.ElementAt(iteration).sensors = new Storage.Sensors();
                foreach (var computerStorage in OHMStorages) {
                    var a = computer.storages.ElementAt(iteration);
                    if (computerStorage.name == computer.storages.ElementAt(iteration).name) {
                        computer.storages.ElementAt(iteration).sensors = computerStorage.sensors;
                    }
                    computer.storages.ElementAt(iteration).sensors.load = GetStorageLoad(computer.storages.ElementAt(iteration).size, index);
                }
                if (readValues.Count > 0) {
                    foreach (var readValue in readValues.Where((dynamic r) => {
                        string name = r.name;
                        var splits = computer.storages.ElementAt(iteration).disks.Split(',');
                        bool exists = false;
                        foreach (var split in splits) {
                            if (name.Contains(split)) {
                                exists = true;
                                break;
                            }
                        }
                        return exists;
                    }))
                    computer.storages.ElementAt(iteration).sensors.read = readValue.value;
                }
                if (writeValues.Count > 0) {
                    foreach (var writeValue in writeValues.Where((dynamic r) => {
                        string name = r.name;
                        var splits = computer.storages.ElementAt(iteration).disks.Split(',');
                        bool exists = false;
                        foreach (var split in splits) {
                            if (name.Contains(split)) {
                                exists = true;
                                break;
                            }
                        }
                        return exists;
                    }))
                    computer.storages.ElementAt(iteration).sensors.write = writeValue.value;
                }

                iteration++;
            }
            if (computer.storages.Count > storageIndexes.Count) {
                var toRemove = new List<Storage>();
                foreach (var storage in computer.storages) {
                    if (!storageIndexes.Contains(storage.index)) {
                        toRemove.Add(storage);
                    }
                }
                for (int i = 0; i < toRemove.Count; i++) {
                    computer.storages.Remove(toRemove.ElementAt(i));
                }
            }
        }

        private void ComputerDataLoader(IAsyncResult result) {
            Computer _computer = computer; ;
            if (Config.localhost == null) {
                _computer.uuid = Config.uuid;
                _computer.name = Environment.MachineName;
                _computer.status = true;
                _computer.processors = new List<Processor>();
                _computer.gpus = new List<GPU>();
                _computer.storages = new List<Storage>();
                _computer.ram = new RAM();
                _computer.ram.physicalMemories = new List<PhysicalMemory>();
                _computer.motherboard = new Motherboard();
                _computer.motherboard.superIO = new SuperIO();
            }
            else {
                _computer = Config.localhost;
            }
            /*_computer = new Computer();
            _computer.uuid = Config.localhost.uuid;
            _computer.status = true;
            _computer.processors = new List<Processor>();
            foreach (var processor in Config.localhost.processors) {
                _computer.processors.Add(new Processor() {
                    uuid = processor.uuid
                });
            }
            _computer.gpus = new List<GPU>();
            foreach (var gpu in Config.localhost.gpus) {
                _computer.gpus.Add(new GPU() {
                    uuid = gpu.uuid
                });
            }
            _computer.storages = new List<Storage>();
            foreach (var storage in Config.localhost.storages) {
                _computer.storages.Add(new Storage() {
                    uuid = storage.uuid
                });
            }
            _computer.ram = new RAM();
            _computer.ram.uuid = Config.localhost.ram.uuid;
            _computer.ram.physicalMemories = new List<PhysicalMemory>();
            foreach (var physicalMemory in Config.localhost.ram.physicalMemories) {
                _computer.ram.physicalMemories.Add(new PhysicalMemory() {
                    uuid = physicalMemory.uuid
                });
            }
            _computer.motherboard = new Motherboard();
            _computer.motherboard.uuid = Config.localhost.motherboard.uuid;
            _computer.motherboard.superIO = new SuperIO();
            _computer.motherboard.superIO.uuid = Config.localhost.motherboard.superIO.uuid;*/
            _computer.name = Environment.MachineName;

            _computer.status = true;

            ICollection<Storage> OHMStorages = new List<Storage>();
            foreach (var hardware in ohmComputer.Hardware) {
                hardware.Update();
                if (hardware.HardwareType == OHMHardware.HardwareType.CPU) {
                    int cpuIdx = GetProcessorNumber(hardware);
                    try {
                        var trash = _computer.processors.ElementAt(cpuIdx);
                    }
                    catch (Exception ex) {
                        _computer.processors.Add(new Processor());
                    }
                    _computer.processors.ElementAt(cpuIdx).name = hardware.Name;
                    _computer.processors.ElementAt(cpuIdx).cores = GetProcessorCoreCount(hardware);
                    _computer.processors.ElementAt(cpuIdx).clock = GetProcessorClock();
                    _computer.processors.ElementAt(cpuIdx).number = GetProcessorNumber(hardware);
                    _computer.processors.ElementAt(cpuIdx).power = GetProcessorPower();
                    _computer.processors.ElementAt(cpuIdx).temperature = GetProcessorTemperature();

                    if (hardware.Sensors.Length > 0) {
                        if (_computer.processors.ElementAt(cpuIdx).sensors == null) {
                            _computer.processors.ElementAt(cpuIdx).sensors = new Processor.Sensors();
                        }
                        _computer.processors.ElementAt(cpuIdx).sensors.clock = 0;
                        foreach (var sensor in hardware.Sensors) {
                            if (sensor.SensorType == OHMHardware.SensorType.Temperature && sensor.Name == "CPU Package") {
                                _computer.processors.ElementAt(cpuIdx).sensors.temperature = sensor.Value.GetValueOrDefault();
                            }
                            else if (sensor.SensorType == OHMHardware.SensorType.Clock && sensor.Name.StartsWith("CPU Core #")) {
                                _computer.processors.ElementAt(cpuIdx).sensors.clock += sensor.Value.GetValueOrDefault();
                            }
                            else if (sensor.SensorType == OHMHardware.SensorType.Power && sensor.Name == "CPU Package") {
                                _computer.processors.ElementAt(cpuIdx).sensors.power = sensor.Value.GetValueOrDefault();
                            }
                            else if (sensor.SensorType == OHMHardware.SensorType.Load && sensor.Name == "CPU Total") {
                                _computer.processors.ElementAt(cpuIdx).sensors.load = sensor.Value.GetValueOrDefault();
                            }
                        }
                        _computer.processors.ElementAt(cpuIdx).sensors.clock /= _computer.processors.ElementAt(cpuIdx).cores;
                    }
                }
                else if (hardware.HardwareType == OHMHardware.HardwareType.RAM) {
                    if (hardware.Sensors.Length > 0) {
                        if (_computer.ram.sensors == null) {
                            _computer.ram.sensors = new RAM.Sensors();
                        }
                        foreach (var sensor in hardware.Sensors) {
                            if (sensor.SensorType == OHMHardware.SensorType.Load) {
                                _computer.ram.sensors.load = sensor.Value.GetValueOrDefault();
                            }
                            else if (sensor.SensorType == OHMHardware.SensorType.Data && sensor.Name == "Used Memory") {
                                _computer.ram.sensors.used = sensor.Value.GetValueOrDefault();
                            }
                            else if (sensor.SensorType == OHMHardware.SensorType.Data && sensor.Name == "Available Memory") {
                                _computer.ram.sensors.free = sensor.Value.GetValueOrDefault();
                            }
                        }
                    }
                }
                else if (hardware.HardwareType == OHMHardware.HardwareType.HDD) {
                    Storage storage = new Storage();
                    storage.name = hardware.Name;

                    if (hardware.Sensors.Length > 0) {
                        storage.sensors = new Storage.Sensors();
                        foreach (var sensor in hardware.Sensors) {
                            if (sensor.SensorType == OHMHardware.SensorType.Temperature) {
                                storage.sensors.temperature = sensor.Value.GetValueOrDefault();
                            }
                            else if (sensor.SensorType == OHMHardware.SensorType.Load) {
                                storage.sensors.load = sensor.Value.GetValueOrDefault();
                            }
                        }
                    }

                    OHMStorages.Add(storage);
                }
                else if (hardware.HardwareType == OHMHardware.HardwareType.Mainboard) {
                    _computer.motherboard.name = hardware.Name;

                    if (hardware.SubHardware.Length > 0) {
                        foreach (var subHardware in hardware.SubHardware) {
                            subHardware.Update();
                            if (subHardware.HardwareType == OHMHardware.HardwareType.SuperIO) {
                                _computer.motherboard.superIO.name = subHardware.Name;
                            }
                        }
                    }
                }
            }

            if (!nvmlV2Error) {
                uint nvidiaGpuCount;
                NvmlWrapper.Nvml.nvmlDeviceGetCount_v2(out nvidiaGpuCount);
                for (uint i = 0; i < nvidiaGpuCount; i++) {
                    IntPtr nvmlDevice;
                    StringBuilder nvmlName = new StringBuilder();
                    uint nvmlMaxTemperature;
                    uint nvmlMaxCoreClock;
                    uint nvmlMaxMemoryClock;
                    uint nvmlMinPower;
                    uint nvmlMaxPower;
                    uint nvmlCurrentTemperature;
                    uint nvmlCurrentCoreClock;
                    uint nvmlCurrentMemoryClock;
                    uint nvmlCurrentPower;
                    NvmlWrapper.nvmlUtilization_t nvmlCurrentLoad;

                    NvmlWrapper.Nvml.nvmlDeviceGetHandleByIndex_v2(i, out nvmlDevice);
                    NvmlWrapper.Nvml.nvmlDeviceGetName(nvmlDevice, nvmlName, 64);
                    NvmlWrapper.Nvml.nvmlDeviceGetTemperatureThreshold(nvmlDevice, NvmlWrapper.nvmlTemperatureThresholds_t.NVML_TEMPERATURE_THRESHOLD_GPU_MAX, out nvmlMaxTemperature);
                    NvmlWrapper.Nvml.nvmlDeviceGetMaxClockInfo(nvmlDevice, NvmlWrapper.nvmlClockType_t.NVML_CLOCK_GRAPHICS, out nvmlMaxCoreClock);
                    NvmlWrapper.Nvml.nvmlDeviceGetMaxClockInfo(nvmlDevice, NvmlWrapper.nvmlClockType_t.NVML_CLOCK_MEM, out nvmlMaxMemoryClock);
                    NvmlWrapper.Nvml.nvmlDeviceGetPowerManagementLimitConstraints(nvmlDevice, out nvmlMinPower, out nvmlMaxPower);
                    NvmlWrapper.Nvml.nvmlDeviceGetTemperature(nvmlDevice, NvmlWrapper.nvmlTemperatureSensors_t.NVML_TEMPERATURE_GPU, out nvmlCurrentTemperature);
                    NvmlWrapper.Nvml.nvmlDeviceGetUtilizationRates(nvmlDevice, out nvmlCurrentLoad);
                    NvmlWrapper.Nvml.nvmlDeviceGetClockInfo(nvmlDevice, NvmlWrapper.nvmlClockType_t.NVML_CLOCK_GRAPHICS, out nvmlCurrentCoreClock);
                    NvmlWrapper.Nvml.nvmlDeviceGetClockInfo(nvmlDevice, NvmlWrapper.nvmlClockType_t.NVML_CLOCK_MEM, out nvmlCurrentMemoryClock);
                    NvmlWrapper.Nvml.nvmlDeviceGetPowerUsage(nvmlDevice, out nvmlCurrentPower);


                    int gpuIdx = (int)i;
                    try {
                        var trash = _computer.gpus.ElementAt(gpuIdx);
                    }
                    catch (Exception ex) {
                        _computer.gpus.Add(new GPU());
                    }
                    _computer.gpus.ElementAt(gpuIdx).number = gpuIdx;
                    _computer.gpus.ElementAt(gpuIdx).name = nvmlName.ToString();
                    _computer.gpus.ElementAt(gpuIdx).temperature = nvmlMaxTemperature;
                    _computer.gpus.ElementAt(gpuIdx).coreClock = nvmlMaxCoreClock;
                    _computer.gpus.ElementAt(gpuIdx).memoryClock = nvmlMaxMemoryClock;
                    _computer.gpus.ElementAt(gpuIdx).power = (double)nvmlMaxPower / 1000;

                    if (_computer.gpus.ElementAt(gpuIdx).sensors == null) {
                        _computer.gpus.ElementAt(gpuIdx).sensors = new GPU.Sensors();
                    }
                    _computer.gpus.ElementAt(gpuIdx).sensors.coreClock = nvmlCurrentCoreClock;
                    _computer.gpus.ElementAt(gpuIdx).sensors.memoryClock = nvmlCurrentMemoryClock;
                    _computer.gpus.ElementAt(gpuIdx).sensors.load = nvmlCurrentLoad.gpu;
                    _computer.gpus.ElementAt(gpuIdx).sensors.temperature = nvmlCurrentTemperature;
                    _computer.gpus.ElementAt(gpuIdx).sensors.power = (double)nvmlCurrentPower / 1000;

                }
            }
            else if (!nvApiError) {
                int nvidiaGpuCount = nvidiaGpus.Count();
                for (int i = 0; i < nvidiaGpuCount; i++) {
                    int gpuIdx = i;
                    try {
                        var trash = _computer.gpus.ElementAt(gpuIdx);
                    }
                    catch (Exception ex) {
                        _computer.gpus.Add(new GPU());
                    }
                    _computer.gpus.ElementAt(gpuIdx).number = gpuIdx;
                    _computer.gpus.ElementAt(gpuIdx).name = "NVIDIA " + nvidiaGpus[i].FullName;

                    var thermalSensors = nvidiaGpus[i].ThermalInformation.ThermalSensors.ToArray();
                    var nvApiMaxTemperature = thermalSensors.Where(t => t.Controller == ThermalController.GPU).FirstOrDefault().DefaultMaximumTemperature;

                    _computer.gpus.ElementAt(gpuIdx).temperature = nvApiMaxTemperature;
                    try {
                        var clockFrequencies = nvidiaGpus[i].BoostClockFrequencies;
                        _computer.gpus.ElementAt(gpuIdx).coreClock = clockFrequencies.GraphicsClock.Frequency;
                        _computer.gpus.ElementAt(gpuIdx).memoryClock = clockFrequencies.MemoryClock.Frequency;
                    }
                    catch {
                        try {
                            var clockFrequencies = nvidiaGpus[i].BaseClockFrequencies;
                            _computer.gpus.ElementAt(gpuIdx).coreClock = clockFrequencies.GraphicsClock.Frequency;
                            _computer.gpus.ElementAt(gpuIdx).memoryClock = clockFrequencies.MemoryClock.Frequency;
                        }
                        catch (Exception ex){
                            _computer.gpus.ElementAt(gpuIdx).coreClock = 0;
                            _computer.gpus.ElementAt(gpuIdx).memoryClock = 0;
                        }
                    }
                    _computer.gpus.ElementAt(gpuIdx).power = 0;

                    if (_computer.gpus.ElementAt(gpuIdx).sensors == null) {
                        _computer.gpus.ElementAt(gpuIdx).sensors = new GPU.Sensors();
                    }
                    _computer.gpus.ElementAt(gpuIdx).sensors.coreClock = nvidiaGpus[i].CurrentClockFrequencies.GraphicsClock.Frequency;
                    _computer.gpus.ElementAt(gpuIdx).sensors.memoryClock = nvidiaGpus[i].CurrentClockFrequencies.MemoryClock.Frequency;
                    _computer.gpus.ElementAt(gpuIdx).sensors.load = nvidiaGpus[i].UsageInformation.GPU.Percentage;

                    var currentTemperature = thermalSensors.Where(t => t.Controller == ThermalController.GPU).FirstOrDefault().CurrentTemperature;

                    _computer.gpus.ElementAt(gpuIdx).sensors.temperature = currentTemperature;
                    _computer.gpus.ElementAt(gpuIdx).sensors.power = 0;
                }
            }


            int ramCount = GetRAMCount();
            double totalPhysicalRam = 0;
            for (int i = 0; i < ramCount; i++) {
                int ramIdx = i;
                try {
                    var trash = _computer.ram.physicalMemories.ElementAt(ramIdx);
                }
                catch (Exception ex) {
                    _computer.ram.physicalMemories.Add(new PhysicalMemory());
                }
                _computer.ram.physicalMemories.ElementAt(ramIdx).capacity = GetPhysicalRAMCapacity(i);
                totalPhysicalRam += _computer.ram.physicalMemories.ElementAt(ramIdx).capacity;
            }
            _computer.ram.total = totalPhysicalRam;

            stopwatch.Stop();
            var readValues = new List<double>();
            var writeValues = new List<double>();
            foreach (PerformanceCounter performanceCounter in readPerfCounters) {
                readValues.Add(performanceCounter.NextValue() / 1000000);
            }
            foreach (PerformanceCounter performanceCounter in writePerfCounters) {
                writeValues.Add(performanceCounter.NextValue() / 1000000);
            }

            List<int> storageIndexes = GetStorageIndexes();
            int iteration = 0;
            foreach (var index in storageIndexes) {
                int storageIdx = index;
                try {
                    var trash = _computer.storages.ElementAt(iteration);
                }
                catch (Exception ex) {
                    _computer.storages.Add(new Storage());
                }

                _computer.storages.ElementAt(iteration).index = storageIdx;
                _computer.storages.ElementAt(iteration).name = GetStorageName(index);
                _computer.storages.ElementAt(iteration).disks = GetStorageDisks(index);
                _computer.storages.ElementAt(iteration).size = GetStorageSize(index);

                int count = 0;
                double readSum = 0;
                double writeSum = 0;
                foreach (dynamic readWrite in readWrites) {
                    if (_computer.storages.ElementAt(iteration).disks.Contains(readWrite.disk.Replace("\\", ""))) {
                        readSum += readWrite.read;
                        writeSum += readWrite.write;
                        count++;
                    }
                }
                _computer.storages.ElementAt(iteration).read = readSum / count;
                _computer.storages.ElementAt(iteration).write = writeSum / count;

                _computer.storages.ElementAt(iteration).sensors = new Storage.Sensors();
                foreach (var _computerStorage in OHMStorages) {
                    if (_computerStorage.name == _computer.storages.ElementAt(iteration).name) {
                        _computer.storages.ElementAt(iteration).sensors = _computerStorage.sensors;
                    }
                }
                if (readValues.Count > 0) {
                    _computer.storages.ElementAt(iteration).sensors.read = readValues[0];
                }
                if (writeValues.Count > 0) {
                    _computer.storages.ElementAt(iteration).sensors.write = writeValues[0];
                }

                iteration++;
            }
            if (_computer.storages.Count > storageIndexes.Count) {
                var toRemove = new List<Storage>();
                foreach (var storage in _computer.storages) {
                    if (!storageIndexes.Contains(storage.index)) {
                        toRemove.Add(storage);
                    }
                }
                for (int i = 0; i < toRemove.Count; i++) {
                    computer.storages.Remove(toRemove.ElementAt(i));
                }
            }

            try {
                File.WriteAllText(Environment.MachineName + ".json", JsonConvert.SerializeObject(_computer, Formatting.Indented));
            }
            catch {

            }

            //Debug.WriteLine(_computer);

            computer = _computer;
            Config.computer = computer;

            this.jsonData = JsonConvert.SerializeObject(Config.computer);
        }

        private int GetProcessorCoreCount(OHMHardware.IHardware cpu) {
            return Int32.Parse(cpu.GetType().BaseType.GetField(
                "coreCount",
                BindingFlags.NonPublic | BindingFlags.Instance
            ).GetValue(cpu).ToString());
        }

        private int GetProcessorNumber(OHMHardware.IHardware cpu) {
            return Int32.Parse(cpu.GetType().BaseType.GetField(
                "processorIndex",
                BindingFlags.NonPublic | BindingFlags.Instance
            ).GetValue(cpu).ToString());
        }

        private double GetProcessorClock() {
            uint eax, edx;
            uint MSR_TURBO_RATIO_LIMIT = 0x1AD;
            uint MSR_PLATFORM_INFO = 0xCE;

            OHMHardware.Ring0.Rdmsr(MSR_PLATFORM_INFO, out eax, out edx);
            double baseClock = (eax >> 8) & 0xFF;

            double maxClock = 0;
            OHMHardware.Ring0.Rdmsr(MSR_TURBO_RATIO_LIMIT, out eax, out edx);
            maxClock = (((eax >> 0) & 0xFF) * 100);

            return maxClock;
        }

        private double GetProcessorPower() {
            uint eax, edx;
            uint MSR_RAPL_POWER_UNIT = 0x606;
            uint MSR_RAPL_POWER_INFO = 0x614;

            OHMHardware.Ring0.Rdmsr(MSR_RAPL_POWER_UNIT, out eax, out edx);
            float powerUnit = 1.0f / (1 << (int)(eax & 0xF));

            OHMHardware.Ring0.Rdmsr(MSR_RAPL_POWER_INFO, out eax, out edx);
            float maxTdp = eax & 0x7FFF;
            maxTdp *= powerUnit;

            return maxTdp;
        }

        private double GetProcessorTemperature() {
            uint eax, edx;
            uint MSR_TEMPERATURE_TARGET = 0x1A2;

            OHMHardware.Ring0.Rdmsr(MSR_TEMPERATURE_TARGET, out eax, out edx);
            float maxTemperature = (eax >> 16) & 0xFF;

            return maxTemperature;
        }

        private int GetGPUNumber(OHMHardware.IHardware gpu) {
            return Int32.Parse(gpu.GetType().GetField(
                "adapterIndex",
                BindingFlags.NonPublic | BindingFlags.Instance
            ).GetValue(gpu).ToString());
        }
        private double GetGPUTemperature(int number) {
            if (this.nvidiaGpus == null) {
                return -1;
            }
            double maxTemperature = 0;
            try {
                PhysicalGPU gpu = this.nvidiaGpus[number];
                GPUThermalSensor[] thermalSensors = gpu.ThermalInformation.ThermalSensors.ToArray();

                double totalTemperature = 0;
                foreach (var thermalSensor in thermalSensors) {
                    totalTemperature += thermalSensor.DefaultMaximumTemperature;
                }
                maxTemperature = totalTemperature / thermalSensors.Length;
            }
            catch {
                maxTemperature = -1;
            }

            return maxTemperature;
        }

        private double GetGPUMemoryClock(int number) {
            if (this.nvidiaGpus == null) {
                return -1;
            }
            double maxMemoryClock = 0;
            try {
                PhysicalGPU gpu = this.nvidiaGpus[number];

                maxMemoryClock = gpu.BoostClockFrequencies.MemoryClock.Frequency / 1000.0;
            }
            catch {
                maxMemoryClock = -1;
            }

            return maxMemoryClock;
        }

        private double GetGPUCoreClock(int number) {
            if (this.nvidiaGpus == null) {
                return -1;
            }
            double maxCoreClock = 0;
            try {
                PhysicalGPU gpu = this.nvidiaGpus[number];

                maxCoreClock = gpu.BoostClockFrequencies.GraphicsClock.Frequency / 1000.0;
            }
            catch {
                maxCoreClock = -1;
            }

            return maxCoreClock;
        }

        private int GetRAMCount() {
            ObjectQuery query = new ObjectQuery("SELECT BankLabel FROM Win32_PhysicalMemory");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(this.scope, query);
            ManagementObjectCollection collection = searcher.Get();

            int ramCount = collection.Count;
            return ramCount;
        }

        private double GetPhysicalRAMCapacity(int index) {
            ObjectQuery query = new ObjectQuery("SELECT Capacity FROM Win32_PhysicalMemory");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(this.scope, query);
            ManagementObjectCollection collection = searcher.Get();

            List<double> capacities = new List<double>();
            foreach (ManagementObject wmiObject in collection) {
                capacities.Add(Double.Parse(wmiObject["Capacity"].ToString()));
            }
            double ramCapacity = capacities[index] / (1024 * 1024);
            return ramCapacity;
        }

        private double GetFreeMemory() {
            ObjectQuery query = new ObjectQuery($"SELECT FreePhysicalMemory FROM Win32_OperatingSystem");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(this.scope, query);
            ManagementObjectCollection collection = searcher.Get();

            double freeMemory = 0;
            foreach (ManagementObject wmiObject in collection) {
                freeMemory = Double.Parse(wmiObject["FreePhysicalMemory"].ToString());
            }
            freeMemory /= 1024 * 1024;
            return freeMemory;
        }

        private int GetStorageCount() {
            ObjectQuery query = new ObjectQuery("SELECT DeviceID FROM Win32_DiskDrive");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(this.scope, query);
            ManagementObjectCollection collection = searcher.Get();

            int storageCount = collection.Count;
            return storageCount;
        }

        private List<int> GetStorageIndexes() {
            ObjectQuery query = new ObjectQuery("SELECT Index FROM Win32_DiskDrive");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(this.scope, query);
            ManagementObjectCollection collection = searcher.Get();

            List<int> indexes = new List<int>();
            foreach (ManagementObject wmiObject in collection) {
                indexes.Add(Int32.Parse(wmiObject["Index"].ToString()));
            }
            return indexes;
        }

        private double GetStorageSize() {
            throw new NotImplementedException();
        }

        private string GetStorageDisks(int index) {
            ObjectQuery query = new ObjectQuery($"SELECT DeviceID FROM Win32_DiskPartition WHERE DiskIndex = '{index}'");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(this.scope, query);
            ManagementObjectCollection collection = searcher.Get();

            string disks = string.Empty;
            foreach (ManagementObject wmiObject in collection) {
                query = new ObjectQuery($"ASSOCIATORS OF {{Win32_DiskPartition.DeviceId='{wmiObject["DeviceID"]}'}} WHERE AssocClass = Win32_LogicalDiskToPartition");
                searcher = new ManagementObjectSearcher(this.scope, query);
                ManagementObjectCollection _collection = searcher.Get();

                foreach (ManagementObject _wmiObject in _collection) {
                    disks += _wmiObject["DeviceID"].ToString() + ", ";
                }
            }
            if (disks.Length > 0) {
                disks = disks.Remove(disks.Length - 2);
            }
            return disks;
        }

        private string GetStorageName(int index) {
            ObjectQuery query = new ObjectQuery($"SELECT * FROM Win32_DiskDrive WHERE Index='{index}'");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(this.scope, query);
            ManagementObjectCollection collection = searcher.Get();

            string name = string.Empty;
            foreach (ManagementObject wmiObject in collection) {
                name = wmiObject["Model"].ToString();
            }
            return name;
        }

        private double GetStorageSize(int index) {
            ObjectQuery query = new ObjectQuery($"SELECT Size FROM Win32_DiskDrive WHERE Index='{index}'");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(this.scope, query);
            ManagementObjectCollection collection = searcher.Get();

            double size = 0;
            foreach (ManagementObject wmiObject in collection) {
                size = Double.Parse(wmiObject["Size"].ToString());
            }
            size /= (1024 * 1024 * 1024);
            return size;
        }

        private double GetStorageLoad(double size, int index) {
            ObjectQuery query = new ObjectQuery($"SELECT DeviceID FROM Win32_DiskPartition WHERE DiskIndex = '{index}'");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(this.scope, query);
            ManagementObjectCollection diskPartitions = searcher.Get();

            double freeSpace= 0;
            foreach (ManagementObject diskPartition in diskPartitions) {
                query = new ObjectQuery($"ASSOCIATORS OF {{Win32_DiskPartition.DeviceId='{diskPartition["DeviceID"]}'}} WHERE AssocClass = Win32_LogicalDiskToPartition");
                searcher = new ManagementObjectSearcher(this.scope, query);
                ManagementObjectCollection logicalDisks = searcher.Get();

                foreach (ManagementObject logicalDisk in logicalDisks) {
                    freeSpace += Double.Parse(logicalDisk["FreeSpace"].ToString());
                }
            }
            freeSpace /= (1024 * 1024 * 1024);

            double load = ((size - freeSpace) * 100) / size;

            return load;
        }

        public string JsonData { 
            get {
                if (this.jsonData == string.Empty) {
                    this.ComputerDataLoader(null);
                    if (this.jsonData == string.Empty) {
                        throw new Exception();
                    }
                }
                return this.jsonData;
            }
            set {
                jsonData = value;   
            }
        }
    }

}

