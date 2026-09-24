using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace WlcSistemaPedidos.ViewModels
{
    public class ConfiguracaoViewModel
    {
        public int Id { get; set; }

        // ==========================================
        // ESTABELECIMENTO
        // ==========================================

        [Required(ErrorMessage = "Informe o nome do estabelecimento.")]
        [StringLength(150)]
        [Display(Name = "Nome do estabelecimento")]
        public string NomeEstabelecimento { get; set; } = string.Empty;

        [StringLength(150)]
        [Display(Name = "Razão social")]
        public string? RazaoSocial { get; set; }

        [StringLength(20)]
        [Display(Name = "CPF / CNPJ")]
        public string? CpfCnpj { get; set; }

        [StringLength(20)]
        [Display(Name = "Telefone")]
        public string? Telefone { get; set; }

        [StringLength(150)]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        [Display(Name = "E-mail")]
        public string? Email { get; set; }

        // ==========================================
        // WHATSAPP DOS PEDIDOS
        // ==========================================

        [StringLength(20)]
        [Display(Name = "WhatsApp para receber pedidos")]
        public string? WhatsAppPedidos { get; set; }

        // ==========================================
        // LINK PÚBLICO DO SISTEMA
        // ==========================================

        [StringLength(500)]
        [Url(ErrorMessage = "Informe uma URL válida.")]
        [Display(Name = "URL pública do sistema")]
        public string? UrlSistema { get; set; }

        // ==========================================
        // ENDEREÇO
        // ==========================================

        [StringLength(10)]
        [Display(Name = "CEP")]
        public string? Cep { get; set; }

        [StringLength(250)]
        [Display(Name = "Endereço")]
        public string? Endereco { get; set; }

        [StringLength(100)]
        [Display(Name = "Bairro")]
        public string? Bairro { get; set; }

        [StringLength(100)]
        [Display(Name = "Cidade")]
        public string? Cidade { get; set; }

        [StringLength(2)]
        [Display(Name = "UF")]
        public string? Estado { get; set; }

        // ==========================================
        // PEDIDOS
        // ==========================================

        [Range(
            0,
            999999.99,
            ErrorMessage = "Informe uma taxa de entrega válida.")]
        [Display(Name = "Taxa de entrega padrão")]
        public decimal TaxaEntregaPadrao { get; set; }

        [Display(Name = "Aceitar novos pedidos")]
        public bool AceitarPedidos { get; set; }

        [StringLength(1000)]
        [Display(Name = "Mensagem inicial")]
        public string? MensagemInicial { get; set; }

        // ==========================================
        // LOGO DO ESTABELECIMENTO
        // ==========================================

        public string? LogoUrlAtual { get; set; }

        public string? BannerUrlAtual { get; set; }

        [Display(Name = "Logo do estabelecimento")]
        public IFormFile? LogoArquivo { get; set; }

        public bool RemoverLogo { get; set; }

        // ==========================================
        // ACESSO DO ADMINISTRADOR
        // ==========================================

        [StringLength(50)]
        [Display(Name = "Novo login")]
        public string? NovoLoginAdministrador { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Senha atual")]
        public string? SenhaAtual { get; set; }

        [DataType(DataType.Password)]
        [RegularExpression(
            @"^\d{6}$",
            ErrorMessage = "A nova senha deve conter exatamente 6 números.")]
        [Display(Name = "Nova senha")]
        public string? NovaSenha { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar nova senha")]
        public string? ConfirmarNovaSenha { get; set; }
    }
}