using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RetroScan.Api.Data;
using RetroScan.Api.Errors;
using RetroScan.Api.LanguageModel;
using RetroScan.Api.Owners;
using RetroScan.Api.Research;
using RetroScan.Api.Sources;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<UpstreamExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<RetroScanDbContext>((services, options) =>
    options.UseMySQL(services.GetRequiredService<IConfiguration>().GetConnectionString("RetroScan")
                     ?? throw new InvalidOperationException("ConnectionStrings:RetroScan is not configured.")));
builder.Services.AddHealthChecks().AddDbContextCheck<RetroScanDbContext>();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<ResearchService>();

builder.Services.AddOptions<GroqOptions>()
    .BindConfiguration(GroqOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddHttpClient<ILanguageModel, GroqLanguageModel>((services, client) =>
{
    client.BaseAddress = services.GetRequiredService<IOptions<GroqOptions>>().Value.BaseUrl;
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddOptions<WikipediaOptions>()
    .BindConfiguration(WikipediaOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddHttpClient<ISourceSearch, WikipediaSourceSearch>((services, client) =>
{
    var wikipedia = services.GetRequiredService<IOptions<WikipediaOptions>>().Value;
    client.BaseAddress = wikipedia.BaseUrl;
    client.DefaultRequestHeaders.UserAgent.ParseAdd(wikipedia.UserAgent);
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddOptions<AskRateLimitOptions>()
    .BindConfiguration(AskRateLimitOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(AskRateLimitOptions.PolicyName, context =>
    {
        var limit = context.RequestServices.GetRequiredService<IOptions<AskRateLimitOptions>>().Value;
        return RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = limit.PermitLimit, Window = limit.Window });
    });
    options.OnRejected = (context, _) => context.HttpContext.RequestServices
        .GetRequiredService<IProblemDetailsService>()
        .WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context.HttpContext,
            ProblemDetails =
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too many questions",
                Detail = "This demo allows a few questions per minute. Try again shortly.",
            },
        });
});

var app = builder.Build();

if (app.Configuration.GetValue("Database:MigrateOnStartup", true))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<RetroScanDbContext>().Database.MigrateAsync();
}

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRateLimiter();
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api"),
    api => api.UseMiddleware<OwnerCookieMiddleware>());

app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();
