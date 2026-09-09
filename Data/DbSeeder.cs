using VinayakaApp.API.Models;

namespace VinayakaApp.API.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        if (db.Teams.Any()) return; // already seeded

        var team1 = new Team { Name = "Team 1", UpiId = "team1@okhdfcbank", OptionalUpiId = null };
        var team2 = new Team { Name = "Team 2", UpiId = "team2@okhdfcbank", OptionalUpiId = "team2alt@okaxis" };
        db.Teams.AddRange(team1, team2);
        db.SaveChanges();

        string Hash(string pwd) => BCrypt.Net.BCrypt.HashPassword(pwd);

        db.Users.AddRange(
            new User { TeamId = team1.Id, Name = "Team1 Admin", Username = "team1admin", Mobile = "9000000001", PasswordHash = Hash("Admin@123"), Role = UserRole.Admin },
            new User { TeamId = team1.Id, Name = "Ravi Kumar", Username = "9000000002", Mobile = "9000000002", PasswordHash = Hash("Member@123"), Role = UserRole.Member, AmountDue = 500, AmountPaid = 0 },
            new User { TeamId = team1.Id, Name = "Sita Devi", Username = "9000000003", Mobile = "9000000003", PasswordHash = Hash("Member@123"), Role = UserRole.Member, AmountDue = 500, AmountPaid = 500 },
            new User { TeamId = team2.Id, Name = "Team2 Admin", Username = "team2admin", Mobile = "9000000004", PasswordHash = Hash("Admin@123"), Role = UserRole.Admin },
            new User { TeamId = team2.Id, Name = "Anil Reddy", Username = "9000000005", Mobile = "9000000005", PasswordHash = Hash("Member@123"), Role = UserRole.Member, AmountDue = 750, AmountPaid = 250 }
        );

        db.Expenditures.AddRange(
            new Expenditure { TeamId = team1.Id, Category = "Decoration", Description = "Flowers and mandap", Amount = 3500, Date = DateTime.UtcNow.AddDays(-5) },
            new Expenditure { TeamId = team1.Id, Category = "Prasadam", Description = "Sweets for 100 people", Amount = 5200, Date = DateTime.UtcNow.AddDays(-3) }
        );

        db.CalendarEvents.AddRange(
            new CalendarEvent { TeamId = team1.Id, Date = DateTime.UtcNow.Date.AddDays(2), Type = "Event", EventName = "Ganesh Sthapana", MemberId = null, AvailableDays = 1 },
            new CalendarEvent { TeamId = team1.Id, Date = DateTime.UtcNow.Date.AddDays(9), Type = "Event", EventName = "Visarjan", MemberId = null, AvailableDays = 1 }
        );

        db.SaveChanges();
    }
}
