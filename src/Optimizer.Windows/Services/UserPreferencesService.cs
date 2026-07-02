using Optimizer_Windows.Models;
using System.Text.Json;

namespace Optimizer_Windows.Services;

public sealed class UserPreferencesService
{
    private const string AppFolderName = "Optimizer233-Windows";
    private const string PreferencesFileName = "preferences.json";

    public static UserPreferencesService Instance { get; } = new();

    private readonly JsonSerializerOptions _serializerOptions = new() { WriteIndented = true };

    private UserPreferencesService()
    {
    }

    public UserPreferences Load()
    {
        try
        {
            var path = GetPreferencesPath();
            if (!File.Exists(path))
            {
                return new UserPreferences();
            }

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<UserPreferences>(json) ?? new UserPreferences();
        }
        catch
        {
            return new UserPreferences();
        }
    }

    public async Task SaveAsync(UserPreferences preferences)
    {
        var directory = Path.GetDirectoryName(GetPreferencesPath())!;
        Directory.CreateDirectory(directory);
        var json = JsonSerializer.Serialize(preferences, _serializerOptions);
        await File.WriteAllTextAsync(GetPreferencesPath(), json);
    }

    private static string GetPreferencesPath()
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppFolderName);
        return Path.Combine(root, PreferencesFileName);
    }
}
