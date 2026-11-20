using Microsoft.EntityFrameworkCore;
using ReservaEspacios.Domain.Espacios;

namespace ReservaEspacios.Infrastructure.Persistence;

public sealed class ReservaEspaciosDbContext(DbContextOptions<ReservaEspaciosDbContext> options)
    : DbContext(options)
{
    public DbSet<Space> Spaces => Set<Space>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Space>(entity =>
        {
            entity.ToTable("spaces");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Nombre).HasColumnName("nombre").IsRequired();
            entity.HasIndex(x => x.Nombre).IsUnique().HasDatabaseName("ux_spaces_nombre");
            entity.Property(x => x.Tipo).HasColumnName("tipo").HasConversion<string>();
            entity.Property(x => x.Capacidad).HasColumnName("capacidad");
            entity.Property(x => x.Ubicacion).HasColumnName("ubicacion");
            entity.Property(x => x.Activo).HasColumnName("activo");

            entity.OwnsOne(x => x.Horario, horario =>
            {
                horario.Property(x => x.Apertura)
                    .HasColumnName("hora_apertura")
                    .HasColumnType("time");
                horario.Property(x => x.Cierre)
                    .HasColumnName("hora_cierre")
                    .HasColumnType("time");
            });

            entity.ToTable(table => table.HasCheckConstraint(
                "ck_spaces_horario",
                "\"hora_apertura\" < \"hora_cierre\""));
        });
    }
}
