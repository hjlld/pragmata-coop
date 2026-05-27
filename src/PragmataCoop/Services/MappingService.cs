using PragmataCoop.Models;

namespace PragmataCoop.Services;

public class MappingService
{
    private const byte TriggerThreshold = 64;
    private const short Deadzone = 8000;
    private short _threshold = 10000;

    public void SetThreshold(short threshold) => _threshold = threshold;
    public void SetDeadzone(short deadzone) { } // Reserved for future use

    public bool IsActivated(ControllerState state1, ControllerState state2, bool hughMainControl, out bool activated)
    {
        if (hughMainControl)
            activated = state1.Connected && state1.LeftTrigger > TriggerThreshold;
        else
            activated = state2.Connected && state2.LeftTrigger > TriggerThreshold;
        return activated;
    }

    public Xbox360VirtualState Map(
        ControllerState state1, ControllerState state2,
        MappingPreset preset, bool puzzleModeActivated, bool hughMainControl)
    {
        var result = new Xbox360VirtualState();

        if (puzzleModeActivated && ((hughMainControl && state1.Connected) || state2.Connected))
        {
            MapPlayer1Core(state1, ref result);
            result.A = false; result.B = false; result.X = false; result.Y = false;
            // C2's LT merges during puzzle mode regardless of who activated it
            if (state2.Connected && state2.LeftTrigger > result.LeftTrigger)
                result.LeftTrigger = state2.LeftTrigger;
            // In Hugh mode, C1's LT triggers puzzle, but C2 still controls ABXY
            MapPlayer2ABXY(state1, state2, preset, ref result);
        }
        else
        {
            MapFullController(state1, ref result);
            // C2's X always passes through (Diana's clue tracking)
            if (state2.Connected && state2.X)
                result.X = true;
        }

        return result;
    }

    private void MapPlayer1Core(ControllerState state1, ref Xbox360VirtualState result)
    {
        if (!state1.Connected) return;

        // Sticks
        result.LeftStickX = state1.LeftStickX;
        result.LeftStickY = state1.LeftStickY;
        result.RightStickX = state1.RightStickX;
        result.RightStickY = state1.RightStickY;

        // Triggers
        result.LeftTrigger = state1.LeftTrigger;
        result.RightTrigger = state1.RightTrigger;

        // Shoulder buttons
        result.LeftShoulder = state1.LeftShoulder;
        result.RightShoulder = state1.RightShoulder;

        // DPad
        result.DPadUp = state1.DPadUp;
        result.DPadDown = state1.DPadDown;
        result.DPadLeft = state1.DPadLeft;
        result.DPadRight = state1.DPadRight;

        // Menu buttons
        result.Start = state1.Start;
        result.Back = state1.Back;

        // Stick buttons
        result.LeftThumb = state1.LeftThumb;
        result.RightThumb = state1.RightThumb;
    }

    private void MapPlayer2ABXY(ControllerState state1, ControllerState state2,
        MappingPreset preset, ref Xbox360VirtualState result)
    {
        switch (preset)
        {
            case MappingPreset.StickToABXY:
                MapStickToABXY(state2, ref result);
                break;
            case MappingPreset.ButtonsToABXY:
                MapButtonsToABXY(state2, ref result);
                break;
            case MappingPreset.Combined:
                MapButtonsToABXY(state2, ref result);
                MapStickToABXY(state2, ref result);
                break;
        }
    }

    private void MapStickToABXY(ControllerState state2, ref Xbox360VirtualState result)
    {
        var thresh = _threshold;

        if (state2.RightStickY > thresh) result.Y = true;      // Up -> Y
        else if (state2.RightStickY < -thresh) result.A = true; // Down -> A

        if (state2.RightStickX < -thresh) result.X = true;     // Left -> X
        else if (state2.RightStickX > thresh) result.B = true; // Right -> B
    }

    private void MapButtonsToABXY(ControllerState state2, ref Xbox360VirtualState result)
    {
        result.A |= state2.A;
        result.B |= state2.B;
        result.X |= state2.X;
        result.Y |= state2.Y;
    }

    private void MapFullController(ControllerState state, ref Xbox360VirtualState result)
    {
        if (!state.Connected) return;

        MapPlayer1Core(state, ref result);
        // Also map ABXY from Player 1
        result.A = state.A;
        result.B = state.B;
        result.X = state.X;
        result.Y = state.Y;
    }
}

public struct Xbox360VirtualState
{
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
}
