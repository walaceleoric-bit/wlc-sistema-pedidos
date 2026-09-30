using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WlcSistemaPedidos.Migrations
{
    /// <inheritdoc />
    public partial class TransformarLembretesEmRecorrentes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LembretesPedidos_Ativo_Enviado_DataHoraAgendada",
                table: "LembretesPedidos");

            migrationBuilder.DropColumn(
                name: "Enviado",
                table: "LembretesPedidos");

            migrationBuilder.RenameColumn(
                name: "DataHoraAgendada",
                table: "LembretesPedidos",
                newName: "ProximoEnvio");

            migrationBuilder.RenameColumn(
                name: "DataEnvio",
                table: "LembretesPedidos",
                newName: "UltimoEnvio");

            migrationBuilder.AddColumn<int>(
                name: "DiaSemana",
                table: "LembretesPedidos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "Horario",
                table: "LembretesPedidos",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.CreateIndex(
                name: "IX_LembretesPedidos_Ativo_ProximoEnvio",
                table: "LembretesPedidos",
                columns: new[] { "Ativo", "ProximoEnvio" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LembretesPedidos_Ativo_ProximoEnvio",
                table: "LembretesPedidos");

            migrationBuilder.DropColumn(
                name: "DiaSemana",
                table: "LembretesPedidos");

            migrationBuilder.DropColumn(
                name: "Horario",
                table: "LembretesPedidos");

            migrationBuilder.RenameColumn(
                name: "UltimoEnvio",
                table: "LembretesPedidos",
                newName: "DataEnvio");

            migrationBuilder.RenameColumn(
                name: "ProximoEnvio",
                table: "LembretesPedidos",
                newName: "DataHoraAgendada");

            migrationBuilder.AddColumn<bool>(
                name: "Enviado",
                table: "LembretesPedidos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_LembretesPedidos_Ativo_Enviado_DataHoraAgendada",
                table: "LembretesPedidos",
                columns: new[] { "Ativo", "Enviado", "DataHoraAgendada" });
        }
    }
}
