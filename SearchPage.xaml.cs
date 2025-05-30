using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using System.Data;
using System.Text;

namespace cinema_project
{
    public partial class SearchPage : Window
    {
        private string connectionString = "datasource=127.0.0.1;port=3307;username=root;password=;database=database_cinema;";
        private List<CheckBox> genreCheckBoxes = new List<CheckBox>();
        private User currentUser;

        public SearchPage(User user)
        {
            InitializeComponent();
            currentUser = user;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.FindName("SearchTextBox") == null || StatusComboBox == null || StatusComboBox2 == null || MoviesPanel == null)
            {
                MessageBox.Show("Search page UI elements (SearchTextBox, StatusComboBox, etc.) not fully loaded. Check XAML x:Name attributes.", "UI XAML Error");
                return;
            }
            await LoadGenresAsync();
            if (StatusComboBox.Items.Count > 0) StatusComboBox.SelectedIndex = 0;
            await LoadMoviesAsync();
        }

        private async Task LoadGenresAsync()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT genre_id, genre_name FROM genres ORDER BY genre_name";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            CheckBox genreCheckBox = new CheckBox { Content = reader["genre_name"].ToString(), Tag = reader.GetInt32("genre_id"), Margin = new Thickness(5, 5, 5, 5), FontSize = 13, VerticalContentAlignment = VerticalAlignment.Center };
                            genreCheckBox.Checked += GenreOrStatusChanged; genreCheckBox.Unchecked += GenreOrStatusChanged;
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show($"Error loading genres: {ex.Message}", "Database Error"); }
        }

        private async void GenreOrStatusChanged(object sender, RoutedEventArgs e) => await LoadMoviesAsync();
        private async void SearchButton_Click(object sender, RoutedEventArgs e) => await LoadMoviesAsync();
        private async void StatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (IsLoaded) await LoadMoviesAsync(); }

        private async Task LoadMoviesAsync()
        {
            MoviesPanel.Children.Clear();

            string searchTerm = "";
            if (this.FindName("SearchTextBox") is TextBox searchTextBoxInstance)
            {
                searchTerm = searchTextBoxInstance.Text.Trim();
            }

            List<int> selectedGenreIds = genreCheckBoxes.Where(cb => cb.IsChecked == true && cb.Tag != null).Select(cb => (int)cb.Tag).ToList();
            string movieStatusFilter = (StatusComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "now_showing";

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    var sqlBuilder = new StringBuilder(@"
                        SELECT DISTINCT m.movie_id, m.title, m.description, m.duration, m.mpaa_rating, 
                               m.release_date, m.poster_path, m.language, m.subtitles,
                               d.director_name, GROUP_CONCAT(DISTINCT g.genre_name SEPARATOR ', ') as genres
                        FROM movies m
                        LEFT JOIN directors d ON m.director_id = d.director_id
                        LEFT JOIN movie_genres mg ON m.movie_id = mg.movie_id
                        LEFT JOIN genres g ON mg.genre_id = g.genre_id
                        WHERE m.is_active = TRUE ");
                    if (!string.IsNullOrWhiteSpace(searchTerm)) sqlBuilder.Append(" AND (m.title LIKE @searchTerm OR m.description LIKE @searchTerm OR d.director_name LIKE @searchTerm) ");
                    if (selectedGenreIds.Any())
                    {
                        sqlBuilder.Append(" AND m.movie_id IN (SELECT DISTINCT movie_id FROM movie_genres WHERE genre_id IN (");
                        for (int i = 0; i < selectedGenreIds.Count; i++) { sqlBuilder.Append($"@genreId{i}"); if (i < selectedGenreIds.Count - 1) sqlBuilder.Append(","); }
                        sqlBuilder.Append(")) ");
                    }
                    if (movieStatusFilter == "now_showing") sqlBuilder.Append(" AND m.release_date <= CURDATE() ");
                    else if (movieStatusFilter == "coming_soon") sqlBuilder.Append(" AND m.release_date > CURDATE() ");
                    sqlBuilder.Append(" GROUP BY m.movie_id ORDER BY m.release_date DESC, m.title;");

                    using (MySqlCommand command = new MySqlCommand(sqlBuilder.ToString(), connection))
                    {
                        if (!string.IsNullOrWhiteSpace(searchTerm)) command.Parameters.AddWithValue("@searchTerm", $"%{searchTerm}%");
                        for (int i = 0; i < selectedGenreIds.Count; i++) command.Parameters.AddWithValue($"@genreId{i}", selectedGenreIds[i]);

                        using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                        {
                            if (!reader.HasRows) { MoviesPanel.Children.Add(new TextBlock { Text = "No movies found matching your criteria.", FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(20, 20, 20, 20) }); return; }
                            while (await reader.ReadAsync()) CreateMovieCard(reader);
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show($"Error loading movies: {ex.Message}", "Database Error"); }
        }

        private void CreateMovieCard(MySqlDataReader reader)
        {
            Border movieCard = new Border { Background = Brushes.White, CornerRadius = new CornerRadius(8), Padding = new Thickness(15, 15, 15, 15), Margin = new Thickness(10, 10, 10, 10), BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(1) };
            Grid cardGrid = new Grid();
            cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) }); cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Image posterImage = new Image { Width = 90, Height = 135, Stretch = Stretch.UniformToFill, Margin = new Thickness(0, 0, 15, 0) };
            string posterPath = reader["poster_path"]?.ToString();
            try { posterImage.Source = !string.IsNullOrEmpty(posterPath) && Uri.IsWellFormedUriString(posterPath, UriKind.RelativeOrAbsolute) ? new BitmapImage(new Uri(posterPath, UriKind.RelativeOrAbsolute)) : new BitmapImage(new Uri("Images/placeholder.png", UriKind.Relative)); } catch { posterImage.Source = new BitmapImage(new Uri("Images/placeholder.png", UriKind.Relative)); }
            Grid.SetColumn(posterImage, 0); cardGrid.Children.Add(posterImage);

            StackPanel detailsPanel = new StackPanel(); Grid.SetColumn(detailsPanel, 1);
            detailsPanel.Children.Add(new TextBlock { Text = reader["title"].ToString(), FontSize = 18, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(0, 64, 128)), Margin = new Thickness(0, 0, 0, 5) });
            detailsPanel.Children.Add(new TextBlock { Text = $"🎬 Director: {reader["director_name"] ?? "N/A"}", FontSize = 12, Foreground = Brushes.DarkSlateGray, Margin = new Thickness(0, 0, 0, 3) });
            detailsPanel.Children.Add(new TextBlock { Text = $"🎭 Genres: {reader["genres"] ?? "N/A"}", FontSize = 12, Foreground = Brushes.DarkSlateGray, Margin = new Thickness(0, 0, 0, 3) });
            detailsPanel.Children.Add(new TextBlock { Text = $"⏳ {reader.GetInt32("duration")} min | {reader["mpaa_rating"] ?? "NR"}", FontSize = 12, Foreground = Brushes.DarkSlateGray, Margin = new Thickness(0, 0, 0, 3) });
            detailsPanel.Children.Add(new TextBlock { Text = $"🗓️ Released: {reader.GetDateTime("release_date"):MMMM dd, politique}", FontSize = 12, Foreground = Brushes.DarkSlateGray, Margin = new Thickness(0, 0, 0, 8) });
            detailsPanel.Children.Add(new TextBlock { Text = reader["description"]?.ToString(), FontSize = 11, TextWrapping = TextWrapping.Wrap, MaxHeight = 60, TextTrimming = TextTrimming.CharacterEllipsis, Foreground = Brushes.Gray, Margin = new Thickness(0, 0, 0, 10) });
            Button viewDetailsButton = new Button { Content = "View Details & Sessions", Background = new SolidColorBrush(Color.FromRgb(0, 119, 204)), Foreground = Brushes.White, Padding = new Thickness(10, 5, 10, 5), HorizontalAlignment = HorizontalAlignment.Left, Tag = reader.GetInt32("movie_id") };
            viewDetailsButton.Click += ViewDetails_Click; detailsPanel.Children.Add(viewDetailsButton);
            cardGrid.Children.Add(detailsPanel); movieCard.Child = cardGrid; MoviesPanel.Children.Add(movieCard);
        }

        private async void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int movieId)
            {
                MovieData selectedMovie = await GetFullMovieDataByIdAsync(movieId);
                if (selectedMovie != null) { MovieDetailsWindow detailsWindow = new MovieDetailsWindow(selectedMovie, currentUser) { Owner = this }; detailsWindow.ShowDialog(); }
                else { MessageBox.Show("Could not retrieve movie details.", "Error"); }
            }
        }

        private async Task<MovieData> GetFullMovieDataByIdAsync(int movieId)
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
        private void MovieSessionCalendar_Click(object sender, RoutedEventArgs e) => NavigateToWindow(new MovieSessionCalendar(currentUser));
        private void MyTickets_Click(object sender, RoutedEventArgs e) { if (currentUser == null) { MessageBox.Show("Please log in."); return; } NavigateToWindow(new MyTickets(currentUser)); }
        private void MyProfile_Click(object sender, RoutedEventArgs e) { if (currentUser == null) { MessageBox.Show("Please log in."); return; } NavigateToWindow(new MyProfile(currentUser)); }
    }
}