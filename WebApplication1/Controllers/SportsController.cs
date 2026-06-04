using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SportsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SportsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Sport>>> GetSports()
    {
        return await _context.Sports.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Sport>> GetSport(int id)
    {
        var sport = await _context.Sports.FindAsync(id);
        if (sport == null) return NotFound();
        return sport;
    }

    [HttpGet("{sportId}/disciplines")]
    public async Task<ActionResult<IEnumerable<object>>> GetDisciplinesBySport(int sportId)
    {
        var disciplines = await _context.Disciplines
            .Where(d => d.SportId == sportId)
            .OrderBy(d => d.Order)
            .Select(d => new
            {
                d.Id,
                d.Name,
                d.Order,
                d.ScoringType,
                d.Unit
            })
            .ToListAsync();

        return Ok(disciplines);
    }

    [HttpPost]
    public async Task<ActionResult<Sport>> CreateSport(Sport sport)
    {
        sport.Id = 0;

        _context.Sports.Add(sport);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSport), new { id = sport.Id }, sport);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSport(int id, Sport updatedSport)
    {
        var sport = await _context.Sports.FindAsync(id);

        if (sport == null)
            return NotFound();

        sport.Name = updatedSport.Name;
        sport.SportType = updatedSport.SportType;
        sport.ParticipantsMale = updatedSport.ParticipantsMale;
        sport.ParticipantsFemale = updatedSport.ParticipantsFemale;
        sport.ParticipantsStaff = updatedSport.ParticipantsStaff;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSport(int id)
    {
        var sport = await _context.Sports.FindAsync(id);

        if (sport == null)
            return NotFound();

        _context.Sports.Remove(sport);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}