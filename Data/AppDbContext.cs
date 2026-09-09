using Microsoft.EntityFrameworkCore;
using VinayakaApp.API.Models;

namespace VinayakaApp.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Team> Teams => Set<Team>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Expenditure> Expenditures => Set<Expenditure>();
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Team>(e =>
        {
            e.HasIndex(t => t.Name).IsUnique();
        });

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.HasIndex(u => new { u.TeamId, u.Mobile }).IsUnique();
            e.Property(u => u.AmountDue).HasColumnType("decimal(10,2)");
            e.Property(u => u.AmountPaid).HasColumnType("decimal(10,2)");
            e.HasOne(u => u.Team)
                .WithMany(t => t.Users)
                .HasForeignKey(u => u.TeamId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Expenditure>(e =>
        {
            e.Property(x => x.Amount).HasColumnType("decimal(10,2)");
            e.HasOne(x => x.Team)
                .WithMany(t => t.Expenditures)
                .HasForeignKey(x => x.TeamId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CalendarEvent>(e =>
        {
            e.Property(c => c.Type).HasMaxLength(30).IsRequired();
            e.Property(c => c.EventName).HasMaxLength(150).IsRequired();
            e.HasOne(c => c.Team)
                .WithMany(t => t.CalendarEvents)
                .HasForeignKey(c => c.TeamId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.Member)
                .WithMany()
                .HasForeignKey(c => c.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.Property(p => p.Amount).HasColumnType("decimal(10,2)");
            e.HasOne(p => p.User)
                .WithMany(u => u.Payments)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
