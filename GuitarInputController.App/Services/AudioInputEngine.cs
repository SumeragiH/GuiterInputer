using System.Timers;
using GuitarInputController.Audio.Interfaces;
using GuitarInputController.Audio.Models;
using GuitarInputController.Core.Enums;
using GuitarInputController.Core.Models;
using GuitarInputController.Core.Services;
using GuitarInputController.Input.Interfaces;
using Timer = System.Timers.Timer;

namespace GuitarInputController.App.Services;

/// <summary>
/// 音频输入引擎 — 连接音频采集、音高检测、映射查找和键鼠模拟的核心管线
/// </summary>
public class AudioInputEngine
{
    private readonly IAudioCaptureService _audioCapture;
    private readonly IPitchDetector _pitchDetector;
    private readonly IMappingService _mappingService;
    private readonly IInputSimulator _inputSimulator;
    private readonly ISettingsService _settingsService;

    private PitchDetectionSettings _settings = new();

    // 音符状态追踪
    private readonly Dictionary<string, NoteState> _noteStates = new();
    private string? _lastNoteName;
    private DateTime _lastNoteTime = DateTime.MinValue;

    // 用于"无音符"期间的定时检查（处理 Note Off）
    private Timer? _silenceTimer;

    public event Action<string?, double, double>? OnNoteDetected;
    public event Action<double>? OnAudioLevelChanged;
    public event Action<string>? OnMappingTriggered;
    public event Action<string?>? OnStatusChanged;

    public bool IsRunning { get; private set; }

    public AudioInputEngine(
        IAudioCaptureService audioCapture,
        IPitchDetector pitchDetector,
        IMappingService mappingService,
        IInputSimulator inputSimulator,
        ISettingsService settingsService)
    {
        _audioCapture = audioCapture;
        _pitchDetector = pitchDetector;
        _mappingService = mappingService;
        _inputSimulator = inputSimulator;
        _settingsService = settingsService;
    }

    public void Start(string deviceId, int sampleRate = 44100, int bufferSizeMs = 20)
    {
        if (IsRunning) Stop();

        // 重新加载设置
        var appSettings = _settingsService.Load();
        _settings = appSettings.PitchDetection;
        _pitchDetector.ConfidenceThreshold = _settings.ConfidenceThreshold;
        _pitchDetector.VolumeThreshold = _settings.VolumeThreshold;

        _audioCapture.AudioDataAvailable += OnAudioDataAvailable;
        _audioCapture.AudioLevelChanged += OnAudioLevelUpdate;
        _audioCapture.CaptureError += OnCaptureError;

        _audioCapture.StartCapture(deviceId, sampleRate, bufferSizeMs);
        IsRunning = true;
        OnStatusChanged?.Invoke("监听中…");
    }

    public void Stop()
    {
        if (!IsRunning) return;

        _audioCapture.AudioDataAvailable -= OnAudioDataAvailable;
        _audioCapture.AudioLevelChanged -= OnAudioLevelUpdate;
        _audioCapture.CaptureError -= OnCaptureError;

        _audioCapture.StopCapture();
        _silenceTimer?.Stop();
        _silenceTimer?.Dispose();
        _silenceTimer = null;

        // 释放所有按住的键
        ReleaseAllActiveNotes();
        IsRunning = false;
        OnStatusChanged?.Invoke("已停止");
    }

    private void OnAudioDataAvailable(object? sender, float[] samples)
    {
        // 使用当前音频采集的采样率进行检测
        var result = _pitchDetector.DetectPitch(samples, 44100);

        if (result is { IsValid: true })
        {
            ProcessDetectedNote(result);
        }
        else
        {
            // 无有效音符 → 一段时间后触发所有活跃音符的 Note Off
            StartSilenceDetection();
        }
    }

