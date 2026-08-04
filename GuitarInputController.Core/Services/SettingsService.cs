using System.Text.Json;
using GuitarInputController.Core.Constants;
using GuitarInputController.Core.Models;

namespace GuitarInputController.Core.Services;

/// <summary>
/// 设置持久化服务实现（JSON 格式）
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public string SettingsFilePath => _settingsFilePath;

    public SettingsService(string settingsFilePath)
    {
        _settingsFilePath = settingsFilePath;

        // 确保目录存在
        var dir = Path.GetDirectoryName(settingsFilePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
    }

    public AppSettings Load()
    {
        if (!File.Exists(_settingsFilePath))
        {
            var defaults = GetDefault();
            Save(defaults);
            return defaults;
        }

        try
        {
            var json = File.ReadAllText(_settingsFilePath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? GetDefault();
        }
        catch
        {
            return GetDefault();
        }
    }

    public void Save(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_settingsFilePath, json);
    }

    public AppSettings GetDefault() => DefaultSettings.CreateDefault();
}
