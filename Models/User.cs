namespace VinayakaApp.API.Models;

public enum UserRole
{
    Admin = 0,
    Member = 1
}

public class User
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public Team? Team { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;  // globally unique — used to log in
    public string Mobile { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }

    // Member-specific fields (unused for Admin)
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public bool IsPaid => AmountPaid >= AmountDue && AmountDue > 0;

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
