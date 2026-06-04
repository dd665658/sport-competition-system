using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ApplicationDbContext _context;

    public AuthController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Проверяем, что логин и пароль не пустые
        if (string.IsNullOrEmpty(request.Login) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { message = "Login and password are required" });
        }

        // Ищем пользователя по email (логину)
        var user = await _userManager.FindByEmailAsync(request.Login);
        if (user == null)
        {
            // Если не нашли по email, пробуем найти по UserName
            user = await _userManager.FindByNameAsync(request.Login);
        }

        if (user == null)
        {
            return Unauthorized(new { message = "Неверный логин или пароль" });
        }

        // Проверяем пароль
        var result = await _signInManager.PasswordSignInAsync(user, request.Password, false, false);
        if (!result.Succeeded)
        {
            return Unauthorized(new { message = "Неверный логин или пароль" });
        }

        // Получаем роли пользователя
        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "User";

        // Если пользователь представитель, ищем его колледж
        if (role == "Representative")
        {
            var college = await _context.Colleges.FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (college != null)
            {
                return Ok(new
                {
                    role = "representative",
                    collegeId = college.Id,
                    collegeName = college.Name
                });
            }
            else
            {
                // Если представитель не привязан к колледжу (ошибка в данных)
                return Ok(new
                {
                    role = "representative",
                    collegeId = 0,
                    collegeName = "Колледж не найден"
                });
            }
        }

        // Если администратор
        if (role == "Admin")
        {
            return Ok(new { role = "admin" });
        }

        // На всякий случай, если роль не определена
        return Ok(new { role = "user" });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new { message = "Logged out successfully" });
    }
}

public class LoginRequest
{
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}