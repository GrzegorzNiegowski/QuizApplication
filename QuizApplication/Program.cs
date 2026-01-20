using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QuizApplication.Data;
using QuizApplication.Hubs;
using QuizApplication.Models;
using QuizApplication.Services;
using QuizApplication.Utilities;

var builder = WebApplication.CreateBuilder(args);
// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
// Identity
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
options.SignIn.RequireConfirmedAccount = false)
.AddRoles<IdentityRole>() // Dodaj role
.AddEntityFrameworkStores<ApplicationDbContext>();
// MVC
builder.Services.AddControllersWithViews();
// Services
builder.Services.AddScoped<IQuizService, QuizService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddSingleton<IGameSessionService, GameSessionService>();
// SignalR with configuration
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = GameConstants.SignalRMaxMessageSizeBytes;
    options.StreamBufferCapacity = GameConstants.SignalRStreamBufferCapacity;
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.KeepAliveInterval = TimeSpan.FromSeconds(30);
});
// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}
else
{
    builder.Logging.SetMinimumLevel(LogLevel.Information);
}
var app = builder.Build();
// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(
name: "default",
pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
app.MapHub<QuizHub>("/quizHub");
app.Logger.LogInformation("Application started in {Environment} mode",
app.Environment.EnvironmentName);
app.Run();
