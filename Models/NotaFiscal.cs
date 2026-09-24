using System.ComponentModel.DataAnnotations;

namespace WlcSistemaPedidos.Models
{
    public class NotaFiscal
    {
        public int Id { get; set; }

        // ==========================================
        // PEDIDO
        // ==========================================

        public int PedidoId { get; set; }

        public Pedido Pedido { get; set; } = null!;

        // ==========================================
        // SITUAÇÃO FISCAL
        // ==========================================

        public StatusNotaFiscal Status { get; set; }
            = StatusNotaFiscal.Pendente;

        // ==========================================
        // IDENTIFICAÇÃO DA NOTA
        // ==========================================

        [StringLength(50)]
        public string? NumeroNota { get; set; }

        [StringLength(20)]
        public string? Serie { get; set; }

        [StringLength(100)]
        public string? ChaveAcesso { get; set; }

        [StringLength(100)]
        public string? Protocolo { get; set; }

        // ==========================================
        // PROVEDOR / API
        // ==========================================

        [StringLength(100)]
        public string? ProvedorFiscal { get; set; }

        [StringLength(100)]
        public string? IdentificadorExterno { get; set; }

        // ==========================================
        // DOCUMENTOS RETORNADOS PELA API
        // ==========================================

        [StringLength(1000)]
        public string? UrlPdf { get; set; }

        [StringLength(1000)]
        public string? UrlXml { get; set; }

        // ==========================================
        // CONTROLE DE ENVIO
        // ==========================================

        public DateTime DataCriacao { get; set; }
            = DateTime.UtcNow;

        public DateTime? DataEnvio { get; set; }

        public DateTime? DataAutorizacao { get; set; }

        public DateTime? DataCancelamento { get; set; }

        // ==========================================
        // RETORNO / ERROS
        // ==========================================

        [StringLength(4000)]
        public string? RetornoFiscal { get; set; }

        [StringLength(2000)]
        public string? ErroFiscal { get; set; }

        // ==========================================
        // ADMINISTRADOR RESPONSÁVEL
        // ==========================================

        public int? UsuarioResponsavelId { get; set; }

        public Usuario? UsuarioResponsavel { get; set; }
    }

    public enum StatusNotaFiscal
    {
        Pendente = 1,
        Processando = 2,
        Autorizada = 3,
        Rejeitada = 4,
        Erro = 5,
        Cancelada = 6
    }
}