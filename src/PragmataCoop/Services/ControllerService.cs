using PragmataCoop.Helpers;
using PragmataCoop.Models;

namespace PragmataCoop.Services;

public class ControllerService
{
    private const short DetectThreshold = 8000;
    private CancellationTokenSource? _cts;
    private Task? _pollTask;
    private int _c1Slot = -1;
    private int _c2Slot = -1;

    /// <summary>Fired when C1 or C2 is assigned. (slot) is the XInput index.</summary>
    public event Action<string, int>? ControllerAssigned;
    /// <summary>Fired when both controllers are detected.</summary>
    public event Action? BothDetected;
    /// <summary>State push for mapping phase</summary>
    public event Action<ControllerState, ControllerState>? StateUpdated;
    public event Action<int, bool>? ConnectionChanged;

    public ControllerState State1 { get; private set; } = ControllerState.Disconnected;
    public ControllerState State2 { get; private set; } = ControllerState.Disconnected;
    public int Index1 => _c1Slot;
    public int Index2 => _c2Slot;
    public bool C1Detected => _c1Slot >= 0;
    public bool C2Detected => _c2Slot >= 0;
    public bool IsRunning => _pollTask != null && !_pollTask.IsCompleted;

    /// <summary>Start detection loop only (no mapping). Runs until both controllers assigned or stopped.</summary>
    public void StartDetection()
    {
        if (IsRunning) return;
        _c1Slot = -1;
        _c2Slot = -1;
        _cts = new CancellationTokenSource();
        _pollTask = Task.Run(() => DetectionLoop(_cts.Token));
    }

    /// <summary>Transition to mapping mode using already-detected slots.</summary>
    public void StartMapping()
    {
        if (_c1Slot < 0 || _c2Slot < 0) return;
        _cts?.Cancel();
        _pollTask = null;
        
        _cts = new CancellationTokenSource();
        _pollTask = Task.Run(() => MappingLoop(_cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _pollTask = null;
        State1 = ControllerState.Disconnected;
        State2 = ControllerState.Disconnected;
        _c1Slot = -1;
        _c2Slot = -1;
    }

    private async Task DetectionLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var raw = ReadAllSlots();

            if (_c1Slot < 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (!raw[i].Connected) continue;
                    if (Math.Abs(raw[i].LeftStickX) > DetectThreshold ||
                        Math.Abs(raw[i].LeftStickY) > DetectThreshold)
                    {
                        _c1Slot = i;
                        ControllerAssigned?.Invoke("C1", i);
                        break;
                    }
                }
            }
            else if (_c2Slot < 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (!raw[i].Connected || i == _c1Slot) continue;
                    if (Math.Abs(raw[i].RightStickX) > DetectThreshold ||
                        Math.Abs(raw[i].RightStickY) > DetectThreshold)
                    {
                        _c2Slot = i;
                        ControllerAssigned?.Invoke("C2", i);
                        BothDetected?.Invoke();
                        break;
                    }
                }
            }

            // Keep emitting state for UI preview even after both detected
            if (_c1Slot >= 0 && _c2Slot >= 0)
            {
                var c1 = raw[_c1Slot];
                var c2 = raw[_c2Slot];
                State1 = c1;
                State2 = c2;
                StateUpdated?.Invoke(c1, c2);
                // Don't break — keep polling for UI until StartMapping is called
            }
            else
            {
                // During detection, still show C1 if assigned
                var c1 = _c1Slot >= 0 ? raw[_c1Slot] : ControllerState.Disconnected;
                var c2 = ControllerState.Disconnected;
                State1 = c1;
                State2 = c2;
                StateUpdated?.Invoke(c1, c2);
            }

            try { await Task.Delay(16, ct); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task MappingLoop(CancellationToken ct)
    {
        bool c1Conn = false, c2Conn = false;
        bool init = false;

        while (!ct.IsCancellationRequested)
        {
            var raw = ReadAllSlots();
            var c1 = raw[_c1Slot];
            var c2 = raw[_c2Slot];

            if (!init)
            {
                init = true;
                c1Conn = c1.Connected;
                c2Conn = c2.Connected;
                ConnectionChanged?.Invoke(1, c1.Connected);
                ConnectionChanged?.Invoke(2, c2.Connected);
            }
            else
            {
                if (c1.Connected != c1Conn) { c1Conn = c1.Connected; ConnectionChanged?.Invoke(1, c1.Connected); }
                if (c2.Connected != c2Conn) { c2Conn = c2.Connected; ConnectionChanged?.Invoke(2, c2.Connected); }
            }

            State1 = c1;
            State2 = c2;
            StateUpdated?.Invoke(c1, c2);

            try { await Task.Delay(16, ct); }
            catch (OperationCanceledException) { break; }
        }
    }

    private static ControllerState[] ReadAllSlots()
    {
        var raw = new ControllerState[4];
        for (int i = 0; i < 4; i++)
        {
            var (res, bytes) = XInputNative.GetStateRaw(i);
            raw[i] = res == XInputNative.ERROR_SUCCESS
                ? XInputNative.ParseRawState(bytes) : ControllerState.Disconnected;
        }
        return raw;
    }
}
