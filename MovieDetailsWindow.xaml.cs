using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using MySql.Data.MySqlClient;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace cinema_project
{
    public partial class MovieDetailsWindow : Window
    {
        private MovieData currentMovie;
        private User currentUser;
        private string connectionString = "datasource=127.0.0.1;port=3307;username=root;password=;database=database_cinema;";
        private List<ShowTimeData> showTimes;

        public MovieDetailsWindow(MovieData movie, User user)
        {
            InitializeComponent();
            currentMovie = movie;
            currentUser = user;
            showTimes = new List<ShowTimeData>();
            DisplayMovieDetails();
            LoadShowTimesAsync();
        }

        private void DisplayMovieDetails()
        {
            MovieTitleHeader.Text = currentMovie.Title;
            MovieGenre.Text = $"{currentMovie.Genre} • {currentMovie.Duration} • {currentMovie.Rating}";
            MovieDirector.Text = $"Director: {currentMovie.Director}";
            MovieReleaseDate.Text = currentMovie.ReleaseDate != DateTime.MinValue ? currentMovie.ReleaseDate.ToString("MMMM dd, yyyy") : "Release date unknown";
            MovieDescription.Text = currentMovie.Description;
            LoadMoviePoster();
        }

        private void LoadMoviePoster()
        {
            try
            {
                MoviePosterImage.Source = !string.IsNullOrEmpty(currentMovie.ImagePath) && Uri.IsWellFormedUriString(currentMovie.ImagePath, UriKind.RelativeOrAbsolute)
                    ? new BitmapImage(new Uri(currentMovie.ImagePath, UriKind.RelativeOrAbsolute)) { CacheOption = BitmapCacheOption.OnLoad }
                    : new BitmapImage(new Uri("Images/placeholder.png", UriKind.Relative));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not load movie poster: {ex.Message}");
                MoviePosterImage.Source = new BitmapImage(new Uri("Images/placeholder.png", UriKind.Relative));
            }
        }

        private async Task LoadShowTimesAsync()
        {
            showTimes.Clear();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
                        SELECT showtime_id, movie_title, theater_name, show_date, show_time, 
                               end_time, price, available_seats, total_seats
                        FROM upcoming_showtimes_view
                        WHERE movie_id = @movieId AND show_date >= CURDATE() AND show_date <= DATE_ADD(CURDATE(), INTERVAL 7 DAY) 
                        ORDER BY show_date, show_time";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@movieId", currentMovie.Id);
                        using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                DateTime showDate = reader.GetDateTime("show_date");
                                TimeSpan showTimeSpan = reader.GetTimeSpan("show_time");
                                showTimes.Add(new ShowTimeData
                                {
                                    ShowtimeId = reader.GetInt32("showtime_id"),
                                    ShowDateTime = showDate.Add(showTimeSpan),
                                    TheaterName = reader.GetString("theater_name"),
                                    AvailableSeats = reader.GetInt32("available_seats"),
                                    TotalSeats = reader.GetInt32("total_seats"),
                                    Price = reader.GetDecimal("price")
                                });
                            }
                        }
                    }
                }
                UpdateShowTimesUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading show times: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void UpdateShowTimesUI()
        {
            ShowTimesPanel.Children.Clear();
            if (!showTimes.Any())
            {
                ShowTimesPanel.Children.Add(new TextBlock { Text = "No show times available in the next 7 days.", FontStyle = FontStyles.Italic });
                return;
            }
            var groupedShowtimes = showTimes.GroupBy(st => st.ShowDateTime.Date).OrderBy(g => g.Key);
            foreach (var group in groupedShowtimes)
            {
                ShowTimesPanel.Children.Add(new TextBlock
                {
                    Text = group.Key.ToString("dddd, MMMM dd"),
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 10, 0, 5)
                });
                WrapPanel timesWrapPanel = new WrapPanel();
                foreach (var showTime in group.OrderBy(st => st.ShowDateTime))
                {
                    Button timeButton = new Button
                    {
                        Content = showTime.ShowDateTime.ToString("HH:mm"),
                        Background = System.Windows.Media.Brushes.DodgerBlue,
                        Foreground = System.Windows.Media.Brushes.White,
                        Padding = new Thickness(10, 5, 10, 5),
                        Margin = new Thickness(5),
                        Tag = showTime.ShowtimeId
                    };
                    timeButton.Click += SelectShowTime_Click;
                    timesWrapPanel.Children.Add(timeButton);
                }
                ShowTimesPanel.Children.Add(timesWrapPanel);
            }
        }

        private void BookTickets_Click(object sender, RoutedEventArgs e)
        {
            if (currentUser == null)
            {
                MessageBox.Show("Please log in to book tickets.", "Authentication Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!showTimes.Any())
            {
                MessageBox.Show("No show times available to book for this movie.", "No Shows", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            MessageBox.Show("Please select a show time from the list to proceed with booking.", "Select Show Time", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void AddToWishlist_Click(object sender, RoutedEventArgs e)
        {
            if (currentUser == null)
            {
                MessageBox.Show("Please log in to add movies to your wishlist.", "Authentication Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            int movieId = currentMovie.Id;
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string checkQuery = "SELECT COUNT(*) FROM wishlist WHERE user_id = @userId AND movie_id = @movieId";
                    using (MySqlCommand checkCmd = new MySqlCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@userId", currentUser.Id);
                        checkCmd.Parameters.AddWithValue("@movieId", movieId);
                        if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0)
                        {
                            MessageBox.Show($"'{currentMovie.Title}' is already in your wishlist.", "Already Added", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                    }
                    string insertQuery = "INSERT INTO wishlist (user_id, movie_id, added_date) VALUES (@userId, @movieId, NOW())";
                    using (MySqlCommand insertCmd = new MySqlCommand(insertQuery, connection))
                    {
                        insertCmd.Parameters.AddWithValue("@userId", currentUser.Id);
                        insertCmd.Parameters.AddWithValue("@movieId", movieId);
                        if (await insertCmd.ExecuteNonQueryAsync() > 0)
                        {
                            MessageBox.Show($"'{currentMovie.Title}' added to your wishlist!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            await LogMovieActivityAsync("WISHLIST_ADD", currentUser.Id, movieId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding to wishlist: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SelectShowTime_Click(object sender, RoutedEventArgs e)
        {
            if (currentUser == null)
            {
                MessageBox.Show("Please log in to book tickets.", "Authentication Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (sender is Button timeButton && timeButton.Tag is int showtimeId)
            {
                var selectedShowTime = showTimes.Find(st => st.ShowtimeId == showtimeId);
                if (selectedShowTime != null)
                {
                    var defaultSeat = new SeatData
                    {
                        SeatId = 1,
                        SeatNumber = 1,
                        RowLetter = "A",
                        SeatClass = "Standard",
                        IsBooked = false
                    };

                    var selectedSeats = new List<SeatData> { defaultSeat };
                    decimal price = 12.00m;

                    PaymentWindow paymentWindow = new PaymentWindow(
                        selectedShowTime,
                        currentMovie,
                        currentUser,
                        selectedSeats,
                        price
                    );
                    paymentWindow.Owner = this;

                    if (paymentWindow.ShowDialog() == true)
                    {
                        await LoadShowTimesAsync();
                        MessageBox.Show("Tickets booked successfully!", "Booking Confirmed", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

        private async void ShareMovie_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (currentUser != null) await LogMovieActivityAsync("SHARE", currentUser.Id, currentMovie.Id);
                string shareText = $"Check out this movie: {currentMovie.Title}\nGenre: {currentMovie.Genre}\nDirector: {currentMovie.Director}\nRelease Date: {currentMovie.ReleaseDate:MMMM dd, yyyy}\n{(currentMovie.TrailerUrl != null ? $"Trailer: {currentMovie.TrailerUrl}\n" : "")}Description: {currentMovie.Description}";
                Clipboard.SetText(shareText);
                MessageBox.Show("Movie information copied to clipboard!", "Share Movie");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not share movie: {ex.Message}", "Share Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void Close_Click(object sender, RoutedEventArgs e)
        {
            if (currentUser != null) await LogMovieActivityAsync("VIEW_DETAIL_CLOSE", currentUser.Id, currentMovie.Id);
            this.Close();
        }

        protected override async void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            if (currentUser != null && IsActive) await LogMovieActivityAsync("VIEW_DETAIL_OPEN", currentUser.Id, currentMovie.Id);
        }

        private async Task LogMovieActivityAsync(string activityType, int userId, int movieId)
        {
            if (userId == 0) return;
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"INSERT INTO user_activities (user_id, movie_id, activity_type, activity_date) VALUES (@userId, @movieId, @activityType, NOW())";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", userId);
                        command.Parameters.AddWithValue("@movieId", movieId);
                        command.Parameters.AddWithValue("@activityType", activityType);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to log activity: {ex.Message}");
            }
        }
    }

    public class ShowTimeData
    {
        public int ShowtimeId { get; set; }
        public DateTime ShowDateTime { get; set; }
        public string TheaterName { get; set; } = "";
        public int AvailableSeats { get; set; }
        public int TotalSeats { get; set; }
        public decimal Price { get; set; }
    }
}