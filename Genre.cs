namespace cinema_project;

public class Genre
{
    public int GenreID { get; set; }
    public string Name { get; set; }

    public ICollection<MovieGenre> MovieGenres { get; set; }
}