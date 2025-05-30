using System;
using System.Windows;

namespace cinema_project
{
    public partial class EditUserWindow : Window
    {
        private DatabaseHelper dbHelper;
        private User currentUser;

        public EditUserWindow(User user)
        {
            InitializeComponent();
            dbHelper = new DatabaseHelper();
            currentUser = user;
            LoadUserData();
        }

        private void LoadUserData()
        {
            if (currentUser != null)
            {
                UsernameTextBox.Text = currentUser.Username;
                EmailTextBox.Text = currentUser.Email;
                FirstNameTextBox.Text = currentUser.FirstName ?? "";
                LastNameTextBox.Text = currentUser.LastName ?? "";
                PhoneTextBox.Text = currentUser.Phone ?? "";
                DateOfBirthPicker.SelectedDate = currentUser.DateOfBirth;
                IsActiveCheckBox.IsChecked = currentUser.IsActive;
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            SaveButton.IsEnabled = false;
            ShowStatusMessage("Saving changes...", System.Windows.Media.Brushes.Blue);

            try
            {
                var updatedUser = new User
                {
                    Id = currentUser.Id,
                    Username = UsernameTextBox.Text.Trim(),
                    Email = EmailTextBox.Text.Trim(),
                    FirstName = FirstNameTextBox.Text.Trim(),
                    LastName = LastNameTextBox.Text.Trim(),
                    Phone = string.IsNullOrWhiteSpace(PhoneTextBox.Text) ? null : PhoneTextBox.Text.Trim(),
                    DateOfBirth = DateOfBirthPicker.SelectedDate,
                    IsActive = IsActiveCheckBox.IsChecked ?? true,
                    ProfilePicture = currentUser.ProfilePicture,
                    JoinDate = currentUser.JoinDate,
                    LastLogin = currentUser.LastLogin
                };

                bool success = await dbHelper.UpdateUserAsync(updatedUser);

                if (success)
                {
                    ShowStatusMessage("User updated successfully!", System.Windows.Media.Brushes.Green);
                    await Task.Delay(1000);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    ShowStatusMessage("Failed to update user. Username or email might already exist.",
                                    System.Windows.Media.Brushes.Red);
                }
            }
            catch (Exception ex)
            {
                ShowStatusMessage($"Error updating user: {ex.Message}", System.Windows.Media.Brushes.Red);
            }
            finally
            {
                SaveButton.IsEnabled = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
            {
                ShowStatusMessage("Username is required.", System.Windows.Media.Brushes.Red);
                UsernameTextBox.Focus();
                return false;
            }

            if (UsernameTextBox.Text.Trim().Length < 3)
            {
                ShowStatusMessage("Username must be at least 3 characters long.", System.Windows.Media.Brushes.Red);
                UsernameTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                ShowStatusMessage("Email is required.", System.Windows.Media.Brushes.Red);
                EmailTextBox.Focus();
                return false;
            }

            if (!IsValidEmail(EmailTextBox.Text.Trim()))
            {
                ShowStatusMessage("Please enter a valid email address.", System.Windows.Media.Brushes.Red);
                EmailTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text))
            {
                ShowStatusMessage("First name is required.", System.Windows.Media.Brushes.Red);
                FirstNameTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(LastNameTextBox.Text))
            {
                ShowStatusMessage("Last name is required.", System.Windows.Media.Brushes.Red);
                LastNameTextBox.Focus();
                return false;
            }

            if (!string.IsNullOrWhiteSpace(PhoneTextBox.Text))
            {
                string phone = PhoneTextBox.Text.Trim();
                if (phone.Length < 10 || !System.Text.RegularExpressions.Regex.IsMatch(phone, @"^[\d\s\-\+\(\)]+$"))
                {
                    ShowStatusMessage("Please enter a valid phone number.", System.Windows.Media.Brushes.Red);
                    PhoneTextBox.Focus();
                    return false;
                }
            }

            if (DateOfBirthPicker.SelectedDate.HasValue)
            {
                var dob = DateOfBirthPicker.SelectedDate.Value;
                var today = DateTime.Today;
                var age = today.Year - dob.Year;

                if (dob > today.AddYears(-age))
                    age--;

                if (age < 13 || age > 120)
                {
                    ShowStatusMessage("Please enter a valid date of birth (age must be between 13 and 120).",
                                    System.Windows.Media.Brushes.Red);
                    DateOfBirthPicker.Focus();
                    return false;
                }
            }

            return true;
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

        private void ShowStatusMessage(string message, System.Windows.Media.Brush color)
        {
            StatusMessage.Text = message;
            StatusMessage.Foreground = color;
        }
    }
}