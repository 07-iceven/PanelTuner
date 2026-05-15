namespace PanelTuner.Models;

public class AppSettings
{
    public MicrophoneSettings Microphone { get; set; } = new();
    public bool AutoStartEnabled { get; set; }
    public PasswordSettings Password { get; set; } = new();
}

public class MicrophoneSettings
{
    public bool LockEnabled { get; set; }
    public int VolumePercent { get; set; } = 80;
    public int CheckIntervalSeconds { get; set; } = 5;
}

public class PasswordSettings
{
    public string? HashedPassword { get; set; }
    public bool IsConfigured => !string.IsNullOrEmpty(HashedPassword);
}
