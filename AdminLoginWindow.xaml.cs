using System;
using System.Windows;
using System.Windows.Controls;

namespace cinema_project
{
    public partial class AdminLoginWindow : Window
    {
        // Admin credentials!!!
        private const string ADMIN_ID = "admin@cinema.com";
        private const string AUTH_KEY = "CinemaAdmin2024!";

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
                if (adminId.Equals(ADMIN_ID, StringComparison.OrdinalIgnoreCase) &&
                    authKey.Equals(AUTH_KEY, StringComparison.Ordinal))
                {
                    ShowStatusMessage("Access granted! Opening admin panel...", System.Windows.Media.Brushes.Green);

                    AdminDashboardWindow adminDashboard = new AdminDashboardWindow();
                    adminDashboard.Show();
                    this.Close();
                }
                else
                {
                    ShowStatusMessage("Invalid admin ID or authentication key.", System.Windows.Media.Brushes.Red);
                }
            }
            catch (Exception ex)
            {
                ShowStatusMessage($"Error: {ex.Message}", System.Windows.Media.Brushes.Red);
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
            if (StatusMessage != null && !string.IsNullOrEmpty(StatusMessage.Text) &&
                StatusMessage.Foreground != System.Windows.Media.Brushes.Blue)
            {
                StatusMessage.Text = "";
            }
        }
    }
}