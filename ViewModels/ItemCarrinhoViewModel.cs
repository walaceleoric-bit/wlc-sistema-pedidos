namespace WlcSistemaPedidos.ViewModels
{
    public class ItemCarrinhoViewModel
    {
        public int ProdutoId { get; set; }

        public string NomeProduto { get; set; } = string.Empty;

        public string? ImagemUrl { get; set; }

        public decimal PrecoUnitario { get; set; }

        public int Quantidade { get; set; } = 1;

        public decimal Subtotal =>
            PrecoUnitario * Quantidade;
    }
}