using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;

    public UsersController(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userManager.Users.ToListAsync();

        var result = new List<object>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            var college = await _context.Colleges
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            result.Add(new
            {
                id = user.Id,
                email = user.Email,
                userName = user.UserName,
                role = roles.FirstOrDefault() ?? "User",
                collegeId = college?.Id,
                collegeName = college?.Name
            });
        }

        return Ok(result);
    }

    [HttpPost("representative")]
    public async Task<IActionResult> CreateRepresentative(CreateRepresentativeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return BadRequest(new { message = "Введите email" });

        if (string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { message = "Введите пароль" });

        var college = await _context.Colleges.FindAsync(dto.CollegeId);
        if (college == null)
            return BadRequest(new { message = "Колледж не найден" });

        if (!await _roleManager.RoleExistsAsync("Representative"))
        {
            await _roleManager.CreateAsync(new IdentityRole("Representative"));
        }

        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
            return BadRequest(new { message = "Пользователь с таким email уже существует" });

        var user = new IdentityUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user, dto.Password);

        if (!createResult.Succeeded)
        {
            var errors = createResult.Errors.Select(e =>
            {
                return e.Code switch
                {
                    "PasswordRequiresDigit" =>
                        "Пароль должен содержать хотя бы одну цифру.",

                    "PasswordRequiresUpper" =>
                        "Пароль должен содержать хотя бы одну заглавную букву.",

                    "PasswordRequiresNonAlphanumeric" =>
                        "Пароль должен содержать хотя бы один специальный символ.",

                    "PasswordTooShort" =>
                        "Пароль слишком короткий.",
                    "PasswordRequiresLower" =>
                        "Пароль должен содержать хотя бы одну строчную букву.",

                    _ => e.Description
                };
            });

            return BadRequest(new
            {
                message = string.Join(" ", errors)
            });
        }

        await _userManager.AddToRoleAsync(user, "Representative");

        college.UserId = user.Id;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Представитель создан" });
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
            return NotFound(new { message = "Пользователь не найден" });

        var college = await _context.Colleges
            .FirstOrDefaultAsync(c => c.UserId == id);

        if (college != null)
        {
            college.UserId = null;
        }

        var result = await _userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = string.Join("; ", result.Errors.Select(e => e.Description))
            });
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Пользователь удалён" });
    }
    [HttpPut("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(string id, ResetPasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NewPassword))
            return BadRequest(new { message = "Введите новый пароль" });

        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
            return NotFound(new { message = "Пользователь не найден" });

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = string.Join("; ", result.Errors.Select(e => e.Description))
            });
        }

        return Ok(new { message = "Пароль изменён" });
    }

    public class ResetPasswordDto
    {
        public string NewPassword { get; set; } = string.Empty;
    }
}


public class CreateRepresentativeDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int CollegeId { get; set; }
}