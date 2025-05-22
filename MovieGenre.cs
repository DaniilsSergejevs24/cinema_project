namespace cinema_project;

public class MovieGenre
{
    public int MovieGenreID { get; set; }
    public int MovieID { get; set; }
    public int GenreID { get; set; }

    public Movie Movie { get; set; }
    public Genre Genre { get; set; }
}
