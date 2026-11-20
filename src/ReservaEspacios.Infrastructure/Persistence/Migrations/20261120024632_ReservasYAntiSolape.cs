using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReservaEspacios.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReservasYAntiSolape : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reservations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    space_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inicio = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fin = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    proposito = table.Column<string>(type: "text", nullable: false),
                    asistentes = table.Column<int>(type: "integer", nullable: false),
                    estado = table.Column<string>(type: "text", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    aprobado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    aprobado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    motivo_rechazo = table.Column<string>(type: "text", nullable: true),
                    motivo_cancelacion = table.Column<string>(type: "text", nullable: true),
                    cancelado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reservations", x => x.id);
                    table.CheckConstraint("ck_reservations_rango", "\"inicio\" < \"fin\"");
                });

            migrationBuilder.CreateIndex(
                name: "ix_reservations_space_id",
                table: "reservations",
                column: "space_id");

            // Doble barrera anti-solapes (plan §3.3): `btree_gist` permite comparar
            // space_id con = dentro de un índice GiST; la expresión tstzrange usa
            // intervalos semiabiertos [inicio, fin) y el WHERE excluye estados terminales.
            // La propia restricción crea el índice GiST que la soporta.
            migrationBuilder.Sql("""
                ALTER TABLE reservations
                ADD CONSTRAINT no_overlap
                EXCLUDE USING gist (
                    space_id WITH =,
                    tstzrange(inicio, fin, '[)') WITH &&
                ) WHERE (estado IN ('PENDIENTE', 'APROBADA'));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE reservations DROP CONSTRAINT IF EXISTS no_overlap;");

            migrationBuilder.DropTable(
                name: "reservations");
        }
    }
}
