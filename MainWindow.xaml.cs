using System.Diagnostics;
using System.ComponentModel;
using System.Windows;
using Drawing = System.Drawing;
using PanelTuner.Models;
using PanelTuner.Services;
using PanelTuner.Windows;
using Forms = System.Windows.Forms;
using WpfMessageBox = System.Windows.MessageBox;

namespace PanelTuner;

public partial class MainWindow : Window
{
    private const string GitHubUrl = "https://github.com/07-iceven/PanelTuner";
    private readonly AppSettings _settings;
    private readonly AudioLockService _audioLockService;
    private readonly StartupService _startupService;
    private readonly Forms.NotifyIcon _notifyIcon;
    private bool _isUnlocked;
    private bool _allowExit;
    private bool _trayTipShown;

    public MainWindow(
        AppSettings settings,
        AudioLockService audioLockService,
        StartupService startupService)
    {
        InitializeComponent();

        _settings = settings;
        _audioLockService = audioLockService;
        _startupService = startupService;
        _notifyIcon = InitializeTrayIcon();

        Closing += MainWindow_Closing;
        Closed += MainWindow_Closed;

        LoadSettingsToView();
        ApplyEditMode();
    }

    public void HideToTrayOnStartup()
    {
        Dispatcher.BeginInvoke(() => HideToTray(showNotification: false));
    }

    private void LoadSettingsToView()
    {
        MicrophoneLockCheckBox.IsChecked = _settings.Microphone.LockEnabled;
        MicrophoneVolumeSlider.Value = Math.Clamp(_settings.Microphone.VolumePercent, 0, 100);
        MicrophoneCheckIntervalTextBox.Text = AudioLockService.NormalizeCheckIntervalTicks(
            _settings.Microphone.CheckIntervalTicks).ToString();
        AutoStartCheckBox.IsChecked = _settings.AutoStartEnabled;
        TimeRestrictionCheckBox.IsChecked = _settings.Microphone.TimeRestrictionEnabled;
        StartTimeTextBox.Text = _settings.Microphone.StartTime;
        EndTimeTextBox.Text = _settings.Microphone.EndTime;
    }

    private void ApplyEditMode()
    {
        SettingsPanel.IsEnabled = _isUnlocked;
        StateTextBlock.Text = _isUnlocked ? "当前为管理员编辑模式" : "当前为只读模式";
        HintTextBlock.Text = _isUnlocked
            ? "可以修改设置并立即应用到当前系统。"
            : "请输入管理员密码后再修改设置。";
    }

    private Forms.NotifyIcon InitializeTrayIcon()
    {
        var contextMenu = new Forms.ContextMenuStrip();

        var openItem = new Forms.ToolStripMenuItem("打开主窗口");
        openItem.Click += (_, _) => Dispatcher.Invoke(ShowMainWindow);

        var exitItem = new Forms.ToolStripMenuItem("退出程序");
        exitItem.Click += (_, _) => Dispatcher.Invoke(RequestExitWithPassword);

        contextMenu.Items.Add(openItem);
        contextMenu.Items.Add(new Forms.ToolStripSeparator());
        contextMenu.Items.Add(exitItem);

        var notifyIcon = new Forms.NotifyIcon
        {
            Icon = Drawing.SystemIcons.Application,
            Text = "Panel Tuner",
            Visible = true,
            ContextMenuStrip = contextMenu
        };

        notifyIcon.DoubleClick += (_, _) => Dispatcher.Invoke(ShowMainWindow);

        return notifyIcon;
    }

    private void HideToTray(bool showNotification)
    {
        Hide();
        ShowInTaskbar = false;

        if (!showNotification || _trayTipShown)
        {
            return;
        }

        _notifyIcon.BalloonTipTitle = "Panel Tuner 正在后台运行";
        _notifyIcon.BalloonTipText = "已最小化到系统托盘。双击托盘图标可重新打开主窗口。";
        _notifyIcon.ShowBalloonTip(3000);
        _trayTipShown = true;
    }

