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
public class MembersController : ControllerBase
{
    private readonly AppDbContext _db;
    public MembersController(AppDbContext db) => _db = db;

    private int TeamId => int.Parse(User.FindFirstValue("teamId")!);
    private int UserId
{
    get
    {
        var value =
            User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);

        if (!int.TryParse(value, out var userId))
            throw new UnauthorizedAccessException("User ID claim is missing or invalid.");

        return userId;
    }
}


    private bool IsAdmin => User.IsInRole(nameof(UserRole.Admin));

    // GET api/members  -> Admin: all members of their team. Member: only self.
    [HttpGet]
    public async Task<ActionResult<List<MemberDto>>> GetMembers()
    {
        var query = _db.Users.Where(u => u.TeamId == TeamId && u.Role == UserRole.Member);
        if (!IsAdmin) query = query.Where(u => u.Id == UserId);

        var members = await query
            .Select(u => new MemberDto(u.Id, u.Name, u.Username, u.Mobile, u.AmountDue, u.AmountPaid, u.IsPaid))
            .ToListAsync();

        return Ok(members);
    }

    // POST api/members  (Admin only - add a new team member)
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<MemberDto>> CreateMember(CreateMemberRequest request)
    {
        var mobileExists = await _db.Users.AnyAsync(u => u.TeamId == TeamId && u.Mobile == request.Mobile);
        if (mobileExists) return BadRequest(new { message = "A member with this mobile number already exists." });

        var username = string.IsNullOrWhiteSpace(request.Username) ? request.Mobile : request.Username;
        if (await _db.Users.AnyAsync(u => u.Username == username))
            return BadRequest(new { message = $"Username '{username}' is already taken." });

        var member = new User
        {
            TeamId = TeamId,
            Name = request.Name,
            Username = username,
            Mobile = request.Mobile,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(string.IsNullOrWhiteSpace(request.Password) ? "Member@123" : request.Password),
            Role = UserRole.Member,
            AmountDue = request.AmountDue,
            AmountPaid = 0
        };

        _db.Users.Add(member);
        await _db.SaveChangesAsync();

        return Ok(new MemberDto(member.Id, member.Name, member.Username, member.Mobile, member.AmountDue, member.AmountPaid, member.IsPaid));
    }

    // POST api/members/bulk-import  (Admin only - upload an .xlsx file to add many members at once)
    // Expected columns (header row required, any order): Name | Mobile | Username | Password | AmountDue
    // Username and Password are optional — Username defaults to Mobile, Password defaults to "Member@123".
    [HttpPost("bulk-import")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<BulkImportResult>> BulkImport(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Please attach an .xlsx file." });

        var details = new List<BulkImportRowResult>();
        int imported = 0;

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
        var sheet = workbook.Worksheets.First();
        var headerRow = sheet.Row(1);

        // Map column letters to header names (case-insensitive)
        var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.CellsUsed())
            columnMap[cell.GetString().Trim()] = cell.Address.ColumnNumber;

        if (!columnMap.ContainsKey("Name") || !columnMap.ContainsKey("Mobile"))
            return BadRequest(new { message = "The file must have at least 'Name' and 'Mobile' columns." });

        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int rowNum = 2; rowNum <= lastRow; rowNum++)
        {
            var row = sheet.Row(rowNum);
            string Get(string col) => columnMap.TryGetValue(col, out var idx) ? row.Cell(idx).GetString().Trim() : "";

            var name = Get("Name");
            var mobile = Get("Mobile");
            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(mobile))
                continue; // blank row, skip silently

            try
            {
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(mobile))
                    throw new Exception("Name and Mobile are required.");

                if (await _db.Users.AnyAsync(u => u.TeamId == TeamId && u.Mobile == mobile))
                    throw new Exception("A member with this mobile number already exists.");

                var username = Get("Username");
                if (string.IsNullOrWhiteSpace(username)) username = mobile;
                if (await _db.Users.AnyAsync(u => u.Username == username))
                    throw new Exception($"Username '{username}' is already taken.");

                var password = Get("Password");
                if (string.IsNullOrWhiteSpace(password)) password = "Member@123";

                var amountDueStr = Get("AmountDue");
                decimal.TryParse(amountDueStr, out var amountDue);

                _db.Users.Add(new User
                {
                    TeamId = TeamId,
                    Name = name,
                    Username = username,
                    Mobile = mobile,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    Role = UserRole.Member,
                    AmountDue = amountDue,
                    AmountPaid = 0
                });
                await _db.SaveChangesAsync(); // save per-row so one bad row doesn't roll back the rest

                imported++;
                details.Add(new BulkImportRowResult(rowNum, name, true, null));
            }
            catch (Exception ex)
            {
                details.Add(new BulkImportRowResult(rowNum, name, false, ex.Message));
            }
        }

        return Ok(new BulkImportResult(details.Count, imported, details.Count - imported, details));
    }

    // PUT api/members/{id}  (Admin only - edit member)
    [HttpPut("{id}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> UpdateMember(int id, UpdateMemberRequest request)
    {
        var member = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.TeamId == TeamId && u.Role == UserRole.Member);
        if (member is null) return NotFound();

        member.Name = request.Name;
        member.Mobile = request.Mobile;
        member.AmountDue = request.AmountDue;

        await _db.SaveChangesAsync();
        return Ok(new MemberDto(member.Id, member.Name, member.Username, member.Mobile, member.AmountDue, member.AmountPaid, member.IsPaid));
    }

    // DELETE api/members/{id}  (Admin only)
    [HttpDelete("{id}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> DeleteMember(int id)
    {
        var member = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.TeamId == TeamId && u.Role == UserRole.Member);
        if (member is null) return NotFound();

        _db.Users.Remove(member);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // POST api/members/{id}/pay  (Member pays their own due, or Admin marks paid on their behalf)
    [HttpPost("{id}/pay")]
    public async Task<ActionResult<MemberDto>> Pay(int id, PayRequest request)
    {
        if (!IsAdmin && id != UserId) return Forbid();
        if (request.Amount <= 0) return BadRequest(new { message = "Payment amount must be greater than zero." });

        var member = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.TeamId == TeamId && u.Role == UserRole.Member);
        if (member is null) return NotFound();

        var team = await _db.Teams.FindAsync(TeamId);

        var payment = new Payment
        {
            UserId = member.Id,
            Amount = request.Amount,
            UpiApp = request.UpiApp,
            UpiId = team?.UpiId ?? string.Empty,
            Status = PaymentStatus.Success,
            PaidAt = DateTime.UtcNow
        };
        _db.Payments.Add(payment);

        member.AmountPaid += request.Amount;
        await _db.SaveChangesAsync();

        return Ok(new MemberDto(member.Id, member.Name, member.Username, member.Mobile, member.AmountDue, member.AmountPaid, member.IsPaid));
    }


    [HttpGet("{id}/upi-options")]
    public async Task<ActionResult<object>> GetUpiOptions(int id, [FromQuery] decimal amount)
    {
        if (amount <= 0)
            return BadRequest(new { message = "Payment amount must be greater than zero." });

        var member = await _db.Users.FirstOrDefaultAsync(u =>
            u.Id == id && u.TeamId == TeamId && u.Role == UserRole.Member);

        if (member is null) return NotFound();

        var team = await _db.Teams.FindAsync(TeamId);
        if (team is null || string.IsNullOrWhiteSpace(team.UpiId))
            return BadRequest(new { message = "No UPI ID is configured for this team." });

        var payeeName = Uri.EscapeDataString(team.Name);
        var upiId = Uri.EscapeDataString(team.UpiId);
        var amountText = amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        var query = $"pa={upiId}&pn={payeeName}&am={amountText}&cu=INR";
        var generic = $"upi://pay?{query}";

        return Ok(new
        {
            amount, upiId = team.UpiId,
            upiLink = generic,
            options = new[]
            {
                new { id = "gpay", name = "Google Pay", scheme = $"gpay://upi/pay?{query}", fallback = $"intent://upi/pay?{query}#Intent;scheme=upi;package=com.google.android.apps.nbu.paisa.user;end", icon = "GPay" },
                new { id = "phonepe", name = "PhonePe", scheme = $"phonepe://pay?{query}", fallback = $"intent://pay?{query}#Intent;scheme=phonepe;package=com.phonepe.app;end", icon = "PP" },
                new { id = "paytm", name = "Paytm", scheme = $"paytmmp://pay?{query}", fallback = $"intent://pay?{query}#Intent;scheme=paytmmp;package=net.one97.paytm;end", icon = "PT" },
                new { id = "bhim", name = "BHIM", scheme = $"bhim://upi/pay?{query}", fallback = $"intent://upi/pay?{query}#Intent;scheme=bhim;package=in.org.npci.upiapp;end", icon = "BHIM" },
                new { id = "upi", name = "Other UPI app", scheme = generic, fallback = generic, icon = "UPI" }
            }
        });
    }

    // GET api/members/{id}/upi-link  -> builds a UPI deep link for the pay button to redirect to
    [HttpGet("{id}/upi-link")]
    public async Task<ActionResult<object>> GetUpiLink(int id, [FromQuery] decimal amount)
    {
        var team = await _db.Teams.FindAsync(TeamId);
        if (team is null) return NotFound();

        // Standard UPI deep link - opens whichever UPI app (PhonePe/GPay/Paytm) is installed
        var link = $"upi://pay?pa={team.UpiId}&pn={Uri.EscapeDataString(team.Name)}&am={amount}&cu=INR";
        return Ok(new { upiLink = link });
    }
}
