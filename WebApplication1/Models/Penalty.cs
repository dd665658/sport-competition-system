using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class Penalty
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(College))]
        public int CollegeId { get; set; }
        public virtual College? College { get; set; }

        [ForeignKey(nameof(Sport))]
        public int SportId { get; set; }
        public virtual Sport? Sport { get; set; }

        [MaxLength(20)]
        public string PenaltyType { get; set; } = string.Empty; // non-participation, falsification

        public int Points { get; set; } // отрицательные баллы

        [MaxLength(500)]
        public string? Reason { get; set; }
    }
}
