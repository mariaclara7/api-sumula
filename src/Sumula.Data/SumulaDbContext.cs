using Microsoft.EntityFrameworkCore;
using Sumula.Data.Entidades;

namespace Sumula.Data;

public class SumulaDbContext(DbContextOptions<SumulaDbContext> options) : DbContext(options)
{
    public DbSet<TimeEntidade> Times => Set<TimeEntidade>();
    public DbSet<ParticipacaoEntidade> Participacoes => Set<ParticipacaoEntidade>();
    public DbSet<PartidaEntidade> Partidas => Set<PartidaEntidade>();
    public DbSet<ArtilheiroEntidade> Artilheiros => Set<ArtilheiroEntidade>();
    public DbSet<ColetaEntidade> Coletas => Set<ColetaEntidade>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TimeEntidade>(e =>
        {
            e.ToTable("times");
            e.Property(t => t.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<ParticipacaoEntidade>(e =>
        {
            e.ToTable("participacoes");
            e.HasKey(p => new { p.Competicao, p.Temporada, p.TimeId });
        });

        modelBuilder.Entity<PartidaEntidade>(e =>
        {
            e.ToTable("partidas");
            e.Property(p => p.Id).ValueGeneratedNever();
            e.Property(p => p.Status).HasConversion<string>();
            e.HasIndex(p => new { p.Competicao, p.Temporada });
            e.HasOne<TimeEntidade>().WithMany().HasForeignKey(p => p.MandanteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<TimeEntidade>().WithMany().HasForeignKey(p => p.VisitanteId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ArtilheiroEntidade>(e =>
        {
            e.ToTable("artilheiros");
            e.HasKey(a => new { a.Competicao, a.Temporada, a.JogadorId });
        });

        modelBuilder.Entity<ColetaEntidade>(e =>
        {
            e.ToTable("coletas");
            e.HasIndex(c => new { c.Competicao, c.Temporada, c.ExecutadaEm });
        });
    }
}
