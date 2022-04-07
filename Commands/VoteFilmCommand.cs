using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using FilmsBot.Commands.Abstractions;
using FilmsBot.Database;
using FilmsBot.Extensions;
using Microsoft.EntityFrameworkCore;

namespace FilmsBot.Commands
{
    public class VoteFilmCommand : DbInteractionSubCommand<FilmsCommand, FilmsBotDbContext>
    {
        private const int MaxAmount = 1000;
        public override string Name => "голос";
        protected override string Description => "Отдать сумму \"денег\" за указанный фильм";

        private readonly CommandOptionHandler<string> _nameOption = new("фильм", "Название фильма", ApplicationCommandOptionType.String);
        private readonly CommandOptionHandler<int> _amountOption = new("сумма", "Сумма денег", ApplicationCommandOptionType.Integer);

        public VoteFilmCommand(IServiceScopeFactory scopeFactory) : base(scopeFactory)
        {
        }
        
        protected override SlashCommandOptionBuilder ConfigureSubCommand(SlashCommandOptionBuilder builder)
        {
            return builder
                .AddOption(_nameOption.GetBuilder(b => b.WithRequired(true).WithAutocomplete(true)))
                .AddOption(_amountOption.GetBuilder(b => b.WithRequired(true).WithMinValue(0).WithMaxValue(1000)));
        }

        protected override async Task<string?> HandleCommandInternal(
            IServiceProvider serviceProvider,
            FilmsBotDbContext db,
            SocketSlashCommand command,
            SocketSlashCommandDataOption options)
        {
            if (command.Channel is not IGuildChannel guildChannel)
                return "Не на сервере";

            var name = _nameOption.GetOptionValue(options);
            var amount = _amountOption.GetOptionValue(options);
            var user = await db.GetParticipant(command.User);
            var s = await db.Sessions.Where(s => s.GuildId == guildChannel.GuildId).Select(s => new
            {
                Session = s,
                UserVotes = s.Votes.Where(v => v.ParticipantId == user.Id)
            }).FirstOrDefaultAsync();

            if (s == null)
                return "Голосование не идет";

            var sum = s.UserVotes.Sum(v => v.Amount);
            var film = await db.Films.FirstOrDefaultAsync(f => f.Name == name && f.GuildId == guildChannel.GuildId);
            if (film == null)
                return "Фильм не найден";
            
            if (sum >= MaxAmount)
                return "Деньги закончились";

            if (sum + amount > MaxAmount)
                return $"Недостаточно денег. Осталось: {MaxAmount - sum}";

            var existing = s.UserVotes.FirstOrDefault(v => v.FilmId == film.Id);
            if (existing == null)
            {
                existing = new FilmVote
                {
                    Amount = amount,
                    Date = DateTime.UtcNow,
                    Film = film,
                    Participant = user,
                    Session = s.Session
                };
                db.Votes.Add(existing);
            }
            else
            {
                existing.Amount += amount;
                existing.Date = DateTime.UtcNow;
            }

            return null;
        }
    }

    public class FilmNameAutocomplete : AutocompleteHandler
    {
        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
            IInteractionContext context, 
            IAutocompleteInteraction autocompleteInteraction,
            IParameterInfo parameter, 
            IServiceProvider services)
        {
            if (autocompleteInteraction.Data.Type != ApplicationCommandType.Slash)
                return AutocompletionResult.FromError(InteractionCommandError.UnmetPrecondition, "Only for slash commands.");

            var db = services.GetRequiredService<FilmsBotDbContext>();

            return AutocompletionResult.FromSuccess();
        }
    }
}