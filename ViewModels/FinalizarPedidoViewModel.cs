using System.ComponentModel.DataAnnotations;

namespace WlcSistemaPedidos.ViewModels
{
    public class FinalizarPedidoViewModel
    {
        // O cliente decide se deseja incluir o endereço cadastrado neste pedido.
        public bool UsarEnderecoCadastrado { get; set; } = true;

        [StringLength(250)]
        public string EnderecoEntrega { get; set; } = string.Empty;

        [StringLength(100)]
        public string BairroEntrega { get; set; } = string.Empty;

        [StringLength(20)]
        public string NumeroEntrega { get; set; } = string.Empty;

        [StringLength(250)]
        public string? ComplementoEntrega { get; set; }

        [StringLength(500)]
        public string? Observacao { get; set; }

        public decimal Subtotal { get; set; }
        public decimal TaxaEntrega { get; set; }
        public decimal Total { get; set; }
    }
}
