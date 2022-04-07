using Discord;
using Discord.WebSocket;
using FilmsBot.Commands.Abstractions;
using FilmsBot.Database;
using FilmsBot.Extensions;
using Microsoft.EntityFrameworkCore;

namespace FilmsBot.Commands
{
    public class RateFilmCommand : DbInteractionSubCommand<FilmsCommand, FilmsBotDbContext>
    {
        public override string Name => "оценка";
        protected override string Description => "Оценить просмотренный фильм";

        private readonly CommandOptionHandler<string> _nameOption = new("фильм", "Название фильма", ApplicationCommandOptionType.String);
        private readonly CommandOptionHandler<double> _ratingOption = new("оценка", "Оценка фильма", ApplicationCommandOptionType.Number);

        public RateFilmCommand(IServiceScopeFactory scopeFactory) : base(scopeFactory)
        {
        }

        protected override SlashCommandOptionBuilder ConfigureSubCommand(SlashCommandOptionBuilder builder)
        {
            return builder
                .AddOption(_nameOption.GetBuilder(b => b.WithRequired(true).WithAutocomplete(true)))
                .AddOption(_ratingOption.GetBuilder(b => b.WithRequired(true).WithMinValue(0).WithMaxValue(10)));
        }

        protected override async Task<string?> HandleCommandInternal(IServiceProvider serviceProvider, FilmsBotDbContext db, SocketSlashCommand command, SocketSlashCommandDataOption options)
        {
            if (command.Channel is not IGuildChannel guildChannel)
                return "Не на сервере";

            var name = _nameOption.GetOptionValue(options);
            var film = await db.Films.FirstOrDefaultAsync(f => f.Name == name && f.GuildId == guildChannel.GuildId);
            if (film == null)
                return "Фильм не найден";

            var rating = _ratingOption.GetOptionValue(options);
            var user = await db.GetParticipant(command.User);
            var rate = await db.Ratings.FirstOrDefaultAsync(r => r.FilmId == film.Id && r.ParticipantId == user.Id);

            if (rate == null)
            {
                rate = new()
                {
                    Date = DateTime.UtcNow,
                    Participant = user,
                    Rating = rating,
                    Film = film
                };
                db.Ratings.Add(rate);
            }
            else
            {
                rate.Rating = rating;
                rate.Date = DateTime.UtcNow;
            }

            return null;
        }
    }
}