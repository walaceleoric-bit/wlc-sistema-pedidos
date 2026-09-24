using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WlcSistemaPedidos.Models
{
    public class Pedido
    {
        public int Id { get; set; }

        // Cliente
        public int? ClienteId { get; set; }
        public Cliente? Cliente { get; set; }

        [Required]
        [StringLength(150)]
        public string NomeCliente { get; set; } = string.Empty;

        [StringLength(20)]
        public string? TelefoneCliente { get; set; }

        // Entrega
        [StringLength(250)]
        public string? EnderecoEntrega { get; set; }

        [StringLength(100)]
        public string? BairroEntrega { get; set; }

        [StringLength(20)]
        public string? NumeroEntrega { get; set; }

        [StringLength(250)]
        public string? ComplementoEntrega { get; set; }

        [StringLength(500)]
        public string? Observacao { get; set; }

        // Valores
        [Column(TypeName = "numeric(12,2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "numeric(12,2)")]
        public decimal TaxaEntrega { get; set; }

        [Column(TypeName = "numeric(12,2)")]
        public decimal Desconto { get; set; }

        [Column(TypeName = "numeric(12,2)")]
        public decimal Total { get; set; }

        // Situação do pedido
        public StatusPedido Status { get; set; } = StatusPedido.Novo;

        public DateTime DataPedido { get; set; } = DateTime.UtcNow;

        public DateTime? DataFinalizacao { get; set; }

        // Controle do envio pelo WhatsApp
        public bool EnviadoWhatsApp { get; set; } = false;

        public DateTime? DataEnvioWhatsApp { get; set; }

        // Financeiro
        public bool Pago { get; set; } = false;

        [Column(TypeName = "numeric(12,2)")]
        public decimal ValorPago { get; set; }

        // Preparação para futura integração fiscal / NF
        public bool EnviadoParaFiscal { get; set; } = false;

        public DateTime? DataEnvioFiscal { get; set; }

        [StringLength(100)]
        public string? IdentificadorFiscal { get; set; }

        [StringLength(1000)]
        public string? RetornoFiscal { get; set; }

        // Itens
        public ICollection<ItemPedido> Itens { get; set; } = new List<ItemPedido>();
    }

    public enum StatusPedido
    {
        Novo = 1,
        Confirmado = 2,
        Preparando = 3,
        SaiuParaEntrega = 4,
        Finalizado = 5,
        Cancelado = 6
    }
}