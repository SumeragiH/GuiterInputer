using GuitarInputController.Core.Enums;
using GuitarInputController.Core.Models;

namespace GuitarInputController.Core.Constants;

/// <summary>
/// 所有可配置参数的默认值
/// </summary>
public static class DefaultSettings
{
    public static AppSettings CreateDefault() => new()
    {
        Audio = new AudioSettings
        {
            InputDeviceId = string.Empty,
            SampleRate = 44100,
            BufferSizeMs = 40,
            InputGainDb = 0.0
        },
        PitchDetection = new PitchDetectionSettings
        {
            A4Frequency = 440.0,
            VolumeThreshold = 0.05,
            MinNoteDurationMs = 30,
            ConfidenceThreshold = 0.7,
            MinInputIntervalMs = 50
        },
        HotKeys = new HotKeySettings
        {
            ToggleInput = "Ctrl+Shift+G",
            SwitchProfile = "Ctrl+Shift+P"
        },
        Appearance = new AppearanceSettings
        {
            FretboardTopMost = true,
            FretboardOpacity = 0.9,
            KeyboardTopMost = true,
            KeyboardOpacity = 0.9,
            KeyboardLayout = KeyboardLayoutType.Key104,
            FretboardLocked = false,
            FretboardSnapEnabled = true
        },
        Behavior = new BehaviorSettings
        {
            AutoStart = false,
            StartMinimized = true,
            CloseToTray = true,
            CurrentProfile = "默认方案"
        },
        WindowPositions = new WindowPositionSettings()
    };
}
