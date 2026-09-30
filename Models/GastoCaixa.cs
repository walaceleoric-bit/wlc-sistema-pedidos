using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WlcSistemaPedidos.Models
{
    public class GastoCaixa
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Categoria { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string Descricao { get; set; } = string.Empty;

        [Column(TypeName = "numeric(12,2)")]
        public decimal Valor { get; set; }

        public DateTime DataGasto { get; set; } = DateTime.UtcNow;

        public int? UsuarioResponsavelId { get; set; }

        public Usuario? UsuarioResponsavel { get; set; }
    }
}