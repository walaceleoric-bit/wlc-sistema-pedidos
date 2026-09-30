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

        // Dia da semana em que o lembrete será enviado.
        // Utiliza DayOfWeek:
        // 0 = Domingo
        // 1 = Segunda-feira
        // 2 = Terça-feira
        // 3 = Quarta-feira
        // 4 = Quinta-feira
        // 5 = Sexta-feira
        // 6 = Sábado
        public DayOfWeek DiaSemana { get; set; }

        // Horário em que o lembrete deverá ser enviado.
        public TimeSpan Horario { get; set; }

        // Próxima data/hora prevista para execução.
        // Será armazenada em UTC.
        public DateTime ProximoEnvio { get; set; }

        // Quando o lembrete foi cadastrado.
        public DateTime DataCadastro { get; set; } =
            DateTime.UtcNow;

        // Último envio realizado com sucesso.
        public DateTime? UltimoEnvio { get; set; }

        // Enquanto estiver ativo, o lembrete continuará
        // sendo executado semanalmente.
        public bool Ativo { get; set; } = true;

        public int? UsuarioResponsavelId { get; set; }

        public Usuario? UsuarioResponsavel { get; set; }

        // Guarda eventual erro ocorrido na última
        // tentativa de envio.
        [StringLength(1000)]
        public string? ErroEnvio { get; set; }
    }
}