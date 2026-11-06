using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FluxoCaixaDiario.Lancamentos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Servico = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Operacao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UsuarioOuChave = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DadosEntrada = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DadosSaida = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sucesso = table.Column<bool>(type: "bit", nullable: false),
                    MensagemErro = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OcorridoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DuracaoMs = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CorrelationId",
                table: "AuditLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Operacao",
                table: "AuditLogs",
                column: "Operacao");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_OcorridoEm",
                table: "AuditLogs",
                column: "OcorridoEm");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AuditLogs");
        }
    }
}