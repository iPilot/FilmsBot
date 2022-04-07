using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FilmsBot.Database
{
    [Table("VOTES")]
    public class FilmVote
    {
        [Key]
        [Column("ID")]
        public long Id { get; set; }

        [Column("FILM_ID")]
        public long FilmId { get; set; }

        [Column("PARTICIPANT_ID")]
        public ulong ParticipantId { get; set; }

        [Column("SESSION_ID")]
        public long SessionId { get; set; }

        [Column("AMOUNT")]
        public int Amount { get; set; }

        [Column("DATE")]
        public DateTime Date { get; set; }

        #region Relations

        public virtual Participant? Participant { get; set; }
        public virtual Film? Film { get; set; }
        public virtual Session? Session { get; set; }

        #endregion

        #region Configuration

        public class Configuration : IEntityTypeConfiguration<FilmVote>
        {
            public void Configure(EntityTypeBuilder<FilmVote> builder)
            {
                builder
                    .HasOne(v => v.Participant)
                    .WithMany(p => p.Votes)
                    .HasForeignKey(v => v.ParticipantId)
                    .HasPrincipalKey(p => p.Id)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired()
                    .HasConstraintName("FK_FILM_VOTE_PARTICIPANT_ID");

                builder
                    .HasOne(v => v.Film)
                    .WithMany(f => f.Votes)
                    .HasForeignKey(v => v.FilmId)
                    .HasPrincipalKey(f => f.Id)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired()
                    .HasConstraintName("FK_FILM_VOTE_FILM_ID");

                builder
                    .HasOne(v => v.Session)
                    .WithMany(s => s.Votes)
                    .HasForeignKey(v => v.SessionId)
                    .HasPrincipalKey(s => s.Id)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired()
                    .HasConstraintName("FK_FILM_VOTE_SESSION_ID");
            }
        }

        #endregion
    }
}