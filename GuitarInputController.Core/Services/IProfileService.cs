using GuitarInputController.Core.Models;

namespace GuitarInputController.Core.Services;

/// <summary>
/// 方案管理服务接口
/// </summary>
public interface IProfileService
{
    /// <summary>获取所有方案名称</summary>
    List<string> ListProfiles();

    /// <summary>加载方案</summary>
    MappingProfile? LoadProfile(string name);

    /// <summary>保存方案</summary>
    void SaveProfile(MappingProfile profile);

    /// <summary>删除方案</summary>
    bool DeleteProfile(string name);

    /// <summary>导入方案（从文件路径）</summary>
    MappingProfile? ImportProfile(string filePath);

    /// <summary>导出方案到文件</summary>
    void ExportProfile(MappingProfile profile, string filePath);

    /// <summary>获取所有方案</summary>
    List<MappingProfile> GetAllProfiles();
}
