using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;    // модели (College, Sport и т.д.)

var builder = WebApplication.CreateBuilder(args);

// Подключение базы данных
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Добавление Identity (аутентификация и управление пользователями)
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Настройка cookie (куда перенаправлять, если не авторизован)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login.html";
});

// Добавление контроллеров и Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();            // для HTML/CSS/JS из wwwroot
app.UseAuthentication();         // включаем аутентификацию
app.UseAuthorization();          // включаем авторизацию
app.MapControllers();

// === ИНИЦИАЛИЗАЦИЯ РОЛЕЙ И ПОЛЬЗОВАТЕЛЕЙ ===
// === ИНИЦИАЛИЗАЦИЯ РОЛЕЙ, ПОЛЬЗОВАТЕЛЕЙ И КОЛЛЕДЖЕЙ ===
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // --- 1. Создаём роли ---
    string[] roles = { "Admin", "Representative" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // --- 2. Создаём администратора ---
    var adminEmail = "admin@spartakiad.by";
    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var admin = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(admin, "Admin123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "Admin");
        }
    }

    // --- 3. Добавляем колледжи (если таблица пуста) ---
    if (!context.Colleges.Any())
    {
        var colleges = new List<College>
    {
        new College { Name = "Березинский аграрно-технический колледж", Group = "Б" },
        new College { Name = "Борисовский государственный строительный колледж", Group = "Б" },
        new College { Name = "Воложинский сельскохозяйственный колледж", Group = "Б" },
        new College { Name = "Жодинский государственный колледж", Group = "А" },
        new College { Name = "Клецкий сельскохозяйственный колледж", Group = "А" },
        new College { Name = "Любанский государственный колледж", Group = "А" },
        new College { Name = "Дзержинский государственный колледж", Group = "А" },
        new College { Name = "Слуцкий государственный индустриальный колледж", Group = "Б" },
        new College { Name = "Смиловичский государственный колледж", Group = "Б" },
        new College { Name = "Смолевичский государственный колледж", Group = "А" },
        new College { Name = "Узденский государственный колледж", Group = "А" },
        new College { Name = "Червенский строительный колледж", Group = "Б" },
        new College { Name = "Борисовский государственный колледж", Group = "А" },
        new College { Name = "Вилейский государственный колледж", Group = "А" },
        new College { Name = "Ильянский государственный аграрный колледж", Group = "А" },
        new College { Name = "Копыльский государственный колледж", Group = "А" },
        new College { Name = "Марьиногорский государственный аграрно-технический колледж им. В.Е.Лобанка", Group = "А" },
        new College { Name = "Минский государственный областной колледж", Group = "А" },
        new College { Name = "Молодечненский государственный колледж", Group = "А" },
        new College { Name = "Несвижский государственный колледж им. Я.Коласа", Group = "А" },
        new College { Name = "Новопольский государственный аграрно-экономический колледж", Group = "А" },
        new College { Name = "Солигорский государственный колледж", Group = "А" },
        new College { Name = "Слуцкий государственный колледж", Group = "А" },
        new College { Name = "Смиловичский государственный аграрный колледж", Group = "А" },
        new College { Name = "Борисовский государственный технический колледж", Group = "Б" },
        new College { Name = "Жодинский политехнический колледж", Group = "Б" },
        new College { Name = "Солигорский горно-химический колледж", Group = "Б" },
        new College { Name = "Молодечненский государственный политехнический колледж", Group = "Б" },
        new College { Name = "Молодечненский торгово-экономический колледж", Group = "Б" },
        new College { Name = "Борисовский государственный медицинский колледж", Group = "А" },
        new College { Name = "Молодечненский государственный медицинский колледж им. И.В.Залуцкого", Group = "А" },
        new College { Name = "Слуцкий государственный медицинский колледж им. С.И.Шкляревского", Group = "А" }
    };

        await context.Colleges.AddRangeAsync(colleges);
        await context.SaveChangesAsync();
    }

    // --- 4. Создаём представителя НГАЭК и связываем с колледжем ---
    var repEmail = "borisov@college.by";
    IdentityUser representative = await userManager.FindByEmailAsync(repEmail);
    if (representative == null)
    {
        representative = new IdentityUser
        {
            UserName = repEmail,
            Email = repEmail,
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(representative, "123456Aa!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(representative, "Representative");
        }
    }

    // Находим колледж НГАЭК и связываем с пользователем
    var ngaekCollege = await context.Colleges.FirstOrDefaultAsync(c => c.Name.Contains("Борисовский государственный колледж"));
    if (ngaekCollege != null && representative != null && ngaekCollege.UserId == null)
    {
        ngaekCollege.UserId = representative.Id;
        await context.SaveChangesAsync();
    }
}

app.Run();

app.Run();