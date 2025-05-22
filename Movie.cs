namespace cinema_project;

public class Movie
{
    public int MovieID { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int? MovieGenreID { get; set; }
    public byte[] Poster { get; set; }
    public decimal? Rating { get; set; }
    public int? Duration { get; set; }

    public MovieGenre MovieGenre { get; set; }
    public ICollection<Showtime> Showtimes { get; set; }
}
