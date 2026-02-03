using System.Text.Json;
using Santy.Web.Models;

namespace Santy.Web.Services;

public class SettingsService
{
    private readonly string _settingsPath = "santy-settings.json";
    private AppSettings? _cachedSettings;

    public AppSettings LoadSettings()
    {
        if (_cachedSettings != null)
        {
            return _cachedSettings;
        }

        if (!File.Exists(_settingsPath))
        {
            _cachedSettings = new AppSettings();
            return _cachedSettings;
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            _cachedSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            return _cachedSettings;
        }
        catch
        {
            _cachedSettings = new AppSettings();
            return _cachedSettings;
        }
    }

    public void SaveSettings(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(_settingsPath, json);
        _cachedSettings = settings;
    }
}
