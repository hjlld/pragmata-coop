using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace PragmataCoop.Helpers;

internal static class HidNameHelper
{
    [DllImport("hid.dll", SetLastError = true)]
    private static extern bool HidD_SetProductString(SafeFileHandle HidDeviceObject, string ProductString, uint ProductStringLength);

    [DllImport("hid.dll", SetLastError = true)]
    private static extern bool HidD_SetManufacturerString(SafeFileHandle HidDeviceObject, string ManufacturerString, uint ManufacturerStringLength);

    [DllImport("hid.dll", SetLastError = true)]
    private static extern bool HidD_SetSerialNumberString(SafeFileHandle HidDeviceObject, string SerialNumberString, uint SerialNumberStringLength);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern SafeFileHandle CreateFileW(
        string lpFileName, uint dwDesiredAccess, uint dwShareMode,
        IntPtr lpSecurityAttributes, uint dwCreationDisposition,
        uint dwFlagsAndAttributes, IntPtr hTemplateFile);

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr SetupDiGetClassDevsW(
        ref Guid ClassGuid, string? Enumerator, IntPtr hwndParent, uint Flags);

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool SetupDiEnumDeviceInterfaces(
        IntPtr DeviceInfoSet, IntPtr DeviceInfoData, ref Guid InterfaceClassGuid,
        uint MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool SetupDiGetDeviceInterfaceDetailW(
        IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData,
        IntPtr DeviceInterfaceDetailData, uint DeviceInterfaceDetailDataSize,
        out uint RequiredSize, IntPtr DeviceInfoData);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

    private const uint DIGCF_PRESENT = 0x00000002;
    private const uint DIGCF_DEVICEINTERFACE = 0x00000010;
    private const uint GENERIC_WRITE = 0x40000000;
    private const uint FILE_SHARE_READ = 1;
    private const uint FILE_SHARE_WRITE = 2;
    private const uint OPEN_EXISTING = 3;
    private const uint FILE_ATTRIBUTE_NORMAL = 0x80;

    [StructLayout(LayoutKind.Sequential)]
    private struct SP_DEVICE_INTERFACE_DATA
    {
        public uint cbSize;
        public Guid InterfaceClassGuid;
        public uint Flags;
        public IntPtr Reserved;
    }

    // GUID_DEVINTERFACE_HID = {4D1E55B2-F16F-11CF-88CB-001111000030}
    private static readonly Guid HidGuid = new Guid("4D1E55B2-F16F-11CF-88CB-001111000030");

    /// <summary>
    /// Set the product string of the ViGEm virtual controller by VID/PID.
    /// Must be called after ViGEm creates the controller.
    /// </summary>
    public static void SetControllerName(ushort vid, ushort pid, string newName)
    {
        // Search for HID device with matching VID/PID
        var hwId = $"VID_{vid:X4}&PID_{pid:X4}";
        var hidGuid = HidGuid; // Local copy for ref parameter

        var devInfo = SetupDiGetClassDevsW(ref hidGuid, null, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
        if (devInfo == IntPtr.Zero || devInfo == new IntPtr(-1)) return;

        try
        {
            var ifaceData = new SP_DEVICE_INTERFACE_DATA { cbSize = (uint)Marshal.SizeOf<SP_DEVICE_INTERFACE_DATA>() };
            for (uint i = 0; SetupDiEnumDeviceInterfaces(devInfo, IntPtr.Zero, ref hidGuid, i, ref ifaceData); i++)
            {
                // Get required buffer size
                SetupDiGetDeviceInterfaceDetailW(devInfo, ref ifaceData, IntPtr.Zero, 0, out uint needed, IntPtr.Zero);
                if (needed == 0) continue;

                var detailPtr = Marshal.AllocHGlobal((int)needed);
                try
                {
                    Marshal.WriteInt32(detailPtr, 8); // cbSize for 64-bit
                    if (!SetupDiGetDeviceInterfaceDetailW(devInfo, ref ifaceData, detailPtr, needed, out _, IntPtr.Zero))
                        continue;

                    // The path starts at offset 4 (cbSize) in the struct
                    var path = Marshal.PtrToStringUni(detailPtr + 4);
                    if (string.IsNullOrEmpty(path) || !path.Contains(hwId)) continue;

                    // Found our device — open it and set the product string
                    using var handle = CreateFileW(path, GENERIC_WRITE,
                        FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero,
                        OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);

                    if (!handle.IsInvalid)
                    {
                        HidD_SetProductString(handle, newName, (uint)(newName.Length * 2));
                        HidD_SetManufacturerString(handle, "PragmataCoop", (uint)("PragmataCoop".Length * 2));
                        HidD_SetSerialNumberString(handle, "PC0001", (uint)("PC0001".Length * 2));
                    }
                    return; // Done after finding and modifying the first match
                }
                finally
                {
                    Marshal.FreeHGlobal(detailPtr);
                }
            }
        }
        finally
        {
            SetupDiDestroyDeviceInfoList(devInfo);
        }
    }
}
