using System;
using System.Runtime.InteropServices;
using System.Text;

namespace NvmlWrapper {
    /// <summary>
    /// Nvml return codes
    /// </summary>
    public enum nvmlReturn_t {
        NVML_SUCCESS = 0,                   // The operation was successful
        NVML_ERROR_UNINITIALIZED = 1,       // NVML was not first initialized with nvml_Init()
        NVML_ERROR_INVALID_ARGUMENT = 2,    // A supplied argument is invalid
        NVML_ERROR_NOT_SUPPORTED = 3,       // The requested operation is not available on target device
        NVML_ERROR_NO_PERMISSION = 4,       // The current user does not have permission for operation
        NVML_ERROR_ALREADY_INITIALIZED = 5, // Deprecated: Multiple initializations are now allowed through ref counting
        NVML_ERROR_NOT_FOUND = 6,           // A query to find an object was unsuccessful
        NVML_ERROR_INSUFFICIENT_SIZE = 7,   // An input argument is not large enough
        NVML_ERROR_INSUFFICIENT_POWER = 8,  // A device's external power cables are not properly attached
        NVML_ERROR_DRIVER_NOT_LOADED = 9,   // NVIDIA driver is not loaded
        NVML_ERROR_TIMEOUT = 10,            // User provided timeout passed
        NVML_ERROR_IRQ_ISSUE = 11,          // NVIDIA Kernel detected an interrupt issue with a GPU
        NVML_ERROR_LIBRARY_NOT_FOUND = 12,  // NVML Shared Library couldn't be found or loaded
        NVML_ERROR_FUNCTION_NOT_FOUND = 13, // Local version of NVML doesn't implement this function
        NVML_ERROR_CORRUPTED_INFOROM = 14,  // infoROM is corrupted
        NVML_ERROR_GPU_IS_LOST = 15,        // The GPU has fallen off the bus or has otherwise become inaccessible
        NVML_ERROR_UNKNOWN = 999            // An internal driver error occurred
    }

    public enum nvmlTemperatureSensors_t {
        // Temperature sensor for the GPU die

        NVML_TEMPERATURE_GPU = 0
    }

    public enum nvmlTemperatureThresholds_t {
        // Temperature thresholds.

        NVML_TEMPERATURE_THRESHOLD_SHUTDOWN = 0,
        NVML_TEMPERATURE_THRESHOLD_SLOWDOWN = 1,
        NVML_TEMPERATURE_THRESHOLD_MEM_MAX = 2,
        NVML_TEMPERATURE_THRESHOLD_GPU_MAX = 3,
        NVML_TEMPERATURE_THRESHOLD_ACOUSTIC_MIN = 4,
        NVML_TEMPERATURE_THRESHOLD_ACOUSTIC_CURR = 5,
        NVML_TEMPERATURE_THRESHOLD_ACOUSTIC_MAX = 6,
        NVML_TEMPERATURE_THRESHOLD_COUNT
    }

    public enum nvmlClockType_t {
        // Clock types. All speeds are in Mhz.

        NVML_CLOCK_GRAPHICS = 0,    // Graphics clock domain.
        NVML_CLOCK_SM = 1,          // SM clock domain.
        NVML_CLOCK_MEM = 2,         // Memory clock domain.
        NVML_CLOCK_VIDEO = 3,       // Video encoder/decoder clock domain.
        NVML_CLOCK_COUNT            // Count of clock types.
    }

    /// <summary>
    /// GPU Utilization pair. Contains info on kernel execution time and gpu memory utilization
    /// </summary>
    public struct nvmlUtilization_t {
        /* 
         * % time over the past sample period during which one or more kernels 
         * were executing on the GPU
         */
        public uint gpu;

        /* % time over the past sample period during which global (device) memory 
         * was being read or written
         */
        public uint memory;
    }

    /// <summary>
    /// NVIDIA Management Library functions
    /// </summary>
    /// <remarks>
    /// nvml.dll needs to be on your PATH, or included with the your application
    /// Device Query Documentation:
    /// https://docs.nvidia.com/deploy/nvml-api/index.html
    /// </remarks>
    public static class Nvml {
        public const string NVML_DLL = "nvml.dll";
        public const uint NVML_INIT_FLAG_NO_ATTACH = 2;
        public const uint NVML_INIT_FLAG_NO_GPUS = 1;

        /// <summary>
        /// Initializes Nvml
        /// </summary>
        /// <returns>
        /// NVML_SUCCESS if NVML has been properly initialized
        /// NVML_ERROR_DRIVER_NOT_LOADED if NVIDIA driver is not running
        /// NVML_ERROR_NO_PERMISSION if NVML does not have permission to talk to the driver
        /// NVML_ERROR_UNKNOWN on any unexpected error
        /// </returns>
        /// <remarks>
        /// Needs to be called before making any other nvml calls. Reference counted,
        /// nvml shutdown only occurs when reference count hits 0
        /// </remarks>
        [DllImport(NVML_DLL)]
        public static extern nvmlReturn_t nvmlInit();

