using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FluxoCaixaDiario.Lancamentos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLancamentoAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Lancamentos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Lancamentos",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Lancamentos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Lancamentos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Lancamentos");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Lancamentos");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Lancamentos");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Lancamentos");
        }
    }
}
