using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class Application // заявки
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
        public string Status { get; set; } = "draft"; // draft, submitted, approved, rejected, rework

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [MaxLength(500)]
        public string? AdminComment { get; set; }

        // Связь с участниками заявки
        public virtual ICollection<ApplicationParticipant>? Participants { get; set; }
    }
}
