namespace VinayakaApp.API.Models;

public class Expenditure
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public Team? Team { get; set; }

    public string Category { get; set; } = string.Empty;   // dropdown value or custom-typed value
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
}
