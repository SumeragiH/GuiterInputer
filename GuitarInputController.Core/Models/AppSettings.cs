using GuitarInputController.Core.Enums;

namespace GuitarInputController.Core.Models;

/// <summary>
/// 应用全局设置聚合模型
/// </summary>
public class AppSettings
{
    public AudioSettings Audio { get; set; } = new();
    public PitchDetectionSettings PitchDetection { get; set; } = new();
    public HotKeySettings HotKeys { get; set; } = new();

    public AppearanceSettings Appearance { get; set; } = new();
    public BehaviorSettings Behavior { get; set; } = new();
    public WindowPositionSettings WindowPositions { get; set; } = new();
}

/// <summary>
/// 外观设置
/// </summary>
public class AppearanceSettings
{
    public bool FretboardTopMost { get; set; } = true;
    public double FretboardOpacity { get; set; } = 0.9;
    public bool KeyboardTopMost { get; set; } = true;
    public double KeyboardOpacity { get; set; } = 0.9;
    public KeyboardLayoutType KeyboardLayout { get; set; } = KeyboardLayoutType.Key104;
    public bool FretboardLocked { get; set; } = false;
    public bool FretboardSnapEnabled { get; set; } = true;

    // Window position persistence
    public int MainWindowLeft { get; set; }
    public int MainWindowTop { get; set; }
    public int MainWindowWidth { get; set; }
    public int MainWindowHeight { get; set; }
    public int FretboardLeft { get; set; }
    public int FretboardTop { get; set; }
    public int FretboardWidth { get; set; }
    public int FretboardHeight { get; set; }
    public int KeyboardLeft { get; set; }
    public int KeyboardTop { get; set; }
    public int KeyboardWidth { get; set; }
    public int KeyboardHeight { get; set; }
}

/// <summary>
/// 应用行为设置
/// </summary>
public class BehaviorSettings
{
    public bool AutoStart { get; set; } = false;
    public bool StartMinimized { get; set; } = true;
    public bool CloseToTray { get; set; } = true;
    public string CurrentProfile { get; set; } = "默认方案";
}

/// <summary>
/// 窗口位置记忆
/// </summary>
public class WindowPositionSettings
{
    public WindowPosition? FretboardWindow { get; set; }
    public WindowPosition? KeyboardWindow { get; set; }
}

/// <summary>
/// 单个窗口位置信息
/// </summary>
public class WindowPosition
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}
