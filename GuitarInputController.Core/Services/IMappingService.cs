using GuitarInputController.Core.Models;

namespace GuitarInputController.Core.Services;

/// <summary>
/// 映射管理服务接口
/// </summary>
public interface IMappingService
{
    /// <summary>当前激活的映射方案</summary>
    MappingProfile? CurrentProfile { get; }

    /// <summary>加载方案作为当前激活方案</summary>
    void LoadProfile(MappingProfile profile);

    /// <summary>根据音符查找对应的映射</summary>
    NoteMapping? FindMapping(NoteInfo note);

    /// <summary>根据音符名称查找对应的映射</summary>
    NoteMapping? FindMapping(string noteFullName);

    /// <summary>获取所有映射</summary>
    IReadOnlyList<NoteMapping> GetAllMappings();

    /// <summary>添加映射</summary>
    void AddMapping(NoteMapping mapping);

    /// <summary>更新映射</summary>
    void UpdateMapping(NoteMapping mapping);

    /// <summary>删除映射</summary>
    bool RemoveMapping(string mappingId);

    /// <summary>当前正在按住状态的音符集合（用于按住模式追踪）</summary>
    IReadOnlySet<string> ActiveNotes { get; }

    /// <summary>记录音符开始</summary>
    bool NoteOn(string noteFullName);

    /// <summary>记录音符结束</summary>
    bool NoteOff(string noteFullName);

    /// <summary>清除所有活跃音符状态</summary>
    void ClearAllNotes();
}
