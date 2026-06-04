using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParticipantsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ParticipantsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/participants/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetParticipant(int id)
    {
        var participant = await _context.Participants
            .Include(p => p.College)
            .Where(p => p.Id == id)
            .Select(p => new
            {
                p.Id,
                p.FullName,
                p.Gender,
                p.BirthDate,
                p.SportNumber,
                p.IsRepublicParticipant,
                CollegeId = p.College != null ? p.College.Id : 0,
                CollegeName = p.College != null ? p.College.Name : "Колледж не найден"
            })
            .FirstOrDefaultAsync();

        if (participant == null)
        {
            return NotFound(new { message = "Участник не найден" });
        }

        return Ok(participant);
    }

    // POST: api/participants/bulk-update-republic
    [HttpPost("bulk-update-republic")]
    public async Task<IActionResult> BulkUpdateRepublic([FromBody] List<UpdateRepublicDto> updates)
    {
        if (updates == null || updates.Count == 0)
            return BadRequest(new { message = "Нет данных для обновления" });

        var participantIds = updates.Select(u => u.ParticipantId).ToList();
        var participants = await _context.Participants
            .Where(p => participantIds.Contains(p.Id))
            .ToListAsync();

        foreach (var p in participants)
        {
            var update = updates.First(u => u.ParticipantId == p.Id);
            p.IsRepublicParticipant = update.IsRepublicParticipant;
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Отбор на РБ сохранён" });
    }



    public class UpdateRepublicDto
    {
        public int ParticipantId { get; set; }
        public bool IsRepublicParticipant { get; set; }
    }


}