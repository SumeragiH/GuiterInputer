namespace GuitarInputController.Core.Models;

/// <summary>
/// 映射方案 — 一组映射的集合，对应一个应用场景
/// </summary>
public class MappingProfile
{
    /// <summary>方案名称（唯一标识）</summary>
    public string Name { get; set; } = "新方案";

    /// <summary>方案描述</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>映射列表</summary>
    public List<NoteMapping> Mappings { get; set; } = new();

    public override string ToString() => Name;
}
