namespace cinema_project;

public class User
{
    public int UserID { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public bool IsAdmin { get; set; }
    public byte[] ProfilePhoto { get; set; }

    public ICollection<Booking> Bookings { get; set; }
}
