using Discord;
using Discord.Interactions;
using FilmsBot.Database;
using Microsoft.EntityFrameworkCore;

namespace FilmsBot.Commands
{
    public class FilmNameAutocompleteHandler : AutocompleteHandler
    {
        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
            IInteractionContext context,
            IAutocompleteInteraction autocompleteInteraction,
            IParameterInfo parameter,
            IServiceProvider services)
        {
            if (context.Channel is not IGuildChannel guildChannel)
                return AutocompletionResult.FromError(InteractionCommandError.UnmetPrecondition, "Not in a guild");

            var db = services.GetRequiredService<FilmsBotDbContext>();
            var prefix = autocompleteInteraction.Data.Current.Value as string ?? "";

            var films = await db
                .Films
                .Where(f => f.GuildId == guildChannel.GuildId && f.Name.StartsWith(prefix))
                .OrderBy(f => f.Name)
                .Take(8)
                .Select(f => new AutocompleteResult(f.Name, f.Name))
                .ToArrayAsync();

            return AutocompletionResult.FromSuccess(films);
        }
    }
}