using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PenaltiesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PenaltiesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> AddOrUpdatePenalty([FromBody] PenaltyRequest request)
    {
        if (request == null) return BadRequest();

        var existing = await _context.Penalties
            .FirstOrDefaultAsync(p => p.CollegeId == request.CollegeId && p.SportId == request.SportId);

        if (existing != null)
        {
            existing.Points = request.Points; // замена, не суммирование
            existing.Reason = request.Reason;
            existing.PenaltyType = "manual";
        }
        else
        {
            var penalty = new Penalty
            {
                CollegeId = request.CollegeId,
                SportId = request.SportId,
                Points = request.Points,
                Reason = request.Reason,
                PenaltyType = "manual"
            };
            _context.Penalties.Add(penalty);
        }
        await _context.SaveChangesAsync();
        return Ok(new { message = "Сохранено" });
    }

    public class PenaltyRequest
    {
        public int CollegeId { get; set; }
        public int SportId { get; set; }
        public int Points { get; set; }
        public string? Reason { get; set; }
    }
}