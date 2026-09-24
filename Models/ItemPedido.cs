using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WlcSistemaPedidos.Models
{
    public class ItemPedido
    {
        public int Id { get; set; }

        // Pedido
        public int PedidoId { get; set; }
        public Pedido Pedido { get; set; } = null!;

        // Produto
        public int ProdutoId { get; set; }
        public Produto Produto { get; set; } = null!;

        // Guardamos o nome e o preço no momento da compra.
        // Assim, alterações futuras no produto não modificam pedidos antigos.
        [Required]
        [StringLength(150)]
        public string NomeProduto { get; set; } = string.Empty;

        public int Quantidade { get; set; } = 1;

        [Column(TypeName = "numeric(12,2)")]
        public decimal PrecoUnitario { get; set; }

        [Column(TypeName = "numeric(12,2)")]
        public decimal Subtotal { get; set; }

        [StringLength(300)]
        public string? Observacao { get; set; }
    }
}