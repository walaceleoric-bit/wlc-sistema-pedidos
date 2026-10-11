using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WlcSistemaPedidos.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarConfiguracaoPix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChavePix",
                table: "ConfiguracoesSistema",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NomeRecebedorPix",
                table: "ConfiguracoesSistema",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChavePix",
                table: "ConfiguracoesSistema");

            migrationBuilder.DropColumn(
                name: "NomeRecebedorPix",
                table: "ConfiguracoesSistema");
        }
    }
}
