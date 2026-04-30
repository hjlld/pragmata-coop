namespace PragmataCoop.Models;

public struct ControllerState
{
    public bool Connected;
    public uint PacketNumber;

    // Buttons
    public bool DPadUp;
    public bool DPadDown;
    public bool DPadLeft;
    public bool DPadRight;
    public bool Start;
    public bool Back;
    public bool LeftThumb;
    public bool RightThumb;
    public bool LeftShoulder;
    public bool RightShoulder;
    public bool A;
    public bool B;
    public bool X;
    public bool Y;

    // Triggers (0-255)
    public byte LeftTrigger;
    public byte RightTrigger;

    // Sticks (-32768 to 32767)
    public short LeftStickX;
    public short LeftStickY;
    public short RightStickX;
    public short RightStickY;

    public static ControllerState Disconnected => new() { Connected = false };
}
