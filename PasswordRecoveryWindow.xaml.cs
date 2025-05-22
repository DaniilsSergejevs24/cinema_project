using System.Windows;

namespace cinema_project
{
    public partial class PasswordRecoveryWindow : Window
    {
        public PasswordRecoveryWindow()
        {
            InitializeComponent();
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailTextBox.Text;

            if (App.Users.TryGetValue(email, out string password))
            {
                StatusMessage.Text = $"Recovery link sent to {email}.\n(Password: {password})";
                StatusMessage.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                StatusMessage.Text = "Email not found.";
                StatusMessage.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void BackToLoginLink_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Just closes the recovery window and returns to login
        }
    }
}