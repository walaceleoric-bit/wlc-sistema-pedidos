using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WlcSistemaPedidos.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarUrlSistema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UrlSistema",
                table: "ConfiguracoesSistema",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UrlSistema",
                table: "ConfiguracoesSistema");
        }
    }
}
