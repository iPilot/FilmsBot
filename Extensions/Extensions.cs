#nullable disable
using Discord;
using Discord.WebSocket;
using FilmsBot.Database;

namespace FilmsBot.Extensions
{
    public static class Extensions
    {
        public static T GetOptionValue<T>(this SocketSlashCommandDataOption data, string name)
        {
            var obj = data.Options.FirstOrDefault(o => o.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (obj?.Value == null)
                return default;

            return MapByAppType<T>(obj.Type, obj.Value);
        }

        public static async Task<Participant> GetParticipant(this FilmsBotDbContext db, IUser user)
        {
            var p = await db.Participants.FindAsync(user.Id);

            if (p == null)
            {
                p = new Participant
                {
                    Id = user.Id,
                    JoinedAt = DateTime.UtcNow
                };
                db.Participants.Add(p);
                await db.SaveChangesAsync();
            }

            return p;
        }

        public static T MapByAppType<T>(ApplicationCommandOptionType type, object value)
        {
            return type switch
            {
                ApplicationCommandOptionType.String => IsOfType<T>(typeof(string)) 
                    ? Map<T>(value) 
                    : throw new ArgumentException(nameof(value)),
                ApplicationCommandOptionType.Integer => IsOfType<T>(typeof(int), typeof(int?), typeof(long), typeof(long?)) 
                    ? Map<T>((long)value)
                    : throw new ArgumentException(nameof(value)),
                ApplicationCommandOptionType.Boolean => IsOfType<T>(typeof(bool), typeof(bool?)) 
                    ? Map<T>(value) 
                    : throw new ArgumentException(nameof(value)),
                ApplicationCommandOptionType.User => IsOfType<T>(typeof(SocketUser)) 
                    ? Map<T>(value) 
                    : throw new ArgumentException(nameof(value)),
                ApplicationCommandOptionType.Number => IsOfType<T>(typeof(double), typeof(double?)) 
                    ? Map<T>(value) 
                    : throw new ArgumentException(nameof(value)),
                _ => throw new ArgumentException(nameof(value))
            };
        }

        private static T Map<T>(object v) => (T)v;
        private static T Map<T>(long v) => typeof(T) == typeof(long) ? (T)(object)v : (T)(object) Convert.ToInt32(v);
        private static bool IsOfType<T>(params Type[] types) => types.Any(t => t == typeof(T));
    }
}