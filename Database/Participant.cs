using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FilmsBot.Database
{
    [Table("PARTICIPANTS")]
    public class Participant
    {
        [Key]
        [Column("ID")]
        public ulong Id { get; set; }

        [Column("JOINED_AT")]
        public DateTime JoinedAt { get; set; }

        #region Relations

        public virtual List<Film>? Films { get; set; }
        public virtual List<FilmVote>? Votes { get; set; }
        public virtual List<FilmRating>? Ratings { get; set; }
        public virtual List<Session>? Sessions { get; set; }

        #endregion

        #region Configuration

        public class Configuration : IEntityTypeConfiguration<Participant>
        {
            public void Configure(EntityTypeBuilder<Participant> builder)
            {
            }
        }

        #endregion
    }
}