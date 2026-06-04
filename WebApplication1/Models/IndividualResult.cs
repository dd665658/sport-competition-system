using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models;

public class IndividualResult
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Participant))]
    public int ParticipantId { get; set; }
    public virtual Participant? Participant { get; set; }

    [ForeignKey(nameof(Discipline))]
    public int DisciplineId { get; set; }
    public virtual Discipline? Discipline { get; set; }

    [Required, MaxLength(50)]
    public string ResultValue { get; set; } = string.Empty; // время, количество раз и т.д.

    public int? Place { get; set; } // место (для видов с сортировкой)

    public int? Points { get; set; } // баллы

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}