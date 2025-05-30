using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace cinema_project
{
    public partial class AdminDashboardWindow : Window
    {
        private DatabaseHelper dbHelper;
        private List<User> allUsers;
        private List<User> filteredUsers;

        public AdminDashboardWindow()
        {
            InitializeComponent();
            dbHelper = new DatabaseHelper();
            Loaded += AdminDashboardWindow_Loaded;
        }

        private async void AdminDashboardWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadUsersData();
        }

        private async Task LoadUsersData()
        {
            try
            {
                allUsers = await dbHelper.GetAllUsersAsync();
                filteredUsers = new List<User>(allUsers);

                UpdateStatistics();
                RefreshDataGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading users data: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            PerformSearch();
        }

        private void PerformSearch()
        {
            if (allUsers == null) return;

            string searchText = SearchTextBox.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(searchText))
            {
                filteredUsers = new List<User>(allUsers);
            }
            else
            {
                filteredUsers = allUsers.Where(u =>
                    u.Username.ToLower().Contains(searchText) ||
                    u.Email.ToLower().Contains(searchText) ||
                    u.FirstName?.ToLower().Contains(searchText) == true ||
                    u.LastName?.ToLower().Contains(searchText) == true ||
                    u.Phone?.Contains(searchText) == true
                ).ToList();
            }

            RefreshDataGrid();
        }

        private void UsersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = UsersDataGrid.SelectedItem != null;
            EditUserButton.IsEnabled = hasSelection;
            ToggleStatusButton.IsEnabled = hasSelection;
            DeleteUserButton.IsEnabled = hasSelection;
        }

        private async void EditUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersDataGrid.SelectedItem is User selectedUser)
            {
                var editWindow = new EditUserWindow(selectedUser);
                editWindow.Owner = this;

                if (editWindow.ShowDialog() == true)
                {
                    await LoadUsersData();
                    MessageBox.Show("User updated successfully!", "Success",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private async void ToggleStatusButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersDataGrid.SelectedItem is User selectedUser)
            {
                string action = selectedUser.IsActive ? "deactivate" : "activate";
                string message = $"Are you sure you want to {action} user '{selectedUser.Username}'?";

                if (MessageBox.Show(message, "Confirm Action", MessageBoxButton.YesNo,
                                  MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        bool success = await dbHelper.ToggleUserStatusAsync(selectedUser.Id);
                        if (success)
                        {
                            await LoadUsersData();
                            MessageBox.Show($"User {action}d successfully!", "Success",
                                          MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Failed to update user status.", "Error",
                                          MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error updating user status: {ex.Message}", "Error",
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersDataGrid.SelectedItem is User selectedUser)
            {
                string message = $"Are you sure you want to permanently delete user '{selectedUser.Username}'?\n\n" +
                               "This action cannot be undone and will remove all associated data.";

                if (MessageBox.Show(message, "Confirm Deletion", MessageBoxButton.YesNo,
                                  MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        bool success = await dbHelper.DeleteUserAsync(selectedUser.Id);
                        if (success)
                        {
                            await LoadUsersData();
                            MessageBox.Show("User deleted successfully!", "Success",
                                          MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Failed to delete user.", "Error",
                                          MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting user: {ex.Message}", "Error",
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to logout?", "Confirm Logout",
                              MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var loginWindow = new AdminLoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }

        private void UpdateStatistics()
        {
            if (allUsers != null)
            {
                TotalUsersCount.Text = allUsers.Count.ToString();
                ActiveUsersCount.Text = allUsers.Count(u => u.IsActive).ToString();
                InactiveUsersCount.Text = allUsers.Count(u => !u.IsActive).ToString();
            }
        }

        private void RefreshDataGrid()
        {
            UsersDataGrid.ItemsSource = null;
            UsersDataGrid.ItemsSource = filteredUsers;
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshButton.IsEnabled = false;
            try
            {
                await LoadUsersData();
                SearchTextBox.Text = "";
                MessageBox.Show("Data refreshed successfully!", "Success",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing data: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                RefreshButton.IsEnabled = true;
            }
        }
    }
}