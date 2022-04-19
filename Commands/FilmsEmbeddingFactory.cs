using System.Text;
using Discord;
using FilmsBot.Database;
using FilmsBot.Extensions;
using Microsoft.EntityFrameworkCore;

namespace FilmsBot.Commands
{
    public class FilmsEmbeddingFactory
    {
        private const int PageSize = 10;

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
            var q = dbContext
                .Films
                .Where(f => f.GuildId == guildChannel.GuildId);

            if (includeRatings)
                q = q
                    .OrderByDescending(f => f.Ratings!.Select(r => r.Rating).Average())
                    .ThenBy(f => f.Name)
                    .Include(f => f.Ratings);
            else
                q = q.OrderBy(f => f.Name);

            var films = await q
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

            var sb = new StringBuilder();
            var i = page * PageSize + 1;  
            foreach (var film in films)
            {
                var n = $"**{i++}. {film.Format()}**";
                sb.Append(n);

                if (includeRatings && film.Ratings is { Count: > 0 })
                {
                    var avg = film.Ratings.Average(r => r.Rating);
                    sb.Append(' ');
                    sb.Append('-', 40 - n.Length);
                    sb.Append($" ** [ {avg:0.0} ] ** ");
                }

                if (includeComments && !string.IsNullOrWhiteSpace(film.Comment))
                    sb.AppendFormat($" || {film.Comment} ||");

                sb.AppendLine();
            }

            embed = embed
                .WithColor(0, 255, 0)
                .WithDescription(sb.ToString());

            return embed.Build();
        }
    }
}