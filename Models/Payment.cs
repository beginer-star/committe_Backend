namespace VinayakaApp.API.Models;

public enum PaymentStatus
{
    Pending = 0,
    Success = 1,
    Failed = 2
}

public class Payment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }

    public decimal Amount { get; set; }
    public string UpiApp { get; set; } = "PhonePe";   // PhonePe / GPay / Paytm etc.
    public string UpiId { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
}
