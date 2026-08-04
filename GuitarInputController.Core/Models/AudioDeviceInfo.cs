namespace GuitarInputController.Core.Models;

/// <summary>
/// 音频设备信息
/// </summary>
public class AudioDeviceInfo
{
    /// <summary>设备 ID</summary>
    public string DeviceId { get; init; } = string.Empty;

    /// <summary>设备显示名称</summary>
    public string DisplayName { get; init; } = string.Empty;

    public override string ToString() => DisplayName;
}
