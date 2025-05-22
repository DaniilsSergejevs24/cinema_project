using System.Windows;

namespace cinema_project
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string name = FullNameTextBox.Text;
            string email = EmailTextBox.Text;
            string password = PasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                StatusMessage.Text = "Please fill in all fields.";
                return;
            }

            if (password != confirmPassword)
            {
                StatusMessage.Text = "Passwords do not match.";
                return;
            }

            if (App.Users.ContainsKey(email))
            {
                StatusMessage.Text = "An account with this email already exists.";
                return;
            }

            // Register user
            App.Users[email] = password;
            StatusMessage.Foreground = System.Windows.Media.Brushes.Green;
            StatusMessage.Text = "Registration successful! You can now log in.";
        }

        private void BackToLoginLink_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Go back to login window
        }
    }
}