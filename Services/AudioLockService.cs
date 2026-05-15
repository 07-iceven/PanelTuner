using System.Timers;
using NAudio.CoreAudioApi;
using PanelTuner.Models;

namespace PanelTuner.Services;

public class AudioLockService : IDisposable
{
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
            _timer.Interval = NormalizeCheckIntervalSeconds(settings.Microphone.CheckIntervalSeconds) * 1000;
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

    public static int NormalizeCheckIntervalSeconds(int seconds)
    {
        return Math.Clamp(seconds, 1, 60);
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        LockVolume();
    }

    private void LockVolume()
    {
        if (_settings == null || !_settings.Microphone.LockEnabled) return;

        try
        {
            var device = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
            if (device != null)
            {
                float targetVolume = _settings.Microphone.VolumePercent / 100f;
                if (Math.Abs(device.AudioEndpointVolume.MasterVolumeLevelScalar - targetVolume) > 0.01f)
                {
                    device.AudioEndpointVolume.MasterVolumeLevelScalar = targetVolume;
                }
            }
        }
        catch
        {
            // Ignore audio device errors
        }
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
        _deviceEnumerator.Dispose();
    }
}
