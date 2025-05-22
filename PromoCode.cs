namespace cinema_project;

public class PromoCode
{
    public int PromoCodeID { get; set; }
    public string Code { get; set; }
    public decimal DiscountAmount { get; set; }
    public DateTime ValidUntil { get; set; }
    public int UsageLimit { get; set; }

    public ICollection<Booking> Bookings { get; set; }
}
