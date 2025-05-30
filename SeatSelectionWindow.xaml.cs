using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;

namespace cinema_project
{
    public partial class SeatSelectionWindow : Window
    {
        private ShowTimeData selectedShowTime;
        private MovieData currentMovie;
        private User currentUser;
        private string connectionString = "datasource=127.0.0.1;port=3307;username=root;password=;database=database_cinema;";
        private List<SeatData> allSeats;
        private List<SeatData> selectedSeats;
        private Dictionary<string, decimal> classPrices;

        public SeatSelectionWindow(ShowTimeData showTime, MovieData movie, User user)
        {
            InitializeComponent();
            selectedShowTime = showTime;
            currentMovie = movie;
            currentUser = user;
            allSeats = new List<SeatData>();
            selectedSeats = new List<SeatData>();

            classPrices = new Dictionary<string, decimal>
            {
                {"Premium", 25.00m},
                {"Standard+", 18.00m},
                {"Standard", 12.00m}
            };

            InitializeDisplay();
            LoadSeatsAsync();
        }

        private void InitializeDisplay()
        {
            MovieTitleText.Text = currentMovie.Title;
            ShowDetailsText.Text = $"{selectedShowTime.TheaterName} • {selectedShowTime.ShowDateTime:MMMM dd, yyyy} • {selectedShowTime.ShowDateTime:HH:mm}";
        }

        private async Task LoadSeatsAsync()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"
                        SELECT s.seat_id, s.seat_number, s.seat_row, s.seat_type, s.theater_id,
                               CASE WHEN b.booking_id IS NOT NULL THEN 1 ELSE 0 END as is_booked
                        FROM seats s
                        LEFT JOIN bookings b ON s.seat_id = b.seat_id AND b.showtime_id = @showtimeId
                        WHERE s.theater_id = (SELECT theater_id FROM showtimes WHERE showtime_id = @showtimeId)
                        ORDER BY s.seat_row, s.seat_number";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@showtimeId", selectedShowTime.ShowtimeId);
                        using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                allSeats.Add(new SeatData
                                {
                                    SeatId = reader.GetInt32("seat_id"),
                                    SeatNumber = reader.GetInt32("seat_number"),
                                    RowLetter = reader.GetString("seat_row"),
                                    SeatClass = reader.GetString("seat_type"),
                                    IsBooked = reader.GetBoolean("is_booked")
                                });
                            }
                        }
                    }
                }

                GenerateSeatMap();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading seats: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateSeatMap()
        {
            SeatMapGrid.Children.Clear();
            SeatMapGrid.RowDefinitions.Clear();
            SeatMapGrid.ColumnDefinitions.Clear();

            if (!allSeats.Any()) return;

            var rowGroups = allSeats.GroupBy(s => s.RowLetter).OrderBy(g => g.Key);
            int maxSeatsInRow = rowGroups.Max(g => g.Count());

            for (int i = 0; i < rowGroups.Count(); i++)
            {
                SeatMapGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
            }

            SeatMapGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            for (int i = 0; i < maxSeatsInRow; i++)
            {
                SeatMapGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(35) });
            }

            int rowIndex = 0;
            foreach (var rowGroup in rowGroups)
            {
                TextBlock rowLabel = new TextBlock
                {
                    Text = rowGroup.Key,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 14
                };
                Grid.SetRow(rowLabel, rowIndex);
                Grid.SetColumn(rowLabel, 0);
                SeatMapGrid.Children.Add(rowLabel);

                var seatsInRow = rowGroup.OrderBy(s => s.SeatNumber).ToList();
                for (int seatIndex = 0; seatIndex < seatsInRow.Count; seatIndex++)
                {
                    var seat = seatsInRow[seatIndex];
                    Button seatButton = CreateSeatButton(seat);
                    Grid.SetRow(seatButton, rowIndex);
                    Grid.SetColumn(seatButton, seatIndex + 1);
                    SeatMapGrid.Children.Add(seatButton);
                }

                rowIndex++;
            }
        }

        private Button CreateSeatButton(SeatData seat)
        {
            Button seatButton = new Button
            {
                Content = seat.SeatNumber.ToString(),
                Width = 30,
                Height = 30,
                Margin = new Thickness(2),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Tag = seat
            };

            if (seat.IsBooked)
            {
                seatButton.Background = new SolidColorBrush(Color.FromRgb(149, 165, 166));
                seatButton.Foreground = Brushes.White;
                seatButton.IsEnabled = false;
            }
            else
            {
                switch (seat.SeatClass.ToUpper())
                {
                    case "A":
                        seatButton.Background = new SolidColorBrush(Color.FromRgb(231, 76, 60));
                        break;
                    case "B":
                        seatButton.Background = new SolidColorBrush(Color.FromRgb(52, 152, 219));
                        break;
                    case "C":
                        seatButton.Background = new SolidColorBrush(Color.FromRgb(46, 204, 113));
                        break;
                    default:
                        seatButton.Background = new SolidColorBrush(Color.FromRgb(127, 140, 141));
                        break;
                }
                seatButton.Foreground = Brushes.White;
                seatButton.Click += SeatButton_Click;
            }

            return seatButton;
        }

        private void SeatButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SeatData seat)
            {
                if (selectedSeats.Contains(seat))
                {
                    selectedSeats.Remove(seat);
                    button.BorderBrush = null;
                    button.BorderThickness = new Thickness(0);
                }
                else
                {
                    selectedSeats.Add(seat);
                    button.BorderBrush = new SolidColorBrush(Color.FromRgb(211, 84, 0));
                    button.BorderThickness = new Thickness(3);
                }

                UpdateSelectionDisplay();
            }
        }

        private void UpdateSelectionDisplay()
        {
            if (selectedSeats.Any())
            {
                var seatNumbers = selectedSeats.Select(s => $"{s.RowLetter}{s.SeatNumber}").OrderBy(s => s);
                SelectedSeatsText.Text = $"Selected Seats: {string.Join(", ", seatNumbers)}";

                decimal totalPrice = selectedSeats.Sum(s => classPrices[s.SeatClass.ToUpper()]);
                TotalPriceText.Text = $"Total: ${totalPrice:F2}";

                ProceedButton.IsEnabled = true;
            }
            else
            {
                SelectedSeatsText.Text = "Selected Seats: None";
                TotalPriceText.Text = "Total: $0.00";
                ProceedButton.IsEnabled = false;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Proceed_Click(object sender, RoutedEventArgs e)
        {
            if (!selectedSeats.Any())
            {
                MessageBox.Show("Please select at least one seat.", "No Seats Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal totalPrice = selectedSeats.Sum(s => classPrices[s.SeatClass.ToUpper()]);

            PaymentWindow paymentWindow = new PaymentWindow(
                selectedShowTime,
                currentMovie,
                currentUser,
                selectedSeats,
                totalPrice
            );

            paymentWindow.Owner = this;
            if (paymentWindow.ShowDialog() == true)
            {
                DialogResult = true;
                this.Close();
            }
        }
    }

    public class SeatData
    {
        public int SeatId { get; set; }
        public int SeatNumber { get; set; }
        public string RowLetter { get; set; } = "";
        public string SeatClass { get; set; } = "";
        public bool IsBooked { get; set; }
    }
}
