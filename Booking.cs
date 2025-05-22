namespace cinema_project;

public class Booking
{
    public int BookingID { get; set; }
    public int UserID { get; set; }
    public int ShowtimeID { get; set; }
    public DateTime BookingTime { get; set; }
    public int? PromoCodeID { get; set; }
    public decimal TotalPrice { get; set; }

    public User User { get; set; }
    public Showtime Showtime { get; set; }
    public PromoCode PromoCode { get; set; }
    public ICollection<BookedSeat> BookedSeats { get; set; }
}
