using Microsoft.EntityFrameworkCore;
using Olwe;
using Olwe.Data;
using Olwe.Data.Extensions;
using Olwe.Extensions;
using Remora.Discord.Hosting.Extensions;
using Serilog;
using Serilog.Templates;

var builder = WebApplication.CreateBuilder(args);

const string consoleLogFormat =
    "[{@t:yyyy-MM-dd HH:mm:ss.fff}] " +
    "[{@l}] " +
    "[{Coalesce(SourceContext, '<none>')}]: {@m}\n{@x}";

const string fileLogFormat =
    "[{@t:yyyy-MM-dd HH:mm:ss.fff}] " +
    "[{@l}] " +
    "[{Coalesce(SourceContext, '<none>')}]: {@m}\n{@x}";

builder.Configuration
    .AddEnvironmentVariables("OLWE_")
    .AddCommandLine(args)
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddJsonFile("appsettings.Production.json", optional: true);

builder.Services.AddControllersWithViews();
builder.Host.UseSerilog((_, configuration) =>
{
    configuration
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .WriteTo.Console(formatter: new ExpressionTemplate(consoleLogFormat, theme: LoggerTheme.Theme))
        .WriteTo.Map(
            _ => $"{DateOnly.FromDateTime(DateTimeOffset.UtcNow.DateTime):yyyy-MM-dd}",
            (v, cf) =>
            {
                cf.File(
                    new ExpressionTemplate(fileLogFormat),
                    $"./Logs/{v}.log",
                    // 32 megabytes
                    fileSizeLimitBytes: 33_554_432,
                    flushToDiskInterval: TimeSpan.FromMinutes(2.5),
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: 50
                );
            },
            sinkMapCountLimit: 1);
});

builder.Services.AddAntiforgery(options => options.HeaderName = "X-XSRF-TOKEN");
builder.Services.AddResponseCompression();

builder.Services.AddDbContext<OlweContext>(options => options
    .UseNpgsql(builder.Configuration["Olwe:Data:ConnectionString"] ??
               throw new InvalidOperationException("ConnectionString is not configured"),
        optionsBuilder => optionsBuilder.UseDateTimeOffsetTranslations()));

builder.Services.AddHttpClient();
builder.Services.AddRemoraServices();
builder.Host.AddDiscordService(x
    => x.GetRequiredService<IConfiguration>()["Olwe:Discord:Token"] ??
       throw new InvalidOperationException("Olwe:Discord:Token is not configured"));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

await using var db = app.Services.CreateScope()
    .ServiceProvider
    .GetRequiredService<OlweContext>();

try { db.Database.GetPendingMigrations();} catch { /* for some reason the first call to this throws an error */ }
var pendingMigrations = db.Database.GetPendingMigrations();
if (pendingMigrations.Any())
    await db.Database.MigrateAsync();

app.UseAuthentication();
app.UseResponseCompression();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapControllers();
app.MapFallbackToFile("index.html");
app.Run();