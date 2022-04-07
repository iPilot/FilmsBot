using Discord;
using Discord.WebSocket;
using FilmsBot.Commands.Abstractions;
using FilmsBot.Database;
using FilmsBot.Extensions;
using Microsoft.EntityFrameworkCore;

namespace FilmsBot.Commands
{
    public class StartFilmVoteCommand : DbInteractionSubCommand<FilmsCommand, FilmsBotDbContext>
    {
        public override string Name => "старт";
        protected override string Description => "Начать голосование за следующий фильм";

        public StartFilmVoteCommand(IServiceScopeFactory scopeFactory) : base(scopeFactory)
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

            var user = await db.GetParticipant(command.User);
            var session = await db.Sessions.FirstOrDefaultAsync(s => s.GuildId == guildChannel.GuildId && s.End == null);

            if (session != null)
                return "Голосование уже идет";

            session = new Session
            {
                Start = DateTime.UtcNow,
                GuildId = guildChannel.GuildId,
                Creator = user
            };
            db.Sessions.Add(session);

            return null;
        }
    }
}