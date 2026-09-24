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
            builder.Entity<ItemPedido>()
                .HasOne(i => i.Produto)
                .WithMany()
                .HasForeignKey(i => i.ProdutoId)
                .OnDelete(DeleteBehavior.Restrict);

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

            // Ajuda na busca dos lembretes que precisam ser enviados
            builder.Entity<LembretePedido>()
                .HasIndex(l => new
                {
                    l.Ativo,
                    l.Enviado,
                    l.DataHoraAgendada
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
        }
    }
}