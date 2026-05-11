using EnglishTestPlatform.Interfaces;
using EnglishTestPlatform.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSession();
builder.Services.AddMemoryCache();

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ITestLoaderService, TestLoaderService>();
builder.Services.AddScoped<TestEvaluatorService>();

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

// Создаём папку Tests, если её нет
string testsDir = Path.Combine(app.Environment.ContentRootPath, "Tests");
if (!Directory.Exists(testsDir))
    Directory.CreateDirectory(testsDir);

app.Run();