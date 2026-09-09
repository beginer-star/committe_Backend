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

    private int TeamId => int.Parse(User.FindFirstValue("teamId")!);

    // GET api/calendar
    [HttpGet]
    public async Task<ActionResult<List<CalendarEventDto>>> GetAll()
    {
        var items = await _db.CalendarEvents
            .Where(c => c.TeamId == TeamId)
            .OrderBy(c => c.Date)
            .Select(c => new CalendarEventDto(c.Id, c.Date, c.EventName, c.MemberName, c.AvailableDays))
            .ToListAsync();
        return Ok(items);
    }

    // POST api/calendar  (Admin or Member can add their own availability entry)
    [HttpPost]
    public async Task<ActionResult<CalendarEventDto>> Create(CreateCalendarEventRequest request)
    {
        var entry = new CalendarEvent
        {
            TeamId = TeamId,
            Date = request.Date,
            EventName = request.EventName,
            MemberName = request.MemberName,
            AvailableDays = request.AvailableDays
        };
        _db.CalendarEvents.Add(entry);
        await _db.SaveChangesAsync();

        return Ok(new CalendarEventDto(entry.Id, entry.Date, entry.EventName, entry.MemberName, entry.AvailableDays));
    }

    // DELETE api/calendar/{id}  (Admin only)
    [HttpDelete("{id}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.CalendarEvents.FirstOrDefaultAsync(c => c.Id == id && c.TeamId == TeamId);
        if (item is null) return NotFound();

        _db.CalendarEvents.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
