using System.ComponentModel.DataAnnotations;

namespace WlcSistemaPedidos.Models
{
    public class Categoria
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nome { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Descricao { get; set; }

        public bool Ativa { get; set; } = true;

        // Define a ordem em que a categoria aparecerá para o cliente.
        public int OrdemExibicao { get; set; } = 0;

        public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

        // Produtos pertencentes à categoria.
        public ICollection<Produto> Produtos { get; set; } = new List<Produto>();
    }
}