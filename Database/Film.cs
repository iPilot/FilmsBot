using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FilmsBot.Database
{
    [Table("FILMS")]
    public class Film
    {
        [Key]
        [Column("ID")]
        public long Id { get; set; }

        [Required]
        [Column("NAME", TypeName = "citext")]
        public string Name { get; set; } = null!;

        [Column("COMMENT")]
        public string? Comment { get; set; }

        [Column("YEAR")]
        public int? Year { get; set; }

        [Column("ADDED_BY_ID")]
        public ulong AddedById { get; set; }

        [Column("GUILD_ID")]
        public ulong GuildId { get; set; }

        [Column("ADDED_AT")]
        public DateTime AddedAt { get; set; }

        #region Relations

        public virtual Participant? AddedBy { get; set; }
        public virtual List<FilmVote>? Votes { get; set; }
        public virtual Session? Session { get; set; }
        public virtual List<FilmRating>? Ratings { get; set; }

        #endregion

        #region Configuration

        public class Configuration : IEntityTypeConfiguration<Film>
        {
            public void Configure(EntityTypeBuilder<Film> builder)
            {
                builder
                    .HasOne(f => f.AddedBy)
                    .WithMany(p => p.Films)
                    .HasForeignKey(f => f.AddedById)
                    .HasPrincipalKey(p => p.Id)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);
            }
        }

        #endregion
    }
}