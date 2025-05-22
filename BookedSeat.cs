namespace cinema_project;

public class BookedSeat
{
    public int BookedSeatID { get; set; }
    public int BookingID { get; set; }
    public int SeatID { get; set; }

    public Booking Booking { get; set; }
    public Seat Seat { get; set; }
}
