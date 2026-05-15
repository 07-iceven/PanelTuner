using System.Windows;
using WpfMessageBox = System.Windows.MessageBox;

namespace PanelTuner.Windows;

public partial class PasswordDialog : Window
{
    private readonly bool _isCreateMode;

    public PasswordDialog(bool isCreateMode)
    {
        InitializeComponent();

        _isCreateMode = isCreateMode;
        ConfirmPanel.Visibility = isCreateMode ? Visibility.Visible : Visibility.Collapsed;
        PasswordLabel.Text = isCreateMode ? "设置密码" : "输入密码";
    }

    public string EnteredPassword { get; private set; } = string.Empty;

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        var password = PasswordBox.Password.Trim();
        if (string.IsNullOrWhiteSpace(password))
        {
            WpfMessageBox.Show(this, "密码不能为空。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_isCreateMode)
        {
            var confirm = ConfirmPasswordBox.Password.Trim();
            if (!string.Equals(password, confirm, StringComparison.Ordinal))
            {
                WpfMessageBox.Show(this, "两次输入的密码不一致。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        EnteredPassword = password;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
