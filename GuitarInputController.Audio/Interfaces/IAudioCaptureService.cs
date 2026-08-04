namespace GuitarInputController.Audio.Interfaces;

/// <summary>
/// 音频采集服务接口
/// </summary>
public interface IAudioCaptureService
{
    /// <summary>获取可用的音频输入设备列表</summary>
    List<Core.Models.AudioDeviceInfo> GetInputDevices();

    /// <summary>当前选中的设备 ID</summary>
    string? CurrentDeviceId { get; }

    /// <summary>是否正在采集</summary>
    bool IsCapturing { get; }

    /// <summary>开始采集</summary>
    void StartCapture(string deviceId, int sampleRate = 44100, int bufferSizeMs = 40);

    /// <summary>停止采集</summary>
    void StopCapture();

    /// <summary>音频数据可用事件（float[] 为单声道 PCM 采样）</summary>
    event EventHandler<float[]>? AudioDataAvailable;

    /// <summary>音频电平变化事件</summary>
    event EventHandler<float>? AudioLevelChanged;

    /// <summary>采集错误事件</summary>
    event EventHandler<string>? CaptureError;

    // ── Gain ────────────────────────────────────────────────────

    /// <summary>输入增益（线性倍率，1.0 = 0dB）</summary>
    float Gain { get; set; }

    // ── Monitor / loopback ───────────────────────────────────────

    /// <summary>是否正在监听（将输入音频直通到系统默认播放设备）</summary>
    bool IsMonitoring { get; }

    /// <summary>开始监听：将采集到的音频实时播放到系统默认音频输出设备</summary>
    void StartMonitor();

    /// <summary>停止监听</summary>
    void StopMonitor();
}
