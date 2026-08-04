using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuitarInputController.Core.Models;
using GuitarInputController.Core.Services;

namespace GuitarInputController.App.ViewModels;

/// <summary>
/// ViewModel for the advanced settings dialog.
/// Provides bound properties for all pitch detection, audio, hotkey, and behavior settings.
/// </summary>
public partial class AdvancedSettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly AppSettings _originalSettings;

    /// <summary>Set by the view to close the dialog.</summary>
    public Action? CloseAction { get; set; }

    public AdvancedSettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        _originalSettings = _settingsService.Load();

        // Clone settings so edits are isolated until Save
        var current = _settingsService.Load();

        // ── Pitch detection ─────────────────────────────────
        VolumeThreshold = current.PitchDetection.VolumeThreshold;
        ConfidenceThreshold = current.PitchDetection.ConfidenceThreshold;
        MinNoteDurationMs = current.PitchDetection.MinNoteDurationMs;
        MinInputIntervalMs = current.PitchDetection.MinInputIntervalMs;

        // ── Audio settings ──────────────────────────────────
        SampleRate = current.Audio.SampleRate;
        BufferSizeMs = current.Audio.BufferSizeMs;
        InputGainDb = current.Audio.InputGainDb;

        // ── Tuning ──────────────────────────────────────────
        A4Frequency = current.PitchDetection.A4Frequency;

        // ── Hot keys ────────────────────────────────────────
        ToggleInputHotKey = current.HotKeys.ToggleInput;
        SwitchProfileHotKey = current.HotKeys.SwitchProfile;

        // ── Behavior ────────────────────────────────────────
        AutoStart = current.Behavior.AutoStart;
        StartMinimized = current.Behavior.StartMinimized;
        CloseToTray = current.Behavior.CloseToTray;
    }

    // ─────────────────────────────────────────────────────────
    //  Pitch detection settings
    // ─────────────────────────────────────────────────────────

    [ObservableProperty]
    private double _volumeThreshold;

    [ObservableProperty]
    private double _confidenceThreshold;

    [ObservableProperty]
    private int _minNoteDurationMs;

    [ObservableProperty]
    private int _minInputIntervalMs;

    // ─────────────────────────────────────────────────────────
    //  Audio settings
    // ─────────────────────────────────────────────────────────

    [ObservableProperty]
    private int _sampleRate;

    [ObservableProperty]
    private int _bufferSizeMs;

    [ObservableProperty]
    private double _inputGainDb;

    // ─────────────────────────────────────────────────────────
    //  Tuning
    // ─────────────────────────────────────────────────────────

    [ObservableProperty]
    private double _a4Frequency;

    // ─────────────────────────────────────────────────────────
    //  Hot key settings
    // ─────────────────────────────────────────────────────────

    [ObservableProperty]
    private string _toggleInputHotKey = "Ctrl+Shift+G";

    [ObservableProperty]
    private string _switchProfileHotKey = "Ctrl+Shift+P";

    // ─────────────────────────────────────────────────────────
    //  Behavior settings
    // ─────────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _autoStart;

    [ObservableProperty]
    private bool _startMinimized;

    [ObservableProperty]
    private bool _closeToTray;

    // ─────────────────────────────────────────────────────────
    //  Sample rate presets
    // ─────────────────────────────────────────────────────────

    public static IReadOnlyList<int> SampleRatePresets { get; } = new[] { 44100, 48000, 96000 };

    public static IReadOnlyList<int> BufferSizePresets { get; } = new[] { 10, 20, 40, 80 };

    // ─────────────────────────────────────────────────────────
    //  Commands
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Saves the current settings to persistent storage and closes the dialog.
    /// </summary>
    [RelayCommand]
    private void Save()
    {
        var settings = new AppSettings
        {
            PitchDetection = new PitchDetectionSettings
            {
                VolumeThreshold = VolumeThreshold,
                ConfidenceThreshold = ConfidenceThreshold,
                MinNoteDurationMs = MinNoteDurationMs,
                MinInputIntervalMs = MinInputIntervalMs,
                A4Frequency = A4Frequency
            },
            Audio = new AudioSettings
            {
                InputDeviceId = _originalSettings.Audio.InputDeviceId,
                SampleRate = SampleRate,
                BufferSizeMs = BufferSizeMs,
                InputGainDb = InputGainDb
            },
            HotKeys = new HotKeySettings
            {
                ToggleInput = ToggleInputHotKey,
                SwitchProfile = SwitchProfileHotKey
            },
            Behavior = new BehaviorSettings
            {
                AutoStart = AutoStart,
                StartMinimized = StartMinimized,
                CloseToTray = CloseToTray,
                CurrentProfile = _originalSettings.Behavior.CurrentProfile
            },
            Appearance = _originalSettings.Appearance,
            WindowPositions = _originalSettings.WindowPositions
        };

        _settingsService.Save(settings);
        CloseAction?.Invoke();
    }

    /// <summary>
    /// Discards changes and closes the dialog.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        CloseAction?.Invoke();
    }

    /// <summary>
    /// Resets all fields to the application defaults without saving.
    /// </summary>
    [RelayCommand]
    private void ResetToDefaults()
    {
        var defaults = _settingsService.GetDefault();

        VolumeThreshold = defaults.PitchDetection.VolumeThreshold;
        ConfidenceThreshold = defaults.PitchDetection.ConfidenceThreshold;
        MinNoteDurationMs = defaults.PitchDetection.MinNoteDurationMs;
        MinInputIntervalMs = defaults.PitchDetection.MinInputIntervalMs;

        SampleRate = defaults.Audio.SampleRate;
        BufferSizeMs = defaults.Audio.BufferSizeMs;
        InputGainDb = defaults.Audio.InputGainDb;

        A4Frequency = defaults.PitchDetection.A4Frequency;

        ToggleInputHotKey = defaults.HotKeys.ToggleInput;
        SwitchProfileHotKey = defaults.HotKeys.SwitchProfile;

        AutoStart = defaults.Behavior.AutoStart;
        StartMinimized = defaults.Behavior.StartMinimized;
        CloseToTray = defaults.Behavior.CloseToTray;
    }

    /// <summary>
    /// Validates the current settings values.
    /// </summary>
    public string? Validate()
    {
        if (VolumeThreshold < 0 || VolumeThreshold > 1.0)
            return "音量阈值必须在 0.0 到 1.0 之间";

        if (ConfidenceThreshold < 0 || ConfidenceThreshold > 1.0)
            return "置信度阈值必须在 0.0 到 1.0 之间";

        if (MinNoteDurationMs < 0 || MinNoteDurationMs > 1000)
            return "最小音符持续时长必须在 0 到 1000 毫秒之间";

        if (MinInputIntervalMs < 0 || MinInputIntervalMs > 5000)
            return "最小输入间隔必须在 0 到 5000 毫秒之间";

        if (SampleRate is not (44100 or 48000 or 96000))
            return "采样率必须是 44100、48000 或 96000 Hz";

        if (BufferSizeMs < 5 || BufferSizeMs > 200)
            return "缓冲区大小必须在 5 到 200 毫秒之间";

        if (A4Frequency < 400 || A4Frequency > 500)
            return "A4 频率必须在 400 到 500 Hz 之间";

        return null; // valid
    }
}
