using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WlcSistemaPedidos.Migrations
{
    /// <inheritdoc />
    public partial class CriarModuloNotaFiscal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotasFiscais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PedidoId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    NumeroNota = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Serie = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ChaveAcesso = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Protocolo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProvedorFiscal = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IdentificadorExterno = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UrlPdf = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UrlXml = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataEnvio = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DataAutorizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DataCancelamento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetornoFiscal = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ErroFiscal = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    UsuarioResponsavelId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotasFiscais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotasFiscais_AspNetUsers_UsuarioResponsavelId",
                        column: x => x.UsuarioResponsavelId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_NotasFiscais_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotasFiscais_PedidoId",
                table: "NotasFiscais",
                column: "PedidoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotasFiscais_Status_DataCriacao",
                table: "NotasFiscais",
                columns: new[] { "Status", "DataCriacao" });

            migrationBuilder.CreateIndex(
                name: "IX_NotasFiscais_UsuarioResponsavelId",
                table: "NotasFiscais",
                column: "UsuarioResponsavelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotasFiscais");
        }
    }
}
