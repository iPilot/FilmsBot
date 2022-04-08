﻿using System.Text.Json;
using Discord.Interactions;
using FilmsBot.Commands.Abstractions;
using FilmsBot.Database;
using FilmsBot.Extensions;
using Microsoft.EntityFrameworkCore;

namespace FilmsBot.Commands
{
    [Group(ConstName, ConstDesc)]
    public class FilmsInteractionModule : DbInteractionSubCommand<FilmsBotDbContext>
    {
        private const string ConstName = "films";
        private const string ConstDesc = "Cooperative films watch management";
        private const int MaxAmount = 1000;

        public FilmsInteractionModule(IServiceScopeFactory scopeFactory) : base(scopeFactory)
        {
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

            return CommandResult.Success;
        }

        [SlashCommand("add", "Addition a new films to watch wishlist")]
        public async Task<RuntimeResult> AddFilm(
            [Summary("film-name", "A new film name")] string filmName,
            [Summary("film-year", "The year of film release"), MinValue(1900), MaxValue(2100)] int? year = null,
            [Summary("comment", "Additional info")] string? comment = null)
        {
            if (!ValidateIfGuild(out var guildChannel, out var result))
                return result;

            if (await DbContext.Films.AnyAsync(f => f.Name == filmName && f.Year == year && f.GuildId == guildChannel.GuildId))
                return new CommandResult(InteractionCommandError.Unsuccessful, "Already added");

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
                Comment = comment
            };

            DbContext.Films.Add(film);

            return CommandResult.Success;
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

            return CommandResult.Success;
        }

        [SlashCommand("all", "A list of all films added to wish-list")]
        public async Task<RuntimeResult> GetAllFilmsData(
            [Summary("include-ratings", "Attach ratings for watched films")] bool includeRatings = false,
            [Summary("include-comments", "Attach stored comments for films")] bool includeComments = false)
        {
            if (!ValidateIfGuild(out var guildChannel, out var result))
                return result;

            var films = await DbContext
                .Films
                .Where(f => f.GuildId == guildChannel.GuildId)
                .OrderBy(f => f.AddedAt)
                .ToListAsync();

            if (films.Count == 0)
            {
                return new CommandResult(InteractionCommandError.Unsuccessful, "Films not registered");
            }

            await Context.Interaction.RespondAsync(films.Format(includeComments, includeRatings));

            return CommandResult.Success;
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

            return CommandResult.Success;
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

            return CommandResult.Success;
        }

        [SlashCommand("vote-start", "Start voting for next film")]
        public async Task<RuntimeResult> StartFilmVoting()
        {
            if (!ValidateIfGuild(out var guildChannel, out var result))
                return result;

            var user = await DbContext.GetParticipant(UserId);
            var session = await DbContext.Sessions.FirstOrDefaultAsync(s => s.GuildId == guildChannel.GuildId && s.End == null);

            if (session != null)
                return new CommandResult(InteractionCommandError.Unsuccessful, "Голосование уже идет");

            session = new Session
            {
                Start = DateTime.UtcNow,
                GuildId = guildChannel.GuildId,
                Creator = user
            };
            DbContext.Sessions.Add(session);

            return CommandResult.Success;
        }

        [SlashCommand("vote", "Place vote for specified film")]
        public async Task<RuntimeResult> Handle(
            [Summary("film-name", "Name of film for vote"), Autocomplete(typeof(FilmNameAutocompleteHandler))] string filmName,
            [Summary("amount", "Amount of money placed for film"), MinValue(0), MaxValue(MaxAmount)] int amount)
        {
            if (!ValidateIfGuild(out var guildChannel, out var result))
                return result;

            var user = await DbContext.GetParticipant(UserId);
            var s = await DbContext.Sessions.Where(s => s.GuildId == guildChannel.GuildId).Select(s => new
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
            }
            else
            {
                if (amount > 0)
                {
                    existing.Amount += amount;
                    existing.Date = DateTime.UtcNow;
                }
                else
                {
                    DbContext.Votes.Remove(existing);
                }
            }

            return CommandResult.Success;
        }
    }
}