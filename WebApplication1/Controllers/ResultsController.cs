using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResultsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ResultsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/results/{sportId}/participants?includeResults=true
    [HttpGet("{sportId}/participants")]
    public async Task<ActionResult<IEnumerable<object>>> GetParticipantsBySport(int sportId, [FromQuery] bool includeResults = false)
    {
        var sport = await _context.Sports.FindAsync(sportId);
        if (sport == null) return NotFound();

        var participants = await _context.Participants
            .Where(p => p.SportId == sportId)
            .Include(p => p.College)
            .ToListAsync();

        var result = new List<object>();
        foreach (var p in participants)
        {
            var disciplineResults = includeResults
                ? await _context.IndividualResults
                    .Where(r => r.ParticipantId == p.Id)
                    .Select(r => new { r.DisciplineId, r.ResultValue, r.Place, r.Points })
                    .ToListAsync()
                : null;

            int totalScore = 0;
            if (disciplineResults != null && disciplineResults.Any())
            {
                if (sport.SportType == "multi")
                {
                    totalScore = disciplineResults.Sum(r => r.Points ?? 0);
                }
                else // individual
                {
                    totalScore = disciplineResults.Sum(r => r.Place ?? 0);
                }
            }

            var item = new
            {
                p.Id,
                p.FullName,
                p.Gender,
                CollegeName = p.College?.Name ?? "Колледж не указан",
                CollegeId = p.College?.Id ?? 0,
                p.SportNumber,
                p.BirthDate,
                p.IsRepublicParticipant,
                Place = (int?)null,
                TotalScore = totalScore,
                SportType = sport.SportType,
                DisciplineResults = disciplineResults
            };
            result.Add(item);
        }
        return Ok(result);
    }

    [HttpPost("{sportId}/save")]
    public async Task<IActionResult> SaveResults(int sportId, [FromBody] SaveResultsRequest request)
    {
        try
        {
            if (request == null)
                return BadRequest(new { message = "Пустой запрос" });

            using var transaction = await _context.Database.BeginTransactionAsync();

            // 1. Удаляем старые командные результаты (если есть)
            var oldTeamResults = await _context.TeamResults
                .Where(tr => tr.SportId == sportId)
                .ToListAsync();
            _context.TeamResults.RemoveRange(oldTeamResults);

            // 2. Сохраняем индивидуальные результаты (обновляем или добавляем)
            if (request.IndividualResults != null && request.IndividualResults.Any())
            {
                foreach (var item in request.IndividualResults)
                {
                    // проверка участника
                    var participantExists = await _context.Participants
                        .AnyAsync(p => p.Id == item.ParticipantId);

                    if (!participantExists)
                        continue;

                    // проверка дисциплины
                    var disciplineExists = await _context.Disciplines
                        .AnyAsync(d => d.Id == item.DisciplineId && d.SportId == sportId);

                    if (!disciplineExists)
                        continue;

                    var existingResult = await _context.IndividualResults
                        .FirstOrDefaultAsync(r =>
                            r.ParticipantId == item.ParticipantId &&
                            r.DisciplineId == item.DisciplineId);

                    if (existingResult != null)
                    {
                        existingResult.ResultValue = item.ResultValue;
                        existingResult.Place = item.Place;
                        existingResult.Points = item.Points;
                        existingResult.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        _context.IndividualResults.Add(new IndividualResult
                        {
                            ParticipantId = item.ParticipantId,
                            DisciplineId = item.DisciplineId,
                            ResultValue = item.ResultValue,
                            Place = item.Place,
                            Points = item.Points,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = null
                        });
                    }
                }
            }

            // 3. Сохраняем командные результаты
            if (request.TeamResults != null && request.TeamResults.Any())
            {
                foreach (var team in request.TeamResults)
                {
                    // Проверяем существование колледжа
                    var collegeExists = await _context.Colleges
                        .AnyAsync(c => c.Id == team.CollegeId);

                    if (!collegeExists)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new { message = $"Колледж с ID {team.CollegeId} не найден" });
                    }

                    // Проверяем Gender (не null и не пустой)
                    if (string.IsNullOrEmpty(team.Gender))
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new { message = "Gender не может быть пустым" });
                    }

                    _context.TeamResults.Add(new TeamResult
                    {
                        CollegeId = team.CollegeId,
                        SportId = sportId,
                        Gender = team.Gender,
                        Place = team.Place,
                        Points = team.Points
                    });
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { message = "Результаты сохранены" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Ошибка сохранения",
                error = ex.Message,
                innerError = ex.InnerException?.Message
            });
        }
    }




    [HttpGet("{sportId}/saved-results")]
    public async Task<ActionResult<object>> GetSavedResults(int sportId)
    {
        // Получаем ID всех дисциплин этого вида спорта
        var disciplineIds = await _context.Disciplines
            .Where(d => d.SportId == sportId)
            .Select(d => d.Id)
            .ToListAsync();

        var individualResults = await _context.IndividualResults
            .Where(ir => disciplineIds.Contains(ir.DisciplineId))
            .Select(ir => new
            {
                ir.ParticipantId,
                ir.DisciplineId,
                ir.ResultValue,
                ir.Place,
                ir.Points
            })
            .ToListAsync();

        var teamResults = await _context.TeamResults
            .Where(tr => tr.SportId == sportId)
            .Select(tr => new
            {
                tr.CollegeId,
                tr.Gender,
                tr.Place,
                tr.Points
            })
            .ToListAsync();

        return Ok(new
        {
            individualResults,
            teamResults
        });
    }

    [HttpGet("standings")]
    public async Task<ActionResult<IEnumerable<object>>> GetStandings(
     [FromQuery] string group = "all",
     [FromQuery] int? sportId = null)
    {
        // 1. Базовый запрос командных результатов
        var teamResults = _context.TeamResults
            .Include(tr => tr.College)
            .Include(tr => tr.Sport)
            .AsQueryable();

        if (sportId.HasValue)
            teamResults = teamResults.Where(tr => tr.SportId == sportId.Value);

        if (group != "all")
            teamResults = teamResults.Where(tr => tr.College != null && tr.College.Group == group);

        // 2. Группируем по колледжу и виду спорта
        var grouped = teamResults
            .GroupBy(tr => new { tr.CollegeId, tr.SportId, SportType = tr.Sport != null ? tr.Sport.SportType : "individual" })
            .Select(g => new
            {
                g.Key.CollegeId,
                g.Key.SportId,
                g.Key.SportType,
                // Для командных видов: берём минимальное место (лучшее)
                BestPlace = g.Key.SportType == "team" ? g.Min(tr => tr.Place) : (int?)null,
                // Для индивидуальных и многоборья: суммируем очки
                TotalPointsIndiv = g.Key.SportType != "team" ? g.Sum(tr => tr.Points) : (int?)null
            })
            .ToList();

        // 3. Для командных видов нужно получить Points, соответствующие лучшему месту
        var collegeSportPoints = new List<CollegeSportPoints>();
        foreach (var item in grouped)
        {
            if (item.SportType == "team")
            {
                // Найти запись в TeamResults с таким же CollegeId, SportId и Place = BestPlace
                var bestResult = await _context.TeamResults
                    .FirstOrDefaultAsync(tr => tr.CollegeId == item.CollegeId && tr.SportId == item.SportId && tr.Place == item.BestPlace);
                if (bestResult != null)
                {
                    collegeSportPoints.Add(new CollegeSportPoints
                    {
                        CollegeId = item.CollegeId,
                        SportId = item.SportId,
                        Points = bestResult.Points
                    });
                }
            }
            else
            {
                collegeSportPoints.Add(new CollegeSportPoints
                {
                    CollegeId = item.CollegeId,
                    SportId = item.SportId,
                    Points = item.TotalPointsIndiv ?? 0
                });
            }
        }

        // 4. Группируем по колледжу для получения базовых баллов (сумма Points по всем видам)
        var basePointsByCollege = collegeSportPoints
            .GroupBy(csp => csp.CollegeId)
            .Select(g => new
            {
                CollegeId = g.Key,
                BasePoints = g.Sum(csp => csp.Points)
            })
            .ToDictionary(x => x.CollegeId, x => x.BasePoints);

        // 5. Получаем названия колледжей
        var colleges = await _context.Colleges.ToDictionaryAsync(c => c.Id, c => c.Name);

        // 6. Получаем штрафы/бонусы (если нужно)
        var penalties = _context.Penalties.AsQueryable();
        if (sportId.HasValue)
            penalties = penalties.Where(p => p.SportId == sportId.Value);
        var penaltiesList = await penalties.ToListAsync();

        // 7. Формируем результат
        var result = new List<object>();
        foreach (var collegeId in basePointsByCollege.Keys)
        {
            var collegeName = colleges.ContainsKey(collegeId) ? colleges[collegeId] : "Неизвестно";
            var basePoints = basePointsByCollege[collegeId];
            var bonusPoints = penaltiesList.Where(p => p.CollegeId == collegeId).Sum(p => p.Points);
            var totalPoints = basePoints + bonusPoints;
            result.Add(new
            {
                CollegeId = collegeId,
                CollegeName = collegeName,
                BasePoints = basePoints,
                BonusPoints = bonusPoints,
                TotalPoints = totalPoints
            });
        }

        // 8. Сортировка и назначение мест
        var finalResult = new List<object>();

        if (sportId.HasValue)
        {
            // если выбран конкретный вид спорта — место берём из БД
            var placesFromDb = await _context.TeamResults
                .Where(tr => tr.SportId == sportId.Value)
                .GroupBy(tr => tr.CollegeId)
                .Select(g => new
                {
                    CollegeId = g.Key,
                    Place = g.Min(x => x.Place) // лучшее место, если есть М/Ж
                })
                .ToDictionaryAsync(x => x.CollegeId, x => x.Place);

            var sorted = result
                .OrderBy(x => placesFromDb.ContainsKey(((dynamic)x).CollegeId)
                    ? placesFromDb[((dynamic)x).CollegeId]
                    : 999)
                .ToList();

            foreach (var item in sorted)
            {
                int collegeId = ((dynamic)item).CollegeId;

                finalResult.Add(new
                {
                    Place = placesFromDb.ContainsKey(collegeId) ? placesFromDb[collegeId] : 0,
                    CollegeId = collegeId,
                    CollegeName = ((dynamic)item).CollegeName,
                    BasePoints = ((dynamic)item).BasePoints,
                    BonusPoints = ((dynamic)item).BonusPoints,
                    TotalPoints = ((dynamic)item).TotalPoints
                });
            }
        }
        else
        {
            // общий зачёт — места считаем по сумме баллов
            var sorted = result
                .OrderByDescending(x => ((dynamic)x).TotalPoints)
                .ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                var item = sorted[i];

                int currentPlace;

                if (i > 0 && ((dynamic)item).TotalPoints == ((dynamic)sorted[i - 1]).TotalPoints)
                {
                    currentPlace = (int)((dynamic)finalResult[i - 1]).Place;
                }
                else
                {
                    currentPlace = i + 1;
                }

                finalResult.Add(new
                {
                    Place = currentPlace,
                    CollegeId = ((dynamic)item).CollegeId,
                    CollegeName = ((dynamic)item).CollegeName,
                    BasePoints = ((dynamic)item).BasePoints,
                    BonusPoints = ((dynamic)item).BonusPoints,
                    TotalPoints = ((dynamic)item).TotalPoints
                });
            }
        }

        return Ok(finalResult);
    }

    // Вспомогательный класс
    public class CollegeSportPoints
    {
        public int CollegeId { get; set; }
        public int SportId { get; set; }
        public int Points { get; set; }
    }

    public class SaveResultsRequest
    {
        public List<IndividualResultDto> IndividualResults { get; set; } = new();
        public List<TeamResultDto> TeamResults { get; set; } = new();
    }

    public class IndividualResultDto
    {
        public int ParticipantId { get; set; }
        public int DisciplineId { get; set; }
        public string ResultValue { get; set; } = string.Empty;
        public int? Place { get; set; }
        public int? Points { get; set; }
    }

    public class TeamResultDto
    {
        public int CollegeId { get; set; }
        public string Gender { get; set; } = string.Empty;
        public int Place { get; set; }
        public int Points { get; set; }
    }
}