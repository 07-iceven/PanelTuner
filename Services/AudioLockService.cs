using System.Timers;
using System.Diagnostics;
using NAudio.CoreAudioApi;
using PanelTuner.Models;

namespace PanelTuner.Services;

public class AudioLockService : IDisposable
{
    private const double TickDurationSeconds = 0.05;
    private readonly System.Timers.Timer _timer;
    private AppSettings? _settings;
    private readonly MMDeviceEnumerator _deviceEnumerator;

    public AudioLockService()
    {
        _deviceEnumerator = new MMDeviceEnumerator();
        _timer = new System.Timers.Timer();
        _timer.Elapsed += OnTimerElapsed;
    }

    public void UpdateSettings(AppSettings settings)
    {
        _settings = settings;
        
        if (settings.Microphone.LockEnabled)
        {
            _timer.Interval = NormalizeCheckIntervalTicks(settings.Microphone.CheckIntervalTicks) * TickDurationSeconds * 1000;
            if (!_timer.Enabled)
            {
                _timer.Start();
            }
            // Initial check
            LockVolume();
        }
        else
        {
            _timer.Stop();
        }
    }

    public static int NormalizeCheckIntervalTicks(int ticks)
    {
        return Math.Clamp(ticks, 1, 1000);
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        LockVolume();
    }

    private void LockVolume()
    {
        if (_settings == null || !_settings.Microphone.LockEnabled) return;

        // Time restriction check: Only lock volume during specified time window
        if (_settings.Microphone.TimeRestrictionEnabled)
        {
            if (!IsCurrentTimeInRestriction(_settings.Microphone.StartTime, _settings.Microphone.EndTime))
            {
                return;
            }
        }

        try
        {
            // Get default recording device
            using var device = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
            if (device != null)
            {
                // Obfuscation strategy: The system's actual master volume scalar is set to (1.0 - display_volume).
                // This makes the volume slider in Windows appear inverted compared to our UI's target.
                float displayVolume = _settings.Microphone.VolumePercent / 100f;
                float actualTargetVolume = 1.0f - displayVolume;

                // Only update if there's a significant deviation (>1%) to avoid unnecessary system calls
                if (Math.Abs(device.AudioEndpointVolume.MasterVolumeLevelScalar - actualTargetVolume) > 0.01f)
                {
                    device.AudioEndpointVolume.MasterVolumeLevelScalar = actualTargetVolume;
                    Debug.WriteLine($"[AudioLock] Adjusted microphone volume to {actualTargetVolume:P0} (Target: {displayVolume:P0})");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioLock] Error accessing audio device: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if current system time falls within the restricted range.
    /// Handles cross-midnight ranges (e.g., 22:00 to 06:00).
    /// </summary>
    private bool IsCurrentTimeInRestriction(string startStr, string endStr)
    {
        if (!TimeSpan.TryParse(startStr, out var start) || !TimeSpan.TryParse(endStr, out var end))
        {
            Debug.WriteLine("[AudioLock] Invalid time format in settings.");
            return true; // Default to active if config is broken
        }

        var now = DateTime.Now.TimeOfDay;
        
        if (start <= end)
        {
            // Normal range: e.g., 09:00 - 17:00
            return now >= start && now <= end;
        }
        else
        {
            // Cross-midnight range: e.g., 22:00 - 06:00
            return now >= start || now <= end;
        }
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
        _deviceEnumerator.Dispose();
    }
}
