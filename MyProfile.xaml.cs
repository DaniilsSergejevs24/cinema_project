using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Mail;

namespace cinema_project
{
    public partial class MyProfile : Window
    {
        private string connectionString = "datasource=127.0.0.1;port=3307;username=root;password=;database=database_cinema;";
        private User currentUser;

        public MyProfile(User user)
        {
            InitializeComponent();
            currentUser = user;
            if (currentUser == null)
            {
                MessageBox.Show("No user logged in. Please log in first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                new MainWindow().Show(); this.Close(); return;
            }
            LoadUserProfileAsync();
        }

        private async Task LoadUserProfileAsync()
        {
            if (UsernameText == null || EmailText == null || TotalTicketsText == null || ProfileImage == null)
            {
                
            }

            UsernameText.Text = currentUser.Username;
            EmailText.Text = currentUser.Email;

            if (this.FindName("FullNameText") is TextBlock fullNameTextBlock) fullNameTextBlock.Text = $"{currentUser.FirstName} {currentUser.LastName}";
            if (this.FindName("JoinDateText") is TextBlock joinDateTextBlock) joinDateTextBlock.Text = currentUser.JoinDate.ToString("MMMM dd, yyyy"); // Fixed date format
            if (this.FindName("PhoneText") is TextBlock phoneTextBlock) phoneTextBlock.Text = string.IsNullOrWhiteSpace(currentUser.Phone) ? "Not provided" : currentUser.Phone;
            if (this.FindName("DateOfBirthText") is TextBlock dobTextBlock) dobTextBlock.Text = currentUser.DateOfBirth.HasValue ? currentUser.DateOfBirth.Value.ToString("MMMM dd, yyyy") : "Not provided"; // Fixed date format


            if (!string.IsNullOrEmpty(currentUser.ProfilePicture) && (currentUser.ProfilePicture.StartsWith("http", StringComparison.OrdinalIgnoreCase) || File.Exists(currentUser.ProfilePicture)))
            {
                try { ProfileImage.Source = new BitmapImage(new Uri(currentUser.ProfilePicture, UriKind.RelativeOrAbsolute)); }
                catch { ProfileImage.Source = new BitmapImage(new Uri("Images/default_profile.jpg", UriKind.RelativeOrAbsolute)); }
            }
            else { ProfileImage.Source = new BitmapImage(new Uri("Images/default_profile.jpg", UriKind.RelativeOrAbsolute)); }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT COUNT(ticket_id) FROM tickets WHERE user_id = @userId";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", currentUser.Id);
                        TotalTicketsText.Text = (await command.ExecuteScalarAsync())?.ToString() ?? "0";
                    }
                }
            }
            catch (Exception ex) { TotalTicketsText.Text = "N/A"; Console.WriteLine($"Error loading ticket count: {ex.Message}"); }
        }

        private async void ChangePhoto_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*", Title = "Select Profile Picture" };
            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FileName;
                try
                {
                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        await connection.OpenAsync();
                        string query = "UPDATE users SET profile_picture = @profilePicture WHERE user_id = @userId";
                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@profilePicture", selectedFilePath);
                            command.Parameters.AddWithValue("@userId", currentUser.Id);
                            await command.ExecuteNonQueryAsync();
                        }
                    }
                    currentUser.ProfilePicture = selectedFilePath;
                    ProfileImage.Source = new BitmapImage(new Uri(selectedFilePath));
                    MessageBox.Show("Profile picture updated successfully!", "Success");
                }
                catch (Exception ex) { MessageBox.Show($"Error updating profile picture: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            ChangePasswordWindow changePasswordWindow = new ChangePasswordWindow(currentUser.Id, currentUser.Email) { Owner = this };
            changePasswordWindow.ShowDialog();
        }

        private async void EditProfile_Click(object sender, RoutedEventArgs e)
        {
            EditProfileWindow editProfileWindow = new EditProfileWindow(currentUser) { Owner = this };
            if (editProfileWindow.ShowDialog() == true) await LoadUserProfileAsync();
        }

        private async void DeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to delete your account? This action cannot be undone.", "Delete Account", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes &&
                MessageBox.Show("FINAL WARNING: All your data will be permanently deleted. Continue?", "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Stop) == MessageBoxResult.Yes)
            {
                try
                {
                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        await connection.OpenAsync();
                        string query = "DELETE FROM users WHERE user_id = @userId";
                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@userId", currentUser.Id);
                            if (await command.ExecuteNonQueryAsync() > 0)
                            {
                                MessageBox.Show("Account deleted successfully.", "Success");
                                Application.Current.Windows.OfType<Window>().Where(w => w != this && !(w is MainWindow)).ToList().ForEach(w => w.Close());
                                new MainWindow().Show(); this.Close();
                            }
                            else { MessageBox.Show("Could not delete account.", "Error"); }
                        }
                    }
                }
                catch (Exception ex) { MessageBox.Show($"Error deleting account: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        private void NavigateToWindow(Window newWindow) { newWindow.Show(); this.Close(); }
        private void CinemaMain_Click(object sender, RoutedEventArgs e) => NavigateToWindow(new CinemaMainWindow(currentUser));
        private void MovieSessionCalendar_Click(object sender, RoutedEventArgs e) => NavigateToWindow(new MovieSessionCalendar(currentUser));
        private void SearchPage_Click(object sender, RoutedEventArgs e) => NavigateToWindow(new SearchPage(currentUser));
        private void MyTickets_Click(object sender, RoutedEventArgs e) => NavigateToWindow(new MyTickets(currentUser));
    }

    public partial class ChangePasswordWindow : Window
    {
        private string connectionString = "datasource=127.0.0.1;port=3307;username=root;password=;database=database_cinema;";
        private int userId;
        private PasswordBox CurrentPasswordBox; private PasswordBox NewPasswordBox; private PasswordBox ConfirmNewPasswordBox;

        public ChangePasswordWindow(int currentUserId, string email)
        {
            userId = currentUserId; InitializeComponentManual();
        }

        private void InitializeComponentManual()
        {
            Title = "Change Password"; Width = 350; Height = 280; WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Grid grid = new Grid { Margin = new Thickness(20, 20, 20, 20) };
            for (int i = 0; i < 7; i++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            TextBlock currentPassLabel = new TextBlock { Text = "Current Password:", Margin = new Thickness(0, 0, 0, 2) };
            CurrentPasswordBox = new PasswordBox { Margin = new Thickness(0, 0, 0, 10) };
            TextBlock newPassLabel = new TextBlock { Text = "New Password:", Margin = new Thickness(0, 0, 0, 2) };
            NewPasswordBox = new PasswordBox { Margin = new Thickness(0, 0, 0, 10) };
            TextBlock confirmPassLabel = new TextBlock { Text = "Confirm New Password:", Margin = new Thickness(0, 0, 0, 2) };
            ConfirmNewPasswordBox = new PasswordBox { Margin = new Thickness(0, 0, 0, 15) };

            grid.Children.Add(currentPassLabel); Grid.SetRow(currentPassLabel, 0);
            grid.Children.Add(CurrentPasswordBox); Grid.SetRow(CurrentPasswordBox, 1);
            grid.Children.Add(newPassLabel); Grid.SetRow(newPassLabel, 2);
            grid.Children.Add(NewPasswordBox); Grid.SetRow(NewPasswordBox, 3);
            grid.Children.Add(confirmPassLabel); Grid.SetRow(confirmPassLabel, 4);
            grid.Children.Add(ConfirmNewPasswordBox); Grid.SetRow(ConfirmNewPasswordBox, 5);

            StackPanel buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            Button saveButton = new Button { Content = "Save Changes", Margin = new Thickness(0, 0, 10, 0), Padding = new Thickness(10, 5, 10, 5) };
            saveButton.Click += SaveButton_Click;
            Button cancelButton = new Button { Content = "Cancel", Padding = new Thickness(10, 5, 10, 5) };
            cancelButton.Click += (s, e) => this.Close();
            buttonPanel.Children.Add(saveButton); buttonPanel.Children.Add(cancelButton);
            grid.Children.Add(buttonPanel); Grid.SetRow(buttonPanel, 6);
            Content = grid;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CurrentPasswordBox.Password) || string.IsNullOrWhiteSpace(NewPasswordBox.Password) || string.IsNullOrWhiteSpace(ConfirmNewPasswordBox.Password)) { MessageBox.Show("All fields are required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (NewPasswordBox.Password.Length < 6) { MessageBox.Show("New password must be at least 6 characters.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (NewPasswordBox.Password != ConfirmNewPasswordBox.Password) { MessageBox.Show("New passwords do not match.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT password FROM users WHERE user_id = @userId";
                    string storedHashedPassword;
                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        var result = await cmd.ExecuteScalarAsync();
                        if (result == null) { MessageBox.Show("User not found.", "Error"); return; }
                        storedHashedPassword = result.ToString();
                    }
                    if (!DatabaseHelper.VerifyPassword(CurrentPasswordBox.Password, storedHashedPassword)) { MessageBox.Show("Current password is incorrect.", "Authentication Error"); return; }
                    string updateQuery = "UPDATE users SET password = @newPassword WHERE user_id = @userId";
                    using (MySqlCommand updateCmd = new MySqlCommand(updateQuery, connection))
                    {
                        updateCmd.Parameters.AddWithValue("@newPassword", DatabaseHelper.HashPassword(NewPasswordBox.Password));
                        updateCmd.Parameters.AddWithValue("@userId", userId);
                        await updateCmd.ExecuteNonQueryAsync();
                    }
                }
                MessageBox.Show("Password changed successfully!", "Success"); this.Close();
            }
            catch (Exception ex) { MessageBox.Show($"Error changing password: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
    }

    public partial class EditProfileWindow : Window
    {
        private string connectionString = "datasource=127.0.0.1;port=3307;username=root;password=;database=database_cinema;";
        private User userToEdit;
        private TextBox UsernameTextBoxE, EmailTextBoxE, FirstNameTextBoxE, LastNameTextBoxE, PhoneTextBoxE; // Renamed to avoid conflict if this class is nested or has same names as outer
        private DatePicker DateOfBirthPickerE;

        public EditProfileWindow(User currentUser)
        {
            userToEdit = currentUser; InitializeComponentManual(); LoadCurrentData();
        }

        private void InitializeComponentManual()
        {
            Title = "Edit Profile"; Width = 400; Height = 420; WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Grid grid = new Grid { Margin = new Thickness(20, 20, 20, 20) };
            for (int i = 0; i < 13; i++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            int row = 0;
            TextBlock userLabel = new TextBlock { Text = "Username:" }; grid.Children.Add(userLabel); Grid.SetRow(userLabel, row++);
            UsernameTextBoxE = new TextBox { Margin = new Thickness(0, 0, 0, 10) }; grid.Children.Add(UsernameTextBoxE); Grid.SetRow(UsernameTextBoxE, row++);
            TextBlock emailLabel = new TextBlock { Text = "Email:" }; grid.Children.Add(emailLabel); Grid.SetRow(emailLabel, row++);
            EmailTextBoxE = new TextBox { Margin = new Thickness(0, 0, 0, 10) }; grid.Children.Add(EmailTextBoxE); Grid.SetRow(EmailTextBoxE, row++);
            TextBlock firstLabel = new TextBlock { Text = "First Name:" }; grid.Children.Add(firstLabel); Grid.SetRow(firstLabel, row++);
            FirstNameTextBoxE = new TextBox { Margin = new Thickness(0, 0, 0, 10) }; grid.Children.Add(FirstNameTextBoxE); Grid.SetRow(FirstNameTextBoxE, row++);
            TextBlock lastLabel = new TextBlock { Text = "Last Name:" }; grid.Children.Add(lastLabel); Grid.SetRow(lastLabel, row++);
            LastNameTextBoxE = new TextBox { Margin = new Thickness(0, 0, 0, 10) }; grid.Children.Add(LastNameTextBoxE); Grid.SetRow(LastNameTextBoxE, row++);
            TextBlock phoneLabel = new TextBlock { Text = "Phone:" }; grid.Children.Add(phoneLabel); Grid.SetRow(phoneLabel, row++);
            PhoneTextBoxE = new TextBox { Margin = new Thickness(0, 0, 0, 10) }; grid.Children.Add(PhoneTextBoxE); Grid.SetRow(PhoneTextBoxE, row++);
            TextBlock dobLabel = new TextBlock { Text = "Date of Birth:" }; grid.Children.Add(dobLabel); Grid.SetRow(dobLabel, row++);
            DateOfBirthPickerE = new DatePicker { Margin = new Thickness(0, 0, 0, 15) }; grid.Children.Add(DateOfBirthPickerE); Grid.SetRow(DateOfBirthPickerE, row++);

            StackPanel buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            Button saveButton = new Button { Content = "Save Changes", Margin = new Thickness(0, 0, 10, 0), Padding = new Thickness(10, 5, 10, 5) };
            saveButton.Click += SaveChanges_Click;
            Button cancelButton = new Button { Content = "Cancel", Padding = new Thickness(10, 5, 10, 5) };
            cancelButton.Click += (s, e) => { this.DialogResult = false; this.Close(); };
            buttonPanel.Children.Add(saveButton); buttonPanel.Children.Add(cancelButton);
            grid.Children.Add(buttonPanel); Grid.SetRow(buttonPanel, row);
            Content = grid;
        }

        private void LoadCurrentData()
        {
            UsernameTextBoxE.Text = userToEdit.Username; EmailTextBoxE.Text = userToEdit.Email;
            FirstNameTextBoxE.Text = userToEdit.FirstName; LastNameTextBoxE.Text = userToEdit.LastName;
            PhoneTextBoxE.Text = userToEdit.Phone; DateOfBirthPickerE.SelectedDate = userToEdit.DateOfBirth;
        }

        private async void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            string nu = UsernameTextBoxE.Text.Trim(), ne = EmailTextBoxE.Text.Trim(), nf = FirstNameTextBoxE.Text.Trim(), nl = LastNameTextBoxE.Text.Trim(), np = PhoneTextBoxE.Text.Trim();
            DateTime? nd = DateOfBirthPickerE.SelectedDate;
            if (string.IsNullOrWhiteSpace(nu) || string.IsNullOrWhiteSpace(ne) || string.IsNullOrWhiteSpace(nf) || string.IsNullOrWhiteSpace(nl)) { MessageBox.Show("Username, Email, First/Last Name are required.", "Validation Error"); return; }
            if (!IsValidEmail(ne)) { MessageBox.Show("Invalid email format.", "Validation Error"); return; }
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string checkQuery = "SELECT COUNT(*) FROM users WHERE (username = @username OR email = @email) AND user_id != @userId";
                    using (MySqlCommand checkCmd = new MySqlCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@username", nu); checkCmd.Parameters.AddWithValue("@email", ne); checkCmd.Parameters.AddWithValue("@userId", userToEdit.Id);
                        if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0) { MessageBox.Show("Username or Email already taken.", "Conflict"); return; }
                    }
                    string query = @"UPDATE users SET username = @u, email = @e, first_name = @f, last_name = @l, phone = @p, date_of_birth = @d WHERE user_id = @id";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@u", nu); command.Parameters.AddWithValue("@e", ne); command.Parameters.AddWithValue("@f", nf);
                        command.Parameters.AddWithValue("@l", nl); command.Parameters.AddWithValue("@p", (object)np ?? DBNull.Value);
                        command.Parameters.AddWithValue("@d", (object)nd ?? DBNull.Value); command.Parameters.AddWithValue("@id", userToEdit.Id);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                userToEdit.Username = nu; userToEdit.Email = ne; userToEdit.FirstName = nf; userToEdit.LastName = nl; userToEdit.Phone = np; userToEdit.DateOfBirth = nd;
                MessageBox.Show("Profile updated successfully!", "Success"); this.DialogResult = true; this.Close();
            }
            catch (Exception ex) { MessageBox.Show($"Error updating profile: {ex.Message}", "Database Error"); }
        }
        private bool IsValidEmail(string email) { try { new MailAddress(email); return true; } catch { return false; } }
    }
}