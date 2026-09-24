using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WlcSistemaPedidos.Migrations
{
    /// <inheritdoc />
    public partial class EstruturaInicialSistemaPedidos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Ativa = table.Column<bool>(type: "boolean", nullable: false),
                    OrdemExibicao = table.Column<int>(type: "integer", nullable: false),
                    DataCadastro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CpfCnpj = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Endereco = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Bairro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Cidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Numero = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Complemento = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Cep = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    PermitirNovosPedidos = table.Column<bool>(type: "boolean", nullable: false),
                    SaldoDevedor = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    DataCadastro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Clientes_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracoesSistema",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NomeEstabelecimento = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CpfCnpj = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    RazaoSocial = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    WhatsAppPedidos = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Endereco = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Bairro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Cidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Estado = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    Cep = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BannerUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TaxaEntregaPadrao = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    AceitarPedidos = table.Column<bool>(type: "boolean", nullable: false),
                    MensagemInicial = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EmissaoFiscalAtiva = table.Column<bool>(type: "boolean", nullable: false),
                    AmbienteFiscal = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProvedorFiscal = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracoesSistema", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Produtos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Preco = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    ImagemUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    Disponivel = table.Column<bool>(type: "boolean", nullable: false),
                    OrdemExibicao = table.Column<int>(type: "integer", nullable: false),
                    DataCadastro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CategoriaId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Produtos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Produtos_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Pedidos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClienteId = table.Column<int>(type: "integer", nullable: true),
                    NomeCliente = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TelefoneCliente = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    EnderecoEntrega = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    BairroEntrega = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NumeroEntrega = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ComplementoEntrega = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Subtotal = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    TaxaEntrega = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Desconto = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DataPedido = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataFinalizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EnviadoWhatsApp = table.Column<bool>(type: "boolean", nullable: false),
                    DataEnvioWhatsApp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Pago = table.Column<bool>(type: "boolean", nullable: false),
                    ValorPago = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    EnviadoParaFiscal = table.Column<bool>(type: "boolean", nullable: false),
                    DataEnvioFiscal = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IdentificadorFiscal = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RetornoFiscal = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pedidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pedidos_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ItensPedido",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PedidoId = table.Column<int>(type: "integer", nullable: false),
                    ProdutoId = table.Column<int>(type: "integer", nullable: false),
                    NomeProduto = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Quantidade = table.Column<int>(type: "integer", nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Observacao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensPedido", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensPedido_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItensPedido_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MovimentacoesFinanceiras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClienteId = table.Column<int>(type: "integer", nullable: false),
                    PedidoId = table.Column<int>(type: "integer", nullable: true),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DataMovimentacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsuarioResponsavelId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimentacoesFinanceiras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimentacoesFinanceiras_AspNetUsers_UsuarioResponsavelId",
                        column: x => x.UsuarioResponsavelId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MovimentacoesFinanceiras_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimentacoesFinanceiras_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_UsuarioId",
                table: "Clientes",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItensPedido_PedidoId",
                table: "ItensPedido",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensPedido_ProdutoId",
                table: "ItensPedido",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentacoesFinanceiras_ClienteId",
                table: "MovimentacoesFinanceiras",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentacoesFinanceiras_PedidoId",
                table: "MovimentacoesFinanceiras",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentacoesFinanceiras_UsuarioResponsavelId",
                table: "MovimentacoesFinanceiras",
                column: "UsuarioResponsavelId");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_ClienteId",
                table: "Pedidos",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_CategoriaId",
                table: "Produtos",
                column: "CategoriaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracoesSistema");

            migrationBuilder.DropTable(
                name: "ItensPedido");

            migrationBuilder.DropTable(
                name: "MovimentacoesFinanceiras");

            migrationBuilder.DropTable(
                name: "Produtos");

            migrationBuilder.DropTable(
                name: "Pedidos");

            migrationBuilder.DropTable(
                name: "Categorias");

            migrationBuilder.DropTable(
                name: "Clientes");
        }
    }
}
