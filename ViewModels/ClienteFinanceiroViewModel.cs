using System.ComponentModel.DataAnnotations;
using WlcSistemaPedidos.Models;

namespace WlcSistemaPedidos.ViewModels
{
    public class ClienteFinanceiroViewModel
    {
        // =========================================================
        // DADOS DO CLIENTE
        // =========================================================

        public int ClienteId { get; set; }

        public string NomeCliente { get; set; } = string.Empty;

        public decimal SaldoDevedor { get; set; }

        public bool PermitirNovosPedidos { get; set; }

        // =========================================================
        // NOVA MOVIMENTAÇÃO
        // =========================================================

        [Required(
            ErrorMessage = "Selecione o tipo da movimentação.")]
        public TipoMovimentacaoFinanceira Tipo { get; set; }

        [Range(
            0.01,
            999999999.99,
            ErrorMessage = "Informe um valor maior que zero.")]
        public decimal Valor { get; set; }

        [StringLength(
            500,
            ErrorMessage = "A observação deve possuir no máximo 500 caracteres.")]
        public string? Observacao { get; set; }

        // =========================================================
        // HISTÓRICO
        // =========================================================

        public List<MovimentacaoFinanceira> Movimentacoes { get; set; }
            = new List<MovimentacaoFinanceira>();
    }
}