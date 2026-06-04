using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class College
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1)]
        public string Group { get; set; } = "A"; // A или B

        // Внешний ключ на пользователя (будет заполняться позже)
        public string? UserId { get; set; }

        // Навигационные свойства (связи)
        // public virtual ICollection<Application> Applications { get; set; }
        // public virtual ICollection<Participant> Participants { get; set; }
        // пока оставим закомментированными, добавим когда создадим другие модели
    }
}
