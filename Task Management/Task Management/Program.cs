using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog;
using Serilog.Formatting.Json;
using System.Data;
using System.Diagnostics;
using Task_Management.Data;
using Task_Management.Services;

System.Diagnostics.Trace.AutoFlush = true;

var builder = WebApplication.CreateBuilder(args);

Tracer.TaskManagerTrace.Switch.Level = SourceLevels.All;
Tracer.TaskManagerTrace.Listeners.Add(new TextWriterTraceListener("logs/taskmanagementTrace.log"));
Tracer.TaskManagerTrace.Flush();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File(
        formatter: new JsonFormatter(),
        path: "logs/taskmanagementLogs.log",
        rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddScoped<ReportService>();
builder.Services.AddSingleton<ExcelExportService>();
builder.Services.AddControllersWithViews();

string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Строка подключения 'DefaultConnection' не найдена в appsettings.json.");

builder.Services.AddScoped<IDbConnection>(_ => new NpgsqlConnection(connectionString));

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<TaskManagementDbContext>(options =>
        options.UseInMemoryDatabase("TestTaskDb"));
}
else
{
    builder.Services.AddDbContext<TaskManagementDbContext>(options =>
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null
            );
        }));
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public static class Tracer
{
    public static TraceSource TaskManagerTrace = new TraceSource("TaskManagerTrace", SourceLevels.Verbose);
}
public partial class Program { }