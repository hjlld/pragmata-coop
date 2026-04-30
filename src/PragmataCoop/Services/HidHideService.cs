using Nefarius.Drivers.HidHide;
using Nefarius.Utilities.DeviceManagement.PnP;

namespace PragmataCoop.Services;

public class HidHideService : IDisposable
{
    private HidHideControlService? _service;
    private readonly HashSet<string> _keyboardVendors = new(StringComparer.OrdinalIgnoreCase)
        { "VID_046A", "VID_1532", "VID_17EF" };

    public bool IsAvailable => _service != null;

    public HidHideService()
    {
        try { _service = new HidHideControlService(); }
        catch { _service = null; }
    }

    /// <summary>
    /// Block all physical gamepad devices (HID + USB instances),
    /// whitelist our app, and activate.
    /// </summary>
    public void BlockAllGamepads(string appPath)
    {
        if (_service == null) return;

        // Step 1: Deactivate
        _service.IsActive = false;
        System.Threading.Thread.Sleep(100);

        // Step 2: Clear
        _service.ClearBlockedInstancesList();
        _service.ClearApplicationsList();

        // Step 3: Collect all instance IDs (HID + USB)
        var ids = CollectAllInstanceIds();

        // Step 4: Add to blocklist
        foreach (var id in ids) _service.AddBlockedInstanceId(id);

        // Step 5: Whitelist our app
        if (!string.IsNullOrEmpty(appPath))
            _service.AddApplicationPath(appPath);

        // Step 6: Activate
        _service.IsActive = true;
    }

    private HashSet<string> CollectAllInstanceIds()
    {
        var result = new HashSet<string>();

        // Pass 1: HID device interfaces
        var hidGuid = DeviceInterfaceIds.HidDevice;
        for (int i = 0; i < 64; i++)
        {
            if (!Devcon.FindByInterfaceGuid(hidGuid, out string _, out string instanceId, i, true))
                break;
            if (instanceId == null) continue;
            if (IsVirtual(instanceId) || IsKeyboard(instanceId)) continue;
            result.Add(instanceId);
        }

        // Pass 2: USB device interfaces
        var usbGuid = DeviceInterfaceIds.UsbDevice;
        for (int i = 0; i < 64; i++)
        {
            if (!Devcon.FindByInterfaceGuid(usbGuid, out string _, out string instanceId, i, true))
                break;
            if (instanceId == null) continue;
            if (IsVirtual(instanceId) || IsKeyboard(instanceId)) continue;
            result.Add(instanceId);
        }

        return result;
    }

    private bool IsVirtual(string id) =>
        id.StartsWith("ROOT\\", StringComparison.OrdinalIgnoreCase);

    private bool IsKeyboard(string id) =>
        _keyboardVendors.Any(v => id.Contains(v));

    public void Deactivate()
    {
        if (_service != null) _service.IsActive = false;
    }

    public void Dispose() => _service = null;
}
