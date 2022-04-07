using System.Text.Json;
using Discord;
using Discord.WebSocket;
using FilmsBot.Commands.Abstractions;
using FilmsBot.Database;
using Microsoft.EntityFrameworkCore;

namespace FilmsBot.Commands
{
    public class LoadFilmsData : DbInteractionSubCommand<FilmsCommand, FilmsBotDbContext>
    {
        public override string Name => "выгрузить";
        protected override string Description => "Выгрузить все фильмы в колесо";

        public LoadFilmsData(IServiceScopeFactory scopeFactory) : base(scopeFactory)
        {
        }

        protected override async Task<string?> HandleCommandInternal(IServiceProvider serviceProvider, FilmsBotDbContext db, SocketSlashCommand command, SocketSlashCommandDataOption options)
        {
            if (command.Channel is not IGuildChannel gc)
                return "Не на сервере";

            var data = await db
                .Films.Where(f => f.GuildId == gc.GuildId && f.Session == null)
                .Select(f => new
                {
                    name = f.Name,
                    amount = f.Votes.Select(v => v.Amount).Sum(),
                    id = f.Id.ToString(),
                    fastId = f.Id,
                    extra = (string?) null
                })
                .OrderBy(d => d.fastId)
                .ToListAsync();

            if (data.Count == 0)
                return "Нет фильмов";

            await using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, data, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await command.RespondWithFileAsync(stream, $"report-{Guid.NewGuid():N}.json");

            return null;
        }
    }
}