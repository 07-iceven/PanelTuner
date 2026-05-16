using System.IO;
using System.Text.Json;
using PanelTuner.Models;

namespace PanelTuner.Services;

public enum SettingsLoadStatus
{
    Success,
    NotFound,
    Corrupted,
    Error
}

public sealed class SettingsLoadResult(SettingsLoadStatus status, AppSettings settings, string? errorMessage = null)
{
    public SettingsLoadStatus Status { get; } = status;
    public AppSettings Settings { get; } = settings;
    public string? ErrorMessage { get; } = errorMessage;
}

public static class SettingsService
{
    private static readonly string AppDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Panel Tuner");

    private static readonly string SettingsPath = Path.Combine(AppDataPath, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static SettingsLoadResult Load()
    {
        if (!File.Exists(SettingsPath))
        {
            return new SettingsLoadResult(SettingsLoadStatus.NotFound, new AppSettings());
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            if (settings == null)
            {
                return new SettingsLoadResult(
                    SettingsLoadStatus.Corrupted,
                    new AppSettings(),
                    "配置文件内容为空或格式不正确。");
            }

            return new SettingsLoadResult(SettingsLoadStatus.Success, settings);
        }
        catch (JsonException ex)
        {
            return new SettingsLoadResult(
                SettingsLoadStatus.Corrupted,
                new AppSettings(),
                $"配置文件格式无效：{ex.Message}");
        }
        catch (Exception ex)
        {
            return new SettingsLoadResult(
                SettingsLoadStatus.Error,
                new AppSettings(),
                $"读取配置文件失败：{ex.Message}");
        }
    }

    public static bool TrySave(AppSettings settings, out string? errorMessage)
    {
        try
        {
            if (!Directory.Exists(AppDataPath))
            {
                Directory.CreateDirectory(AppDataPath);
            }

            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(SettingsPath, json);
            errorMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"保存配置文件失败：{ex.Message}";
            return false;
        }
    }
}
