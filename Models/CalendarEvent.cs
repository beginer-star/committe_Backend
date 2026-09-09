namespace VinayakaApp.API.Models;

public class CalendarEvent
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public Team? Team { get; set; }

    public DateTime Date { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;   // member who selected the date
    public int AvailableDays { get; set; }                    // days member marked as available
}
