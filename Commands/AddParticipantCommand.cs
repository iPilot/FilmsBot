using Discord.WebSocket;
using FilmsBot.Commands.Abstractions;
using FilmsBot.Database;

namespace FilmsBot.Commands
{
    public class AddParticipantCommand : DbInteractionSubCommand<FilmsCommand, FilmsBotDbContext>
    {
        public override string Name => "смотреть";
        protected override string Description => "Стать смотрящим фильмы";

        public AddParticipantCommand(IServiceScopeFactory scopeFactory) : base(scopeFactory)
        {
        }

        protected override async Task<string?> HandleCommandInternal(IServiceProvider serviceProvider, FilmsBotDbContext db, SocketSlashCommand command, SocketSlashCommandDataOption options)
        {
            if (await db.Participants.FindAsync(command.User.Id) != null)
            {
                return "Уже смотрящий!";
            }

            db.Participants.Add(new Participant
            {
                Id = command.User.Id,
                JoinedAt = DateTime.UtcNow
            });

            return null;
        }
    }
}