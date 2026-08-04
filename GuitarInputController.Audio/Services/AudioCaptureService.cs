using NAudio.Wave;
using GuitarInputController.Audio.Interfaces;

namespace GuitarInputController.Audio.Services;

/// <summary>
/// 基于 NAudio 的音频采集服务实现
/// 支持输入增益调节和音频监听（直通播放）
/// </summary>
public class AudioCaptureService : IAudioCaptureService
{
    private WaveInEvent? _waveIn;
    private int _sampleRate = 44100;

    // ── Monitor / loopback ──────────────────────────────────────
    private WaveOutEvent? _waveOut;
    private BufferedWaveProvider? _bufferedProvider;
    private VolumeWaveProvider16? _volumeProvider;

    public string? CurrentDeviceId { get; private set; }
    public bool IsCapturing => _waveIn != null;
    public bool IsMonitoring => _waveOut != null;

    /// <summary>输入增益（线性倍率，1.0 = 0dB，默认 1.0）</summary>
    public float Gain { get; set; } = 1.0f;

    public event EventHandler<float[]>? AudioDataAvailable;
    public event EventHandler<float>? AudioLevelChanged;
    public event EventHandler<string>? CaptureError;

    public List<Core.Models.AudioDeviceInfo> GetInputDevices()
    {
        var devices = new List<Core.Models.AudioDeviceInfo>();
        for (int i = 0; i < WaveInEvent.DeviceCount; i++)
        {
            var capabilities = WaveInEvent.GetCapabilities(i);
            devices.Add(new Core.Models.AudioDeviceInfo
            {
                DeviceId = i.ToString(),
                DisplayName = capabilities.ProductName
            });
        }
        return devices;
    }

    public void StartCapture(string deviceId, int sampleRate = 44100, int bufferSizeMs = 40)
    {
        StopCapture();

        if (!int.TryParse(deviceId, out int deviceNumber))
        {
            CaptureError?.Invoke(this, $"无效的设备 ID: {deviceId}");
            return;
        }

        if (deviceNumber < 0 || deviceNumber >= WaveInEvent.DeviceCount)
        {
            CaptureError?.Invoke(this, $"设备 ID 超出范围: {deviceId}");
            return;
        }

        try
        {
            _sampleRate = sampleRate;
            CurrentDeviceId = deviceId;

            _waveIn = new WaveInEvent
            {
                DeviceNumber = deviceNumber,
                WaveFormat = new WaveFormat(sampleRate, 16, 1), // 16-bit, mono
                BufferMilliseconds = bufferSizeMs
            };

            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.RecordingStopped += OnRecordingStopped;
            _waveIn.StartRecording();
        }
        catch (Exception ex)
        {
            _waveIn?.Dispose();
            _waveIn = null;
            CaptureError?.Invoke(this, $"启动音频采集失败: {ex.Message}");
        }
    }

    public void StopCapture()
    {
        if (_waveIn != null)
        {
            try
            {
                _waveIn.StopRecording();
                _waveIn.DataAvailable -= OnDataAvailable;
                _waveIn.RecordingStopped -= OnRecordingStopped;
                _waveIn.Dispose();
            }
            catch
            {
                // 忽略停止时的异常
            }
            finally
            {
                _waveIn = null;
                CurrentDeviceId = null;
            }
        }

        // 同时停止监听
        StopMonitor();
    }

    // ─────────────────────────────────────────────────────────────
    //  Monitor / loopback
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 开始监听：将采集的音频实时播放到系统默认音频输出设备
    /// </summary>
    public void StartMonitor()
    {
        if (IsMonitoring) return;

        try
        {
            _bufferedProvider = new BufferedWaveProvider(new WaveFormat(_sampleRate, 16, 1))
            {
                DiscardOnBufferOverflow = true,
                BufferDuration = TimeSpan.FromMilliseconds(100)
            };

            _volumeProvider = new VolumeWaveProvider16(_bufferedProvider)
            {
                Volume = 1.0f
            };

            _waveOut = new WaveOutEvent
            {
                DesiredLatency = 80
            };
            _waveOut.Init(_volumeProvider);
            _waveOut.Play();
        }
        catch (Exception ex)
        {
            StopMonitor();
            CaptureError?.Invoke(this, $"启动监听失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 停止监听
    /// </summary>
    public void StopMonitor()
    {
        if (_waveOut != null)
        {
            try
            {
                _waveOut.Stop();
                _waveOut.Dispose();
            }
            catch { }
            finally
            {
                _waveOut = null;
                _bufferedProvider = null;
                _volumeProvider = null;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Audio data processing
    // ─────────────────────────────────────────────────────────────

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        // 将 16-bit PCM 字节转换为 float[] (-1.0 ~ 1.0)
        int sampleCount = e.BytesRecorded / 2;
        float[] samples = new float[sampleCount];

        float sum = 0;
        for (int i = 0; i < sampleCount; i++)
        {
            short value = BitConverter.ToInt16(e.Buffer, i * 2);
            float normalized = value / 32768f;

            // 应用输入增益（线性倍率）
            normalized = Math.Clamp(normalized * Gain, -1.0f, 1.0f);

            samples[i] = normalized;
            sum += normalized * normalized;
        }

        float rms = sampleCount > 0 ? MathF.Sqrt(sum / sampleCount) : 0;
        AudioLevelChanged?.Invoke(this, rms);
        AudioDataAvailable?.Invoke(this, samples);

        // 如果开启了监听，将音频数据送入播放缓冲区
        FeedMonitor(samples);
    }

    /// <summary>
    /// 将处理后的音频数据送入监听播放缓冲区
    /// </summary>
    private void FeedMonitor(float[] samples)
    {
        if (_bufferedProvider == null) return;

        // 将 float[] 转回 16-bit PCM 字节
        byte[] buffer = new byte[samples.Length * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            // 钳位并转回 short
            float clamped = Math.Clamp(samples[i], -1.0f, 1.0f);
            short value = (short)(clamped * 32767f);
            BitConverter.TryWriteBytes(new Span<byte>(buffer, i * 2, 2), value);
        }

        try
        {
            _bufferedProvider.AddSamples(buffer, 0, buffer.Length);
        }
        catch
        {
            // 缓冲区满等异常，静默处理
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception != null)
        {
            CaptureError?.Invoke(this, $"录音异常停止: {e.Exception.Message}");
        }
    }
}
