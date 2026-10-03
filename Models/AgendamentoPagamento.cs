using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WlcSistemaPedidos.Models
{
    public class AgendamentoPagamento
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Beneficiario { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Categoria { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string Descricao { get; set; } = string.Empty;

        [Column(TypeName = "numeric(12,2)")]
        public decimal Valor { get; set; }

        public DateTime DataVencimento { get; set; }

        [StringLength(500)]
        public string? Observacao { get; set; }

        public bool Recorrente { get; set; } = false;

        public FrequenciaAgendamentoPagamento? Frequencia { get; set; }

        public bool Ativo { get; set; } = true;

        public bool Pago { get; set; } = false;

        public DateTime? DataPagamento { get; set; }

        public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

        public int? UsuarioResponsavelId { get; set; }

        public Usuario? UsuarioResponsavel { get; set; }
    }

    public enum FrequenciaAgendamentoPagamento
    {
        Semanal = 1,
        Mensal = 2
    }
}
