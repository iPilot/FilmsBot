#nullable disable
using System.Text;
using FilmsBot.Database;

namespace FilmsBot.Extensions
{
    public static class Extensions
    {
        public static async Task<Participant> GetParticipant(this FilmsBotDbContext db, ulong userId)
        {
            var p = await db.Participants.FindAsync(userId);

            if (p == null)
            {
                p = new Participant
                {
                    Id = userId,
                    JoinedAt = DateTime.UtcNow
                };
                db.Participants.Add(p);
                await db.SaveChangesAsync();
            }

            return p;
        }
        
        public static string Format(this IEnumerable<Film> films, bool includeRating, bool includeComments)
        {
            var sb = new StringBuilder();

            foreach (var film in films)
            {
                sb.Append(film.Name);
                if (film.Year.HasValue)
                    sb.Append($" ({film.Year.Value})");

                if (includeRating && film.Ratings is { Count: > 0 })
                {
                    var avg = film.Ratings.Average(r => r.Rating);
                    sb.Append($" --- [ {avg:0.0} ] ---");
                }

                if (includeComments && !string.IsNullOrWhiteSpace(film.Comment))
                    sb.AppendFormat($" || {film.Comment} ||");

                sb.AppendLine();
            }

            return sb.ToString();
        }

        public static string Format(this Film film) => film.Year.HasValue ? $"{film.Name} ({film.Year})" : film.Name;
    }
}