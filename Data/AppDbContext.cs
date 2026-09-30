using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WlcSistemaPedidos.Models;

namespace WlcSistemaPedidos.Data
{
    public class AppDbContext : IdentityDbContext<Usuario, IdentityRole<int>, int>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Produto> Produtos { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<ItemPedido> ItensPedido { get; set; }
        public DbSet<MovimentacaoFinanceira> MovimentacoesFinanceiras { get; set; }
        public DbSet<ConfiguracaoSistema> ConfiguracoesSistema { get; set; }
        public DbSet<LembretePedido> LembretesPedidos { get; set; }
        public DbSet<NotaFiscal> NotasFiscais { get; set; }
        public DbSet<GastoCaixa> GastosCaixa { get; set; }
        public DbSet<CategoriaGasto> CategoriasGastos { get; set; }

        // Acessos seguros enviados aos clientes
        public DbSet<AcessoCliente> AcessosClientes { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Um usuário poderá estar vinculado a apenas um cliente.
            builder.Entity<Cliente>()
                .HasIndex(c => c.UsuarioId)
                .IsUnique();

            // Categoria -> Produtos
            builder.Entity<Produto>()
                .HasOne(p => p.Categoria)
                .WithMany(c => c.Produtos)
                .HasForeignKey(p => p.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cliente -> Pedidos
            builder.Entity<Pedido>()
                .HasOne(p => p.Cliente)
                .WithMany()
                .HasForeignKey(p => p.ClienteId)
                .OnDelete(DeleteBehavior.SetNull);

            // Pedido -> Itens
            builder.Entity<ItemPedido>()
                .HasOne(i => i.Pedido)
                .WithMany(p => p.Itens)
                .HasForeignKey(i => i.PedidoId)
                .OnDelete(DeleteBehavior.Cascade);

            // Produto -> Itens de pedidos antigos
            //
            // Se o produto for excluído, o ProdutoId do item
            // ficará nulo. Os dados históricos do pedido
            // continuarão preservados em NomeProduto,
            // PrecoUnitario, Quantidade e Subtotal.
            builder.Entity<ItemPedido>()
                .HasOne(i => i.Produto)
                .WithMany()
                .HasForeignKey(i => i.ProdutoId)
                .OnDelete(DeleteBehavior.SetNull);

            // Cliente -> Movimentações financeiras
            builder.Entity<MovimentacaoFinanceira>()
                .HasOne(m => m.Cliente)
                .WithMany()
                .HasForeignKey(m => m.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            // Pedido -> Movimentações financeiras
            builder.Entity<MovimentacaoFinanceira>()
                .HasOne(m => m.Pedido)
                .WithMany()
                .HasForeignKey(m => m.PedidoId)
                .OnDelete(DeleteBehavior.SetNull);

            // Administrador responsável pela movimentação
            builder.Entity<MovimentacaoFinanceira>()
                .HasOne(m => m.UsuarioResponsavel)
                .WithMany()
                .HasForeignKey(m => m.UsuarioResponsavelId)
                .OnDelete(DeleteBehavior.SetNull);

            // ==========================================
            // LEMBRETES RECORRENTES
            // ==========================================

            // Cliente -> Lembretes de pedido
            builder.Entity<LembretePedido>()
                .HasOne(l => l.Cliente)
                .WithMany()
                .HasForeignKey(l => l.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            // Administrador responsável pelo lembrete
            builder.Entity<LembretePedido>()
                .HasOne(l => l.UsuarioResponsavel)
                .WithMany()
                .HasForeignKey(l => l.UsuarioResponsavelId)
                .OnDelete(DeleteBehavior.SetNull);

            /*
             * Ajuda o serviço automático a localizar rapidamente
             * os lembretes ativos cujo horário de envio chegou.
             */
            builder.Entity<LembretePedido>()
                .HasIndex(l => new
                {
                    l.Ativo,
                    l.ProximoEnvio
                });

            // ==========================================
            // ACESSO SEGURO DO CLIENTE
            // ==========================================

            // Cliente -> Acessos seguros
            //
            // Um cliente poderá possuir vários acessos ao longo
            // do tempo, por exemplo links enviados em diferentes
            // lembretes pelo WhatsApp.
            builder.Entity<AcessoCliente>()
                .HasOne(a => a.Cliente)
                .WithMany()
                .HasForeignKey(a => a.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            // O hash identifica de forma única cada link.
            builder.Entity<AcessoCliente>()
                .HasIndex(a => a.TokenHash)
                .IsUnique();

            // Ajuda a localizar acessos válidos/ativos.
            builder.Entity<AcessoCliente>()
                .HasIndex(a => new
                {
                    a.ClienteId,
                    a.Ativo,
                    a.DataExpiracao
                });

            // ==========================================
            // NOTA FISCAL
            // ==========================================

            // Pedido -> Nota Fiscal
            // Um pedido poderá possuir apenas uma nota fiscal.
            builder.Entity<NotaFiscal>()
                .HasOne(n => n.Pedido)
                .WithOne(p => p.NotaFiscal)
                .HasForeignKey<NotaFiscal>(n => n.PedidoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Índice único para reforçar a regra no banco.
            builder.Entity<NotaFiscal>()
                .HasIndex(n => n.PedidoId)
                .IsUnique();

            // Administrador responsável pela operação fiscal
            builder.Entity<NotaFiscal>()
                .HasOne(n => n.UsuarioResponsavel)
                .WithMany()
                .HasForeignKey(n => n.UsuarioResponsavelId)
                .OnDelete(DeleteBehavior.SetNull);

            // Ajuda nas consultas da tela fiscal.
            builder.Entity<NotaFiscal>()
                .HasIndex(n => new
                {
                    n.Status,
                    n.DataCriacao
                });

            // ==========================================
            // CAIXA - GASTOS
            // ==========================================

            // Administrador responsável pelo lançamento do gasto
            builder.Entity<GastoCaixa>()
                .HasOne(g => g.UsuarioResponsavel)
                .WithMany()
                .HasForeignKey(g => g.UsuarioResponsavelId)
                .OnDelete(DeleteBehavior.SetNull);

            // Ajuda nas consultas diária, semanal e mensal
            builder.Entity<GastoCaixa>()
                .HasIndex(g => g.DataGasto);

            // Ajuda nos relatórios agrupados por categoria
            builder.Entity<GastoCaixa>()
                .HasIndex(g => new
                {
                    g.Categoria,
                    g.DataGasto
                });

            // ==========================================
            // CAIXA - CATEGORIAS DE GASTOS
            // ==========================================

            // Não permite duas categorias com o mesmo nome.
            builder.Entity<CategoriaGasto>()
                .HasIndex(c => c.Nome)
                .IsUnique();
        }
    }
}