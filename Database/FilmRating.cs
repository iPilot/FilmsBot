using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FilmsBot.Database
{
    [Table("RATINGS")]
    public class FilmRating 
    {
        [Key]
        [Column("ID")]
        public long Id { get; set; }

        [Column("PARTICIPANT_ID")]
        public ulong ParticipantId { get; set; }

        [Column("FILM_ID")]
        public long FilmId { get; set; }

        [Column("RATING")]
        public double Rating { get; set; }

        [Column("DATE")]
        public DateTime Date { get; set; }

        #region Relations

        public virtual Participant? Participant { get; set; }
        public virtual Film? Film { get; set; }

        #endregion

        #region Configuration

        public class Configuration : IEntityTypeConfiguration<FilmRating>
        {
            public void Configure(EntityTypeBuilder<FilmRating> builder)
            {
                builder
                    .HasOne(r => r.Participant)
                    .WithMany(p => p.Ratings)
                    .HasForeignKey(r => r.ParticipantId)
                    .HasPrincipalKey(p => p.Id)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_FILM_RATING_PARTICIPANT_ID");

                builder
                    .HasOne(r => r.Film)
                    .WithMany(f => f.Ratings)
                    .HasForeignKey(r => r.FilmId)
                    .HasPrincipalKey(f => f.Id)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired()
                    .HasConstraintName("FK_FILM_RATING_FILM_ID");
            }
        }

        #endregion
    }
}