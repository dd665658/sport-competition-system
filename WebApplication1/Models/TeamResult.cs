using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class TeamResult
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(College))]
        public int CollegeId { get; set; }
        public virtual College? College { get; set; }

        [ForeignKey(nameof(Sport))]
        public int SportId { get; set; }
        public virtual Sport? Sport { get; set; }

        [MaxLength(1)]
        public string Gender { get; set; } = "M"; // M или F

        public int Place { get; set; }
        public int Points { get; set; } // баллы за это место
    }
}
