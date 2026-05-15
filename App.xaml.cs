using System.Windows;
using PanelTuner.Services;
using PanelTuner.Windows;
using WpfMessageBox = System.Windows.MessageBox;

namespace PanelTuner;

public partial class App : System.Windows.Application
{
    private SettingsService? _settingsService;
    private AudioLockService? _audioLockService;
    private StartupService? _startupService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _settingsService = new SettingsService();
        var settings = _settingsService.Load();

        _audioLockService = new AudioLockService();
        _startupService = new StartupService();

        if (!settings.Password.IsConfigured)
        {
            var createPasswordDialog = new PasswordDialog(isCreateMode: true)
            {
                Title = "初始化管理员密码"
            };

            var passwordCreated = createPasswordDialog.ShowDialog();
            if (passwordCreated != true || string.IsNullOrWhiteSpace(createPasswordDialog.EnteredPassword))
            {
                Shutdown();
                return;
            }

            PasswordService.SetPassword(settings, createPasswordDialog.EnteredPassword);
            _settingsService.Save(settings);
        }

        ApplyManagedSettings(settings);

        var mainWindow = new MainWindow(
            settings,
            _settingsService,
            _audioLockService,
            _startupService);

        MainWindow = mainWindow;
        mainWindow.Show();

        if (e.Args.Any(arg => string.Equals(arg, "--minimized", StringComparison.OrdinalIgnoreCase)))
        {
            mainWindow.HideToTrayOnStartup();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _audioLockService?.Dispose();
        base.OnExit(e);
    }

    private void ApplyManagedSettings(Models.AppSettings settings)
    {
        _audioLockService?.UpdateSettings(settings);
        _startupService?.Apply(settings);
    }
}
