namespace cinema_project;

public class Seat
{
    public int SeatID { get; set; }
    public string SeatNumber { get; set; }
    public string Row { get; set; }
    public int ScreenID { get; set; }

    public Screen Screen { get; set; }
    public ICollection<BookedSeat> BookedSeats { get; set; }
}
