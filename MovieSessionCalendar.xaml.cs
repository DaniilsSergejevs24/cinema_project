using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MySql.Data.MySqlClient;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace cinema_project
{
    public partial class MovieSessionCalendar : Window
    {
        private string connectionString = "datasource=127.0.0.1;port=3307;username=root;password=;database=database_cinema;";
        private User currentUser;

        public MovieSessionCalendar(User user)
        {
            InitializeComponent();
            currentUser = user;
            SessionCalendar.SelectedDate = DateTime.Today;
            LoadSessionsForDateAsync(DateTime.Today);
        }

        private void SessionCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SessionCalendar.SelectedDate.HasValue)
            {
                DateTime selectedDate = SessionCalendar.SelectedDate.Value;
                SelectedDateText.Text = $"Sessions for {selectedDate:dddd, MMMM dd}";
                LoadSessionsForDateAsync(selectedDate);
            }
        }

        private async Task LoadSessionsForDateAsync(DateTime date)
        {
            SessionsPanel.Children.Clear();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
                        SELECT s.showtime_id, s.movie_id, m.title, m.poster_path, m.duration, 
                               m.language, m.subtitles, th.theater_name,
                               s.show_date, s.show_time, s.end_time, s.price, s.available_seats
                        FROM showtimes s
                        JOIN movies m ON s.movie_id = m.movie_id
                        JOIN theaters th ON s.theater_id = th.theater_id
                        WHERE s.show_date = @selectedDate AND s.is_active = TRUE AND m.is_active = TRUE AND s.available_seats > 0
                        ORDER BY s.show_time, m.title";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@selectedDate", date.Date);
                        using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                        {
                            if (!reader.HasRows)
                            {
                                SessionsPanel.Children.Add(new TextBlock { Text = "No movie sessions scheduled for this date.", FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(20, 20, 20, 20) });
                                return;
                            }
                            while (await reader.ReadAsync()) CreateSessionCard(reader);
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show($"Error loading sessions: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void CreateSessionCard(MySqlDataReader reader)
        {
            Border sessionCard = new Border { Background = Brushes.White, CornerRadius = new CornerRadius(8), Padding = new Thickness(15, 15, 15, 15), Margin = new Thickness(10, 10, 10, 10), BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(1), Effect = new System.Windows.Media.Effects.DropShadowEffect { ShadowDepth = 2, BlurRadius = 5, Color = Colors.LightGray } };
            Grid cardGrid = new Grid();
            cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Image posterImage = new Image { Width = 90, Height = 135, Stretch = Stretch.UniformToFill, Margin = new Thickness(0, 0, 15, 0) };
            string posterPath = reader["poster_path"]?.ToString();
            try { posterImage.Source = !string.IsNullOrEmpty(posterPath) && Uri.IsWellFormedUriString(posterPath, UriKind.RelativeOrAbsolute) ? new BitmapImage(new Uri(posterPath, UriKind.RelativeOrAbsolute)) : new BitmapImage(new Uri("Images/placeholder.png", UriKind.Relative)); } catch { posterImage.Source = new BitmapImage(new Uri("Images/placeholder.png", UriKind.Relative)); }
            Grid.SetColumn(posterImage, 0); cardGrid.Children.Add(posterImage);

            StackPanel detailsPanel = new StackPanel(); Grid.SetColumn(detailsPanel, 1);
            detailsPanel.Children.Add(new TextBlock { Text = reader["title"].ToString(), FontSize = 18, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(0, 64, 128)), Margin = new Thickness(0, 0, 0, 5) });
            DateTime showDate = reader.GetDateTime("show_date"); TimeSpan showTimeSpan = reader.GetTimeSpan("show_time"); TimeSpan endTimeSpan = reader.GetTimeSpan("end_time");
            detailsPanel.Children.Add(new TextBlock { Text = $"🕒 {showDate.Add(showTimeSpan):HH:mm} - {showDate.Add(endTimeSpan):HH:mm}  |  📍 {reader["theater_name"]}", FontSize = 14, Margin = new Thickness(0, 0, 0, 3) });
            detailsPanel.Children.Add(new TextBlock { Text = $"⏳ Duration: {reader["duration"]} min", FontSize = 12, Foreground = Brushes.Gray, Margin = new Thickness(0, 0, 0, 3) });
            detailsPanel.Children.Add(new TextBlock { Text = $"🗣️ Language: {reader["language"]}, Subtitles: {reader["subtitles"] ?? "N/A"}", FontSize = 12, Foreground = Brushes.Gray, Margin = new Thickness(0, 0, 0, 8) });
            Button buyTicketButton = new Button { Content = $"Book Now (${reader.GetDecimal("price"):F2})", Background = new SolidColorBrush(Color.FromRgb(0, 119, 204)), Foreground = Brushes.White, Padding = new Thickness(12, 6, 12, 6), HorizontalAlignment = HorizontalAlignment.Left, Tag = new Tuple<int, int, decimal>(reader.GetInt32("showtime_id"), reader.GetInt32("movie_id"), reader.GetDecimal("price")) };
            buyTicketButton.Click += BuyTicket_Click; detailsPanel.Children.Add(buyTicketButton);
            cardGrid.Children.Add(detailsPanel); sessionCard.Child = cardGrid; SessionsPanel.Children.Add(sessionCard);
        }

        private async void BuyTicket_Click(object sender, RoutedEventArgs e)
        {
            if (currentUser == null) { MessageBox.Show("Please log in to book tickets.", "Login Required", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (sender is Button button && button.Tag is Tuple<int, int, decimal> sessionInfo)
            {
                var movieData = await GetMovieByIdAsync(sessionInfo.Item2);
                if (movieData != null)
                {
                    MovieDetailsWindow detailsWindow = new MovieDetailsWindow(movieData, currentUser) { Owner = this };
                    detailsWindow.ShowDialog();
                    await LoadSessionsForDateAsync(SessionCalendar.SelectedDate ?? DateTime.Today);
                }
                else { MessageBox.Show("Could not load movie details for booking.", "Error"); }
            }
        }

        private async Task<MovieData> GetMovieByIdAsync(int movieId)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @" SELECT movie_id, title, description, duration, mpaa_rating, release_date, poster_path, trailer_url, language, subtitles, director_name, genres, average_rating, review_count FROM movie_details_view WHERE movie_id = @movieId";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@movieId", movieId);
                        using (var reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync()) return new MovieData
                            {
                                Id = reader.GetInt32("movie_id"),
                                Title = reader.GetString("title"),
                                ImagePath = reader.IsDBNull(reader.GetOrdinal("poster_path")) ? "Images/placeholder.png" : reader.GetString("poster_path"),
                                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? "N/A" : reader.GetString("description"),
                                Genre = reader.IsDBNull(reader.GetOrdinal("genres")) ? "N/A" : reader.GetString("genres"),
                                Duration = reader.IsDBNull(reader.GetOrdinal("duration")) ? "N/A" : $"{reader.GetInt32("duration")} min",
                                Rating = reader.IsDBNull(reader.GetOrdinal("mpaa_rating")) ? "N/A" : reader.GetString("mpaa_rating"),
                                Director = reader.IsDBNull(reader.GetOrdinal("director_name")) ? "N/A" : reader.GetString("director_name"),
                                ReleaseDate = reader.IsDBNull(reader.GetOrdinal("release_date")) ? DateTime.MinValue : reader.GetDateTime("release_date"),
                                TrailerUrl = reader.IsDBNull(reader.GetOrdinal("trailer_url")) ? null : reader.GetString("trailer_url"),
                                AverageRating = reader.IsDBNull(reader.GetOrdinal("average_rating")) ? 0 : reader.GetDouble("average_rating"),
                                ReviewCount = reader.IsDBNull(reader.GetOrdinal("review_count")) ? 0 : reader.GetInt32("review_count")
                            };
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show($"Error fetching movie: {ex.Message}"); }
            return null;
        }

        private void NavigateToWindow(Window newWindow) { newWindow.Show(); this.Close(); }
        private void CinemaMain_Click(object sender, RoutedEventArgs e) => NavigateToWindow(new CinemaMainWindow(currentUser));
        private void SearchPage_Click(object sender, RoutedEventArgs e) => NavigateToWindow(new SearchPage(currentUser));
        private void MyTickets_Click(object sender, RoutedEventArgs e) { if (currentUser == null) { MessageBox.Show("Please log in."); return; } NavigateToWindow(new MyTickets(currentUser)); }
        private void MyProfile_Click(object sender, RoutedEventArgs e) { if (currentUser == null) { MessageBox.Show("Please log in."); return; } NavigateToWindow(new MyProfile(currentUser)); }
    }
}