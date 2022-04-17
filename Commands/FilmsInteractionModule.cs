using System.Text.Json;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using FilmsBot.Commands.Abstractions;
using FilmsBot.Database;
using FilmsBot.Extensions;
using Microsoft.EntityFrameworkCore;

namespace FilmsBot.Commands
{
    [Group(ConstName, ConstDesc)]
    public class FilmsInteractionModule : DbInteractionSubCommand<FilmsBotDbContext>
    {
        private readonly IBotDeveloperProvider _developerProvider;
        private readonly FilmsEmbeddingFactory _embeddingFactory;
        private const string ConstName = "films";
        private const string ConstDesc = "Cooperative films watch management";
        private const int MaxAmount = 1000;

        private static string GetRatingsComponentsId(ulong guildId) => $"films-ratings;{guildId}";
        private static string GetListComponentsId(bool ratings, bool comments) => $"films-list;{ratings};{comments}";

        public FilmsInteractionModule(IServiceScopeFactory scopeFactory, IBotDeveloperProvider developerProvider, FilmsEmbeddingFactory embeddingFactory) : base(scopeFactory)
        {
            _developerProvider = developerProvider;
            _embeddingFactory = embeddingFactory;
        }

        [SlashCommand("watch", "Join to watchers")]
        public async Task<RuntimeResult> BecomeParticipant()
        {
            if (await DbContext.Participants.FindAsync(UserId) != null)
                return new CommandResult(InteractionCommandError.Unsuccessful, "Already participant");

            DbContext.Participants.Add(new Participant
            {
                Id = UserId,
                JoinedAt = DateTime.UtcNow
            });

            return new CommandResult("Prepare to watch!");
        }

