using System;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;

namespace cinema_project
{
    public partial class MainWindow : Window
    {
        private DatabaseHelper dbHelper;

        public MainWindow()
        {
            InitializeComponent();
            dbHelper = new DatabaseHelper();
            InitializeDatabase();
        }

        private async void InitializeDatabase()
        {
            await dbHelper.InitializeDatabaseAsync();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {

            string loginIdentifier = EmailTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(loginIdentifier) || string.IsNullOrWhiteSpace(password))
            {
                StatusMessage.Text = "Please fill in all fields.";
                StatusMessage.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            if (loginIdentifier.Contains("@") && !IsValidEmail(loginIdentifier))
            {
                StatusMessage.Text = "Please enter a valid email address.";
                StatusMessage.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            LoginButton.IsEnabled = false;
            StatusMessage.Text = "Authenticating...";
            StatusMessage.Foreground = System.Windows.Media.Brushes.Blue;

            try
            {
                User currentUser = await dbHelper.AuthenticateUserAsync(loginIdentifier, password);

                if (currentUser != null)
                {
                    StatusMessage.Text = "Login successful!";
                    StatusMessage.Foreground = System.Windows.Media.Brushes.Green;

                    CinemaMainWindow mainWindow = new CinemaMainWindow(currentUser);
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    StatusMessage.Text = "Invalid login identifier or password.";
                    StatusMessage.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                StatusMessage.Text = $"Login failed: {ex.Message}";
                StatusMessage.Foreground = System.Windows.Media.Brushes.Red;
            }
            finally
            {
                LoginButton.IsEnabled = true;
            }
        }

        private void RegisterLink_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = new RegisterWindow();
            registerWindow.Owner = this;

            if (registerWindow.ShowDialog() == true)
            {
                StatusMessage.Text = "Registration successful! You can now log in.";
                StatusMessage.Foreground = System.Windows.Media.Brushes.Green;

                if (!string.IsNullOrEmpty(registerWindow.RegisteredEmail))
                {
                    EmailTextBox.Text = registerWindow.RegisteredEmail;
                }
            }
        }

        private void ForgotPasswordLink_Click(object sender, RoutedEventArgs e)
        {
            var recoveryWindow = new PasswordRecoveryWindow();
            recoveryWindow.Owner = this;
            recoveryWindow.ShowDialog();
        }

        private void AdminLoginLink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var adminLoginWindow = new AdminLoginWindow();
                adminLoginWindow.Owner = this;
                adminLoginWindow.Show();
                this.Hide();
            }
            catch (Exception ex)
            {
                StatusMessage.Text = $"Failed to open admin login: {ex.Message}";
                StatusMessage.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void EmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearStatusMessage();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ClearStatusMessage();
        }

        private void ClearStatusMessage()
        {
            if (StatusMessage != null && !string.IsNullOrEmpty(StatusMessage.Text) &&
                StatusMessage.Foreground != System.Windows.Media.Brushes.Blue)
            {
                StatusMessage.Text = "";
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            this.Show();
        }
    }
}