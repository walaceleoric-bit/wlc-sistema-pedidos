using System.ComponentModel.DataAnnotations;

namespace WlcSistemaPedidos.ViewModels
{
    public class ClienteCriarViewModel
    {
        // =========================================================
        // DADOS DO CLIENTE
        // =========================================================

        [Required(ErrorMessage = "Informe o nome do cliente.")]
        [StringLength(150)]
        public string Nome { get; set; } = string.Empty;

        [StringLength(20)]
        public string? CpfCnpj { get; set; }

        [StringLength(20)]
        public string? Telefone { get; set; }

        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        [StringLength(150)]
        public string? Email { get; set; }

        // =========================================================
        // ENDEREÇO
        // =========================================================

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

        // =========================================================
        // CONTROLE
        // =========================================================

        public bool Ativo { get; set; } = true;

        public bool PermitirNovosPedidos { get; set; } = true;

        // =========================================================
        // ACESSO AO SISTEMA
        // =========================================================

        public bool CriarAcesso { get; set; } = true;

        [StringLength(50)]
        public string? Login { get; set; }

        [DataType(DataType.Password)]
        public string? Senha { get; set; }

        [DataType(DataType.Password)]
        [Compare(
            nameof(Senha),
            ErrorMessage = "A confirmação da senha não confere.")]
        public string? ConfirmarSenha { get; set; }
    }
}