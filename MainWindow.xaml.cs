using System.Windows;
using System.Windows.Controls;

namespace cinema_project
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent(); // This links the XAML to code-behind
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailTextBox.Text;
            string password = PasswordBox.Password;

            // Check for empty fields
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                StatusMessage.Text = "Please fill in all fields.";
                StatusMessage.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            // Authenticate user
            if (App.Users.TryGetValue(email, out string storedPassword) && storedPassword == password)
            {
                // Login successful
                CinemaMainWindow mainWindow = new CinemaMainWindow();
                mainWindow.Show();
                this.Close();
            }
            else
            {
                StatusMessage.Text = "Invalid email or password.";
                StatusMessage.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void RegisterLink_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = new RegisterWindow();
            registerWindow.ShowDialog();
        }

        private void ForgotPasswordLink_Click(object sender, RoutedEventArgs e)
        {
            var recoveryWindow = new PasswordRecoveryWindow();
            recoveryWindow.ShowDialog();
        }

    }
}