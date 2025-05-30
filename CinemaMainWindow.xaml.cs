using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MySql.Data.MySqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace cinema_project
{
    public partial class CinemaMainWindow : Window
    {
        private DispatcherTimer rouletteTimer;
        private int currentMovieIndex = 0;
        private List<MovieData> featuredMovies;
        private List<MovieData> allMovies;
        private User currentUser;
        private readonly string connectionString = "datasource=127.0.0.1;port=3307;username=root;password=;database=database_cinema;";

        public CinemaMainWindow()
        {
            InitializeComponent();
        }

        public CinemaMainWindow(User user) : this()
        {
            currentUser = user;
        }

        private async void CinemaMainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (currentUser != null)
            {
                if (this.FindName("WelcomeTextBlock") is TextBlock welcomeTextBlock)
                {
                    welcomeTextBlock.Text = $"Welcome, {currentUser.FirstName} {currentUser.LastName}";
                }
            }
            else
            {
                if (this.FindName("WelcomeTextBlock") is TextBlock welcomeTextBlock)
                {
                    welcomeTextBlock.Text = "Welcome, Guest";
                }
            }
            await LoadFeaturedMoviesAsync();
            await LoadAllMoviesAsync();
        }

        private async Task LoadFeaturedMoviesAsync()
        {
            featuredMovies = new List<MovieData>();
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
                        SELECT movie_id, title, description, duration, mpaa_rating,
                               release_date, poster_path, trailer_url, language, subtitles,
                               director_name, genres, average_rating, review_count, is_featured
                        FROM movie_details_view
                        WHERE is_featured = TRUE AND is_active = TRUE
                        ORDER BY release_date DESC";

                    using (var command = new MySqlCommand(query, connection))
                    using (var reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            featuredMovies.Add(new MovieData
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
                                Language = reader.IsDBNull(reader.GetOrdinal("language")) ? "English" : reader.GetString("language"),
                                Subtitles = reader.IsDBNull(reader.GetOrdinal("subtitles")) ? null : reader.GetString("subtitles"),
                                AverageRating = reader.IsDBNull(reader.GetOrdinal("average_rating")) ? 0 : reader.GetDouble("average_rating"),
                                ReviewCount = reader.IsDBNull(reader.GetOrdinal("review_count")) ? 0 : reader.GetInt32("review_count")
                            });
                        }
                    }
                }

                if (featuredMovies.Any())
                {
                    InitializeMovieRoulette();
                }
                else
                {

                    if (this.FindName("FeaturedMovieTitle") is TextBlock featuredMovieTitle)
                        featuredMovieTitle.Text = "No Featured Movies Available";
                    if (this.FindName("FeaturedMovieImage") is Image featuredMovieImage)
                        featuredMovieImage.Source = new BitmapImage(new Uri("Images/placeholder.png", UriKind.Relative));
                    if (this.FindName("FeaturedMovieButton") is Button featuredMovieButton)
                        featuredMovieButton.IsEnabled = false;
                    HideAllIndicators();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading featured movies: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                if (this.FindName("FeaturedMovieTitle") is TextBlock featuredMovieTitle)
                    featuredMovieTitle.Text = "Error Loading Movies";
                if (this.FindName("FeaturedMovieImage") is Image featuredMovieImage)
                    featuredMovieImage.Source = new BitmapImage(new Uri("Images/placeholder.png", UriKind.Relative));
                if (this.FindName("FeaturedMovieButton") is Button featuredMovieButton)
                    featuredMovieButton.IsEnabled = false;
                HideAllIndicators();
            }
        }

        private async Task LoadAllMoviesAsync()
        {
            allMovies = new List<MovieData>();
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
                        SELECT movie_id, title, description, duration, mpaa_rating,
                               release_date, poster_path, trailer_url, language, subtitles,
                               director_name, genres, average_rating, review_count
                        FROM movie_details_view
                        WHERE is_active = TRUE
                        ORDER BY release_date DESC";

                    using (var command = new MySqlCommand(query, connection))
                    using (var reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            allMovies.Add(new MovieData
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
                                Language = reader.IsDBNull(reader.GetOrdinal("language")) ? "English" : reader.GetString("language"),
                                Subtitles = reader.IsDBNull(reader.GetOrdinal("subtitles")) ? null : reader.GetString("subtitles"),
                                AverageRating = reader.IsDBNull(reader.GetOrdinal("average_rating")) ? 0 : reader.GetDouble("average_rating"),
                                ReviewCount = reader.IsDBNull(reader.GetOrdinal("review_count")) ? 0 : reader.GetInt32("review_count")
                            });
                        }
                    }
                }

                UpdateMoviesListUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading movies: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                if (this.FindName("MoviesListPanel") is Panel moviesPanel)
                {
                    moviesPanel.Children.Clear();
                    moviesPanel.Children.Add(new TextBlock
                    {
                        Text = "Error loading movies",
                        FontStyle = FontStyles.Italic,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(10)
                    });
                }
            }
        }

        private void UpdateMoviesListUI()
        {
            if (this.FindName("MoviesListPanel") is Panel moviesPanel)
            {
                moviesPanel.Children.Clear();

                if (!allMovies.Any())
                {
                    moviesPanel.Children.Add(new TextBlock
                    {
                        Text = "No movies available",
                        FontStyle = FontStyles.Italic,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(10)
                    });
                    return;
                }

                WrapPanel wrapPanel = new WrapPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                foreach (var movie in allMovies)
                {
                    Border movieContainer = new Border
                    {
                        Background = Brushes.White,
                        BorderBrush = Brushes.LightGray,
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(8),
                        Margin = new Thickness(10),
                        Width = 200,
                        Height = 350,
                        Effect = new System.Windows.Media.Effects.DropShadowEffect
                        {
                            Color = Colors.Gray,
                            ShadowDepth = 3,
                            BlurRadius = 5,
                            Opacity = 0.3
                        }
                    };

                    StackPanel movieContent = new StackPanel
                    {
                        Margin = new Thickness(10)
                    };

                    Image posterImage = new Image
                    {
                        Width = 160,
                        Height = 220,
                        Stretch = Stretch.UniformToFill,
                        Margin = new Thickness(0, 0, 0, 10)
                    };

                    try
                    {
                        if (!string.IsNullOrWhiteSpace(movie.ImagePath) &&
                            Uri.IsWellFormedUriString(movie.ImagePath, UriKind.RelativeOrAbsolute))
                        {
                            posterImage.Source = new BitmapImage(new Uri(movie.ImagePath, UriKind.RelativeOrAbsolute));
                        }
                        else
                        {
                            posterImage.Source = new BitmapImage(new Uri("Images/placeholder.png", UriKind.Relative));
                        }
                    }
                    catch
                    {
                        posterImage.Source = new BitmapImage(new Uri("Images/placeholder.png", UriKind.Relative));
                    }

                    TextBlock titleText = new TextBlock
                    {
                        Text = movie.Title,
                        FontWeight = FontWeights.Bold,
                        FontSize = 14,
                        TextWrapping = TextWrapping.Wrap,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 5),
                        Height = 40
                    };

                    TextBlock genreText = new TextBlock
                    {
                        Text = $"{movie.Genre} • {movie.Rating}",
                        FontSize = 11,
                        Foreground = Brushes.Gray,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 10)
                    };

                    Button viewDetailsButton = new Button
                    {
                        Content = "View Details",
                        Background = new SolidColorBrush(Color.FromRgb(0, 119, 204)),
                        Foreground = Brushes.White,
                        BorderThickness = new Thickness(0),
                        Padding = new Thickness(15, 5, 15, 5),
                        FontWeight = FontWeights.SemiBold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Tag = movie.Id,
                        Cursor = System.Windows.Input.Cursors.Hand
                    };

                    viewDetailsButton.Click += ViewMovieDetailsFromList_Click;

                    viewDetailsButton.MouseEnter += (s, e) =>
                    {
                        viewDetailsButton.Background = new SolidColorBrush(Color.FromRgb(0, 100, 180));
                    };
                    viewDetailsButton.MouseLeave += (s, e) =>
                    {
                        viewDetailsButton.Background = new SolidColorBrush(Color.FromRgb(0, 119, 204));
                    };

                    movieContent.Children.Add(posterImage);
                    movieContent.Children.Add(titleText);
                    movieContent.Children.Add(genreText);
                    movieContent.Children.Add(viewDetailsButton);

                    movieContainer.Child = movieContent;
                    wrapPanel.Children.Add(movieContainer);
                }

                moviesPanel.Children.Add(wrapPanel);
            }
        }

        private async void ViewMovieDetailsFromList_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button?.Tag is int movieId)
            {
                try
                {
                    rouletteTimer?.Stop();

                    MovieData selectedMovie = await GetMovieByIdAsync(movieId);
                    if (selectedMovie != null)
                    {
                        MovieDetailsWindow detailsWindow = new MovieDetailsWindow(selectedMovie, currentUser);
                        detailsWindow.Owner = this;
                        detailsWindow.ShowDialog();
                    }
                    else
                    {
                        MessageBox.Show("Movie details not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening movie details: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    if (rouletteTimer != null && featuredMovies != null && featuredMovies.Any())
                    {
                        rouletteTimer.Start();
                    }
                }
            }
            else
            {
                MessageBox.Show("Could not identify the movie to view details.", "Selection Error", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void InitializeMovieRoulette()
        {
            if (featuredMovies == null || !featuredMovies.Any()) return;

            rouletteTimer?.Stop();

            currentMovieIndex = 0;
            UpdateFeaturedMovie();

            rouletteTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            rouletteTimer.Tick += RouletteTimer_Tick;
            rouletteTimer.Start();
        }

        private void RouletteTimer_Tick(object sender, EventArgs e)
        {
            if (featuredMovies == null || featuredMovies.Count == 0) return;

            currentMovieIndex = (currentMovieIndex + 1) % featuredMovies.Count;
            UpdateFeaturedMovie();
        }

        private void UpdateFeaturedMovie()
        {
            if (featuredMovies != null && featuredMovies.Count > 0 && currentMovieIndex < featuredMovies.Count)
            {
                var currentMovie = featuredMovies[currentMovieIndex];

                Image featuredMovieImage = this.FindName("FeaturedMovieImage") as Image;
                TextBlock featuredMovieTitle = this.FindName("FeaturedMovieTitle") as TextBlock;
                Button featuredMovieButton = this.FindName("FeaturedMovieButton") as Button;

                if (featuredMovieImage != null)
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(currentMovie.ImagePath) &&
                            Uri.IsWellFormedUriString(currentMovie.ImagePath, UriKind.RelativeOrAbsolute))
                        {
                            featuredMovieImage.Source = new BitmapImage(new Uri(currentMovie.ImagePath, UriKind.RelativeOrAbsolute));
                        }
                        else
                        {
                            featuredMovieImage.Source = new BitmapImage(new Uri("Images/placeholder.png", UriKind.Relative));
                        }
                    }
                    catch
                    {
                        featuredMovieImage.Source = new BitmapImage(new Uri("Images/placeholder.png", UriKind.Relative));
                    }
                }

                if (featuredMovieTitle != null)
                {
                    featuredMovieTitle.Text = currentMovie.Title;
                }

                if (featuredMovieButton != null)
                {
                    featuredMovieButton.Tag = currentMovie.Id;
                    featuredMovieButton.IsEnabled = true;
                }
                UpdateRouletteIndicators();
            }
        }

        private void UpdateRouletteIndicators()
        {
            var indicator1 = this.FindName("Indicator1") as System.Windows.Shapes.Shape;
            var indicator2 = this.FindName("Indicator2") as System.Windows.Shapes.Shape;
            var indicator3 = this.FindName("Indicator3") as System.Windows.Shapes.Shape;

            if (indicator1 != null) indicator1.Fill = Brushes.LightGray;
            if (indicator2 != null) indicator2.Fill = Brushes.LightGray;
            if (indicator3 != null) indicator3.Fill = Brushes.LightGray;

            if (featuredMovies == null || featuredMovies.Count == 0) return;

            var activeBrush = new SolidColorBrush(Color.FromRgb(0, 119, 204));

            if (indicator1 != null)
            {
                indicator1.Visibility = featuredMovies.Count >= 1 ? Visibility.Visible : Visibility.Collapsed;
                if (currentMovieIndex == 0 && featuredMovies.Count >= 1) indicator1.Fill = activeBrush;
            }
            if (indicator2 != null)
            {
                indicator2.Visibility = featuredMovies.Count >= 2 ? Visibility.Visible : Visibility.Collapsed;
                if (currentMovieIndex == 1 && featuredMovies.Count >= 2) indicator2.Fill = activeBrush;
            }
            if (indicator3 != null)
            {
                indicator3.Visibility = featuredMovies.Count >= 3 ? Visibility.Visible : Visibility.Collapsed;
                if (currentMovieIndex == 2 && featuredMovies.Count >= 3) indicator3.Fill = activeBrush;
            }
        }

        private void HideAllIndicators()
        {
            var indicator1 = this.FindName("Indicator1") as System.Windows.Shapes.Shape;
            var indicator2 = this.FindName("Indicator2") as System.Windows.Shapes.Shape;
            var indicator3 = this.FindName("Indicator3") as System.Windows.Shapes.Shape;

            if (indicator1 != null) indicator1.Visibility = Visibility.Collapsed;
            if (indicator2 != null) indicator2.Visibility = Visibility.Collapsed;
            if (indicator3 != null) indicator3.Visibility = Visibility.Collapsed;
        }

        private async void ViewMovieDetails_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button?.Tag is int movieId)
            {
                try
                {
                    rouletteTimer?.Stop();

                    MovieData selectedMovie = await GetMovieByIdAsync(movieId);
                    if (selectedMovie != null)
                    {
                        MovieDetailsWindow detailsWindow = new MovieDetailsWindow(selectedMovie, currentUser);
                        detailsWindow.Owner = this;
                        detailsWindow.ShowDialog();
                    }
                    else
                    {
                        MessageBox.Show("Movie details not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening movie details: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    if (rouletteTimer != null && featuredMovies != null && featuredMovies.Any())
                    {
                        rouletteTimer.Start();
                    }
                }
            }
            else
            {
                MessageBox.Show("Could not identify the movie to view details.", "Selection Error", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async Task<MovieData> GetMovieByIdAsync(int movieId)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
                        SELECT movie_id, title, description, duration, mpaa_rating,
                               release_date, poster_path, trailer_url, language, subtitles,
                               director_name, genres, average_rating, review_count
                        FROM movie_details_view
                        WHERE movie_id = @movieId AND is_active = TRUE";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@movieId", movieId);
                        using (var reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new MovieData
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
                                    Language = reader.IsDBNull(reader.GetOrdinal("language")) ? "English" : reader.GetString("language"),
                                    Subtitles = reader.IsDBNull(reader.GetOrdinal("subtitles")) ? null : reader.GetString("subtitles"),
                                    AverageRating = reader.IsDBNull(reader.GetOrdinal("average_rating")) ? 0 : reader.GetDouble("average_rating"),
                                    ReviewCount = reader.IsDBNull(reader.GetOrdinal("review_count")) ? 0 : reader.GetInt32("review_count")
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fetching movie details: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }

        private void NavigateToWindow(Window newWindow) { newWindow.Show(); this.Close(); }
        private void CinemaMain_Click(object sender, RoutedEventArgs e) => NavigateToWindow(new CinemaMainWindow(currentUser));
        private void MovieSessionCalendar_Click(object sender, RoutedEventArgs e) => NavigateToWindow(new MovieSessionCalendar(currentUser));
        private void SearchPage_Click(object sender, RoutedEventArgs e) => NavigateToWindow(new SearchPage(currentUser));
        private void MyTickets_Click(object sender, RoutedEventArgs e) => NavigateToWindow(new MyTickets(currentUser));

    }

    public class MovieData
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string ImagePath { get; set; }
        public string Description { get; set; }
        public string Genre { get; set; }
        public string Duration { get; set; }
        public string Rating { get; set; }
        public string Director { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string TrailerUrl { get; set; }
        public string Language { get; set; }
        public string Subtitles { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
    }
}