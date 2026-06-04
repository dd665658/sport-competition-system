using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class Participant
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(College))]
        public int CollegeId { get; set; }
        public virtual College? College { get; set; }

        [ForeignKey(nameof(Sport))]
        public int SportId { get; set; }
        public virtual Sport? Sport { get; set; }

        [Required, MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        public DateTime BirthDate { get; set; }

        [MaxLength(1)]
        public string Gender { get; set; } = "M";

        public int SportNumber { get; set; } // уникальный номер

        public bool IsRepublicParticipant { get; set; } = false; // бонус 3 очка

        // Связь с результатами (индивидуальными)
        public virtual ICollection<IndividualResult>? IndividualResults { get; set; }
    }
}
