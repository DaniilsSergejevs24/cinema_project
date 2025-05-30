using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;
using System.Data;
using System.Threading.Tasks;

namespace cinema_project
{
    public partial class PaymentWindow : Window
    {
        private ShowTimeData selectedShowTime;
        private MovieData currentMovie;
        private User currentUser;
        private List<SeatData> selectedSeats;
        private decimal totalAmount;
        private string connectionString = "datasource=127.0.0.1;port=3307;username=root;password=;database=database_cinema;";

        public PaymentWindow(ShowTimeData showTime, MovieData movie, User user, List<SeatData> seats, decimal amount)
        {
            InitializeComponent();
            selectedShowTime = showTime;
            currentMovie = movie;
            currentUser = user;
            selectedSeats = seats;
            totalAmount = amount;

            InitializeDisplay();
        }

        private void InitializeDisplay()
        {
            MovieTitleText.Text = currentMovie.Title;
            TheaterText.Text = selectedShowTime.TheaterName;
            DateTimeText.Text = $"{selectedShowTime.ShowDateTime:MMMM dd, yyyy} at {selectedShowTime.ShowDateTime:HH:mm}";

            var seatNumbers = selectedSeats.Select(s => $"{s.RowLetter}{s.SeatNumber}").OrderBy(s => s);
            SeatsText.Text = string.Join(", ", seatNumbers);

            TotalAmountText.Text = $"${totalAmount:F2}";

            if (!string.IsNullOrEmpty(currentUser?.Email))
            {
                EmailTextBox.Text = currentUser.Email;
            }
        }

        private void CardNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                string text = textBox.Text.Replace(" ", "");
                if (text.Length > 16) text = text.Substring(0, 16);

                string formatted = "";
                for (int i = 0; i < text.Length; i++)
                {
                    if (i > 0 && i % 4 == 0) formatted += " ";
                    formatted += text[i];
                }

