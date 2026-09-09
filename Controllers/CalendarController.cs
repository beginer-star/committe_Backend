using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinayakaApp.API.Data;
using VinayakaApp.API.DTOs;
using VinayakaApp.API.Models;

namespace VinayakaApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CalendarController : ControllerBase
{
    private readonly AppDbContext _db;

    public CalendarController(AppDbContext db) => _db = db;

    private bool TryGetTeamId(out int teamId) =>
        int.TryParse(User.FindFirstValue("teamId"), out teamId);

    private bool TryGetUserId(out int userId) =>
        int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue(ClaimTypes.Name) ??
            User.FindFirstValue("sub"),
            out userId);

    private bool IsAdmin => User.IsInRole(nameof(UserRole.Admin));

    [HttpGet]
    public async Task<ActionResult<List<CalendarEventDto>>> GetAll()
    {
        if (!TryGetTeamId(out var teamId))
            return Unauthorized(new { message = "Invalid login session. Please log in again." });

        var query = _db.CalendarEvents
            .Include(c => c.Member)
            .Where(c => c.TeamId == teamId);

        if (!IsAdmin)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Invalid login session. Please log in again." });

            query = query.Where(c => c.MemberId == userId || c.Type == "Event");
        }

        var items = await query
            .OrderBy(c => c.Date)
            .ThenBy(c => c.Member != null ? c.Member.Name : "")
            .Select(c => new CalendarEventDto(
                c.Id,
                c.Date,
                c.Type,
                c.EventName,
                c.MemberId,
                c.Member != null ? c.Member.Name : "",
                c.AvailableDays))
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<CalendarEventDto>> Create(
        [FromBody] CreateCalendarEventRequest request)
    {
        if (!TryGetTeamId(out var teamId))
            return Unauthorized(new { message = "Invalid login session. Please log in again." });

        if (request.Date == default)
            return BadRequest(new { message = "Please select a valid date." });

        var selectedDate = DateTime.SpecifyKind(request.Date.Date, DateTimeKind.Utc);
        var requestedType = (request.Type ?? string.Empty).Trim();

        if (IsAdmin)
        {
            if (!string.Equals(requestedType, "Event", StringComparison.OrdinalIgnoreCase))
                return Forbid();

            var eventName = string.IsNullOrWhiteSpace(request.EventName)
                ? "Team Event"
                : request.EventName.Trim();

            var existingEvent = await _db.CalendarEvents
                .FirstOrDefaultAsync(c =>
                    c.TeamId == teamId &&
                    c.MemberId == null &&
                    c.Type == "Event" &&
                    c.Date.Date == selectedDate.Date);

            if (existingEvent != null)
            {
                existingEvent.EventName = eventName;
                existingEvent.AvailableDays = 1;
                await _db.SaveChangesAsync();
                return Ok(ToDto(existingEvent));
            }

            var adminEvent = new CalendarEvent
            {
                TeamId = teamId,
                Date = selectedDate,
                Type = "Event",
                EventName = eventName,
                MemberId = null,
                AvailableDays = 1
            };

            _db.CalendarEvents.Add(adminEvent);
            await _db.SaveChangesAsync();
            return Ok(ToDto(adminEvent));
        }

        if (!string.Equals(requestedType, "Available", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(requestedType, "Unavailable", StringComparison.OrdinalIgnoreCase))
            return Forbid();

        if (!TryGetUserId(out var memberId))
            return Unauthorized(new { message = "Invalid login session. Please log in again." });

        var member = await _db.Users.FirstOrDefaultAsync(u =>
            u.Id == memberId &&
            u.TeamId == teamId &&
            u.Role == UserRole.Member);

        if (member == null)
            return BadRequest(new { message = "Your member account does not belong to this team." });

        var type = string.Equals(requestedType, "Unavailable", StringComparison.OrdinalIgnoreCase)
            ? "Unavailable"
            : "Available";

        var existing = await _db.CalendarEvents
            .FirstOrDefaultAsync(c =>
                c.TeamId == teamId &&
                c.MemberId == memberId &&
                c.Date.Date == selectedDate.Date);

        if (existing != null)
        {
            existing.Type = type;
            existing.EventName = string.IsNullOrWhiteSpace(request.EventName)
                ? type
                : request.EventName.Trim();
            existing.AvailableDays = Math.Max(1, request.AvailableDays);
            await _db.SaveChangesAsync();
            return Ok(ToDto(existing, member.Name));
        }

        var entry = new CalendarEvent
        {
            TeamId = teamId,
            Date = selectedDate,
            Type = type,
            EventName = string.IsNullOrWhiteSpace(request.EventName)
                ? type
                : request.EventName.Trim(),
            MemberId = member.Id,
            AvailableDays = Math.Max(1, request.AvailableDays)
        };

        _db.CalendarEvents.Add(entry);
        await _db.SaveChangesAsync();
        return Ok(ToDto(entry, member.Name));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Delete(int id)
    {
        if (!TryGetTeamId(out var teamId))
            return Unauthorized();

        var item = await _db.CalendarEvents.FirstOrDefaultAsync(c =>
            c.Id == id && c.TeamId == teamId && c.Type == "Event");

        if (item == null)
            return NotFound();

        _db.CalendarEvents.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static CalendarEventDto ToDto(CalendarEvent c, string? memberName = null) =>
        new(
            c.Id,
            c.Date,
            c.Type,
            c.EventName,
            c.MemberId,
            memberName ?? c.Member?.Name ?? "",
            c.AvailableDays);
}