        [DllImport(NVML_DLL)]
        public static extern nvmlReturn_t nvmlInit_v2();
        [DllImport(NVML_DLL)]
        public static extern nvmlReturn_t nvmlInitWithFlags(uint flags);

        /// <summary>
        /// Shuts down Nvml
        /// </summary>
        /// <returns>
        /// NVML_SUCCESS if NVML has been properly shut down
        /// NVML_ERROR_UNINITIALIZED if the library has not been successfully initialized
        /// NVML_ERROR_UNKNOWN on any unexpected error
        /// </returns>
        /// <remarks>
        /// Reference counted, nvml shutdown only occurs when reference count hits 0
        /// </remarks>
        [DllImport(NVML_DLL)]
        public static extern nvmlReturn_t nvmlShutdown();

        /// <summary>
        /// Queries nvml for GPU device count
        /// </summary>
        /// <param name="deviceCount">out parameter containing device count</param>
        /// <returns>
        /// NVML_SUCCESS if deviceCount has been set
        /// NVML_ERROR_UNINITIALIZED if the library has not been successfully initialized
        /// NVML_ERROR_INVALID_ARGUMENT if deviceCount is NULL
        /// NVML_ERROR_UNKNOWN on any unexpected error
        /// </returns>
        [DllImport(NVML_DLL)]
        public static extern nvmlReturn_t nvmlDeviceGetCount(out uint deviceCount);
        [DllImport(NVML_DLL)]
        public static extern nvmlReturn_t nvmlDeviceGetCount_v2(out uint deviceCount);

        /// <summary>
        /// Queries device for name
        /// </summary>
        /// <param name="device">Device handle</param>
        /// <param name="name">"out" parameter containing the device name</param>
        /// <param name="length">maximum length of the string returned by name</param>
        /// <returns>
        /// NVML_SUCCESS if name has been set
        /// NVML_ERROR_UNINITIALIZED if the library has not been successfully initialized
        /// NVML_ERROR_INVALID_ARGUMENT if device is invalid, or name is NULL
        /// NVML_ERROR_INSUFFICIENT_SIZE if length is too small
        /// NVML_ERROR_GPU_IS_LOST if the target GPU has fallen off the bus or is otherwise inaccessible
        /// NVML_ERROR_UNKNOWN on any unexpected error
        /// </returns>
        [DllImport(NVML_DLL)]
        public static extern nvmlReturn_t nvmlDeviceGetName(IntPtr device, [MarshalAs(UnmanagedType.LPStr)] StringBuilder name, uint length);

        /// <summary>
        /// Queries device handle by index
        /// </summary>
        /// <param name="index">Device index</param>
        /// <param name="device">out parameter for device handle</param>
        /// <returns>
        /// NVML_SUCCESS if device has been set
        /// NVML_ERROR_UNINITIALIZED if the library has not been successfully initialized
        /// NVML_ERROR_INVALID_ARGUMENT if index is invalid or device is NULL
        /// NVML_ERROR_INSUFFICIENT_POWER if any attached devices have improperly attached external power cables
        /// NVML_ERROR_NO_PERMISSION if the user doesn't have permission to talk to this device
        /// NVML_ERROR_IRQ_ISSUE if NVIDIA kernel detected an interrupt issue with the attached GPUs
        /// NVML_ERROR_GPU_IS_LOST if the target GPU has fallen off the bus or is otherwise inaccessible
        /// NVML_ERROR_UNKNOWN on any unexpected error
        /// </returns>
        [DllImport(NVML_DLL)]
        public static extern nvmlReturn_t nvmlDeviceGetHandleByIndex(uint index, out IntPtr device);
        [DllImport(NVML_DLL)]
        public static extern nvmlReturn_t nvmlDeviceGetHandleByIndex_v2(uint index, out IntPtr device);

        /// <summary>
        /// Queries temperature of the device
        /// </summary>
        /// <param name="device">device handle</param>
        /// <param name="sensorType">sensor type, api currently only supports one value here</param>
        /// <param name="temp">out parameter containing gpu temperature</param>
        /// <returns>
        /// NVML_SUCCESS if temp has been set
        /// NVML_ERROR_UNINITIALIZED if the library has not been successfully initialized
        /// NVML_ERROR_INVALID_ARGUMENT if device is invalid, sensorType is invalid or temp is NULL
        /// NVML_ERROR_NOT_SUPPORTED if the device does not have the specified sensor
        /// NVML_ERROR_GPU_IS_LOST if the target GPU has fallen off the bus or is otherwise inaccessible
        /// NVML_ERROR_UNKNOWN on any unexpected error
        /// </returns>
        [DllImport(NVML_DLL)]
        public static extern nvmlReturn_t nvmlDeviceGetTemperature(IntPtr device, nvmlTemperatureSensors_t sensorType, out uint temp);

