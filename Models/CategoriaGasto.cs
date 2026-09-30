using System.ComponentModel.DataAnnotations;

namespace WlcSistemaPedidos.Models
{
    public class CategoriaGasto
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nome { get; set; } = string.Empty;

        public bool Ativa { get; set; } = true;

        public DateTime DataCadastro { get; set; } = DateTime.UtcNow;
    }
}