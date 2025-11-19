using System;
using System.IO;
using System.Text.Json;
using ZLauncher.Models;


namespace ZLauncher.Services;

public class ConfigService
{
private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "launcher_settings.json");

public LauncherConfig CurrentConfig { get; private set; }

public ConfigService()
{
    CurrentConfig = Load();
}

public LauncherConfig Load()
{
    if (!File.Exists(ConfigPath))
    {
        return new LauncherConfig();
    }

    try
    {
        var json = File.ReadAllText(ConfigPath);
        var config = JsonSerializer.Deserialize<LauncherConfig>(json);
        return config ?? new LauncherConfig();
    }
    catch
    {
        return new LauncherConfig();
    }
}

public void Save()
{
    try
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(CurrentConfig, options);
        File.WriteAllText(ConfigPath, json);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to save config: {ex.Message}");
    }
}

}
