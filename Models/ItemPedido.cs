using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WlcSistemaPedidos.Models
{
    public class ItemPedido
    {
        public int Id { get; set; }

        // ==========================================
        // PEDIDO
        // ==========================================

        public int PedidoId { get; set; }

        public Pedido Pedido { get; set; } = null!;

        // ==========================================
        // PRODUTO
        // ==========================================
        // O vínculo com Produto é opcional.
        //
        // Isso permite que o administrador exclua um
        // produto do cadastro sem apagar ou prejudicar
        // os pedidos antigos.
        //
        // Se o produto for excluído, ProdutoId ficará
        // nulo, mas NomeProduto, PrecoUnitario,
        // Quantidade e Subtotal continuarão registrados.
        // ==========================================

        public int? ProdutoId { get; set; }

        public Produto? Produto { get; set; }

        // ==========================================
        // DADOS HISTÓRICOS DO PRODUTO
        // ==========================================
        // Guardamos o nome e o preço existentes no
        // momento da compra.
        //
        // Alterações ou exclusões futuras do produto
        // não modificam o pedido antigo.
        // ==========================================

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