        /// <summary>
        /// Retrieves information about possible values of power management limits on this device.
        /// </summary>
        /// <param name="device">The identifier of the target device</param>
        /// <param name="minLimit">Reference in which to return the minimum power management limit in milliwatts</param>
        /// <param name="maxLimit">Reference in which to return the maximum power management limit in milliwatts</param>
        /// <returns>
        /// NVML_SUCCESS if limit has been set
        /// NVML_ERROR_UNINITIALIZED if the library has not been successfully initialized
        /// NVML_ERROR_INVALID_ARGUMENT if device is invalid or limit is NULL
        /// NVML_ERROR_NOT_SUPPORTED if the device does not support this feature
        /// NVML_ERROR_GPU_IS_LOST if the target GPU has fallen off the bus or is otherwise inaccessible
        /// NVML_ERROR_UNKNOWN on any unexpected error
        /// </returns>
        [DllImport(NVML_DLL)]
        public static extern nvmlReturn_t nvmlDeviceGetPowerManagementLimitConstraints(IntPtr device, out uint minLimit, out uint maxLimit);

        /// <summary>
        /// Queries device utilization information
        /// </summary>
        /// <param name="device">device handle</param>
        /// <param name="utilization">out parameter containing utilization info</param>
        /// <returns>
        /// NVML_SUCCESS if utilization has been populated
        /// NVML_ERROR_UNINITIALIZED if the library has not been successfully initialized
        /// NVML_ERROR_INVALID_ARGUMENT if device is invalid or utilization is NULL
        /// NVML_ERROR_NOT_SUPPORTED if the device does not support this feature
        /// NVML_ERROR_GPU_IS_LOST if the target GPU has fallen off the bus or is otherwise inaccessible
        /// NVML_ERROR_UNKNOWN on any unexpected error
        /// </returns>
        [DllImport(NVML_DLL)]
        public static extern nvmlReturn_t nvmlDeviceGetUtilizationRates(IntPtr device, out nvmlUtilization_t utilization);

        [DllImport(NVML_DLL)]
        public static extern nvmlReturn_t nvmlDeviceGetTemperatureThreshold(IntPtr device, nvmlTemperatureThresholds_t thresholdType, out uint temp);

        [DllImport(NVML_DLL)]
        public static extern nvmlReturn_t nvmlDeviceGetMaxClockInfo(IntPtr device, nvmlClockType_t type, out uint clock);

        [DllImport(NVML_DLL)]
        public static extern nvmlReturn_t nvmlDeviceGetClockInfo(IntPtr device, nvmlClockType_t type, out uint clock);

        [DllImport(NVML_DLL)]
        public static extern nvmlReturn_t nvmlDeviceGetPowerUsage(IntPtr device, out uint power);
    }

    /// <summary>
    /// Encapsulates a GPU Device in way that a csharp user doesn't have
    /// to worry about Nvml native interop
    /// </summary>
    /// <remarks>
    /// To Use:
    /// 1. Call static nvmlInit before anything else
    /// 2. Use static GetDeviceCount to enumerate devices
    /// 3. Create an instance of NvGpu for each device
    /// 4. Call static nvmlShutdown() when done with all NvGpu instances
    /// GetDeviceCount is not guaranteed to enumerate devices in the same 
    /// order across reboots
    /// </remarks>
    public class NvGpu {
        private const uint MAX_NAME_LENGTH = 100;

        private IntPtr _handle;

        /// <summary>
        /// GPU Name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of NvGpu, using device index
        /// to initialize handle and name for the device
        /// </summary>
        /// <param name="deviceIdx">device index</param>
        public NvGpu(uint deviceIdx) {
            var r = Nvml.nvmlDeviceGetHandleByIndex(deviceIdx, out _handle);
            if (r != nvmlReturn_t.NVML_SUCCESS) {
                throw new Exception($"Unable to get device by handle: {r.ToString()}");
            }

            var name = new StringBuilder();
            r = Nvml.nvmlDeviceGetName(_handle, name, MAX_NAME_LENGTH);
            if (r != nvmlReturn_t.NVML_SUCCESS) {
                throw new Exception($"Unable to get device name: {r.ToString()}");
            }
        }

        /// <summary>
        /// Gets device utilization info
        /// </summary>
        /// <returns>utilization info and nvml return code</returns>
        public (nvmlUtilization_t, nvmlReturn_t) GetUtilization() {
            var r = Nvml.nvmlDeviceGetUtilizationRates(_handle, out nvmlUtilization_t u);
            return (u, r);
        }

        /// <summary>
        /// Gets device temperature in degrees celsius
        /// </summary>
        /// <returns>device temperature and nvml return code</returns>
        public (uint, nvmlReturn_t) GetTemperature() {
            var r = Nvml.nvmlDeviceGetTemperature(_handle, nvmlTemperatureSensors_t.NVML_TEMPERATURE_GPU, out uint t);
            return (t, r);
        }
    }
}