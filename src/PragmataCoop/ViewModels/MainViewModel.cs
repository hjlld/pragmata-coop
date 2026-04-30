using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using PragmataCoop.Helpers;
using PragmataCoop.Models;
using PragmataCoop.Services;

namespace PragmataCoop.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly ControllerService _controllerService;
    private readonly VirtualControllerService _virtualController;
    private readonly MappingService _mappingService;
    private readonly HidHideService _hidHideService;
    private readonly AppSettings _settings;

    private string _status = "请转动爸爸手柄的左摇杆来识别手柄1";
    private string _controller1Status = "请转动爸爸手柄的左摇杆";
    private string _controller2Status = "请转动女儿手柄的右摇杆";
    private string _controller1Name = "";
    private string _controller2Name = "";
    private string _detectedSlots = "";
    private bool _controller1Connected;
    private bool _controller2Connected;
    private int _selectedPresetIndex;
    private bool _puzzleModeActive;
    private bool _started;

    private short _leftStickX1, _leftStickY1, _rightStickX1, _rightStickY1;
    private short _rightStickX2, _rightStickY2;
    private byte _leftTrigger1, _rightTrigger1, _leftTrigger2, _rightTrigger2;
    private bool _virtualConnected;
    private bool _c1Detected, _c2Detected;

    public MainViewModel()
    {
        _controllerService = new ControllerService();
        _virtualController = new VirtualControllerService();
        _mappingService = new MappingService();
        _hidHideService = new HidHideService();
        _settings = AppSettings.Load();
        _selectedPresetIndex = (int)_settings.SelectedPreset;

        _controllerService.ControllerAssigned += OnControllerAssigned;
        _controllerService.StateUpdated += OnStateUpdated;
        _controllerService.ConnectionChanged += OnConnectionChanged;

        StartCommand = new RelayCommand(StartMapping, () => _c1Detected && _c2Detected && !_started);
        StopCommand = new RelayCommand(StopAll, () => _started);

        if (_settings.StickThreshold != 0) _mappingService.SetThreshold((short)_settings.StickThreshold);
        _controllerService.StartDetection();
        UpdateDetectedSlots();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public string Status { get => _status; set { _status = value; OnPropertyChanged(); } }
    public string Controller1Status { get => _controller1Status; set { _controller1Status = value; OnPropertyChanged(); } }
    public string Controller2Status { get => _controller2Status; set { _controller2Status = value; OnPropertyChanged(); } }
    public string Controller1Name { get => _controller1Name; set { _controller1Name = value; OnPropertyChanged(); } }
    public string Controller2Name { get => _controller2Name; set { _controller2Name = value; OnPropertyChanged(); } }
    public string DetectedSlots { get => _detectedSlots; set { _detectedSlots = value; OnPropertyChanged(); } }
    public bool Controller1Connected { get => _controller1Connected; set { _controller1Connected = value; OnPropertyChanged(); } }
    public bool Controller2Connected { get => _controller2Connected; set { _controller2Connected = value; OnPropertyChanged(); } }
    public bool IsRunning => _started;
    public bool PuzzleModeActive { get => _puzzleModeActive; set { _puzzleModeActive = value; OnPropertyChanged(); } }
    public bool IsStartEnabled => _c1Detected && _c2Detected && !_started;
    public short LeftStickX1 { get => _leftStickX1; set { _leftStickX1 = value; OnPropertyChanged(); } }
    public short LeftStickY1 { get => _leftStickY1; set { _leftStickY1 = value; OnPropertyChanged(); } }
    public short RightStickX1 { get => _rightStickX1; set { _rightStickX1 = value; OnPropertyChanged(); } }
    public short RightStickY1 { get => _rightStickY1; set { _rightStickY1 = value; OnPropertyChanged(); } }
    public short RightStickX2 { get => _rightStickX2; set { _rightStickX2 = value; OnPropertyChanged(); } }
    public short RightStickY2 { get => _rightStickY2; set { _rightStickY2 = value; OnPropertyChanged(); } }
    public byte LeftTrigger1 { get => _leftTrigger1; set { _leftTrigger1 = value; OnPropertyChanged(); } }
    public byte RightTrigger1 { get => _rightTrigger1; set { _rightTrigger1 = value; OnPropertyChanged(); } }
    public byte LeftTrigger2 { get => _leftTrigger2; set { _leftTrigger2 = value; OnPropertyChanged(); } }
    public byte RightTrigger2 { get => _rightTrigger2; set { _rightTrigger2 = value; OnPropertyChanged(); } }
    public static string[] PresetDisplayNames { get; } = { "右摇杆", "按键", "混合" };
    public int SelectedPresetIndex { get => _selectedPresetIndex; set { _selectedPresetIndex = value; OnPropertyChanged(); SaveSettings(); } }
    public MappingPreset CurrentPreset => _selectedPresetIndex switch { 1 => MappingPreset.ButtonsToABXY, 2 => MappingPreset.Combined, _ => MappingPreset.StickToABXY };
    public RelayCommand StartCommand { get; }
    public RelayCommand StopCommand { get; }

    private void OnControllerAssigned(string which, int slot)
    {
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
        {
            UpdateDetectedSlots();
            if (which == "C1") { _c1Detected = true; Controller1Connected = true; Controller1Name = $"XInput #{slot}"; Controller1Status = $"已识别 ✓ (槽位 {slot})"; Status = "手柄1已识别 — 请转动女儿手柄的右摇杆"; }
            else { _c2Detected = true; Controller2Connected = true; Controller2Name = $"XInput #{slot}"; Controller2Status = $"已识别 ✓ (槽位 {slot})"; Status = "两个手柄已识别 — 请点击「启动」"; }
            OnPropertyChanged(nameof(IsStartEnabled));
            CommandManager.InvalidateRequerySuggested();
        });
    }

    public void StartMapping()
    {
        if (_started) return;
        try { _virtualController.Connect(); _virtualConnected = true;
            try { HidNameHelper.SetControllerName(0x1234, 0x5678, "PragmataVirtualController"); } catch { }
        }
        catch (Exception ex) { Status = "ViGEm 连接失败: " + ex.Message; _virtualConnected = false; }

        if (_virtualConnected)
        {
            try { int s = FindVirtualControllerSlot(); File.WriteAllText(Path.Combine(Path.GetTempPath(), "PragmataCoop.ini"), "[Settings]\r\nVirtualSlot=" + s + "\r\n"); } catch { }
            try { _hidHideService.BlockAllGamepads(System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? ""); } catch { }
        }

        _controllerService.StartMapping();
        _started = true;
        Status = "运行中";
        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(IsStartEnabled));
        CommandManager.InvalidateRequerySuggested();
    }

    public void StopAll()
    {
        _controllerService.Stop(); _virtualController.Disconnect(); _virtualConnected = false;
        _hidHideService.Deactivate();
        _started = false; _c1Detected = _c2Detected = false;
        Controller1Status = "请转动爸爸手柄的左摇杆"; Controller2Status = "请转动女儿手柄的右摇杆";
        Controller1Connected = false; Controller2Connected = false;
        Controller1Name = ""; Controller2Name = "";
        Status = "请转动爸爸手柄的左摇杆来识别手柄1";
        OnPropertyChanged(nameof(IsRunning)); OnPropertyChanged(nameof(IsStartEnabled));
        CommandManager.InvalidateRequerySuggested();
        _controllerService.StartDetection();
    }

    private void UpdateDetectedSlots()
    {
        try { var c = new System.Collections.Generic.List<int>(); for (int i = 0; i < 4; i++) { var (res, _) = XInputNative.GetStateRaw(i); if (res == 0) c.Add(i); } _detectedSlots = c.Count > 0 ? $"XInput: [{string.Join(",", c)}]" : ""; } catch { _detectedSlots = ""; }
        OnPropertyChanged(nameof(DetectedSlots));
    }

    private int FindVirtualControllerSlot()
    {
        for (int i = 0; i < 4; i++) { if (i == _controllerService.Index1 || i == _controllerService.Index2) continue; var (res, _) = XInputNative.GetStateRaw(i); if (res == XInputNative.ERROR_SUCCESS) return i; }
        return 0;
    }

    private void OnStateUpdated(ControllerState s1, ControllerState s2)
    {
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
        {
            if (s1.Connected) { LeftStickX1 = s1.LeftStickX; LeftStickY1 = s1.LeftStickY; RightStickX1 = s1.RightStickX; RightStickY1 = s1.RightStickY; LeftTrigger1 = s1.LeftTrigger; RightTrigger1 = s1.RightTrigger; }
            if (s2.Connected) { RightStickX2 = s2.RightStickX; RightStickY2 = s2.RightStickY; LeftTrigger2 = s2.LeftTrigger; RightTrigger2 = s2.RightTrigger; }
            _mappingService.IsActivated(s2, out var a); PuzzleModeActive = a;
            if (!_started) return;
            ApplyVirtualState(_mappingService.Map(s1, s2, CurrentPreset, a));
        });
    }

    private void ApplyVirtualState(Xbox360VirtualState s)
    {
        if (!_virtualConnected) return;
        _virtualController.SetButtonState(Xbox360Button.A, s.A); _virtualController.SetButtonState(Xbox360Button.B, s.B);
        _virtualController.SetButtonState(Xbox360Button.X, s.X); _virtualController.SetButtonState(Xbox360Button.Y, s.Y);
        _virtualController.SetButtonState(Xbox360Button.LeftShoulder, s.LeftShoulder); _virtualController.SetButtonState(Xbox360Button.RightShoulder, s.RightShoulder);
        _virtualController.SetButtonState(Xbox360Button.Start, s.Start); _virtualController.SetButtonState(Xbox360Button.Back, s.Back);
        _virtualController.SetButtonState(Xbox360Button.LeftThumb, s.LeftThumb); _virtualController.SetButtonState(Xbox360Button.RightThumb, s.RightThumb);
        _virtualController.SetButtonState(Xbox360Button.Up, s.DPadUp); _virtualController.SetButtonState(Xbox360Button.Down, s.DPadDown);
        _virtualController.SetButtonState(Xbox360Button.Left, s.DPadLeft); _virtualController.SetButtonState(Xbox360Button.Right, s.DPadRight);
        _virtualController.SetAxisValue(Xbox360Axis.LeftThumbX, s.LeftStickX); _virtualController.SetAxisValue(Xbox360Axis.LeftThumbY, s.LeftStickY);
        _virtualController.SetAxisValue(Xbox360Axis.RightThumbX, s.RightStickX); _virtualController.SetAxisValue(Xbox360Axis.RightThumbY, s.RightStickY);
        _virtualController.SetSliderValue(Xbox360Slider.LeftTrigger, s.LeftTrigger); _virtualController.SetSliderValue(Xbox360Slider.RightTrigger, s.RightTrigger);
    }

    private void OnConnectionChanged(int idx, bool con) => System.Windows.Application.Current?.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () => { if (idx == 1) Controller1Connected = con; else Controller2Connected = con; });

    private void SaveSettings() { _settings.SelectedPreset = CurrentPreset; _settings.Save(); }
    private void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
