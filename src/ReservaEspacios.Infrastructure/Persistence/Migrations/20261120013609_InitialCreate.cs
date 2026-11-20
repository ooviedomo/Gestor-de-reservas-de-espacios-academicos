using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReservaEspacios.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Migration Plan del change: la extensión se necesita en el siguiente change
            // para la restricción de exclusión sobre reservas.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");

            migrationBuilder.CreateTable(
                name: "spaces",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: false),
                    capacidad = table.Column<int>(type: "integer", nullable: false),
                    ubicacion = table.Column<string>(type: "text", nullable: false),
                    hora_apertura = table.Column<TimeOnly>(type: "time", nullable: false),
                    hora_cierre = table.Column<TimeOnly>(type: "time", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spaces", x => x.id);
                    table.CheckConstraint("ck_spaces_horario", "\"hora_apertura\" < \"hora_cierre\"");
                });

            migrationBuilder.CreateIndex(
                name: "ux_spaces_nombre",
                table: "spaces",
                column: "nombre",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "spaces");
        }
    }
}
