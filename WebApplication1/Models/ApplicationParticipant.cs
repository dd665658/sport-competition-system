using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class ApplicationParticipant
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Application))]
        public int ApplicationId { get; set; }
        public virtual Application? Application { get; set; }

        [Required, MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        public DateTime BirthDate { get; set; }

        [MaxLength(1)]
        public string Gender { get; set; } = "M"; // M или F

        public int? SportNumber { get; set; } // номер, который указал представитель (может быть null, если не заполнил)
    }
}
