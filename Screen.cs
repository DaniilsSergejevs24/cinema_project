namespace cinema_project;

public class Screen
{
    public int ScreenID { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public int TotalSeats { get; set; }

    public ICollection<Seat> Seats { get; set; }
    public ICollection<Showtime> Showtimes { get; set; }
}