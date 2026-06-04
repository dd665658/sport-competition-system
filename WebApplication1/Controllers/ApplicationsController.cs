using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication1.Models;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApplicationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ApplicationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/applications
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAllApplications()
    {
        var applications = await _context.Applications
            .Include(a => a.College)
            .Include(a => a.Sport)
            .Select(a => new
            {
                a.Id,
                a.CollegeId,
                a.SportId,
                CollegeName = a.College != null ? a.College.Name : null,
                SportName = a.Sport != null ? a.Sport.Name : null,
                a.Status,
                a.CreatedAt
            })
            .ToListAsync();

        return Ok(applications);
    }

    // GET: api/applications/pending
    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<object>>> GetPendingApplications()
    {
        var applications = await _context.Applications
            .Include(a => a.College)
            .Include(a => a.Sport)
            .Where(a => a.Status == "submitted")
            .OrderBy(a => a.CreatedAt)
            .Select(a => new
            {
                a.Id,
                CollegeName = a.College != null ? a.College.Name : null,
                SportName = a.Sport != null ? a.Sport.Name : null,
                a.CreatedAt,
                a.Status
            })
            .ToListAsync();

        return Ok(applications);
    }

    // GET: api/applications/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetApplication(int id)
    {
        var application = await _context.Applications
            .Include(a => a.College)
            .Include(a => a.Sport)
            .Include(a => a.Participants)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application == null)
        {
            return NotFound(new { message = "Заявка не найдена" });
        }

        return Ok(new
        {
            application.Id,
            CollegeName = application.College?.Name,
            SportName = application.Sport?.Name,
            application.CreatedAt,
            application.Status,
            application.AdminComment,
            Participants = application.Participants?.Select(p => new
            {
                p.FullName,
                p.BirthDate,
                p.Gender,
                p.SportNumber
            })
        });
    }

    // POST: api/applications/{id}/approve
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> ApproveApplication(int id)
    {
        try
        {
            var application = await _context.Applications
                .Include(a => a.Participants)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
                return NotFound(new { message = "Заявка не найдена" });

            if (application.Status != "submitted")
                return BadRequest(new { message = "Заявка уже обработана" });

            if (application.Participants == null || !application.Participants.Any())
                return BadRequest(new { message = "Заявка не содержит участников" });

            foreach (var appParticipant in application.Participants)
            {
                var participant = new Participant
                {
                    CollegeId = application.CollegeId,
                    SportId = application.SportId,
                    FullName = appParticipant.FullName,
                    BirthDate = appParticipant.BirthDate,
                    Gender = appParticipant.Gender,
                    SportNumber = appParticipant.SportNumber ?? 0,
                    IsRepublicParticipant = false
                };
                _context.Participants.Add(participant);
            }

            application.Status = "approved";
            application.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Заявка утверждена" });
        }
        catch (Exception ex)
        {
            // Логируем ошибку и возвращаем её текст
            return StatusCode(500, new { message = "Ошибка при утверждении заявки", error = ex.Message });
        }
    }

    [HttpPost("{id}/cancel-approval")]
    public async Task<IActionResult> CancelApproval(int id)
    {
        var application = await _context.Applications
            .Include(a => a.Participants)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application == null)
            return NotFound(new { message = "Заявка не найдена" });

        if (application.Status != "approved")
            return BadRequest(new { message = "Заявка не утверждена" });

        // Удаляем всех участников, связанных с этой заявкой
        var participantsToDelete = _context.Participants
            .Where(p => p.CollegeId == application.CollegeId && p.SportId == application.SportId);
        _context.Participants.RemoveRange(participantsToDelete);

        // Меняем статус заявки обратно на submitted (или rejected)
        application.Status = "submitted";
        application.UpdatedAt = DateTime.UtcNow;
        application.AdminComment = "Утверждение отменено администратором";

        await _context.SaveChangesAsync();

        return Ok(new { message = "Утверждение заявки отменено, участники удалены" });
    }

    // POST: api/applications/{id}/rework
    [HttpPost("{id}/rework")]
    public async Task<IActionResult> ReworkApplication(int id, [FromBody] ApplicationCommentRequest request)
    {
        var application = await _context.Applications.FindAsync(id);
        if (application == null)
        {
            return NotFound(new { message = "Заявка не найдена" });
        }

        if (application.Status != "submitted")
        {
            return BadRequest(new { message = "Заявка уже обработана" });
        }

        application.Status = "rework";
        application.AdminComment = request.Comment;
        application.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Заявка отправлена на доработку" });
    }

    // POST: api/applications/{id}/reject
    [HttpPost("{id}/reject")]
    public async Task<IActionResult> RejectApplication(int id, [FromBody] ApplicationCommentRequest request)
    {
        var application = await _context.Applications.FindAsync(id);
        if (application == null)
            return NotFound(new { message = "Заявка не найдена" });

        if (application.Status != "submitted" && application.Status != "approved")
            return BadRequest(new { message = "Заявка уже обработана" });

        if (application.Status == "approved")
        {
            // 1. Удаляем ВСЕ командные результаты по этому виду спорта (для всех колледжей)
            var allTeamResults = _context.TeamResults
                .Where(tr => tr.SportId == application.SportId);
            _context.TeamResults.RemoveRange(allTeamResults);

            // 2. Находим всех участников этой заявки в таблице Participants
            var participants = _context.Participants
                .Where(p => p.CollegeId == application.CollegeId && p.SportId == application.SportId);

            // 3. Удаляем индивидуальные результаты
            var participantIds = participants.Select(p => p.Id).ToList();
            var individualResults = _context.IndividualResults
                .Where(ir => participantIds.Contains(ir.ParticipantId));
            _context.IndividualResults.RemoveRange(individualResults);

            // 4. Удаляем самих участников
            _context.Participants.RemoveRange(participants);
        }

        application.Status = "rejected";
        application.AdminComment = request.Comment;
        application.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Заявка отклонена, командные результаты удалены" });
    }


    [HttpPost("representative")]
    public async Task<IActionResult> CreateByRepresentative([FromBody] CreateApplicationDto dto)
    {
        if (dto.CollegeId <= 0)
            return BadRequest(new { message = "Колледж не найден" });

        if (dto.Participants == null || !dto.Participants.Any())
            return BadRequest(new { message = "Нет участников" });

        var application = new Application
        {
            CollegeId = dto.CollegeId,
            SportId = dto.SportId,
            Status = dto.Status == "draft" ? "draft" : "submitted",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            AdminComment = null
        };

        _context.Applications.Add(application);
        await _context.SaveChangesAsync();

        foreach (var p in dto.Participants)
        {
            _context.ApplicationParticipants.Add(new ApplicationParticipant
            {
                ApplicationId = application.Id,
                FullName = p.FullName,
                BirthDate = p.BirthDate,
                Gender = p.Gender,
                SportNumber = p.SportNumber
            });
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Заявка создана",
            applicationId = application.Id
        });
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyApplications([FromQuery] int collegeId)
    {
        var applications = await _context.Applications
            .Where(a => a.CollegeId == collegeId)
            .Include(a => a.Sport)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                id = a.Id,
                sportName = a.Sport.Name,
                status = a.Status,
                createdAt = a.CreatedAt,
                adminComment = a.AdminComment
            })
            .ToListAsync();

        return Ok(applications);
    }

    [HttpPut("{id}/representative-update")]
    public async Task<IActionResult> UpdateByRepresentative(int id, [FromBody] ApplicationUpdateRequest request)
    {
        var app = await _context.Applications
            .Include(a => a.Participants)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (app == null)
            return NotFound();

        if (app.Status != "draft" && app.Status != "rework")
        {
            return BadRequest(new { message = "Заявку нельзя редактировать" });
        }

        // удаляем старых участников
        _context.ApplicationParticipants.RemoveRange(app.Participants);

        // добавляем новых
        var newParticipants = request.Participants.Select(p => new ApplicationParticipant
        {
            ApplicationId = app.Id,
            FullName = p.FullName,
            BirthDate = p.BirthDate,
            Gender = p.Gender,
            SportNumber = p.SportNumber
        });

        await _context.ApplicationParticipants.AddRangeAsync(newParticipants);

        // 🔥 вот ключевое
        app.Status = request.Status; // draft или submitted
        app.UpdatedAt = DateTime.Now;

        if (request.Status == "submitted")
        {
            app.AdminComment = null; // очищаем только если отправили
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Заявка обновлена" });
    }


    [HttpGet("{id}/edit")]
    public async Task<IActionResult> GetForEdit(int id)
    {
        var app = await _context.Applications
            .Include(a => a.Participants)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (app == null)
            return NotFound();

        return Ok(new
        {
            id = app.Id,
            sportId = app.SportId,
            status = app.Status,
            participants = app.Participants.Select(p => new
            {
                fullName = p.FullName,
                birthDate = p.BirthDate,
                gender = p.Gender,
                sportNumber = p.SportNumber
            })
        });
    }

    public class ApplicationUpdateRequest
    {
        public string Status { get; set; } = "draft"; // draft / submitted
        public List<CreateApplicationParticipantDto> Participants { get; set; } = new();
    }

    
    public class CreateApplicationDto
    {
        public int CollegeId { get; set; }
        public int SportId { get; set; }
        public string Status { get; set; } = "submitted";
        public List<CreateApplicationParticipantDto> Participants { get; set; } = new();
    }

    public class CreateApplicationParticipantDto
    {
        public string FullName { get; set; } = "";
        public DateTime BirthDate { get; set; }
        public string Gender { get; set; } = "";
        public int SportNumber { get; set; }
    }
    public class ApplicationCommentRequest
    {
        public string Comment { get; set; } = string.Empty;
    }
}