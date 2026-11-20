using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ReservaEspacios.Domain.Espacios;
using ReservaEspacios.Domain.Usuarios;
using ReservaEspacios.Infrastructure.Identity;

namespace ReservaEspacios.Infrastructure.Persistence;

public sealed class ReservaEspaciosDbContext(DbContextOptions<ReservaEspaciosDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<Space> Spaces => Set<Space>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Identity configura primero sus propias tablas y relaciones.
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("users");
            entity.Property(x => x.Nombre).HasColumnName("nombre").IsRequired();
            entity.Property(x => x.Activo).HasColumnName("activo");
        });
        modelBuilder.Entity<ApplicationRole>(entity =>
        {
            entity.ToTable("roles");
            entity.Property(x => x.Etiqueta).HasColumnName("etiqueta").IsRequired();
        });
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");

        SembrarRoles(modelBuilder);

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

    /// <summary>
    /// Semilla canónica: exactamente tres roles (tarea 1.1). Los IDs son fijos para
    /// que la migración sea reproducible y las pruebas puedan referirse a ellos.
    /// </summary>
    private static void SembrarRoles(ModelBuilder modelBuilder)
    {
        Rol[] roles = [Rol.Solicitante, Rol.Coordinador, Rol.Administrador];
        modelBuilder.Entity<ApplicationRole>().HasData(roles.Select((rol, indice) => new ApplicationRole
        {
            Id = IdsRoles[indice],
            Name = rol.Codigo(),
            NormalizedName = rol.Codigo(),
            Etiqueta = rol.Etiqueta(),
            ConcurrencyStamp = $"seed-{rol.Codigo()}"
        }));
    }

    private static readonly Guid[] IdsRoles =
    [
        new("11111111-1111-1111-1111-111111111111"),
        new("22222222-2222-2222-2222-222222222222"),
        new("33333333-3333-3333-3333-333333333333")
    ];
}
