namespace cinema_project;

public class Showtime
{
    public int ShowtimeID { get; set; }
    public int MovieID { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int ScreenID { get; set; }

    public Movie Movie { get; set; }
    public Screen Screen { get; set; }
    public ICollection<Booking> Bookings { get; set; }
}
