using MediatR;
using Microsoft.EntityFrameworkCore;
using Olwe;
using Olwe.Data;
using Olwe.Extensions;
using Olwe.Remora;
using Olwe.Services;
using Olwe.Services.Data;
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
    .AddEnvironmentVariables()
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

builder.Services.AddDbContext<OlweContext>(options =>
{
    options.UseNpgsql(builder.Configuration["Olwe:Data:ConnectionString"] ??
                      throw new InvalidOperationException("No connection string found"));
    options.EnableDetailedErrors();
    options.EnableSensitiveDataLogging();
});

builder.Services.AddHttpClient();
builder.Services.AddRemoraServices();
builder.Services.AddScoped<PrefixCacheService>();
builder.Services.AddScoped<ModConfigCacheService>();
builder.Services.AddMediatR(configuration => configuration.AsScoped(), typeof(OlweContext).Assembly, typeof(PrefixCacheService).Assembly, typeof(Setup).Assembly, typeof(Program).Assembly);
builder.Services.AddHostedService<DbMigrationService>();
builder.Host.AddDiscordService(x
    => x.GetRequiredService<IConfiguration>()["Olwe:Discord:Token"] ??
       throw new InvalidOperationException("Olwe:Discord:Token is not configured"));


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseAuthentication();
app.UseResponseCompression();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapControllers();
app.MapFallbackToFile("index.html");
app.Run();