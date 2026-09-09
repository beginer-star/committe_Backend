namespace VinayakaApp.API.Models;

public class Team
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;           // e.g. "Team 1"
    public string UpiId { get; set; } = string.Empty;           // primary UPI ID, e.g. "team1@okhdfcbank"
    public string? OptionalUpiId { get; set; }                  // secondary/alternate UPI ID, optional

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Expenditure> Expenditures { get; set; } = new List<Expenditure>();
    public ICollection<CalendarEvent> CalendarEvents { get; set; } = new List<CalendarEvent>();
}
