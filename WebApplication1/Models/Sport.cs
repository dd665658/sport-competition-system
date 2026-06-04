using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Sport
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(20)]
        public string SportType { get; set; } = "individual"; // individual / team

        public int ParticipantsMale { get; set; }
        public int ParticipantsFemale { get; set; }
        public int ParticipantsStaff { get; set; }
    }
}
