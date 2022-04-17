#nullable disable
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
        
        public static string RemoveExcessSpaces(this string s)
        {
            return string.Join(' ', s.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        public static string Format(this Film film) => film.Year.HasValue ? $"{film.Name} ({film.Year})" : film.Name;
    }
}