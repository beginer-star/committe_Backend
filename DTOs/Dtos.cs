using VinayakaApp.API.Models;

namespace VinayakaApp.API.DTOs;

// ---------- Auth ----------
public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token, string Name, string Username, string Role, int TeamId, string TeamName, int UserId);

// ---------- Team ----------
public record TeamDto(int Id, string Name, string UpiId, string? OptionalUpiId);
public record RegisterTeamRequest(
    string TeamName,
    string Username,
    string UpiId,
    string? OptionalUpiId,
    string MobileNumber,
    string Password);

// ---------- Member ----------
public record MemberDto(int Id, string Name, string Username, string Mobile, decimal AmountDue, decimal AmountPaid, bool IsPaid);
public record CreateMemberRequest(string Name, string Mobile, string? Username, string? Password, decimal AmountDue);
public record UpdateMemberRequest(string Name, string Mobile, decimal AmountDue);
public record PayRequest(decimal Amount, string UpiApp);

// ---------- Bulk import ----------
public record BulkImportRowResult(int RowNumber, string Name, bool Success, string? Error);
public record BulkImportResult(int TotalRows, int Imported, int Skipped, List<BulkImportRowResult> Details);

// ---------- Expenditure ----------
public record ExpenditureDto(int Id, string Category, string Description, decimal Amount, DateTime Date);
public record CreateExpenditureRequest(string Category, string Description, decimal Amount, DateTime Date);

// ---------- Calendar ----------
public record CalendarEventDto(int Id, DateTime Date, string EventName, string MemberName, int AvailableDays);
public record CreateCalendarEventRequest(DateTime Date, string EventName, string MemberName, int AvailableDays);
