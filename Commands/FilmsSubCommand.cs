using Discord;
using Discord.WebSocket;
using FilmsBot.Commands.Abstractions;
using FilmsBot.Database;
using Microsoft.EntityFrameworkCore;

namespace FilmsBot.Commands
{
    public class FilmsSubCommand : DbInteractionSubCommand<FilmsCommand, FilmsBotDbContext>
    {
        public override string Name => "все";
        protected override string Description => "Список все добавленных фильмов";

        public FilmsSubCommand(IServiceScopeFactory scopeFactory) : base(scopeFactory)
        {
        }

        protected override async Task<string?> HandleCommandInternal(
            IServiceProvider serviceProvider,
            FilmsBotDbContext db, 
            SocketSlashCommand command,
            SocketSlashCommandDataOption options)
        {
            if (command.Channel is not IGuildChannel guildChannel)
                return "Не на сервере";

            var films = await db
                .Films
                .Where(f => f.GuildId == guildChannel.GuildId)
                .OrderBy(f => f.AddedAt)
                .ToListAsync();

            if (films.Count == 0)
                return "Список пуст";

            await command.RespondAsync(string.Join(Environment.NewLine, films.Select(f => f.Format())));

            return null;
        }
    }
}