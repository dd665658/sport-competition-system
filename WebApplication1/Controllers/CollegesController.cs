using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CollegesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CollegesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/colleges
    [HttpGet]
    public async Task<ActionResult<IEnumerable<College>>> GetColleges()
    {
        return await _context.Colleges.ToListAsync();
    }

    // GET: api/colleges/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<College>> GetCollege(int id)
    {
        var college = await _context.Colleges.FindAsync(id);
        if (college == null)
        {
            return NotFound();
        }
        return college;
    }

    // POST: api/colleges
    [HttpPost]
    public async Task<ActionResult<College>> CreateCollege(College college)
    {
        college.Id = 0;

        _context.Colleges.Add(college);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCollege), new { id = college.Id }, college);
    }

    // PUT: api/colleges/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCollege(int id, College updatedCollege)
    {
        var college = await _context.Colleges.FindAsync(id);

        if (college == null)
            return NotFound();

        college.Name = updatedCollege.Name;
        college.Group = updatedCollege.Group;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/colleges/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCollege(int id)
    {
        var college = await _context.Colleges.FindAsync(id);
        if (college == null)
        {
            return NotFound();
        }

        _context.Colleges.Remove(college);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool CollegeExists(int id)
    {
        return _context.Colleges.Any(e => e.Id == id);
    }
}