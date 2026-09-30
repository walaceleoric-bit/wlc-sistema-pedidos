namespace WlcSistemaPedidos.Services
{
    public class WhatsAppSenderPendente : IWhatsAppSender
    {
        /*
         * Enquanto a API real do WhatsApp
         * não estiver configurada, permanece false.
         *
         * O processador de lembretes verificará
         * esta propriedade antes de gerar qualquer
         * token ou tentar realizar um envio.
         */
        public bool EstaConfigurado => false;

        public Task<ResultadoEnvioWhatsApp> EnviarAsync(
            string telefone,
            string mensagem,
            CancellationToken cancellationToken = default)
        {
            /*
             * Este método não envia absolutamente nada.
             *
             * Ele existe somente para manter toda a
             * infraestrutura pronta até instalarmos
             * a integração real do WhatsApp.
             */
            return Task.FromResult(
                ResultadoEnvioWhatsApp.Falhou(
                    "A API do WhatsApp ainda não está configurada."));
        }
    }
}