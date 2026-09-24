using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WlcSistemaPedidos.Models
{
    public class ConfiguracaoSistema
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string NomeEstabelecimento { get; set; } = string.Empty;

        [StringLength(20)]
        public string? CpfCnpj { get; set; }

        [StringLength(150)]
        public string? RazaoSocial { get; set; }

        [StringLength(20)]
        public string? Telefone { get; set; }

        // Número que receberá os pedidos enviados pelo WhatsApp
        [StringLength(20)]
        public string? WhatsAppPedidos { get; set; }

        // URL pública utilizada nos links enviados aos clientes
        [StringLength(500)]
        public string? UrlSistema { get; set; }

        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(250)]
        public string? Endereco { get; set; }

        [StringLength(100)]
        public string? Bairro { get; set; }

        [StringLength(100)]
        public string? Cidade { get; set; }

        [StringLength(2)]
        public string? Estado { get; set; }

        [StringLength(10)]
        public string? Cep { get; set; }

        // Imagens que o ADM poderá alterar
        [StringLength(500)]
        public string? LogoUrl { get; set; }

        [StringLength(500)]
        public string? BannerUrl { get; set; }

        // Entrega
        [Column(TypeName = "numeric(12,2)")]
        public decimal TaxaEntregaPadrao { get; set; } = 0;

        public bool AceitarPedidos { get; set; } = true;

        // Texto exibido na página inicial/cardápio
        [StringLength(1000)]
        public string? MensagemInicial { get; set; }

        // Preparação para o módulo fiscal futuro
        public bool EmissaoFiscalAtiva { get; set; } = false;

        [StringLength(50)]
        public string? AmbienteFiscal { get; set; }

        [StringLength(100)]
        public string? ProvedorFiscal { get; set; }

        public DateTime DataAtualizacao { get; set; } = DateTime.UtcNow;
    }
}