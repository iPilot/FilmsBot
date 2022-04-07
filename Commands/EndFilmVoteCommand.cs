using Discord;
using Discord.WebSocket;
using FilmsBot.Commands.Abstractions;
using FilmsBot.Database;
using Microsoft.EntityFrameworkCore;

namespace FilmsBot.Commands
{
    public class EndFilmVoteCommand : DbInteractionSubCommand<FilmsCommand, FilmsBotDbContext>
    {
        public override string Name => "стоп";
        protected override string Description => "Завершение голосования за фильмы";

        public EndFilmVoteCommand(IServiceScopeFactory scopeFactory) : base(scopeFactory)
        {
        }

        protected override async Task<string?> HandleCommandInternal(
            IServiceProvider serviceProvider,
            FilmsBotDbContext db, 
            SocketSlashCommand command,
            SocketSlashCommandDataOption options)
        {
            if (command.Channel is not IGuildChannel)
                return "Не на сервере";

            var session = await db.Sessions.FirstOrDefaultAsync(s => s.End == null);
            if (session == null)
                return "Голосование не идет";

            session.End = DateTime.UtcNow;

            // select random film?

            return null;
        }
    }
}