using Data;
using EnglishTestPlatform.Interfaces;
using EnglishTestPlatform.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSession();
builder.Services.AddMemoryCache();

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ITestLoaderService, TestLoaderService>();
builder.Services.AddScoped<TestEvaluatorService>();

// Определяем путь к файлу БД
var dbPath = Path.Combine(
    builder.Environment.ContentRootPath,,
    "Data",
    "mydatabase.db"
);

// Убеждаемся, что папка существует
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

// Регистрируем DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}")
);


var app = builder.Build(); 

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Автоматически применяем миграции при запуске
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // EnsureCreated — создаст БД и таблицы, если их нет.
    // Не использует миграции, создаёт по текущей модели.
    context.Database.EnsureCreated();
}

// Создаём папку Tests, если её нет
string testsDir = Path.Combine(app.Environment.ContentRootPath, "Tests");
if (!Directory.Exists(testsDir))
    Directory.CreateDirectory(testsDir);

app.Run();