                textBox.TextChanged -= CardNumber_TextChanged;
                textBox.Text = formatted;
                textBox.CaretIndex = formatted.Length;
                textBox.TextChanged += CardNumber_TextChanged;
            }
        }

        private void ExpiryDate_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                string text = textBox.Text.Replace("/", "");
                if (text.Length > 4) text = text.Substring(0, 4);

                if (text.Length >= 2)
                {
                    string formatted = text.Substring(0, 2) + "/" + text.Substring(2);
                    textBox.TextChanged -= ExpiryDate_TextChanged;
                    textBox.Text = formatted;
                    textBox.CaretIndex = formatted.Length;
                    textBox.TextChanged += ExpiryDate_TextChanged;
                }
            }
        }

        private bool ValidatePaymentForm()
        {
            List<string> errors = new List<string>();

            string cardNumber = CardNumberTextBox.Text.Replace(" ", "");
            if (string.IsNullOrWhiteSpace(cardNumber) || cardNumber.Length < 13 || !cardNumber.All(char.IsDigit))
            {
                errors.Add("Please enter a valid card number");
            }

            string expiryDate = ExpiryDateTextBox.Text;
            if (string.IsNullOrWhiteSpace(expiryDate) || !Regex.IsMatch(expiryDate, @"^\d{2}/\d{2}$"))
            {
                errors.Add("Please enter expiry date in MM/YY format");
            }
            else
            {
                var parts = expiryDate.Split('/');
                int month = int.Parse(parts[0]);
                int year = 2000 + int.Parse(parts[1]);
                DateTime expiry = new DateTime(year, month, DateTime.DaysInMonth(year, month));
                if (expiry < DateTime.Now)
                {
                    errors.Add("Card has expired");
                }
            }

            string cvv = CVVTextBox.Text;
            if (string.IsNullOrWhiteSpace(cvv) || cvv.Length < 3 || !cvv.All(char.IsDigit))
            {
                errors.Add("Please enter a valid CVV");
            }

            if (string.IsNullOrWhiteSpace(CardholderNameTextBox.Text))
            {
                errors.Add("Please enter cardholder name");
            }

            string email = EmailTextBox.Text;
            if (string.IsNullOrWhiteSpace(email) || !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                errors.Add("Please enter a valid email address");
            }

            if (!TermsCheckBox.IsChecked == true)
            {
                errors.Add("Please accept the terms and conditions");
            }

            if (errors.Any())
            {
                PaymentStatusText.Text = string.Join("\n", errors);
                PaymentStatusText.Visibility = Visibility.Visible;
                return false;
            }

            PaymentStatusText.Visibility = Visibility.Collapsed;
            return true;
        }

        private async void Pay_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidatePaymentForm())
                return;

            PayButton.IsEnabled = false;
            PayButton.Content = "Processing...";

            try
            {
                await Task.Delay(2000);

                bool allBookingsSuccessful = true;
                List<string> bookingReferences = new List<string>();

                foreach (var seat in selectedSeats)
                {
                    string bookingRef = await ProcessSeatBooking(seat);
                    if (!string.IsNullOrEmpty(bookingRef))
                    {
                        bookingReferences.Add(bookingRef);
                    }
                    else
                    {
                        allBookingsSuccessful = false;
                        break;
                    }
                }

                if (allBookingsSuccessful)
                {
                    await LogMovieActivityAsync("BOOK", currentUser.Id, currentMovie.Id);

                    string message = $"Payment successful!\n\n";
                    message += $"Booking References: {string.Join(", ", bookingReferences)}\n";
                    message += $"Movie: {currentMovie.Title}\n";
                    message += $"Theater: {selectedShowTime.TheaterName}\n";
                    message += $"Date & Time: {selectedShowTime.ShowDateTime:MMMM dd, yyyy} at {selectedShowTime.ShowDateTime:HH:mm}\n";
                    message += $"Seats: {string.Join(", ", selectedSeats.Select(s => $"{s.RowLetter}{s.SeatNumber}"))}\n";
                    message += $"Total Paid: ${totalAmount:F2}\n\n";
                    message += $"A confirmation email has been sent to {EmailTextBox.Text}";

                    MessageBox.Show(message, "Booking Successful", MessageBoxButton.OK, MessageBoxImage.Information);

                    DialogResult = true;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Some bookings failed. Please try again or contact support.", "Booking Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Payment processing failed: {ex.Message}", "Payment Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                PayButton.IsEnabled = true;
                PayButton.Content = "Pay Now";
            }
        }

        private async Task<string> ProcessSeatBooking(SeatData seat)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new MySqlCommand("BookTicket", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("p_user_id", currentUser.Id);
                        command.Parameters.AddWithValue("p_showtime_id", selectedShowTime.ShowtimeId);
                        command.Parameters.AddWithValue("p_seat_id", seat.SeatId);
                        command.Parameters.AddWithValue("p_quantity", 1);
                        command.Parameters.AddWithValue("p_payment_method", "Credit Card");

                        MySqlParameter pBookingRef = new MySqlParameter("p_booking_reference", MySqlDbType.VarChar, 20)
                        { Direction = ParameterDirection.Output };
                        MySqlParameter pResult = new MySqlParameter("p_result", MySqlDbType.VarChar, 100)
                        { Direction = ParameterDirection.Output };

                        command.Parameters.Add(pBookingRef);
                        command.Parameters.Add(pResult);

                        await command.ExecuteNonQueryAsync();

                        string result = pResult.Value?.ToString() ?? "";
                        if (result.ToLower().Contains("successful"))
                        {
                            return pBookingRef.Value?.ToString() ?? "";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing booking for seat {seat.RowLetter}{seat.SeatNumber}: {ex.Message}");
            }
            return null;
        }

        private async Task LogMovieActivityAsync(string activityType, int userId, int movieId)
        {
            if (userId == 0) return;
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"INSERT INTO user_activities (user_id, movie_id, activity_type, activity_date) 
                                   VALUES (@userId, @movieId, @activityType, NOW())";
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

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
    }
}
