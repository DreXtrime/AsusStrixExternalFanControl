using System;
using System.Runtime.InteropServices;

namespace AsusFanControl;

/// <summary>
/// Minimal NvAPI wrapper for I2C communication with the ASUS GPU microcontroller.
///
/// Reverse engineered from ASUSGPUFanServiceEx.exe / Vender.dll on an ASUS GTX 1080 Strix.
/// The external fan headers are controlled via I2C through the NVIDIA driver:
///   - Device address: 0x52 (ASUS onboard microcontroller)
///   - Port ID: 1
///   - Fan speed register: 0x11
///   - Data: 0x00-0xFF (PWM, where 0xFF = 100%)
/// </summary>
internal static class NvApi
{
    [DllImport("nvapi.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr nvapi_QueryInterface(uint id);

    // NvAPI function IDs
    private const uint ID_Initialize        = 0x0150E828;
    private const uint ID_EnumPhysicalGPUs  = 0xE5AC921F;
    private const uint ID_I2CWriteEx        = 0x283AC65A;
    private const uint ID_I2CReadEx         = 0x4D7B0709;
    private const uint ID_GetThermalSettings = 0xE3640A56;

    // Delegates matching the actual NvAPI calling convention
    private delegate int InitializeDelegate();
    private delegate int EnumPhysicalGPUsDelegate(
        [Out, MarshalAs(UnmanagedType.LPArray, SizeConst = 64)] IntPtr[] gpuHandles,
        out int gpuCount);
    private delegate int I2CWriteExDelegate(IntPtr gpuHandle, ref NV_I2C_INFO_V3 i2cInfo, ref uint unknown);
    private delegate int I2CReadExDelegate(IntPtr gpuHandle, ref NV_I2C_INFO_V3 i2cInfo, ref uint unknown);
    private delegate int GetThermalSettingsDelegate(IntPtr gpuHandle, uint sensorIndex, ref NV_GPU_THERMAL_SETTINGS settings);

    private static InitializeDelegate?        _initialize;
    private static EnumPhysicalGPUsDelegate?  _enumPhysicalGPUs;
    private static I2CWriteExDelegate?        _i2cWriteEx;
    private static I2CReadExDelegate?         _i2cReadEx;
    private static GetThermalSettingsDelegate? _getThermalSettings;

    private static IntPtr[] _gpuHandles = Array.Empty<IntPtr>();

    public static void Initialize()
    {
        _initialize        = GetDelegate<InitializeDelegate>(ID_Initialize);
        _enumPhysicalGPUs  = GetDelegate<EnumPhysicalGPUsDelegate>(ID_EnumPhysicalGPUs);
        _i2cWriteEx        = GetDelegate<I2CWriteExDelegate>(ID_I2CWriteEx);
        _i2cReadEx         = GetDelegate<I2CReadExDelegate>(ID_I2CReadEx);
        _getThermalSettings = GetDelegate<GetThermalSettingsDelegate>(ID_GetThermalSettings);

        int status = _initialize();
        if (status != 0)
            throw new NvApiException("NvAPI_Initialize", status);

        var handles = new IntPtr[64];
        status = _enumPhysicalGPUs(handles, out int count);
        if (status != 0)
            throw new NvApiException("NvAPI_EnumPhysicalGPUs", status);
        if (count == 0)
            throw new Exception("No NVIDIA GPU found.");

        _gpuHandles = new IntPtr[count];
        Array.Copy(handles, _gpuHandles, count);
    }

    /// <summary>
    /// Writes a single byte to an I2C register on the GPU's onboard microcontroller.
    /// </summary>
    public static void I2CWrite(byte i2cAddress, byte portId, byte register, byte data, int gpuIndex = 0)
    {
        if (_i2cWriteEx is null) throw new InvalidOperationException("NvAPI not initialized.");

        byte[] regBuf  = [register];
        byte[] dataBuf = [data];

        unsafe
        {
            fixed (byte* pReg = regBuf, pData = dataBuf)
            {
                var info = new NV_I2C_INFO_V3
                {
                    version           = 0x0003002C,  // NV_I2C_INFO_VER3, size 0x2C
                    displayMask       = 0,
                    bIsDDCPort        = 0,
                    i2cDevAddress     = i2cAddress,
                    pbI2cRegAddress   = (IntPtr)pReg,
                    regAddrSize       = 1,
                    pbData            = (IntPtr)pData,
                    cbSize            = 1,
                    i2cSpeed          = 0xFFFF,       // NVAPI_I2C_SPEED_DEFAULT
                    i2cSpeedKhz       = 6,            // NV_I2C_SPEED_100KHZ
                    portId            = portId,
                    bIsPortIdSet      = 1
                };

                uint unknown = 0;
                int status = _i2cWriteEx(_gpuHandles[gpuIndex], ref info, ref unknown);
                if (status != 0)
                    throw new NvApiException("NvAPI_I2CWriteEx", status);
            }
        }
    }

    /// <summary>
    /// Reads a single byte from an I2C register.
    /// </summary>
    public static byte I2CRead(byte i2cAddress, byte portId, byte register, int gpuIndex = 0)
    {
        if (_i2cReadEx is null) throw new InvalidOperationException("NvAPI not initialized.");

        byte[] regBuf  = [register];
        byte[] dataBuf = [0];

        unsafe
        {
            fixed (byte* pReg = regBuf, pData = dataBuf)
            {
                var info = new NV_I2C_INFO_V3
                {
                    version           = 0x0003002C,
                    displayMask       = 0,
                    bIsDDCPort        = 0,
                    i2cDevAddress     = i2cAddress,
                    pbI2cRegAddress   = (IntPtr)pReg,
                    regAddrSize       = 1,
                    pbData            = (IntPtr)pData,
                    cbSize            = 1,
                    i2cSpeed          = 0xFFFF,
                    i2cSpeedKhz       = 6,
                    portId            = portId,
                    bIsPortIdSet      = 1
                };

                uint unknown = 0;
                int status = _i2cReadEx(_gpuHandles[gpuIndex], ref info, ref unknown);
                if (status != 0)
                    throw new NvApiException("NvAPI_I2CReadEx", status);

                return dataBuf[0];
            }
        }
    }

    /// <summary>
    /// Returns GPU core temperature in Celsius.
    /// </summary>
    public static int GetGpuTemperature(int gpuIndex = 0)
    {
        if (_getThermalSettings is null) throw new InvalidOperationException("NvAPI not initialized.");

        var settings = new NV_GPU_THERMAL_SETTINGS { version = 0x00020008 };
        int status = _getThermalSettings(_gpuHandles[gpuIndex], 0, ref settings);
        if (status != 0)
            throw new NvApiException("NvAPI_GPU_GetThermalSettings", status);

        return settings.sensor0CurrentTemp;
    }

    private static T GetDelegate<T>(uint id) where T : Delegate
    {
        IntPtr ptr = nvapi_QueryInterface(id);
        if (ptr == IntPtr.Zero || ptr == new IntPtr(unchecked((int)0xDEADBEEF)))
            throw new Exception($"NvAPI function 0x{id:X8} not available.");
        return Marshal.GetDelegateForFunctionPointer<T>(ptr);
    }
}

/// <summary>
/// NV_I2C_INFO_V3 struct, 32-bit layout (size 0x2C = 44 bytes).
/// Layout verified via x32dbg capture from Vender.dll on ASUS GTX 1080 Strix.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct NV_I2C_INFO_V3
{
    public uint    version;           // 0x00  0x0003002C
    public uint    displayMask;       // 0x04  0
    public byte    bIsDDCPort;        // 0x08  0
    public byte    i2cDevAddress;     // 0x09  0x52
    private ushort _pad0;             // 0x0A
    public IntPtr  pbI2cRegAddress;   // 0x0C  -> register byte
    public uint    regAddrSize;       // 0x10  1
    public IntPtr  pbData;            // 0x14  -> data byte
    public uint    cbSize;            // 0x18  1
    public uint    i2cSpeed;          // 0x1C  0xFFFF
    public uint    i2cSpeedKhz;       // 0x20  6
    public byte    portId;            // 0x24  1
    private byte   _pad1;             // 0x25
    private ushort _pad2;             // 0x26
    public uint    bIsPortIdSet;      // 0x28  1
    // Total: 0x2C = 44 bytes ✓
}

[StructLayout(LayoutKind.Sequential)]
internal struct NV_GPU_THERMAL_SETTINGS
{
    public uint version;
    public uint count;
    public int  sensor0CurrentTemp;
    // (abbreviated - we only need the first sensor temp)
}

internal class NvApiException : Exception
{
    public int ErrorCode { get; }
    public NvApiException(string function, int code)
        : base($"{function} returned error code 0x{code:X8}") => ErrorCode = code;
}