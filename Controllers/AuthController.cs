using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinayakaApp.API.Data;
using VinayakaApp.API.DTOs;
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

    // POST api/auth/login
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var user = await _db.Users
            .Include(u => u.Team)
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid username or password." });

        var token = _jwt.GenerateToken(user);
        return Ok(new LoginResponse(token, user.Name, user.Username, user.Role.ToString(), user.TeamId, user.Team?.Name ?? "", user.Id));
    }
}
