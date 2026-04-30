using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace PragmataCoop.Services;

public class VirtualControllerService : IDisposable
{
    private ViGEmClient? _client;
    private IXbox360Controller? _controller;
    private bool _disposed;

    public bool IsConnected => _controller != null;

    public void Connect()
    {
        if (IsConnected) return;

        _client = new ViGEmClient();
        // Custom VID/PID to distinguish from physical Xbox 360 (VID_045E/PID_028E)
        // In HidHide Configuration Client, look for: HID\VID_1234&PID_5678
        _controller = _client.CreateXbox360Controller(0x1234, 0x5678);
        _controller.Connect();
        _controller.FeedbackReceived += OnFeedbackReceived;
    }

    public void Disconnect()
    {
        if (_controller != null)
        {
            _controller.FeedbackReceived -= OnFeedbackReceived;
            _controller.Disconnect();
            _controller = null;
        }
        _client?.Dispose();
        _client = null;
    }

    public void SetButtonState(Xbox360Button button, bool pressed)
    {
        _controller?.SetButtonState(button, pressed);
    }

    public void SetAxisValue(Xbox360Axis axis, short value)
    {
        var clamped = value;
        if (clamped < -32768) clamped = -32768;
        if (clamped > 32767) clamped = 32767;
        _controller?.SetAxisValue(axis, clamped);
    }

    public void SetSliderValue(Xbox360Slider slider, byte value)
    {
        _controller?.SetSliderValue(slider, value);
    }

    private void OnFeedbackReceived(object sender, Xbox360FeedbackReceivedEventArgs e)
    {
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Disconnect();
            _disposed = true;
        }
    }
}
