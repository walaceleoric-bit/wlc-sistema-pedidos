using System.ComponentModel.DataAnnotations;

namespace WlcSistemaPedidos.Models
{
    public class LembretePedido
    {
        public int Id { get; set; }

        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; } = null!;

        [Required]
        [StringLength(150)]
        public string NomeCliente { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Telefone { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Mensagem { get; set; } = string.Empty;

        public DateTime DataHoraAgendada { get; set; }

        public DateTime DataCadastro { get; set; } =
            DateTime.UtcNow;

        public DateTime? DataEnvio { get; set; }

        public bool Enviado { get; set; } = false;

        public bool Ativo { get; set; } = true;

        public int? UsuarioResponsavelId { get; set; }

        public Usuario? UsuarioResponsavel { get; set; }

        [StringLength(1000)]
        public string? ErroEnvio { get; set; }
    }
}