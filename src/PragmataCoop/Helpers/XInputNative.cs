using System.Runtime.InteropServices;
using PragmataCoop.Models;

namespace PragmataCoop.Helpers;

internal static class XInputNative
{
    private const string XInputDll4 = "xinput1_4.dll";
    private const string XInputDll3 = "xinput1_3.dll";

    public const int ERROR_SUCCESS = 0;
    public const int ERROR_DEVICE_NOT_CONNECTED = 0x48F;

    private static bool _probedDll3;
    private static bool _useDll3;

    public static int GetState(int userIndex, out XINPUT_STATE state)
    {
        if (!_probedDll3)
        {
            _probedDll3 = true;
            try
            {
                var probe = default(XINPUT_STATE);
                _ = XInputGetState4(0, ref probe);
            }
            catch (DllNotFoundException)
            {
                _useDll3 = true;
            }
        }

        state = default;
        if (_useDll3)
            return XInputGetState3(userIndex, ref state);
        else
            return XInputGetState4(userIndex, ref state);
    }

    // Use ref instead of out for safer marshaling
    [DllImport(XInputDll4, EntryPoint = "XInputGetState")]
    private static extern int XInputGetState4(int dwUserIndex, ref XINPUT_STATE pState);

    [DllImport(XInputDll3, EntryPoint = "XInputGetState")]
    private static extern int XInputGetState3(int dwUserIndex, ref XINPUT_STATE pState);

    /// <summary>
    /// Read raw bytes from XInputGetState, bypassing struct marshaling, for debugging.
    /// </summary>
    public static (int result, byte[] raw) GetStateRaw(int userIndex)
    {
        var bytes = new byte[16];
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            int result;
            if (_useDll3 || !_probedDll3)
            {
                if (!_probedDll3)
                {
                    _probedDll3 = true;
                    try { XInputGetStateRaw4(0, handle.AddrOfPinnedObject()); }
                    catch (DllNotFoundException) { _useDll3 = true; }
                }
                result = _useDll3
                    ? XInputGetStateRaw3(userIndex, handle.AddrOfPinnedObject())
                    : XInputGetStateRaw4(userIndex, handle.AddrOfPinnedObject());
            }
            else
            {
                result = XInputGetStateRaw4(userIndex, handle.AddrOfPinnedObject());
            }
            return (result, bytes);
        }
        finally
        {
            handle.Free();
        }
    }

    [DllImport(XInputDll4, EntryPoint = "XInputGetState")]
    private static extern int XInputGetStateRaw4(int dwUserIndex, IntPtr pState);

    [DllImport(XInputDll3, EntryPoint = "XInputGetState")]
    private static extern int XInputGetStateRaw3(int dwUserIndex, IntPtr pState);

    public static ControllerState ParseRawState(byte[] raw)
    {
        if (raw.Length < 16) return ControllerState.Disconnected;

        uint packetNumber = BitConverter.ToUInt32(raw, 0);
        ushort buttons = BitConverter.ToUInt16(raw, 4);
        byte leftTrigger = raw[6];
        byte rightTrigger = raw[7];
        short thumbLX = BitConverter.ToInt16(raw, 8);
        short thumbLY = BitConverter.ToInt16(raw, 10);
        short thumbRX = BitConverter.ToInt16(raw, 12);
        short thumbRY = BitConverter.ToInt16(raw, 14);

        return new ControllerState
        {
            Connected = true,
            PacketNumber = packetNumber,
            DPadUp = (buttons & 0x0001) != 0,
            DPadDown = (buttons & 0x0002) != 0,
            DPadLeft = (buttons & 0x0004) != 0,
            DPadRight = (buttons & 0x0008) != 0,
            Start = (buttons & 0x0010) != 0,
            Back = (buttons & 0x0020) != 0,
            LeftThumb = (buttons & 0x0040) != 0,
            RightThumb = (buttons & 0x0080) != 0,
            LeftShoulder = (buttons & 0x0100) != 0,
            RightShoulder = (buttons & 0x0200) != 0,
            A = (buttons & 0x1000) != 0,
            B = (buttons & 0x2000) != 0,
            X = (buttons & 0x4000) != 0,
            Y = (buttons & 0x8000) != 0,
            LeftTrigger = leftTrigger,
            RightTrigger = rightTrigger,
            LeftStickX = thumbLX,
            LeftStickY = thumbLY,
            RightStickX = thumbRX,
            RightStickY = thumbRY
        };
    }

    public static ControllerState ToControllerState(this XINPUT_STATE state)
    {
        var gp = state.Gamepad;
        return new ControllerState
        {
            Connected = true,
            PacketNumber = state.dwPacketNumber,
            DPadUp = (gp.wButtons & 0x0001) != 0,
            DPadDown = (gp.wButtons & 0x0002) != 0,
            DPadLeft = (gp.wButtons & 0x0004) != 0,
            DPadRight = (gp.wButtons & 0x0008) != 0,
            Start = (gp.wButtons & 0x0010) != 0,
            Back = (gp.wButtons & 0x0020) != 0,
            LeftThumb = (gp.wButtons & 0x0040) != 0,
            RightThumb = (gp.wButtons & 0x0080) != 0,
            LeftShoulder = (gp.wButtons & 0x0100) != 0,
            RightShoulder = (gp.wButtons & 0x0200) != 0,
            A = (gp.wButtons & 0x1000) != 0,
            B = (gp.wButtons & 0x2000) != 0,
            X = (gp.wButtons & 0x4000) != 0,
            Y = (gp.wButtons & 0x8000) != 0,
            LeftTrigger = gp.bLeftTrigger,
            RightTrigger = gp.bRightTrigger,
            LeftStickX = gp.sThumbLX,
            LeftStickY = gp.sThumbLY,
            RightStickX = gp.sThumbRX,
            RightStickY = gp.sThumbRY
        };
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XINPUT_STATE
    {
        public uint dwPacketNumber;
        public XINPUT_GAMEPAD Gamepad;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XINPUT_GAMEPAD
    {
        public ushort wButtons;
        public byte bLeftTrigger;
        public byte bRightTrigger;
        public short sThumbLX;
        public short sThumbLY;
        public short sThumbRX;
        public short sThumbRY;
    }
}
