using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WlcSistemaPedidos.Models
{
    public class Produto
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Nome { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Descricao { get; set; }

        [Column(TypeName = "numeric(12,2)")]
        public decimal Preco { get; set; }

        // Caminho ou URL da imagem do produto.
        [StringLength(500)]
        public string? ImagemUrl { get; set; }

        public bool Ativo { get; set; } = true;

        // Permite retirar temporariamente um produto do cardápio
        // sem precisar excluí-lo.
        public bool Disponivel { get; set; } = true;

        public int OrdemExibicao { get; set; } = 0;

        public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

        public DateTime? DataAtualizacao { get; set; }

        // Categoria
        public int CategoriaId { get; set; }

        public Categoria Categoria { get; set; } = null!;
    }
}