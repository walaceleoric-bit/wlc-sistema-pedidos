using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WlcSistemaPedidos.Models
{
    public class Cliente
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Nome { get; set; } = string.Empty;

        [StringLength(20)]
        public string? CpfCnpj { get; set; }

        [StringLength(20)]
        public string? Telefone { get; set; }

        [EmailAddress]
        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(250)]
        public string? Endereco { get; set; }

        [StringLength(100)]
        public string? Bairro { get; set; }

        [StringLength(100)]
        public string? Cidade { get; set; }

        [StringLength(10)]
        public string? Numero { get; set; }

        [StringLength(250)]
        public string? Complemento { get; set; }

        [StringLength(10)]
        public string? Cep { get; set; }

        public bool Ativo { get; set; } = true;

        public bool PermitirNovosPedidos { get; set; } = true;

        [Column(TypeName = "numeric(12,2)")]
        public decimal SaldoDevedor { get; set; } = 0;

        public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

        public int? UsuarioId { get; set; }

        public Usuario? Usuario { get; set; }
    }
}