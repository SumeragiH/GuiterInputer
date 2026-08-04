using GuitarInputController.Core.Models;

namespace GuitarInputController.Core.Services;

/// <summary>
/// 设置持久化服务接口
/// </summary>
public interface ISettingsService
{
    /// <summary>加载全局设置</summary>
    AppSettings Load();

    /// <summary>保存全局设置</summary>
    void Save(AppSettings settings);

    /// <summary>获取默认设置</summary>
    AppSettings GetDefault();

    /// <summary>设置文件存储路径</summary>
    string SettingsFilePath { get; }
}
