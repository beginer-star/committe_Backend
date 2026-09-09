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
public class ExpendituresController : ControllerBase
{
    private readonly AppDbContext _db;
    public ExpendituresController(AppDbContext db) => _db = db;

    private int TeamId => int.Parse(User.FindFirstValue("teamId")!);

    // GET api/expenditures
    [HttpGet]
    public async Task<ActionResult<List<ExpenditureDto>>> GetAll()
    {
        var items = await _db.Expenditures
            .Where(e => e.TeamId == TeamId)
            .OrderByDescending(e => e.Date)
            .Select(e => new ExpenditureDto(e.Id, e.Category, e.Description, e.Amount, e.Date))
            .ToListAsync();
        return Ok(items);
    }

    // POST api/expenditures  (Admin only)
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<ExpenditureDto>> Create(CreateExpenditureRequest request)
    {
        var expenditure = new Expenditure
        {
            TeamId = TeamId,
            Category = request.Category,
            Description = request.Description,
            Amount = request.Amount,
            Date = request.Date
        };
        _db.Expenditures.Add(expenditure);
        await _db.SaveChangesAsync();

        return Ok(new ExpenditureDto(expenditure.Id, expenditure.Category, expenditure.Description, expenditure.Amount, expenditure.Date));
    }

    // DELETE api/expenditures/{id}  (Admin only)
    [HttpDelete("{id}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Expenditures.FirstOrDefaultAsync(e => e.Id == id && e.TeamId == TeamId);
        if (item is null) return NotFound();

        _db.Expenditures.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
