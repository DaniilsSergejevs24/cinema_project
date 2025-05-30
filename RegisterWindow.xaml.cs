using System;

using System.Windows;

using System.Threading.Tasks;



namespace cinema_project

{

    public partial class RegisterWindow : Window

    {

        private DatabaseHelper dbHelper;

        public string RegisteredEmail { get; private set; }



        public RegisterWindow()

        {

            InitializeComponent();

            dbHelper = new DatabaseHelper();


        }



        private async void RegisterButton_Click(object sender, RoutedEventArgs e)

        {


            string username = UsernameTextBox.Text.Trim();

            string firstName = FirstNameTextBox.Text.Trim();

            string lastName = LastNameTextBox.Text.Trim();

            string email = EmailTextBox.Text.Trim();

            string phone = PhoneTextBox.Text.Trim();

            string password = PasswordBox.Password;

            string confirmPassword = ConfirmPasswordBox.Password;




            if (string.IsNullOrWhiteSpace(username))

            {

                ShowStatusMessage("Username is required.", false);

                return;

            }

            if (string.IsNullOrWhiteSpace(firstName))

            {

                ShowStatusMessage("First name is required.", false);

                return;

            }

            if (string.IsNullOrWhiteSpace(lastName))

            {

                ShowStatusMessage("Last name is required.", false);

                return;

            }

            if (string.IsNullOrWhiteSpace(email))

            {

                ShowStatusMessage("Email is required.", false);

                return;

            }

            if (!IsValidEmail(email))

            {

                ShowStatusMessage("Please enter a valid email address.", false);

                return;

            }

            if (!IsValidNumber(phone))

            {

                ShowStatusMessage("Please enter a valid phone number.", false);

                return;

            }

            if (string.IsNullOrWhiteSpace(password))

            {

                ShowStatusMessage("Password is required.", false);

                return;

            }

            if (password.Length < 6)

            {

                ShowStatusMessage("Password must be at least 6 characters long.", false);

                return;

            }

            if (password != confirmPassword)

            {

                ShowStatusMessage("Passwords do not match.", false);

                return;

            }

            if (!TermsCheckBox.IsChecked == true)

            {

                ShowStatusMessage("Please accept the Terms and Conditions.", false);

                return;

            }



            RegisterButton.IsEnabled = false;

            StatusMessage.Text = "Creating account...";

            StatusMessage.Foreground = System.Windows.Media.Brushes.Blue;



            try

            {


                bool registrationSuccess = await dbHelper.RegisterUserAsync(username, email, password, firstName, lastName, phone);



                if (registrationSuccess)

                {

                    ShowStatusMessage("Account created successfully!", true);

                    RegisteredEmail = email;

                    await Task.Delay(1500);

                    DialogResult = true;

                    Close();

                }

                else

                {

                    ShowStatusMessage("Username or email already exists.", false);

                }

            }

            catch (Exception ex)

            {

                ShowStatusMessage($"Registration failed: {ex.Message}", false);

            }

            finally

            {

                RegisterButton.IsEnabled = true;

            }

        }



        private void CancelButton_Click(object sender, RoutedEventArgs e)

        {

            DialogResult = false;

            Close();

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

        private bool IsValidNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber) || !phoneNumber.All(char.IsDigit))
            {
                return false;
            }
            return true;
        }



        private void ShowStatusMessage(string message, bool isSuccess)

        {

            StatusMessage.Text = message;

            StatusMessage.Foreground = isSuccess ?

                System.Windows.Media.Brushes.Green :

                System.Windows.Media.Brushes.Red;

        }




        private void UsernameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => ClearStatusMessage();

        private void FirstNameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => ClearStatusMessage();

        private void LastNameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => ClearStatusMessage();

        private void EmailTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => ClearStatusMessage();

        private void PhoneTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => ClearStatusMessage();

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e) => ClearStatusMessage();

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e) => ClearStatusMessage();





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