using System;
using System.Windows;
using System.Threading.Tasks;
using System.Net.Mail;

namespace cinema_project
{
    public partial class PasswordRecoveryWindow : Window
    {
        private DatabaseHelper dbHelper;
        public PasswordRecoveryWindow()
        {
            InitializeComponent();
            dbHelper = new DatabaseHelper();
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (EmailTextBox == null || SubmitButton == null || StatusMessage == null)
            {
                MessageBox.Show("UI elements not loaded. Check XAML.", "UI Error"); return;
            }

            string email = EmailTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
            {
                StatusMessage.Text = "Please enter a valid email address.";
                StatusMessage.Foreground = System.Windows.Media.Brushes.Red; return;
            }
            SubmitButton.IsEnabled = false; StatusMessage.Text = "Processing..."; StatusMessage.Foreground = System.Windows.Media.Brushes.Blue;
            try
            {
                if (await dbHelper.UserExistsByEmailAsync(email))
                {
                    StatusMessage.Text = $"If an account exists for {email}, a password recovery link has been sent (simulation).";
                    StatusMessage.Foreground = System.Windows.Media.Brushes.Green;
                }
                else { StatusMessage.Text = "Email not found in our records."; StatusMessage.Foreground = System.Windows.Media.Brushes.Red; }
            }
            catch (Exception ex) { StatusMessage.Text = $"An error occurred: {ex.Message}"; StatusMessage.Foreground = System.Windows.Media.Brushes.Red; }
            finally { SubmitButton.IsEnabled = true; }
        }

        private bool IsValidEmail(string email) { try { new MailAddress(email); return true; } catch { return false; } }
        private void BackToLoginLink_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}