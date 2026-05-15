using Microsoft.Win32;
using PanelTuner.Models;

namespace PanelTuner.Services;

public class StartupService
{
    private const string AppName = "Panel Tuner";
    private const string RunRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public void Apply(AppSettings settings)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryPath, true);
            if (key == null) return;

            if (settings.AutoStartEnabled)
            {
                var exePath = Environment.ProcessPath;
                if (exePath != null)
                {
                    key.SetValue(AppName, $"\"{exePath}\" --minimized");
                }
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
        }
        catch
        {
            // Ignore registry access errors
        }
    }
}
