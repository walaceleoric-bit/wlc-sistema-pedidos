using System.ComponentModel.DataAnnotations;

namespace WlcSistemaPedidos.Models
{
    public class AcessoCliente
    {
        public int Id { get; set; }

        // Cliente ao qual este acesso pertence
        public int ClienteId { get; set; }

        public Cliente Cliente { get; set; } = null!;

        // Guardaremos o HASH do token, nunca o token original.
        // Assim, mesmo alguém com acesso ao banco não consegue
        // simplesmente copiar o link de acesso do cliente.
        [Required]
        [StringLength(64)]
        public string TokenHash { get; set; } = string.Empty;

        // Data em que o acesso foi criado
        public DateTime DataCriacao { get; set; } =
            DateTime.UtcNow;

        // Até quando o link poderá ser utilizado
        public DateTime DataExpiracao { get; set; }

        // Última vez em que o link foi utilizado
        public DateTime? DataUltimoAcesso { get; set; }

        // Permite revogar o link sem precisar excluí-lo
        public bool Ativo { get; set; } = true;
    }
}