namespace VinayakaApp.API.Models;

public class CalendarEvent
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public Team? Team { get; set; }
    public DateTime Date { get; set; }
    public string Type { get; set; } = "Available";
    public string EventName { get; set; } = string.Empty;
    public int? MemberId { get; set; }
    public User? Member { get; set; }
    public int AvailableDays { get; set; }
}
