using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WlcSistemaPedidos.Migrations
{
    /// <inheritdoc />
    public partial class CriarAgendaPagamentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgendamentosPagamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Beneficiario = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Categoria = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    DataVencimento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Recorrente = table.Column<bool>(type: "boolean", nullable: false),
                    Frequencia = table.Column<int>(type: "integer", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    Pago = table.Column<bool>(type: "boolean", nullable: false),
                    DataPagamento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DataCadastro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsuarioResponsavelId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgendamentosPagamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgendamentosPagamentos_AspNetUsers_UsuarioResponsavelId",
                        column: x => x.UsuarioResponsavelId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgendamentosPagamentos_Ativo_Pago_DataVencimento",
                table: "AgendamentosPagamentos",
                columns: new[] { "Ativo", "Pago", "DataVencimento" });

            migrationBuilder.CreateIndex(
                name: "IX_AgendamentosPagamentos_Categoria_DataVencimento",
                table: "AgendamentosPagamentos",
                columns: new[] { "Categoria", "DataVencimento" });

            migrationBuilder.CreateIndex(
                name: "IX_AgendamentosPagamentos_DataVencimento",
                table: "AgendamentosPagamentos",
                column: "DataVencimento");

            migrationBuilder.CreateIndex(
                name: "IX_AgendamentosPagamentos_UsuarioResponsavelId",
                table: "AgendamentosPagamentos",
                column: "UsuarioResponsavelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgendamentosPagamentos");
        }
    }
}
