using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WlcSistemaPedidos.Migrations
{
    /// <inheritdoc />
    public partial class CriarLembretesPedidos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LembretesPedidos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClienteId = table.Column<int>(type: "integer", nullable: false),
                    NomeCliente = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Mensagem = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DataHoraAgendada = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataCadastro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataEnvio = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Enviado = table.Column<bool>(type: "boolean", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    UsuarioResponsavelId = table.Column<int>(type: "integer", nullable: true),
                    ErroEnvio = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LembretesPedidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LembretesPedidos_AspNetUsers_UsuarioResponsavelId",
                        column: x => x.UsuarioResponsavelId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LembretesPedidos_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LembretesPedidos_Ativo_Enviado_DataHoraAgendada",
                table: "LembretesPedidos",
                columns: new[] { "Ativo", "Enviado", "DataHoraAgendada" });

            migrationBuilder.CreateIndex(
                name: "IX_LembretesPedidos_ClienteId",
                table: "LembretesPedidos",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_LembretesPedidos_UsuarioResponsavelId",
                table: "LembretesPedidos",
                column: "UsuarioResponsavelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LembretesPedidos");
        }
    }
}
