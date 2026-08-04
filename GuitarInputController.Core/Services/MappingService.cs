using GuitarInputController.Core.Models;

namespace GuitarInputController.Core.Services;

/// <summary>
/// 映射管理服务实现
/// </summary>
public class MappingService : IMappingService
{
    private MappingProfile? _currentProfile;
    private readonly Dictionary<string, NoteMapping> _mappingIndex = new();
    private readonly HashSet<string> _activeNotes = new();

    public MappingProfile? CurrentProfile => _currentProfile;
    public IReadOnlySet<string> ActiveNotes => _activeNotes;

    public void LoadProfile(MappingProfile profile)
    {
        _currentProfile = profile;
        _mappingIndex.Clear();
        _activeNotes.Clear();

        foreach (var mapping in profile.Mappings)
        {
            if (!string.IsNullOrWhiteSpace(mapping.Note))
                _mappingIndex[mapping.Note] = mapping;
        }
    }

    public NoteMapping? FindMapping(NoteInfo note)
    {
        return FindMapping(note.FullName);
    }

    public NoteMapping? FindMapping(string noteFullName)
    {
        _mappingIndex.TryGetValue(noteFullName, out var mapping);
        return mapping;
    }

    public IReadOnlyList<NoteMapping> GetAllMappings()
    {
        return _currentProfile?.Mappings.AsReadOnly()
            ?? (IReadOnlyList<NoteMapping>)Array.Empty<NoteMapping>();
    }

    public void AddMapping(NoteMapping mapping)
    {
        if (_currentProfile == null) return;

        _currentProfile.Mappings.Add(mapping);
        if (!string.IsNullOrWhiteSpace(mapping.Note))
            _mappingIndex[mapping.Note] = mapping;
    }

    public void UpdateMapping(NoteMapping mapping)
    {
        if (_currentProfile == null) return;

        var existing = _currentProfile.Mappings.Find(m => m.Id == mapping.Id);
        if (existing != null)
        {
            // 从索引中移除旧键
            if (!string.IsNullOrWhiteSpace(existing.Note))
                _mappingIndex.Remove(existing.Note);

            var index = _currentProfile.Mappings.IndexOf(existing);
            _currentProfile.Mappings[index] = mapping;

            // 添加新键到索引
            if (!string.IsNullOrWhiteSpace(mapping.Note))
                _mappingIndex[mapping.Note] = mapping;
        }
    }

    public bool RemoveMapping(string mappingId)
    {
        if (_currentProfile == null) return false;

        var mapping = _currentProfile.Mappings.Find(m => m.Id == mappingId);
        if (mapping == null) return false;

        if (!string.IsNullOrWhiteSpace(mapping.Note))
            _mappingIndex.Remove(mapping.Note);

        _currentProfile.Mappings.Remove(mapping);
        return true;
    }

    public bool NoteOn(string noteFullName)
    {
        return _activeNotes.Add(noteFullName);
    }

    public bool NoteOff(string noteFullName)
    {
        return _activeNotes.Remove(noteFullName);
    }

    public void ClearAllNotes()
    {
        _activeNotes.Clear();
    }
}
