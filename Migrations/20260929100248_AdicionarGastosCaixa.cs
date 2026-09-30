using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WlcSistemaPedidos.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarGastosCaixa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GastosCaixa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Categoria = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    DataGasto = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsuarioResponsavelId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GastosCaixa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GastosCaixa_AspNetUsers_UsuarioResponsavelId",
                        column: x => x.UsuarioResponsavelId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GastosCaixa_Categoria_DataGasto",
                table: "GastosCaixa",
                columns: new[] { "Categoria", "DataGasto" });

            migrationBuilder.CreateIndex(
                name: "IX_GastosCaixa_DataGasto",
                table: "GastosCaixa",
                column: "DataGasto");

            migrationBuilder.CreateIndex(
                name: "IX_GastosCaixa_UsuarioResponsavelId",
                table: "GastosCaixa",
                column: "UsuarioResponsavelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GastosCaixa");
        }
    }
}
