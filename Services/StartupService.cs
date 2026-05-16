using Microsoft.Win32;
using PanelTuner.Models;

namespace PanelTuner.Services;

public sealed class StartupApplyResult
{
    public StartupApplyResult(bool succeeded, string? errorMessage = null)
    {
        Succeeded = succeeded;
        ErrorMessage = errorMessage;
    }

    public bool Succeeded { get; }
    public string? ErrorMessage { get; }
}

public class StartupService
{
    private const string AppName = "Panel Tuner";
    private const string RunRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public StartupApplyResult Apply(AppSettings settings)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryPath, true);
            if (key == null)
            {
                return new StartupApplyResult(false, "无法打开开机启动注册表项。");
            }

            if (settings.AutoStartEnabled)
            {
                var exePath = Environment.ProcessPath;
                if (string.IsNullOrWhiteSpace(exePath))
                {
                    return new StartupApplyResult(false, "无法获取当前程序路径，不能设置开机启动。");
                }

                key.SetValue(AppName, $"\"{exePath}\" --minimized");
            }
            else
            {
                key.DeleteValue(AppName, false);
            }

            return new StartupApplyResult(true);
        }
        catch (Exception ex)
        {
            return new StartupApplyResult(false, $"应用开机自启动设置失败：{ex.Message}");
        }
    }
}
