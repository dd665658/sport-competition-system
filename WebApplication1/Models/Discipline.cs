using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models;

public class Discipline
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Sport))]
    public int SportId { get; set; }
    public virtual Sport? Sport { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int Order { get; set; } = 0;

    [MaxLength(20)]
    public string ScoringType { get; set; } = "place"; // place или points

    [MaxLength(20)]
    public string? Unit { get; set; } // сек, раз, м

    // Навигационные свойства
    public virtual ICollection<IndividualResult>? IndividualResults { get; set; }
}