        [SlashCommand("add", "Addition a new films to watch wish-list")]
        public async Task<RuntimeResult> AddFilm(
            [Summary("film-name", "A new film name")] string filmName,
            [Summary("film-year", "The year of film release"), MinValue(1900), MaxValue(2100)] int? year = null,
            [Summary("comment", "Additional info")] string? comment = null)
        {
            if (!ValidateIfGuild(out var guildChannel, out var result))
                return result;

            filmName = string.Join(' ', filmName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

            if (string.IsNullOrWhiteSpace(filmName))
                return new CommandResult(InteractionCommandError.UnmetPrecondition, "Film name cannot be empty");

            if (await DbContext.Films.AnyAsync(f => EF.Functions.ILike(f.Name, filmName) && f.Year == year && f.GuildId == guildChannel.GuildId))
                return new CommandResult(InteractionCommandError.Unsuccessful, "Film already added");

            var user = await DbContext.Participants.FindAsync(UserId);
            if (user == null)
            {
                user = new Participant
                {
                    Id = UserId,
                    JoinedAt = DateTime.UtcNow
                };
                DbContext.Participants.Add(user);
            }

            var film = new Film
            {
                GuildId = guildChannel.GuildId,
                Name = filmName,
                AddedBy = user,
                Year = year,
                AddedAt = DateTime.UtcNow,
                Comment = comment?.RemoveExcessSpaces()
            };

            DbContext.Films.Add(film);

            return new CommandResult($"Added \"{film.Format()}\"");
        }

        [SlashCommand("vote-end", "Complete vote for next film")]
        public async Task<RuntimeResult> CompleteFilmVoting()
        {
            if (!ValidateIfGuild(out var guildChannel, out var result))
                return result;

            var session = await DbContext.Sessions.FirstOrDefaultAsync(s => s.GuildId == guildChannel.GuildId && s.End == null);
            if (session == null)
                return new CommandResult(InteractionCommandError.Unsuccessful, "Voting not started");

            session.End = DateTime.UtcNow;

            return new CommandResult("Voting has completed");
        }

        [SlashCommand("all", "A list of all films added to wish-list")]
        public async Task<RuntimeResult> GetAllFilmsData(
            [Summary("ratings", "Attach ratings for watched films")] bool includeRatings = false,
            [Summary("comments", "Attach stored comments for films")] bool includeComments = false)
        {
            if (!ValidateIfGuild(out var guildChannel, out var result))
                return result;

            var totalFilms = await DbContext.Films.Where(f => f.GuildId == guildChannel.GuildId).CountAsync();
            var embed = await _embeddingFactory.GetListEmbed(DbContext, guildChannel, 1, includeRatings, includeComments);
            var components = _embeddingFactory.GetButtonsForList(GetListComponentsId(includeRatings, includeComments), totalFilms, 1);

            await Context
                .Interaction
                .RespondAsync(embed: embed, components: components);

            return CommandResult.DefaultSuccess;
        }

        [SlashCommand("load", "Load all films formatted for \"Pointauc\"")]
        public async Task<RuntimeResult> LoadFormattedData()
        {
            if (!ValidateIfGuild(out var guildChannel, out var result))
                return result;

            var data = await DbContext
                .Films.Where(f => f.GuildId == guildChannel.GuildId && f.Session == null)
                .Select(f => new
                {
                    name = f.Name,
                    amount = f.Votes!.Select(v => v.Amount).Sum(),
                    id = f.Id.ToString(),
                    fastId = f.Id,
                    extra = (string?)null
                })
                .OrderBy(d => d.fastId)
                .ToListAsync();

            if (data.Count == 0)
                return new CommandResult(InteractionCommandError.Unsuccessful, "Films list is empty");

            await using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, data, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await Context.Interaction.RespondWithFileAsync(stream, $"report-{Guid.NewGuid():N}.json");

            return CommandResult.DefaultSuccess;
        }

        [SlashCommand("rate", "Set personal rating for specified film")]
        public async Task<RuntimeResult> RateFilm(
            [Summary("film-name", "Name of film to vote"), Autocomplete(typeof(FilmNameAutocompleteHandler))] string filmName,
            [Summary("rating", "Your personal rating of specified film"), MinValue(0), MaxValue(10)] double rating)
        {
            if (!ValidateIfGuild(out var guildChannel, out var result))
                return result;

            var user = await DbContext.GetParticipant(UserId);
            var film = await DbContext
                .Films
                .Where(f => f.Name == filmName && f.GuildId == guildChannel.GuildId)
                .Select(f => new
                {
                    Film = f,
                    Rating = f.Ratings!.FirstOrDefault(r => r.ParticipantId == UserId)
                })
                .FirstOrDefaultAsync();

            if (film == null)
                return new CommandResult(InteractionCommandError.Unsuccessful, "Film not found");

            var rate = film.Rating;

            if (rate == null)
            {
                rate = new()
                {
                    Date = DateTime.UtcNow,
                    Participant = user,
                    Rating = rating,
                    Film = film.Film
                };
                DbContext.Ratings.Add(rate);
            }
            else
            {
                rate.Rating = rating;
                rate.Date = DateTime.UtcNow;
            }

            return new CommandResult($"You rate \"{film.Film.Format()}\" as {rating:0.0} ");
        }

        [SlashCommand("vote-start", "Start voting for next film")]
        public async Task<RuntimeResult> StartFilmVoting()
        {
            if (!ValidateIfGuild(out var guildChannel, out var result))
                return result;

            var user = await DbContext.GetParticipant(UserId);
            var session = await DbContext.Sessions.FirstOrDefaultAsync(s => s.GuildId == guildChannel.GuildId && s.End == null);

            if (session != null)
                return new CommandResult(InteractionCommandError.Unsuccessful, "Voting is in progress");

            session = new Session
            {
                Start = DateTime.UtcNow,
                GuildId = guildChannel.GuildId,
                Creator = user
            };

            DbContext.Sessions.Add(session);

            return new CommandResult("Voting started. Place your bets!");
        }

        [SlashCommand("vote", "Place vote for specified film")]
        public async Task<RuntimeResult> VoteForFilm(
            [Summary("film-name", "Name of film for vote"), Autocomplete(typeof(FilmNameAutocompleteHandler))] string filmName,
            [Summary("amount", "Amount of money placed for film. Use 0 to remove vote."), MinValue(0), MaxValue(MaxAmount)] int amount)
        {
            if (!ValidateIfGuild(out var guildChannel, out var result))
                return result;

            var user = await DbContext.GetParticipant(UserId);
            var s = await DbContext.Sessions.Where(s => s.GuildId == guildChannel.GuildId && s.End == null).Select(s => new
            {
                Session = s,
                UserVotes = s.Votes!.Where(v => v.ParticipantId == UserId).ToList()
            }).FirstOrDefaultAsync();

            if (s == null)
                return new CommandResult(InteractionCommandError.Unsuccessful, "Voting not started");

            var sum = s.UserVotes.Sum(v => v.Amount);
            var film = await DbContext.Films.FirstOrDefaultAsync(f => f.Name == filmName && f.GuildId == guildChannel.GuildId);
            if (film == null)
                return new CommandResult(InteractionCommandError.Unsuccessful, "Film not found");

            if (sum >= MaxAmount)
                return new CommandResult(InteractionCommandError.Unsuccessful, "Money limit reached");

            if (sum + amount > MaxAmount)
                return new CommandResult(InteractionCommandError.Unsuccessful, $"Not enough money. Left: {MaxAmount - sum}");

            string r;
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

                DbContext.Votes.Add(existing);
                r = $"You placed {amount} for \"{film.Format()}\"";
            }
            else
            {
                if (amount > 0)
                {
                    existing.Amount += amount;
                    existing.Date = DateTime.UtcNow;
                    r = $"Added to bet. Your total bet for \"{film.Format()}\" is {existing.Amount}";
                }
                else
                {
                    DbContext.Votes.Remove(existing);
                    r = $"Your vote for \"{film.Format()}\" has been reset";
                }
            }

            return new CommandResult(r);
        }

