using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MySql.Data.MySqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace cinema_project
{
    public partial class MyTickets : Window
    {
        private string connectionString = "datasource=127.0.0.1;port=3307;username=root;password=;database=database_cinema;";
        private User currentUser;

        public MyTickets(User user)
        {
            InitializeComponent();
            currentUser = user;
            if (currentUser == null)
            {
                MessageBox.Show("No user logged in. Please log in first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                new MainWindow().Show(); this.Close(); return;
            }
            LoadUserTicketsAsync();
        }

        private async Task LoadUserTicketsAsync()
        {
            TicketsPanel.Children.Clear(); NoTicketsPanel.Visibility = Visibility.Collapsed; TicketsScrollViewer.Visibility = Visibility.Collapsed;
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
                        SELECT b.booking_id, b.booking_reference, b.quantity, b.total_amount, b.status,
                               s.show_date, s.show_time, 
                               m.title AS movie_title, m.poster_path, m.language, m.subtitles, m.duration,
                               th.theater_name
                        FROM bookings b
                        JOIN showtimes s ON b.showtime_id = s.showtime_id
                        JOIN movies m ON s.movie_id = m.movie_id
                        JOIN theaters th ON s.theater_id = th.theater_id
                        WHERE b.user_id = @userId
                        ORDER BY s.show_date DESC, s.show_time DESC";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", currentUser.Id);
                        using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                        {
                            if (!reader.HasRows) { ShowNoTicketsMessage(); return; }
                            TicketsScrollViewer.Visibility = Visibility.Visible;
                            while (await reader.ReadAsync()) CreateTicketCard(reader);
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show($"Error loading tickets: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error); ShowNoTicketsMessage(); }
        }

        private void ShowNoTicketsMessage() { NoTicketsPanel.Visibility = Visibility.Visible; TicketsScrollViewer.Visibility = Visibility.Collapsed; }

        private void CreateTicketCard(MySqlDataReader reader)
        {
            Border ticketCard = new Border { Background = Brushes.White, CornerRadius = new CornerRadius(10), Padding = new Thickness(15, 15, 15, 15), Margin = new Thickness(10, 10, 10, 10), BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(1), Effect = new System.Windows.Media.Effects.DropShadowEffect { ShadowDepth = 1, BlurRadius = 4, Color = Colors.Gainsboro } };
            Grid cardGrid = new Grid();
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            StackPanel headerPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            Grid.SetRow(headerPanel, 0);
            headerPanel.Children.Add(new TextBlock { Text = $"Ref: {reader["booking_reference"]}", FontWeight = FontWeights.Bold, FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(0, 100, 200)) });
            headerPanel.Children.Add(new TextBlock { Text = $"Status: {reader["status"]}", Margin = new Thickness(15, 0, 0, 0), FontStyle = FontStyles.Italic, FontSize = 12, VerticalAlignment = VerticalAlignment.Center });
            cardGrid.Children.Add(headerPanel);

            Grid contentGrid = new Grid(); Grid.SetRow(contentGrid, 1);
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) }); contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Image posterImage = new Image { Width = 80, Height = 120, Stretch = Stretch.UniformToFill, Margin = new Thickness(0, 0, 10, 0) };
            string posterPath = reader["poster_path"]?.ToString();
            try { posterImage.Source = !string.IsNullOrEmpty(posterPath) && Uri.IsWellFormedUriString(posterPath, UriKind.RelativeOrAbsolute) ? new BitmapImage(new Uri(posterPath, UriKind.RelativeOrAbsolute)) : new BitmapImage(new Uri("Images/placeholder.png", UriKind.Relative)); } catch { posterImage.Source = new BitmapImage(new Uri("Images/placeholder.png", UriKind.Relative)); }
            Grid.SetColumn(posterImage, 0); contentGrid.Children.Add(posterImage);

            StackPanel detailsPanel = new StackPanel(); Grid.SetColumn(detailsPanel, 1);
            detailsPanel.Children.Add(new TextBlock { Text = reader["movie_title"].ToString(), FontSize = 18, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
            DateTime showDate = reader.GetDateTime("show_date"); TimeSpan showTimeSpan = reader.GetTimeSpan("show_time");
            detailsPanel.Children.Add(new TextBlock { Text = $"📅 {showDate.Add(showTimeSpan):dddd, MMMM dd}", FontSize = 13 });
            detailsPanel.Children.Add(new TextBlock { Text = $"🕒 {showDate.Add(showTimeSpan):HH:mm} at {reader["theater_name"]}", FontSize = 13, Margin = new Thickness(0, 2, 0, 2) });
            detailsPanel.Children.Add(new TextBlock { Text = $"🎟️ Quantity: {reader.GetInt32("quantity")}", FontSize = 13, Margin = new Thickness(0, 2, 0, 5) });
            detailsPanel.Children.Add(new TextBlock { Text = $"🗣️ {reader["language"]} ({reader["subtitles"] ?? "No Subtitles"})", FontSize = 11, Foreground = Brushes.DarkSlateGray, Margin = new Thickness(0, 0, 0, 5) });
            Border priceBorder = new Border { Background = new SolidColorBrush(Color.FromRgb(0, 119, 204)), CornerRadius = new CornerRadius(5), Padding = new Thickness(8, 4, 8, 4), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 5, 0, 0) };
            priceBorder.Child = new TextBlock { Text = $"Total: ${reader.GetDecimal("total_amount"):F2}", FontWeight = FontWeights.Bold, Foreground = Brushes.White, FontSize = 14 };
            detailsPanel.Children.Add(priceBorder);
            contentGrid.Children.Add(detailsPanel); cardGrid.Children.Add(contentGrid); ticketCard.Child = cardGrid; TicketsPanel.Children.Add(ticketCard);
        }

        private void FindMovie_Click(object sender, RoutedEventArgs e) => NavigateToWindow(new MovieSessionCalendar(currentUser));
        private void NavigateToWindow(Window newWindow) { newWindow.Show(); this.Close(); }
        private void CinemaMain_Click(object sender, RoutedEventArgs e) => NavigateToWindow(new CinemaMainWindow(currentUser));
        private void MovieSessionCalendar_Click(object sender, RoutedEventArgs e) => NavigateToWindow(new MovieSessionCalendar(currentUser));
        private void SearchPage_Click(object sender, RoutedEventArgs e) => NavigateToWindow(new SearchPage(currentUser));
        private void MyProfile_Click(object sender, RoutedEventArgs e) => NavigateToWindow(new MyProfile(currentUser));
    }
}