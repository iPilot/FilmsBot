using Discord;
using Discord.WebSocket;
using FilmsBot.Commands.Abstractions;
using FilmsBot.Database;
using FilmsBot.Extensions;
using Microsoft.EntityFrameworkCore;

namespace FilmsBot.Commands
{
    public class AddFilmSubCommand : DbInteractionSubCommand<FilmsCommand, FilmsBotDbContext>
    {
        public override string Name => "добавить";
        protected override string Description => "Добавление нового фильма в список";

        private readonly CommandOptionHandler<string> _nameOption = new("название", "Название фильма", ApplicationCommandOptionType.String);
        private readonly CommandOptionHandler<int?> _yearOption = new("год", "Год выхода фильма", ApplicationCommandOptionType.Integer);

        public AddFilmSubCommand(IServiceScopeFactory scopeFactory) : base(scopeFactory)
        {
        }

        protected override SlashCommandOptionBuilder ConfigureSubCommand(SlashCommandOptionBuilder builder)
        {
            return builder
                .AddOption(_nameOption.GetBuilder(b => b.WithRequired(true)))
                .AddOption(_yearOption.GetBuilder(b => b.WithRequired(false).WithMinValue(1900).WithMaxValue(2100)));
        }

        protected override async Task<string?> HandleCommandInternal(
            IServiceProvider serviceProvider,
            FilmsBotDbContext db,
            SocketSlashCommand command, 
            SocketSlashCommandDataOption options)
        {
            if (command.Channel is not IGuildChannel guildChannel)
                return "Не на сервере";

            var name = options.GetOptionValue<string>("название").Trim();
            var year = options.GetOptionValue<int?>("год");

            if (await db.Films.AnyAsync(f => f.Name == name && f.Year == year && f.GuildId == guildChannel.GuildId))
                return "Уже добавлено";

            var user = await db.Participants.FindAsync(command.User.Id);
            if (user == null)
            {
                user = new Participant
                {
                    Id = command.User.Id,
                    JoinedAt = DateTime.UtcNow
                };
                db.Participants.Add(user);
            }

            var film = new Film
            {
                GuildId = guildChannel.GuildId,
                Name = name,
                AddedBy = user,
                Year = year,
                AddedAt = DateTime.UtcNow
            };

            db.Films.Add(film);

            return null;
        }
    }

}