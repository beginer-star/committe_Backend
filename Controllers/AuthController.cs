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
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtTokenService _jwt;

    public AuthController(AppDbContext db, JwtTokenService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var user = await _db.Users
            .Include(u => u.Team)
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.Role == UserRole.Admin);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid admin username or password." });

        return Ok(CreateLoginResponse(user));
    }

    [HttpGet("teams")]
    [AllowAnonymous]
    public async Task<ActionResult<List<LoginTeamDto>>> GetLoginTeams()
    {
        var teams = await _db.Teams
            .OrderBy(t => t.Name)
            .Select(t => new LoginTeamDto(t.Id, t.Name))
            .ToListAsync();

        return Ok(teams);
    }

    [HttpGet("teams/{teamId:int}/members")]
    [AllowAnonymous]
    public async Task<ActionResult<List<LoginMemberDto>>> GetTeamMembers(int teamId)
    {
        if (!await _db.Teams.AnyAsync(t => t.Id == teamId))
            return NotFound(new { message = "Team not found." });

        var members = await _db.Users
            .Where(u => u.TeamId == teamId && u.Role == UserRole.Member)
            .OrderBy(u => u.Name)
            .Select(u => new LoginMemberDto(u.Id, u.Name, u.Mobile))
            .ToListAsync();

        return Ok(members);
    }

    [HttpPost("member-login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> MemberLogin(MemberLoginRequest request)
    {
        var user = await _db.Users
            .Include(u => u.Team)
            .FirstOrDefaultAsync(u =>
                u.Id == request.MemberId &&
                u.TeamId == request.TeamId &&
                u.Role == UserRole.Member);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid team member or password." });

        return Ok(CreateLoginResponse(user));
    }

    private LoginResponse CreateLoginResponse(User user)
    {
        var token = _jwt.GenerateToken(user);
        return new LoginResponse(
            token,
            user.Name,
            user.Username,
            user.Role.ToString(),
            user.TeamId,
            user.Team?.Name ?? "",
            user.Id);
    }
}
