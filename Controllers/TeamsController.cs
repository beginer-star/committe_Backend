using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinayakaApp.API.Data;
using VinayakaApp.API.DTOs;
using VinayakaApp.API.Models;
using VinayakaApp.API.Services;

namespace VinayakaApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeamsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtTokenService _jwt;
    public TeamsController(AppDbContext db, JwtTokenService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    // GET api/teams   (public - informational list of registered teams)
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<TeamDto>>> GetTeams()
    {
        var teams = await _db.Teams
            .Select(t => new TeamDto(t.Id, t.Name, t.UpiId, t.OptionalUpiId))
            .ToListAsync();
        return Ok(teams);
    }

    // POST api/teams/register  (public - a new committee registers itself + its Admin account)
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Register(RegisterTeamRequest request)
    {
        if (await _db.Teams.AnyAsync(t => t.Name == request.TeamName))
            return BadRequest(new { message = "A team with this name already exists." });

        if (await _db.Users.AnyAsync(u => u.Username == request.Username))
            return BadRequest(new { message = "This username is already taken." });

        if (string.IsNullOrWhiteSpace(request.UpiId))
            return BadRequest(new { message = "UPI ID is required." });

        var team = new Team
        {
            Name = request.TeamName,
            UpiId = request.UpiId,
            OptionalUpiId = string.IsNullOrWhiteSpace(request.OptionalUpiId) ? null : request.OptionalUpiId
        };
        _db.Teams.Add(team);
        await _db.SaveChangesAsync();

        var admin = new User
        {
            TeamId = team.Id,
            Name = request.Username,
            Username = request.Username,
            Mobile = request.MobileNumber,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRole.Admin
        };
        _db.Users.Add(admin);
        await _db.SaveChangesAsync();

        var token = _jwt.GenerateToken(admin);
        return Ok(new LoginResponse(token, admin.Name, admin.Username, admin.Role.ToString(), team.Id, team.Name, admin.Id));
    }
}
