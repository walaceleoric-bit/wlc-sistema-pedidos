using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WlcSistemaPedidos.Models
{
    public class MovimentacaoFinanceira
    {
        public int Id { get; set; }

        // Cliente
        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; } = null!;

        // Pedido relacionado, quando existir
        public int? PedidoId { get; set; }
        public Pedido? Pedido { get; set; }

        public TipoMovimentacaoFinanceira Tipo { get; set; }

        [Column(TypeName = "numeric(12,2)")]
        public decimal Valor { get; set; }

        [StringLength(500)]
        public string? Observacao { get; set; }

        public DateTime DataMovimentacao { get; set; } = DateTime.UtcNow;

        // Administrador que registrou pagamento/ajuste
        public int? UsuarioResponsavelId { get; set; }
        public Usuario? UsuarioResponsavel { get; set; }
    }

    public enum TipoMovimentacaoFinanceira
    {
        Debito = 1,
        Pagamento = 2,
        AjusteCredito = 3,
        AjusteDebito = 4,
        Estorno = 5
    }
}