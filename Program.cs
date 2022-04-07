using Discord;
using Discord.WebSocket;
using FilmsBot;
using FilmsBot.Client;
using FilmsBot.Commands;
using FilmsBot.Commands.Abstractions;
using FilmsBot.Database;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile($"conf/appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true, false);

builder.Services.AddSingleton<DiscordClient>();
builder.Services.AddScoped<FilmsInteractionModule>();
builder.Services.AddDbContextPool<FilmsBotDbContext>(o =>
{
    var connectionString = builder.Configuration["ConnectionString"];
    Console.WriteLine(connectionString);
    o.UseNpgsql(connectionString);
}, 64);
builder.Services.Scan(s => s.FromApplicationDependencies().AddClasses(a => a.AssignableTo<ISlashCommandHandler>()).AsImplementedInterfaces().WithSingletonLifetime());
builder.Services.Scan(s => s.FromApplicationDependencies().AddClasses(a => a.AssignableTo(typeof(ISlashSubCommandHandler<>))).AsImplementedInterfaces().WithSingletonLifetime());
builder.Services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
{
    AlwaysDownloadUsers = true,
    MessageCacheSize = 250,
    LogLevel = LogSeverity.Verbose
}));
builder.Services.AddSingleton<IBotDeveloperProvider, BotDeveloperProvider>();
builder.Host.UseSerilog((_, c) => c.ReadFrom.Configuration(builder.Configuration));

var app = builder.Build();

var client = app.Services.GetRequiredService<DiscordClient>();

client.Run();
app.Run();