        [SlashCommand("ratings", "Get films ratings")]
        public async Task<RuntimeResult> GetFilmRatings([Summary("filmName", "Name of film"), Autocomplete(typeof(FilmNameAutocompleteHandler))] string filmName)
        { 
            if (!ValidateIfGuild(out var guildChannel, out var result))
                return result;

            var film = await DbContext
                .Films
                .Where(r => r.GuildId == guildChannel.GuildId && EF.Functions.ILike(r.Name, $"%{filmName}%"))
                .Select(f => new
                {
                    Film = f,
                    f.Ratings!.Count,
                    Average = f.Ratings!.Select(r => r.Rating).Average()
                })
                .FirstOrDefaultAsync();

            Embed embed;
            MessageComponent? component = null;
            if (film == null)
            {
                embed = _embeddingFactory.GetNotFoundEmbed($"\"{filmName}\"", "Film not found");
            }
            else
            {
                embed = await _embeddingFactory.GetRatingsEmbed(DbContext, film.Film, film.Count, film.Average, 1);
                component = _embeddingFactory.GetButtonsForList($"films-ratings;{guildChannel.GuildId}", film.Count, 1);
            }

            await Context
                .Interaction
                .RespondAsync(null, embed: embed, components: component);

            return CommandResult.DefaultSuccess;
        }

        public async Task ProcessButtonExecution(SocketMessageComponent component)
        {
            var parsedId = component.Data.CustomId.Split(";", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parsedId.Length == 0)
            {
                await component.RespondAsync("invalid id");
                return;
            }

            switch (parsedId[0])
            {
                case "films-ratings":
                {
                    await ProcessRatingsPaging(component, parsedId);
                    break;
                }
                case "films-list":
                {
                    await ProcessListPaging(component, parsedId);
                    return;
                }
                default:
                {
                    await component.RespondAsync("invalid id");
                    return;
                }
            }
        }

        private async Task ProcessListPaging(SocketMessageComponent component, IReadOnlyList<string> parsedArgs)
        {
            if (component.Channel is not IGuildChannel guildChannel)
            {
                await component.RespondAsync("not in a guild");
                return;
            }
            
            int page;
            bool ratings;
            bool comments;
            try
            {
                ratings = bool.Parse(parsedArgs[1]);
                comments = bool.Parse(parsedArgs[2]);
                page = int.Parse(parsedArgs[3]);
            }
            catch (Exception)
            {
                await component.RespondAsync("invalid id");
                return;
            }

            var totalFilms = await DbContext.Films.Where(f => f.GuildId == guildChannel.GuildId).CountAsync();
            var newEmbed = await _embeddingFactory.GetListEmbed(DbContext, guildChannel, page, ratings, comments);
            var newComponents = _embeddingFactory.GetButtonsForList(GetListComponentsId(ratings, comments), totalFilms, page);

            await component.UpdateAsync(p =>
            {
                p.Embed = newEmbed;
                p.Components = newComponents;
            });
        }

        private async Task ProcessRatingsPaging(SocketMessageComponent component, IReadOnlyList<string> parsedArgs)
        {
            if (component.Channel is not IGuildChannel guildChannel)
            {
                await component.RespondAsync("not in a guild");
                return;
            }

            long filmId;
            int page;
            try
            {
                filmId = long.Parse(parsedArgs[1]);
                page = int.Parse(parsedArgs[2]);
            }
            catch (Exception)
            {
                await component.RespondAsync("invalid id");
                return;
            }

            var film = await DbContext
                .Films
                .Where(r => r.GuildId == guildChannel.GuildId && r.Id == filmId)
                .Select(f => new
                {
                    Film = f,
                    f.Ratings!.Count,
                    Average = f.Ratings!.Select(r => r.Rating).Average()
                })
                .FirstOrDefaultAsync();

            if (film == null)
            {
                await component.RespondAsync("invalid id");
                return;
            }

            var newEmbed = await _embeddingFactory.GetRatingsEmbed(DbContext, film.Film, film.Count, film.Average, page);
            var newComponents = _embeddingFactory.GetButtonsForList(GetRatingsComponentsId(guildChannel.GuildId), film.Count, page);

            await component.UpdateAsync(p =>
            {
                p.Embed = newEmbed;
                p.Components = newComponents;
            });
        }
    }
}