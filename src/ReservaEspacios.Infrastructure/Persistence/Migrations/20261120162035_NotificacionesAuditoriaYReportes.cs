using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReservaEspacios.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NotificacionesAuditoriaYReportes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entidad = table.Column<string>(type: "text", nullable: false),
                    entidad_id = table.Column<Guid>(type: "uuid", nullable: false),
                    accion = table.Column<string>(type: "text", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_rol = table.Column<string>(type: "text", nullable: true),
                    ocurrido_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    metadata = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reserva_id = table.Column<Guid>(type: "uuid", nullable: false),
                    canal = table.Column<string>(type: "text", nullable: false),
                    destinatario = table.Column<string>(type: "text", nullable: false),
                    asunto = table.Column<string>(type: "text", nullable: false),
                    cuerpo = table.Column<string>(type: "text", nullable: false),
                    estado = table.Column<string>(type: "text", nullable: false),
                    intentos = table.Column<int>(type: "integer", nullable: false),
                    ultimo_error = table.Column<string>(type: "text", nullable: true),
                    enviado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    proximo_intento_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_actor_id",
                table: "audit_logs",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_entidad",
                table: "audit_logs",
                column: "entidad");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_entidad_id",
                table: "audit_logs",
                column: "entidad_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_ocurrido_en",
                table: "audit_logs",
                column: "ocurrido_en");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_estado",
                table: "notifications",
                column: "estado");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_proximo_intento",
                table: "notifications",
                column: "proximo_intento_en");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_reserva_id",
                table: "notifications",
                column: "reserva_id");

            // AUD-001: inmutabilidad en la propia base de datos. La restricción no depende
            // de que el usuario de aplicación sea propietario de la tabla (ADR-003).
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION evitar_modificacion_auditoria()
                RETURNS trigger AS $$
                BEGIN
                  RAISE EXCEPTION 'AUDITORIA_INMUTABLE';
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER audit_logs_inmutable
                BEFORE UPDATE OR DELETE ON audit_logs
                FOR EACH ROW EXECUTE FUNCTION evitar_modificacion_auditoria();

                REVOKE UPDATE, DELETE ON audit_logs FROM PUBLIC;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS audit_logs_inmutable ON audit_logs;
                DROP FUNCTION IF EXISTS evitar_modificacion_auditoria();
                """);

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "notifications");
        }
    }
}
