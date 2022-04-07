using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FilmsBot.Database
{
    [Table("SESSIONS")]
    public class Session
    {
        [Column("ID")]
        public long Id { get; set; }

        [Column("START")]
        public DateTime Start { get; set; }

        [Column("END")]
        public DateTime? End { get; set; }

        [Column("FILM_ID")]
        public long? FilmId { get; set; }

        [Column("GUILD_ID")]
        public ulong GuildId { get; set; }

        [Column("CREATED_BY")]
        public ulong CreatedBy { get; set; }

        #region Relations
        
        public virtual List<FilmVote>? Votes { get; set; }
        public virtual Film? Film { get; set; }
        public virtual Participant? Creator { get; set; }

        #endregion

        #region Configuration

        public class Configuration : IEntityTypeConfiguration<Session>
        {
            public void Configure(EntityTypeBuilder<Session> builder)
            {
                builder
                    .HasOne(s => s.Film)
                    .WithOne(f => f.Session)
                    .HasForeignKey<Session>(s => s.FilmId)
                    .HasPrincipalKey<Film>(f => f.Id)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_FILM_SESSION_FILM_ID");

                builder
                    .HasOne(s => s.Creator)
                    .WithMany(p => p.Sessions)
                    .HasForeignKey(s => s.CreatedBy)
                    .HasPrincipalKey(p => p.Id)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_FILM_SESSION_CREATOR_ID");
            }
        }

        #endregion
    }
}