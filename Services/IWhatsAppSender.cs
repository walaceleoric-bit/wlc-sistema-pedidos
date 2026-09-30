namespace WlcSistemaPedidos.Services
{
    public interface IWhatsAppSender
    {
        /*
         * Indica se a integração com o WhatsApp
         * está configurada e pronta para envio.
         *
         * Enquanto não instalarmos/configurarmos
         * a API, será false.
         */
        bool EstaConfigurado { get; }

        /*
         * Envia uma mensagem para o telefone informado.
         *
         * A implementação real deste método ficará
         * para a etapa da API do WhatsApp.
         */
        Task<ResultadoEnvioWhatsApp> EnviarAsync(
            string telefone,
            string mensagem,
            CancellationToken cancellationToken = default);
    }

    public class ResultadoEnvioWhatsApp
    {
        public bool Sucesso { get; set; }

        public string? IdentificadorMensagem { get; set; }

        public string? Erro { get; set; }

        public static ResultadoEnvioWhatsApp Enviado(
            string? identificadorMensagem = null)
        {
            return new ResultadoEnvioWhatsApp
            {
                Sucesso = true,
                IdentificadorMensagem =
                    identificadorMensagem
            };
        }

        public static ResultadoEnvioWhatsApp Falhou(
            string erro)
        {
            return new ResultadoEnvioWhatsApp
            {
                Sucesso = false,
                Erro = erro
            };
        }
    }
}