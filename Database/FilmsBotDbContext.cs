#nullable disable
using Microsoft.EntityFrameworkCore;

namespace FilmsBot.Database
{
    public class FilmsBotDbContext : DbContext
    {
        public virtual DbSet<Film> Films { get; set; }
        public virtual DbSet<Participant> Participants { get; set; }
        public virtual DbSet<Session> Sessions { get; set; }
        public virtual DbSet<FilmVote> Votes { get; set; }
        public virtual DbSet<FilmRating> Ratings { get; set; }

        public FilmsBotDbContext(DbContextOptions<FilmsBotDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
        }
    }
}