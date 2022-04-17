using Discord;
using Discord.WebSocket;
using FilmsBot;
using FilmsBot.Client;
using FilmsBot.Commands;
using FilmsBot.Database;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile($"conf/appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true, false);

builder.Services.AddSingleton<DiscordClient>();
builder.Services.AddTransient<FilmsInteractionModule>();
builder.Services.AddSingleton<FilmsInteractionService>();
builder.Services.AddDbContextPool<FilmsBotDbContext>(o =>
{
    var connectionString = builder.Configuration["ConnectionString"];
    Console.WriteLine(connectionString);
    o.UseNpgsql(connectionString);
}, 64);
builder.Services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
{
    AlwaysDownloadUsers = true,
    MessageCacheSize = 250,
    LogLevel = LogSeverity.Verbose
}));
builder.Services.AddSingleton<FilmsEmbeddingFactory>();
builder.Services.AddSingleton<IBotDeveloperProvider, BotDeveloperProvider>();
builder.Host.UseSerilog((_, c) => c.ReadFrom.Configuration(builder.Configuration));

var app = builder.Build();
var client = app.Services.GetRequiredService<DiscordClient>();

client.Run();
app.Run();