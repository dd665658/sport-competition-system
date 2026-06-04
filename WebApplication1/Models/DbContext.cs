namespace WebApplication1.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;


public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    public DbSet<College> Colleges { get; set; }
    public DbSet<Sport> Sports { get; set; }
    public DbSet<Application> Applications { get; set; }
    public DbSet<ApplicationParticipant> ApplicationParticipants { get; set; }
    public DbSet<Participant> Participants { get; set; }
    public DbSet<IndividualResult> IndividualResults { get; set; }
    public DbSet<TeamResult> TeamResults { get; set; }
    public DbSet<Penalty> Penalties { get; set; }
    public DbSet<Discipline> Disciplines { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // обязательно вызывать для Identity

        // Настройка связей и индексов
        builder.Entity<Participant>()
            .HasIndex(p => new { p.SportId, p.SportNumber })
            .IsUnique();

        builder.Entity<TeamResult>()
            .HasIndex(tr => new { tr.CollegeId, tr.SportId, tr.Gender })
            .IsUnique();

        builder.Entity<Application>()
            .HasIndex(a => new { a.CollegeId, a.SportId, a.Status });

        // Настройка для IndividualResult (связь с Participant)
        builder.Entity<IndividualResult>()
            .HasOne(ir => ir.Participant)
            .WithMany(p => p.IndividualResults)
            .HasForeignKey(ir => ir.ParticipantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Настройка для IndividualResult (связь с Discipline) — добавляем это!
        builder.Entity<IndividualResult>()
            .HasOne(ir => ir.Discipline)
            .WithMany(d => d.IndividualResults)
            .HasForeignKey(ir => ir.DisciplineId)
            .OnDelete(DeleteBehavior.NoAction); // БЕЗ каскада, чтобы избежать ошибки

        // Индексы для IndividualResult (для скорости)
        builder.Entity<IndividualResult>()
            .HasIndex(ir => ir.ParticipantId);

        builder.Entity<IndividualResult>()
            .HasIndex(ir => ir.DisciplineId);
    }
}
