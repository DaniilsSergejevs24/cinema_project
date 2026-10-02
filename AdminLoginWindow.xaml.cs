using System;
using System.Windows;
using System.Windows.Controls;

namespace cinema_project
{
    public partial class AdminLoginWindow : Window
    {
        public AdminLoginWindow()
        {
            InitializeComponent();
        }

        private void AdminLoginButton_Click(object sender, RoutedEventArgs e)
        {
            string adminId = AdminIdTextBox.Text.Trim();
            string authKey = AuthKeyPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(adminId) || string.IsNullOrWhiteSpace(authKey))
            {
                ShowStatusMessage("Please fill in all fields.", System.Windows.Media.Brushes.Red);
                return;
            }

            AdminLoginButton.IsEnabled = false;
            ShowStatusMessage("Verifying credentials...", System.Windows.Media.Brushes.Blue);

            try
            {
                string expectedAdminId =
                    Environment.GetEnvironmentVariable("CINEMA_ADMIN_ID");

                string expectedAuthKey =
                    Environment.GetEnvironmentVariable("CINEMA_ADMIN_KEY");

                if (string.IsNullOrWhiteSpace(expectedAdminId) ||
                    string.IsNullOrWhiteSpace(expectedAuthKey))
                {
                    ShowStatusMessage(
                        "Admin credentials are not configured.",
                        System.Windows.Media.Brushes.Red
                    );
                    return;
                }

                if (adminId.Equals(expectedAdminId, StringComparison.OrdinalIgnoreCase) &&
                    authKey.Equals(expectedAuthKey, StringComparison.Ordinal))
                {
                    ShowStatusMessage(
                        "Access granted! Opening admin panel...",
                        System.Windows.Media.Brushes.Green
                    );

                    AdminDashboardWindow adminDashboard = new AdminDashboardWindow();
                    adminDashboard.Show();
                    this.Close();
                }
                else
                {
                    ShowStatusMessage(
                        "Invalid admin ID or authentication key.",
                        System.Windows.Media.Brushes.Red
                    );
                }
            }
            catch (Exception ex)
            {
                ShowStatusMessage(
                    $"Error: {ex.Message}",
                    System.Windows.Media.Brushes.Red
                );
            }
            finally
            {
                AdminLoginButton.IsEnabled = true;
            }
        }

        private void AdminIdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearStatusMessage();
        }

        private void AuthKeyPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ClearStatusMessage();
        }

        private void ShowStatusMessage(string message, System.Windows.Media.Brush color)
        {
            StatusMessage.Text = message;
            StatusMessage.Foreground = color;
        }

        private void ClearStatusMessage()
        {
            if (StatusMessage != null &&
                !string.IsNullOrEmpty(StatusMessage.Text) &&
                StatusMessage.Foreground != System.Windows.Media.Brushes.Blue)
            {
                StatusMessage.Text = "";
            }
        }
    }
}
