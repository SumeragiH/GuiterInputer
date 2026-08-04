using System.Text.Json;
using GuitarInputController.Core.Models;

namespace GuitarInputController.Core.Services;

/// <summary>
/// 方案管理服务实现
/// </summary>
public class ProfileService : IProfileService
{
    private readonly string _profilesDirectory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public ProfileService(string profilesDirectory)
    {
        _profilesDirectory = profilesDirectory;
        Directory.CreateDirectory(_profilesDirectory);
    }

    public List<string> ListProfiles()
    {
        if (!Directory.Exists(_profilesDirectory))
            return new List<string>();

        return Directory.GetFiles(_profilesDirectory, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .ToList()!;
    }

    public MappingProfile? LoadProfile(string name)
    {
        var filePath = GetProfilePath(name);
        if (!File.Exists(filePath))
            return null;

        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<MappingProfile>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public void SaveProfile(MappingProfile profile)
    {
        var filePath = GetProfilePath(profile.Name);
        var json = JsonSerializer.Serialize(profile, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    public bool DeleteProfile(string name)
    {
        var filePath = GetProfilePath(name);
        if (!File.Exists(filePath))
            return false;

        File.Delete(filePath);
        return true;
    }

    public MappingProfile? ImportProfile(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        try
        {
            var json = File.ReadAllText(filePath);
            var profile = JsonSerializer.Deserialize<MappingProfile>(json, JsonOptions);
            if (profile != null)
            {
                SaveProfile(profile);
            }
            return profile;
        }
        catch
        {
            return null;
        }
    }

    public void ExportProfile(MappingProfile profile, string filePath)
    {
        var json = JsonSerializer.Serialize(profile, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    public List<MappingProfile> GetAllProfiles()
    {
        var names = ListProfiles();
        var profiles = new List<MappingProfile>();
        foreach (var name in names)
        {
            var profile = LoadProfile(name);
            if (profile != null)
                profiles.Add(profile);
        }
        return profiles;
    }

    private string GetProfilePath(string name) =>
        Path.Combine(_profilesDirectory, $"{name}.json");
}