    private void ProcessDetectedNote(PitchResult result)
    {
        _silenceTimer?.Stop();

        string noteName = result.FullName;
        var mapping = _mappingService.FindMapping(noteName);
        if (mapping == null) return;

        var now = DateTime.Now;

        // 确保该音符有状态记录
        if (!_noteStates.TryGetValue(noteName, out var state))
        {
            state = new NoteState { NoteName = noteName };
            _noteStates[noteName] = state;
        }

        // 去重检查：如果是相同音符且间隔太短，忽略
        if (_lastNoteName == noteName &&
            (now - _lastNoteTime).TotalMilliseconds < _settings.MinInputIntervalMs)
        {
            return;
        }

        // 更新状态
        state.LastSeen = now;

        if (!state.IsActive)
        {
            // 音符开始
            state.IsActive = true;
            state.StartTime = now;

            if (mapping.TriggerMode == TriggerMode.Hold)
            {
                ExecuteMappingAction(mapping);
                _mappingService.NoteOn(noteName);
            }
            else // Pulse
            {
                ExecuteMappingAction(mapping);
                // 脉冲模式立即结束
                state.IsActive = false;
            }
        }
        else if (mapping.TriggerMode == TriggerMode.Pulse && state.IsActive)
        {
            // 按住模式下连续拨弦 = 快速连击
            ExecuteMappingAction(mapping);
        }

        _lastNoteName = noteName;
        _lastNoteTime = now;

        OnNoteDetected?.Invoke(noteName, result.Frequency, result.Confidence);
    }

    private void ExecuteMappingAction(NoteMapping mapping)
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
                            if (mapping.TriggerMode == TriggerMode.Pulse)
                                _inputSimulator.KeyPress(vk.Value);
                            else
                                _inputSimulator.KeyDown(vk.Value);
                        }
                    }
                    break;

                case ActionType.Combination:
                    if (!string.IsNullOrEmpty(mapping.KeyCode) && mapping.ModifierKeys.Count > 0)
                    {
                        _inputSimulator.SendCombination(mapping.ModifierKeys, mapping.KeyCode);
                    }
                    break;

                case ActionType.MouseClick:
                    if (mapping.MouseButton.HasValue)
                    {
                        _inputSimulator.MouseClick(mapping.MouseButton.Value);
                    }
                    break;
            }

            OnMappingTriggered?.Invoke(mapping.GetDescription());
        }
        catch (Exception ex)
        {
            OnStatusChanged?.Invoke($"输入模拟失败: {ex.Message}");
        }
    }

    private void OnAudioLevelUpdate(object? sender, float rms)
    {
        OnAudioLevelChanged?.Invoke(rms);
    }

    private void OnCaptureError(object? sender, string error)
    {
        OnStatusChanged?.Invoke($"错误: {error}");
    }

    private void StartSilenceDetection()
    {
        if (_silenceTimer != null) return;

        _silenceTimer = new Timer(_settings.MinNoteDurationMs + 10);
        _silenceTimer.Elapsed += OnSilenceTimeout;
        _silenceTimer.AutoReset = false;
        _silenceTimer.Start();
    }

    private void OnSilenceTimeout(object? sender, ElapsedEventArgs e)
    {
        _silenceTimer?.Dispose();
        _silenceTimer = null;

        // 释放所有活跃音符（Note Off）
        ReleaseAllActiveNotes();
    }

    private void ReleaseAllActiveNotes()
    {
        foreach (var (noteName, state) in _noteStates)
        {
            if (!state.IsActive) continue;

            var mapping = _mappingService.FindMapping(noteName);
            if (mapping is { TriggerMode: TriggerMode.Hold })
            {
                var vk = _inputSimulator.StringToVirtualKey(mapping.KeyCode ?? "");
                if (vk.HasValue)
                    _inputSimulator.KeyUp(vk.Value);
            }

            _mappingService.NoteOff(noteName);
            state.IsActive = false;
        }
    }

    private class NoteState
    {
        public string NoteName { get; init; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime LastSeen { get; set; }
    }
}
