using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuitarInputController.Audio.Interfaces;
using GuitarInputController.Audio.Models;
using GuitarInputController.Core.Enums;
using GuitarInputController.Core.Models;
using GuitarInputController.Core.Services;
using GuitarInputController.Input.Interfaces;

namespace GuitarInputController.App.ViewModels;

/// <summary>
/// Central ViewModel for the main application window.
/// Manages audio capture, pitch detection, mapping configuration,
/// profile management, and auxiliary window control.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    // ── Injected services ────────────────────────────────────────
    private readonly IAudioCaptureService _audioCapture;
    private readonly IPitchDetector _pitchDetector;
    private readonly IMappingService _mappingService;
    private readonly IProfileService _profileService;
    private readonly ISettingsService _settingsService;
    private readonly IInputSimulator _inputSimulator;

    // ── Audio engine state ───────────────────────────────────────
    private readonly object _engineLock = new();
    private string? _lastNoteName;
    private DateTime _lastNoteOnTime = DateTime.MinValue;
    private DateTime _lastInputTime = DateTime.MinValue;
    private readonly HashSet<string> _activeHoldNotes = new();
    private bool _engineRunning;

    // Track per-note hold state for pulse vs hold mode
    private readonly Dictionary<string, DateTime> _noteStartTimes = new();

    // Cached settings snapshot for hot-path access
    private PitchDetectionSettings _pitchSettings = new();
    private AudioSettings _audioSettings = new();

    // ── Buffer accumulation for YIN pitch detection ──────────────
    // YIN 需要至少 2 * maxPeriod 个采样，其中 maxPeriod = sampleRate / minFrequency
    // 例如 44100/65 ≈ 678 → 至少需要 1356 个采样
    // 单个 buffer（40ms @ 44100Hz = 1764 采样）已经足够，但为了稳健性额外累积
    private readonly List<float> _sampleAccumulator = new();
    private const int MinAccumulatedSamples = 2048; // 略大于 2 * 44100/65

    // ── Silence detection for Hold-mode note release ─────────────
    private DispatcherTimer? _silenceTimer;
    private static readonly TimeSpan SilenceTimeout = TimeSpan.FromMilliseconds(80);

    public MainViewModel(
        IAudioCaptureService audioCapture,
        IPitchDetector pitchDetector,
        IMappingService mappingService,
        IProfileService profileService,
        ISettingsService settingsService,
        IInputSimulator inputSimulator)
    {
        _audioCapture = audioCapture;
        _pitchDetector = pitchDetector;
        _mappingService = mappingService;
        _profileService = profileService;
        _settingsService = settingsService;
        _inputSimulator = inputSimulator;

        // Load persisted settings
        var appSettings = _settingsService.Load();
        _pitchSettings = appSettings.PitchDetection;
        _audioSettings = appSettings.Audio;
        ApplyPitchDetectionSettings(_pitchSettings);

        // Apply input gain from saved settings
        _inputGainDb = _audioSettings.InputGainDb;
        _audioCapture.Gain = DbToLinear((float)_inputGainDb);

        // Initialize collections
        AudioDevices = new ObservableCollection<AudioDeviceInfo>();
        Mappings = new ObservableCollection<NoteMapping>();
        ProfileNames = new ObservableCollection<string>();

        // Initialise window state from settings
        FretboardTopMost = appSettings.Appearance.FretboardTopMost;
        FretboardOpacity = appSettings.Appearance.FretboardOpacity;
        KeyboardTopMost = appSettings.Appearance.KeyboardTopMost;
        KeyboardOpacity = appSettings.Appearance.KeyboardOpacity;

        // Load initial data
        RefreshDevices();
        RefreshProfiles();

        // Subscribe to audio events
        _audioCapture.AudioLevelChanged += OnAudioLevelChanged;
        _audioCapture.CaptureError += OnCaptureError;
    }

    // ─────────────────────────────────────────────────────────────
    //  Observable properties
    // ─────────────────────────────────────────────────────────────

    [ObservableProperty]
    private ObservableCollection<AudioDeviceInfo> _audioDevices;

    [ObservableProperty]
    private AudioDeviceInfo? _selectedDevice;

    [ObservableProperty]
    private bool _isCapturing;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCaptureCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopCaptureCommand))]
    private string _statusText = "就绪";

    [ObservableProperty]
    private string _currentNote = "—";

    [ObservableProperty]
    private double _currentFrequency;

    [ObservableProperty]
    private double _audioLevel;

    [ObservableProperty]
    private ObservableCollection<NoteMapping> _mappings;

    [ObservableProperty]
    private NoteMapping? _selectedMapping;

    [ObservableProperty]
    private ObservableCollection<string> _profileNames;

    [ObservableProperty]
    private string? _selectedProfile;

    // ── Fretboard window ─────────────────────────────────────────

    [ObservableProperty]
    private bool _isFretboardVisible;

    [ObservableProperty]
    private bool _fretboardTopMost;

    [ObservableProperty]
    private double _fretboardOpacity;

    // ── Keyboard window ──────────────────────────────────────────

    [ObservableProperty]
    private bool _isKeyboardVisible;

    [ObservableProperty]
    private bool _keyboardTopMost;

    [ObservableProperty]
    private double _keyboardOpacity;

    // ── Input gain ───────────────────────────────────────────────

    [ObservableProperty]
    private double _inputGainDb = 0.0;

    /// <summary>Called when InputGainDb changes; applies gain to the capture service.</summary>
    partial void OnInputGainDbChanged(double value)
    {
        _audioCapture.Gain = DbToLinear((float)value);
        // Persist to settings
        var settings = _settingsService.Load();
        settings.Audio.InputGainDb = value;
        _settingsService.Save(settings);
    }

    // ── Audio monitor ────────────────────────────────────────────

    [ObservableProperty]
    private bool _isMonitoring;

    /// <summary>Called when IsMonitoring changes; starts/stops the audio monitor loopback.</summary>
    partial void OnIsMonitoringChanged(bool value)
    {
        if (value)
            _audioCapture.StartMonitor();
        else
            _audioCapture.StopMonitor();
    }

    // ─────────────────────────────────────────────────────────────
    //  Commands — Audio capture
    // ─────────────────────────────────────────────────────────────

    private bool CanStartCapture() => SelectedDevice != null && !IsCapturing;

    [RelayCommand(CanExecute = nameof(CanStartCapture))]
    private void StartCapture()
    {
        if (SelectedDevice == null) return;

        StopAudioEngine();
        _audioCapture.StopCapture();

        try
        {
            // Apply current gain before starting
            _audioCapture.Gain = DbToLinear((float)_inputGainDb);

            _audioCapture.StartCapture(
                SelectedDevice.DeviceId,
                _audioSettings.SampleRate,
                _audioSettings.BufferSizeMs);

            StartAudioEngine();
            IsCapturing = true;
            StatusText = $"正在采集 — {SelectedDevice.DisplayName}";
            StartCaptureCommand.NotifyCanExecuteChanged();
            StopCaptureCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            StatusText = $"启动失败: {ex.Message}";
        }
    }

    private bool CanStopCapture() => IsCapturing;

    [RelayCommand(CanExecute = nameof(CanStopCapture))]
    private void StopCapture()
    {
        StopAudioEngine();
        _audioCapture.StopCapture();
        IsCapturing = false;
        CurrentNote = "—";
        CurrentFrequency = 0;
        AudioLevel = 0;
        StatusText = "已停止";
        StartCaptureCommand.NotifyCanExecuteChanged();
        StopCaptureCommand.NotifyCanExecuteChanged();
    }

    // ─────────────────────────────────────────────────────────────
    //  Commands — Monitor toggle
    // ─────────────────────────────────────────────────────────────

    [RelayCommand]
    private void ToggleMonitor()
    {
        IsMonitoring = !IsMonitoring;
    }

    // ─────────────────────────────────────────────────────────────
    //  Commands — Device & refresh
    // ─────────────────────────────────────────────────────────────

    [RelayCommand]
    private void RefreshDevices()
    {
        AudioDevices.Clear();
        var devices = _audioCapture.GetInputDevices();
        foreach (var d in devices)
            AudioDevices.Add(d);

        // Restore last-used device from settings
        if (SelectedDevice == null && !string.IsNullOrEmpty(_audioSettings.InputDeviceId))
        {
            SelectedDevice = AudioDevices.FirstOrDefault(
                d => d.DeviceId == _audioSettings.InputDeviceId);
        }

        if (SelectedDevice == null && AudioDevices.Count > 0)
            SelectedDevice = AudioDevices[0];
    }

    // ─────────────────────────────────────────────────────────────
    //  Commands — Mapping CRUD
    // ─────────────────────────────────────────────────────────────

    [RelayCommand]
    private void AddMapping()
    {
        var editor = new MappingEditorViewModel();
        var dialog = new Views.MappingEditorDialog { DataContext = editor };
        dialog.Owner = System.Windows.Application.Current.MainWindow;
        if (dialog.ShowDialog() == true && editor.Result != null)
        {
            _mappingService.AddMapping(editor.Result);
            Mappings = new ObservableCollection<NoteMapping>(_mappingService.GetAllMappings());
        }
    }

    [RelayCommand]
    private void EditMapping()
    {
        if (SelectedMapping == null) return;
        var editor = new MappingEditorViewModel(SelectedMapping);
        var dialog = new Views.MappingEditorDialog { DataContext = editor };
        dialog.Owner = System.Windows.Application.Current.MainWindow;
        if (dialog.ShowDialog() == true && editor.Result != null)
        {
            _mappingService.UpdateMapping(editor.Result);
            Mappings = new ObservableCollection<NoteMapping>(_mappingService.GetAllMappings());
        }
    }

    [RelayCommand]
    private void DeleteMapping()
    {
        if (SelectedMapping == null) return;
        _mappingService.RemoveMapping(SelectedMapping.Id);
        Mappings.Remove(SelectedMapping);
        SelectedMapping = null;
    }

    /// <summary>
    /// Called by the view layer after the user saves a new mapping in the dialog.
    /// </summary>
    public void CommitNewMapping(NoteMapping mapping)
    {
        _mappingService.AddMapping(mapping);
        Mappings.Add(mapping);
        _profileService.SaveProfile(_mappingService.CurrentProfile!);
        RefreshProfiles();
    }

    /// <summary>
    /// Called by the view layer after the user saves an edited mapping in the dialog.
    /// </summary>
    public void CommitEditMapping(NoteMapping mapping)
    {
        _mappingService.UpdateMapping(mapping);
        // Replace in observable collection
        var idx = -1;
        for (int i = 0; i < Mappings.Count; i++)
        {
            if (Mappings[i].Id == mapping.Id) { idx = i; break; }
        }

        if (idx >= 0)
        {
            Mappings[idx] = mapping;
        }

        _profileService.SaveProfile(_mappingService.CurrentProfile!);
    }

    // ─────────────────────────────────────────────────────────────
    //  Commands — Profile management
    // ─────────────────────────────────────────────────────────────

    [RelayCommand]
    private void NewProfile()
    {
        var profile = new MappingProfile
        {
            Name = $"新方案 {ProfileNames.Count + 1}",
            Description = string.Empty,
            Mappings = new List<NoteMapping>()
        };

        _profileService.SaveProfile(profile);
        LoadProfileInternal(profile);
        RefreshProfiles();
    }

    [RelayCommand]
    private void LoadProfile()
    {
        if (string.IsNullOrEmpty(SelectedProfile)) return;

        var profile = _profileService.LoadProfile(SelectedProfile);
        if (profile != null)
        {
            LoadProfileInternal(profile);
        }
    }

    [RelayCommand]
    private void SaveProfile()
    {
        if (_mappingService.CurrentProfile != null)
        {
            _profileService.SaveProfile(_mappingService.CurrentProfile);
            RefreshProfiles();
            StatusText = "方案已保存";
        }
    }

    [RelayCommand]
    private void ImportProfile()
    {
        // The view layer provides the file path via a file dialog.
    }

    [RelayCommand]
    private void ExportProfile()
    {
        // The view layer provides the file path via a save dialog.
    }

    /// <summary>Called by the view layer with the chosen file path.</summary>
    public void ImportProfileFromFile(string filePath)
    {
        var profile = _profileService.ImportProfile(filePath);
        if (profile != null)
        {
            LoadProfileInternal(profile);
            _profileService.SaveProfile(profile);
            RefreshProfiles();
            StatusText = $"已导入方案: {profile.Name}";
        }
    }

    /// <summary>Called by the view layer with the chosen file path.</summary>
    public void ExportProfileToFile(string filePath)
    {
        if (_mappingService.CurrentProfile != null)
        {
            _profileService.ExportProfile(_mappingService.CurrentProfile, filePath);
            StatusText = $"方案已导出至: {filePath}";
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Commands — Window toggles
    // ─────────────────────────────────────────────────────────────

    [RelayCommand]
    private void ToggleFretboard()
    {
        IsFretboardVisible = !IsFretboardVisible;
    }

    [RelayCommand]
    private void ToggleKeyboard()
    {
        IsKeyboardVisible = !IsKeyboardVisible;
    }

    // ─────────────────────────────────────────────────────────────
    //  Commands — Advanced settings
    // ─────────────────────────────────────────────────────────────

    [RelayCommand]
    private void OpenAdvancedSettings()
    {
        var vm = new AdvancedSettingsViewModel(_settingsService);
        var dialog = new Views.AdvancedSettingsWindow { DataContext = vm };
        dialog.Owner = System.Windows.Application.Current.MainWindow;
        dialog.ShowDialog();
        RefreshSettings();
    }

    /// <summary>
    /// Called after the advanced settings dialog is closed (whether saved or not),
    /// to re-apply the latest settings to the engine.
    /// </summary>
    public void RefreshSettings()
    {
        var appSettings = _settingsService.Load();
        _pitchSettings = appSettings.PitchDetection;
        _audioSettings = appSettings.Audio;
        ApplyPitchDetectionSettings(_pitchSettings);

        // Sync gain from settings
        InputGainDb = _audioSettings.InputGainDb;
        _audioCapture.Gain = DbToLinear((float)InputGainDb);

        FretboardTopMost = appSettings.Appearance.FretboardTopMost;
        FretboardOpacity = appSettings.Appearance.FretboardOpacity;
        KeyboardTopMost = appSettings.Appearance.KeyboardTopMost;
        KeyboardOpacity = appSettings.Appearance.KeyboardOpacity;
    }

    // ─────────────────────────────────────────────────────────────
    //  Audio engine
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Wires up the audio pipeline:
    /// AudioCaptureService.AudioDataAvailable → accumulate → YinPitchDetector.DetectPitch() → process result.
    /// </summary>
    private void StartAudioEngine()
    {
        lock (_engineLock)
        {
            if (_engineRunning) return;
            _engineRunning = true;
            _activeHoldNotes.Clear();
            _noteStartTimes.Clear();
            _lastNoteName = null;
            _lastNoteOnTime = DateTime.MinValue;
            _lastInputTime = DateTime.MinValue;
            _sampleAccumulator.Clear();

            _audioCapture.AudioDataAvailable += OnAudioDataAvailable;
        }
    }

    /// <summary>
    /// Tears down the audio pipeline and releases any held keys.
    /// </summary>
    private void StopAudioEngine()
    {
        lock (_engineLock)
        {
            if (!_engineRunning) return;
            _engineRunning = false;

            _audioCapture.AudioDataAvailable -= OnAudioDataAvailable;

            StopSilenceTimer();

            // Release all currently held keys
            foreach (var noteName in _activeHoldNotes.ToList())
            {
                ReleaseNote(noteName);
            }
            _activeHoldNotes.Clear();
            _noteStartTimes.Clear();
            _sampleAccumulator.Clear();
        }
    }

    private void OnAudioDataAvailable(object? sender, float[] samples)
    {
        // Fast path: skip if engine was stopped concurrently
        if (!_engineRunning) return;

        // ── Buffer accumulation ──────────────────────────────────
        // 累积采样直到有足够的数据供给 YIN 算法
        _sampleAccumulator.AddRange(samples);

        // 限制累积器大小，避免无限增长
        if (_sampleAccumulator.Count > 8192)
        {
            // 移除最旧的采样，保留最近的数据
            int removeCount = _sampleAccumulator.Count - 4096;
            _sampleAccumulator.RemoveRange(0, removeCount);
        }

        float[] bufferForDetection;
        if (_sampleAccumulator.Count >= MinAccumulatedSamples)
        {
            bufferForDetection = _sampleAccumulator.ToArray();
            // 保留最后一半以避免边界效应，确保连续检测
            int keepCount = _sampleAccumulator.Count / 2;
            _sampleAccumulator.RemoveRange(0, _sampleAccumulator.Count - keepCount);
        }
        else
        {
            // 累积不足，但至少尝试用现有数据检测
            bufferForDetection = _sampleAccumulator.ToArray();
        }

        // 在音频回调线程上执行音高检测
        var result = _pitchDetector.DetectPitch(bufferForDetection, _audioSettings.SampleRate);

        // 将 UI 属性更新封送至 UI 线程
        System.Windows.Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            ProcessPitchResult(result);
        });
    }

    private void ProcessPitchResult(PitchResult? result)
    {
        if (result == null || !result.IsValid)
        {
            // 无有效音符 → 启动静默检测（用于 Hold 模式音符释放）
            StartSilenceTimer();
            return;
        }

        // Confidence filter
        if (result.Confidence < _pitchSettings.ConfidenceThreshold)
        {
            StartSilenceTimer();
            return;
        }

        // Volume filter
        if (result.Volume < _pitchSettings.VolumeThreshold)
        {
            StartSilenceTimer();
            return;
        }

        // 检测到有效音符 → 取消静默计时器
        StopSilenceTimer();

        var now = DateTime.UtcNow;
        var noteFullName = result.FullName;

        // 此时已在 UI 线程上（调用方通过 Dispatcher.InvokeAsync 封送）
        CurrentFrequency = result.Frequency;
        CurrentNote = noteFullName;

        // Min duration filter — track when the note first appeared
        if (_lastNoteName != noteFullName)
        {
            // Different note detected; start timing
            _lastNoteName = noteFullName;
            _lastNoteOnTime = now;
        }
        else
        {
            // Same note continuing; enforce minimum duration
            var duration = (now - _lastNoteOnTime).TotalMilliseconds;
            if (duration < _pitchSettings.MinNoteDurationMs) return;
        }

        // Debounce — enforce minimum interval between inputs
        var intervalSinceLastInput = (now - _lastInputTime).TotalMilliseconds;
        if (intervalSinceLastInput < _pitchSettings.MinInputIntervalMs) return;

        // Look up mapping
        var mapping = _mappingService.FindMapping(noteFullName);
        if (mapping == null) return;

        // Process based on trigger mode
        if (mapping.TriggerMode == TriggerMode.Hold)
        {
            ProcessHoldMode(mapping, noteFullName, now);
        }
        else
        {
            ProcessPulseMode(mapping, noteFullName, now);
        }
    }

    private void ProcessHoldMode(NoteMapping mapping, string noteFullName, DateTime now)
    {
        // Record note start if not already tracking
        if (!_noteStartTimes.ContainsKey(noteFullName))
        {
            _noteStartTimes[noteFullName] = now;
        }

        var elapsed = (now - _noteStartTimes[noteFullName]).TotalMilliseconds;
        if (elapsed < _pitchSettings.MinNoteDurationMs) return;

        // Only fire NoteOn once per note
        if (!_activeHoldNotes.Contains(noteFullName))
        {
            _mappingService.NoteOn(noteFullName);
            _activeHoldNotes.Add(noteFullName);
            ExecuteMappingAction(mapping, isKeyDown: true, isHoldStart: true);
            _lastInputTime = now;
        }
    }

    private void ProcessPulseMode(NoteMapping mapping, string noteFullName, DateTime now)
    {
        // Pulse: fire once per note appearance, then debounce via MinInputInterval
        if (_lastNoteName == noteFullName && _lastInputTime != DateTime.MinValue)
        {
            var sinceLast = (now - _lastInputTime).TotalMilliseconds;
            if (sinceLast < _pitchSettings.MinInputIntervalMs) return;
        }

        ExecuteMappingAction(mapping, isKeyDown: false, isHoldStart: false);
        _lastInputTime = now;
    }

    private void ExecuteMappingAction(NoteMapping mapping, bool isKeyDown, bool isHoldStart)
    {
        try
        {
            switch (mapping.ActionType)
            {
                case ActionType.KeyPress:
                    if (!string.IsNullOrEmpty(mapping.KeyCode))
                    {
                        var vk = _inputSimulator.StringToVirtualKey(mapping.KeyCode);
                        if (vk.HasValue)
                        {
                            if (mapping.TriggerMode == TriggerMode.Hold)
                            {
                                if (isKeyDown)
                                    _inputSimulator.KeyDown(vk.Value);
                                else
                                    _inputSimulator.KeyUp(vk.Value);
                            }
                            else
                            {
                                _inputSimulator.KeyPress(vk.Value);
                            }
                        }
                    }
                    break;

                case ActionType.Combination:
                    if (!string.IsNullOrEmpty(mapping.KeyCode) && mapping.ModifierKeys.Count > 0)
                    {
                        _inputSimulator.SendCombination(mapping.ModifierKeys, mapping.KeyCode);
                    }
                    else if (!string.IsNullOrEmpty(mapping.KeyCode))
                    {
                        var vk = _inputSimulator.StringToVirtualKey(mapping.KeyCode);
                        if (vk.HasValue)
                            _inputSimulator.KeyPress(vk.Value);
                    }
                    break;

                case ActionType.MouseClick:
                    if (mapping.MouseButton.HasValue)
                    {
                        if (mapping.TriggerMode == TriggerMode.Hold)
                        {
                            if (isKeyDown)
                                _inputSimulator.MouseDown(mapping.MouseButton.Value);
                            else
                                _inputSimulator.MouseUp(mapping.MouseButton.Value);
                        }
                        else
                        {
                            _inputSimulator.MouseClick(mapping.MouseButton.Value);
                        }
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Windows.Application.Current?.Dispatcher.InvokeAsync(() =>
            {
                StatusText = $"输入模拟错误: {ex.Message}";
            });
        }
    }

    /// <summary>
    /// Releases a held note — sends KeyUp / MouseUp for the mapping.
    /// </summary>
    private void ReleaseNote(string noteFullName)
    {
        var mapping = _mappingService.FindMapping(noteFullName);
        if (mapping == null || mapping.TriggerMode != TriggerMode.Hold) return;

        _mappingService.NoteOff(noteFullName);

        switch (mapping.ActionType)
        {
            case ActionType.KeyPress:
                if (!string.IsNullOrEmpty(mapping.KeyCode))
                {
                    var vk = _inputSimulator.StringToVirtualKey(mapping.KeyCode);
                    if (vk.HasValue)
                        _inputSimulator.KeyUp(vk.Value);
                }
                break;

            case ActionType.Combination:
                if (!string.IsNullOrEmpty(mapping.KeyCode))
                {
                    var vk = _inputSimulator.StringToVirtualKey(mapping.KeyCode);
                    if (vk.HasValue)
                        _inputSimulator.KeyUp(vk.Value);
                }
                break;

            case ActionType.MouseClick:
                if (mapping.MouseButton.HasValue)
                    _inputSimulator.MouseUp(mapping.MouseButton.Value);
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Silence detection (for Hold-mode note release)
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 启动静默检测计时器。当连续无有效音符时，释放所有 Hold 模式的按键。
    /// </summary>
    private void StartSilenceTimer()
    {
        if (_silenceTimer != null) return;

        _silenceTimer = new DispatcherTimer(
            SilenceTimeout,
            DispatcherPriority.Normal,
            OnSilenceTimeout,
            System.Windows.Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher);
        _silenceTimer.Start();
    }

    /// <summary>
    /// 取消静默检测计时器。
    /// </summary>
    private void StopSilenceTimer()
    {
        if (_silenceTimer == null) return;
        _silenceTimer.Stop();
        _silenceTimer = null;
    }

    /// <summary>
    /// 静默超时回调：释放所有活跃的 Hold 模式按键。
    /// </summary>
    private void OnSilenceTimeout(object? sender, EventArgs e)
    {
        StopSilenceTimer();

        // 释放所有 Hold 模式按键
        var notesToRelease = _activeHoldNotes.ToList();
        foreach (var noteName in notesToRelease)
        {
            ReleaseNote(noteName);
            _activeHoldNotes.Remove(noteName);
            _noteStartTimes.Remove(noteName);
        }

        // 更新 UI
        System.Windows.Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            CurrentNote = "—";
            CurrentFrequency = 0;
        });
    }

    // ─────────────────────────────────────────────────────────────
    //  Event handlers
    // ─────────────────────────────────────────────────────────────

    private void OnAudioLevelChanged(object? sender, float level)
    {
        // WaveInEvent 在后台线程触发回调，需封送至 UI 线程更新绑定属性
        System.Windows.Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            AudioLevel = level;
        });
    }

    private void OnCaptureError(object? sender, string message)
    {
        // WaveInEvent 在后台线程触发回调，需封送至 UI 线程更新绑定属性
        System.Windows.Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            StatusText = $"采集错误: {message}";
            // Attempt graceful stop
            StopAudioEngine();
            IsCapturing = false;
            CurrentNote = "—";
            CurrentFrequency = 0;
            StartCaptureCommand.NotifyCanExecuteChanged();
            StopCaptureCommand.NotifyCanExecuteChanged();
        });
    }

    // ─────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────

    private void ApplyPitchDetectionSettings(PitchDetectionSettings settings)
    {
        _pitchDetector.ConfidenceThreshold = settings.ConfidenceThreshold;
        _pitchDetector.VolumeThreshold = settings.VolumeThreshold;
    }

    /// <summary>
    /// 将 dB 值转换为线性增益倍率
    /// </summary>
    private static float DbToLinear(float db)
    {
        return MathF.Pow(10.0f, db / 20.0f);
    }

    private void LoadProfileInternal(MappingProfile profile)
    {
        _mappingService.LoadProfile(profile);
        Mappings.Clear();
        foreach (var m in _mappingService.GetAllMappings())
        {
            Mappings.Add(m);
        }
        SelectedProfile = profile.Name;
        StatusText = $"已加载方案: {profile.Name}";
    }

    private void RefreshProfiles()
    {
        ProfileNames.Clear();
        var names = _profileService.ListProfiles();
        foreach (var n in names)
            ProfileNames.Add(n);
    }
}
