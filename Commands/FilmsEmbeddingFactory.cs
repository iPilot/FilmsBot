using System.Text;
using Discord;
using FilmsBot.Database;
using FilmsBot.Extensions;
using Microsoft.EntityFrameworkCore;

namespace FilmsBot.Commands
{
    public class FilmsEmbeddingFactory
    {
        private const int PageSize = 8;

        public Embed GetNotFoundEmbed(string title, string description)
        {
            return new EmbedBuilder()
                .WithColor(255, 0, 0)
                .WithTitle(title)
                .WithDescription(description)
                .Build();
        }

        public async Task<Embed> GetRatingsEmbed(FilmsBotDbContext dbContext, Film film, int totalCount, double average, int page)
        {
            var ratings = await dbContext
                .Ratings
                .Where(r => r.FilmId == film.Id)
                .OrderByDescending(r => r.Rating)
                .ThenByDescending(r => r.Date)
                .Skip((Math.Max(page - 1, 0) * PageSize))
                .Take(PageSize)
                .ToListAsync();
            
            var embed = new EmbedBuilder();//.WithFields(new EmbedFieldBuilder());
            var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
            if (totalCount > PageSize)
                embed = embed.WithFooter($"Page 1 of {totalPages}");

            if (ratings.Count > 0)
            {
                var b = new StringBuilder();
                b.AppendLine($"**Average   -   {average:0.##}**");
                b.AppendLine();

                var i = (page - 1) * PageSize + 1;
                foreach (var r in ratings)
                {
                    b.AppendLine($"**{i++}.** {MentionUtils.MentionUser(r.ParticipantId)}  -  **{r.Rating:0.##}**");
                }

                embed = embed
                    .WithTitle($"\"{film.Format()}\" ratings")
                    .WithColor(0, 255, 0)
                    .WithDescription(b.ToString());
            }
            else
            {
                embed = embed
                    .WithTitle($"\"{film.Format()}\"")
                    .WithColor(255, 0, 0)
                    .WithDescription("This film is not rated");
            }

            return embed.Build();
        }

        public MessageComponent? GetButtonsForList(string idPrefix, int totalCount, int page)
        {
            if (totalCount <= PageSize)
                return null;

            var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

            return new ComponentBuilder()
                .WithButton(new ButtonBuilder().WithLabel("<<").WithStyle(ButtonStyle.Primary).WithCustomId($"{idPrefix};{page - 1}").WithDisabled(page <= 1))
                .WithButton(new ButtonBuilder().WithLabel(">>").WithStyle(ButtonStyle.Primary).WithCustomId($"{idPrefix};{page + 1}").WithDisabled(page >= totalPages))
                .Build();
        }

        public async Task<Embed> GetListEmbed(FilmsBotDbContext dbContext, IGuildChannel guildChannel, int page, bool includeRatings, bool includeComments)
        {
            page = Math.Max(0, page - 1);
            var films = await dbContext
                .Films
                .Where(f => f.GuildId == guildChannel.GuildId)
                .Select(f => new
                {
                    Film = f,
                    Rating = (double?) f.Ratings.Average(r => r.Rating)
                })
                .OrderByDescending(f => f.Rating ?? 0)
                .Skip(page * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var totalFilms = await dbContext.Films.Where(f => f.GuildId == guildChannel.GuildId).CountAsync();
            var embed = new EmbedBuilder()
                .WithTitle($"\"{guildChannel.Guild.Name}\" server films:");

            if (films.Count == 0)
                return embed
                    .WithColor(255, 0, 0)
                    .WithDescription("Films not added")
                    .Build();

            if (totalFilms > PageSize)
            {
                embed = embed
                    .WithFooter($"Page {page + 1} of {(int)Math.Ceiling(totalFilms / (double)PageSize)}{Environment.NewLine}Total films: {totalFilms}");
            }

            IEnumerable<EmbedFieldBuilder> GetFields()
            {
                var i = page * PageSize + 1;
                foreach (var film in films)
                {
                    var b = new EmbedFieldBuilder();
                    b.WithName($"**{i++}. {film.Film.Format()}**");

                    var value = $"Rating: {film.Rating?.ToString("0.#") ?? "No rating"}";

                    if (includeComments && !string.IsNullOrWhiteSpace(film.Film.Comment))
                        value = $"{film.Film.Comment}{Environment.NewLine}" + value;

                    b.WithValue(value);

                    yield return b;
                }
            }

            embed = embed
                .WithColor(0, 255, 0)
                .WithFields(GetFields());

            return embed.Build();
        }
    }
}