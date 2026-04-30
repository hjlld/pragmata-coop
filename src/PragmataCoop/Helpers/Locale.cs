using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace PragmataCoop.Helpers;

public class Locale : INotifyPropertyChanged
{
    public static Locale Instance { get; } = new();
    private bool _isZh;

    private Locale()
    {
        _isZh = CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
        RefreshStrings();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsChinese => _isZh;

    public void SetLanguage(string lang)
    {
        bool newZh = lang == "zh" || lang == "Chinese";
        if (newZh == _isZh) return;
        _isZh = newZh;
        RefreshStrings();
        OnPropertyChanged(null);
    }

    private void RefreshStrings()
    {
        WindowTitle = _isZh ? "PRAGMATA 双人合作助手" : "PRAGMATA Co-op Assistant";
        AppTitle = _isZh ? "PRAGMATA 双人合作助手" : "PRAGMATA Co-op Assistant";
        BilibiliPromo = _isZh ? "如果你觉得好玩的话，请帮忙关注我的B站账号" : "If you enjoy this, could you buy me a beer";
        PromoImage = _isZh ? "Assets/bilibili_logo.png" : "Assets/support_me_on_kofi_beige.png";
        PromoUrl = _isZh ? "https://space.bilibili.com/121034560" : "https://ko-fi.com/hjlld";
        Controller1Label = _isZh ? "手柄1 (射击)" : "Controller 1 (Shooting)";
        Controller2Label = _isZh ? "手柄2 (走格子)" : "Controller 2 (Puzzle)";
        PresetLabel = _isZh ? "映射方案" : "Mapping Mode";
        PresetHint = _isZh ? "手柄2 按住 LT 时启用，释放后手柄1完全控制。" : "Hold C2 LT to activate puzzle mode. Release for C1 full control.";
        ButtonStart = _isZh ? " 启动 " : " Start ";
        ButtonStop = _isZh ? " 停止 " : " Stop ";
        PuzzleLabel = _isZh ? "合作: " : "Co-op: ";
        PuzzleActive = _isZh ? "已激活" : "Active";
        PuzzleInactive = _isZh ? "未激活" : "Inactive";
        PresetNames = _isZh ? new[] { "右摇杆", "按键", "混合" } : new[] { "Right Stick", "Buttons", "Mixed" };
        DetectC1Initial = _isZh ? "请转动爸爸手柄的左摇杆来识别手柄1" : "Move Hugh's left stick to identify Controller 1";
        DetectC1Prompt = _isZh ? "请转动爸爸手柄的左摇杆" : "Move Hugh's left stick";
        DetectC2Prompt = _isZh ? "请转动女儿手柄的右摇杆" : "Move Diana's right stick";
        DetectC1Next = _isZh ? "手柄1已识别 — 请转动女儿手柄的右摇杆" : "C1 detected — Move Diana's right stick";
        DetectBothDone = _isZh ? "两个手柄已识别 — 请点击「启动」" : "Both detected — Click Start";
        StatusRunning = _isZh ? "运行中" : "Running";
        DetectNoControllers = _isZh ? "未检测到已连接的手柄" : "No controllers detected";
        XInputSlotPrefix = "XInput #";
    }

    public string WindowTitle { get; private set; }
    public string AppTitle { get; private set; }
    public string BilibiliPromo { get; private set; }
    public string Controller1Label { get; private set; }
    public string Controller2Label { get; private set; }
    public string PresetLabel { get; private set; }
    public string PresetHint { get; private set; }
    public string ButtonStart { get; private set; }
    public string ButtonStop { get; private set; }
    public string PuzzleLabel { get; private set; }
    public string PuzzleActive { get; private set; }
    public string PuzzleInactive { get; private set; }
    public string[] PresetNames { get; private set; }
    public string DetectC1Initial { get; private set; }
    public string DetectC1Prompt { get; private set; }
    public string DetectC2Prompt { get; private set; }
    public string DetectC1Next { get; private set; }
    public string DetectBothDone { get; private set; }
    public string StatusRunning { get; private set; }
    public string DetectNoControllers { get; private set; }
    public string XInputSlotPrefix { get; private set; }
    public string PromoImage { get; private set; }
    public string PromoUrl { get; private set; }

    public string DetectC1Done(int slot) => _isZh ? $"已识别 ✓ (槽位 {slot})" : $"Detected ✓ (Slot {slot})";
    public string DetectC2Done(int slot) => _isZh ? $"已识别 ✓ (槽位 {slot})" : $"Detected ✓ (Slot {slot})";
    public string StatusVigemFail(string msg) => $"ViGEm {(_isZh ? "连接失败" : "connection failed")}: {msg}";
    public string XInputSlots(string slots) => (_isZh ? "XInput 槽位: [" : "XInput slots: [") + slots + "]";

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