    private void ShowMainWindow()
    {
        Show();
        ShowInTaskbar = true;
        WindowState = WindowState.Normal;
        Activate();
        Topmost = true;
        Topmost = false;
        Focus();
    }

    private void RequestExitWithPassword()
    {
        var dialog = new PasswordDialog(isCreateMode: false)
        {
            Title = "退出程序验证"
        };

        if (IsVisible)
        {
            dialog.Owner = this;
        }

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        if (!PasswordService.Verify(_settings, dialog.EnteredPassword))
        {
            if (IsVisible)
            {
                WpfMessageBox.Show(this, "管理员密码不正确，无法退出程序。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                WpfMessageBox.Show("管理员密码不正确，无法退出程序。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            return;
        }

        _allowExit = true;
        _notifyIcon.Visible = false;
        Close();
    }

    private void UnlockButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new PasswordDialog(isCreateMode: false)
        {
            Owner = this,
            Title = "管理员解锁"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        if (!PasswordService.Verify(_settings, dialog.EnteredPassword))
        {
            WpfMessageBox.Show(this, "管理员密码不正确。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _isUnlocked = true;
        ApplyEditMode();
    }

    private void LockButton_Click(object sender, RoutedEventArgs e)
    {
        _isUnlocked = false;
        ApplyEditMode();
    }

    private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isUnlocked)
        {
            WpfMessageBox.Show(this, "请先完成管理员解锁，再修改密码。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new PasswordDialog(isCreateMode: true)
        {
            Owner = this,
            Title = "修改管理员密码"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        PasswordService.SetPassword(_settings, dialog.EnteredPassword);
        if (!SettingsService.TrySave(_settings, out var saveError))
        {
            WpfMessageBox.Show(this, saveError, "保存失败", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        WpfMessageBox.Show(this, "管理员密码已成功修改。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isUnlocked)
        {
            WpfMessageBox.Show(this, "请先完成管理员解锁，再修改设置。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!int.TryParse(MicrophoneCheckIntervalTextBox.Text, out int ticks))
        {
            WpfMessageBox.Show(this, "检查间隔必须是有效的数字（Ticks）。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Simple time format validation
        if (TimeRestrictionCheckBox.IsChecked == true)
        {
            if (!TimeSpan.TryParse(StartTimeTextBox.Text, out _) || !TimeSpan.TryParse(EndTimeTextBox.Text, out _))
            {
                WpfMessageBox.Show(this, "时间格式无效，请输入正确的 HH:mm 格式。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        _settings.Microphone.LockEnabled = MicrophoneLockCheckBox.IsChecked ?? false;
        _settings.Microphone.VolumePercent = (int)MicrophoneVolumeSlider.Value;
        _settings.Microphone.CheckIntervalTicks = ticks;
        _settings.Microphone.TimeRestrictionEnabled = TimeRestrictionCheckBox.IsChecked ?? false;
        _settings.Microphone.StartTime = StartTimeTextBox.Text;
        _settings.Microphone.EndTime = EndTimeTextBox.Text;
        _settings.AutoStartEnabled = AutoStartCheckBox.IsChecked ?? false;

        if (!SettingsService.TrySave(_settings, out var saveError))
        {
            WpfMessageBox.Show(this, saveError, "保存失败", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        _audioLockService.UpdateSettings(_settings);
        var startupResult = _startupService.Apply(_settings);

        if (!startupResult.Succeeded)
        {
            WpfMessageBox.Show(
                this,
                $"配置文件已保存，麦克风锁定设置也已应用，但开机自启动设置未能同步到系统。{Environment.NewLine}{startupResult.ErrorMessage}",
                "部分设置未应用",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        WpfMessageBox.Show(this, "设置已保存并立即应用。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void MicrophoneVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // UI logic only, saving is handled by SaveButton_Click
    }

    private void GitHubLink_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = GitHubUrl,
                UseShellExecute = true
            });
        }
        catch
        {
            WpfMessageBox.Show(this, "无法打开 GitHub 链接。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (!_allowExit)
        {
            e.Cancel = true;
            HideToTray(showNotification: true);
        }
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _notifyIcon.Dispose();
    }
}
