using Microsoft.EntityFrameworkCore;
using WlcSistemaPedidos.Data;

namespace WlcSistemaPedidos.Services
{
    public class ProcessadorLembretesService
    {
        private readonly AppDbContext _context;
        private readonly AcessoClienteService _acessoClienteService;
        private readonly IWhatsAppSender _whatsAppSender;

        public ProcessadorLembretesService(
            AppDbContext context,
            AcessoClienteService acessoClienteService,
            IWhatsAppSender whatsAppSender)
        {
            _context = context;
            _acessoClienteService = acessoClienteService;
            _whatsAppSender = whatsAppSender;
        }

        // =========================================================
        // PROCESSAR LEMBRETES PENDENTES
        // =========================================================

        public async Task ProcessarPendentesAsync(
            CancellationToken cancellationToken = default)
        {
            /*
             * Enquanto a API real do WhatsApp não estiver
             * configurada, não fazemos absolutamente nada.
             *
             * Isso também impede a criação desnecessária
             * de tokens de acesso.
             */
            if (!_whatsAppSender.EstaConfigurado)
            {
                return;
            }

            var agoraUtc = DateTime.UtcNow;

            /*
             * Buscamos somente lembretes:
             *
             * - ativos;
             * - cujo horário já chegou;
             * - vinculados a cliente ativo.
             *
             * Limitamos a quantidade por execução para evitar
             * carregar muitos registros de uma única vez.
             */
            var lembretes =
                await _context.LembretesPedidos
                    .Include(l => l.Cliente)
                    .Where(l =>
                        l.Ativo &&
                        l.ProximoEnvio <= agoraUtc &&
                        l.Cliente.Ativo)
                    .OrderBy(l => l.ProximoEnvio)
                    .Take(50)
                    .ToListAsync(cancellationToken);

            foreach (var lembrete in lembretes)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                await ProcessarLembreteAsync(
                    lembrete,
                    cancellationToken);
            }
        }

        // =========================================================
        // PROCESSAR UM LEMBRETE
        // =========================================================

        private async Task ProcessarLembreteAsync(
            Models.LembretePedido lembrete,
            CancellationToken cancellationToken)
        {
            string? token = null;

            try
            {
                /*
                 * Conferimos novamente as condições principais.
                 */
                if (!lembrete.Ativo ||
                    lembrete.Cliente == null ||
                    !lembrete.Cliente.Ativo)
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(
                        lembrete.Telefone))
                {
                    lembrete.ErroEnvio =
                        "O cliente não possui telefone para envio.";

                    await _context.SaveChangesAsync(
                        cancellationToken);

                    return;
                }

                var configuracao =
                    await _context.ConfiguracoesSistema
                        .AsNoTracking()
                        .OrderBy(c => c.Id)
                        .FirstOrDefaultAsync(
                            cancellationToken);

                if (string.IsNullOrWhiteSpace(
                        configuracao?.UrlSistema))
                {
                    lembrete.ErroEnvio =
                        "A URL pública do sistema não está configurada.";

                    await _context.SaveChangesAsync(
                        cancellationToken);

                    return;
                }

                var urlSistema =
                    configuracao.UrlSistema
                        .Trim()
                        .TrimEnd('/');

                /*
                 * O acesso ficará válido por 30 dias.
                 *
                 * O token puro existe somente durante
                 * esta tentativa de envio.
                 */
                var dataExpiracao =
                    DateTime.UtcNow.AddDays(30);

                token =
                    await _acessoClienteService
                        .CriarTokenAsync(
                            lembrete.ClienteId,
                            dataExpiracao);

                var linkSeguro =
                    $"{urlSistema}/Cliente/EntrarPorLink" +
                    $"?token={Uri.EscapeDataString(token)}";

                /*
                 * A mensagem armazenada no banco contém
                 * somente o texto cadastrado pelo administrador.
                 *
                 * O link é acrescentado somente agora.
                 */
                var mensagemCompleta =
                    lembrete.Mensagem.Trim() +
                    Environment.NewLine +
                    Environment.NewLine +
                    linkSeguro;

                var resultado =
                    await _whatsAppSender.EnviarAsync(
                        lembrete.Telefone,
                        mensagemCompleta,
                        cancellationToken);

                if (!resultado.Sucesso)
                {
                    /*
                     * Como o WhatsApp não confirmou o envio,
                     * revogamos o acesso criado nesta tentativa.
                     */
                    await _acessoClienteService
                        .RevogarTokenAsync(token);

                    lembrete.ErroEnvio =
                        string.IsNullOrWhiteSpace(resultado.Erro)
                            ? "Não foi possível enviar o lembrete."
                            : LimitarTexto(
                                resultado.Erro,
                                1000);

                    await _context.SaveChangesAsync(
                        cancellationToken);

                    return;
                }

                /*
                 * Somente depois da confirmação do envio
                 * consideramos esta ocorrência concluída.
                 */
                var dataEnvio =
                    DateTime.UtcNow;

                lembrete.UltimoEnvio =
                    dataEnvio;

                lembrete.ProximoEnvio =
                    CalcularProximaOcorrenciaUtc(
                        lembrete.DiaSemana,
                        lembrete.Horario,
                        dataEnvio);

                lembrete.ErroEnvio =
                    null;

                await _context.SaveChangesAsync(
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                /*
                 * Se o token chegou a ser criado mas ocorreu
                 * uma falha antes da confirmação do envio,
                 * tentamos revogá-lo.
                 */
                if (!string.IsNullOrWhiteSpace(token))
                {
                    try
                    {
                        await _acessoClienteService
                            .RevogarTokenAsync(token);
                    }
                    catch
                    {
                        /*
                         * Não escondemos o erro original por
                         * causa de uma eventual falha na revogação.
                         */
                    }
                }

                lembrete.ErroEnvio =
                    LimitarTexto(
                        ex.Message,
                        1000);

                try
                {
                    await _context.SaveChangesAsync(
                        cancellationToken);
                }
                catch
                {
                    /*
                     * O processador continuará disponível para
                     * os próximos ciclos mesmo que não seja
                     * possível registrar o erro no banco.
                     */
                }
            }
        }

        // =========================================================
        // CALCULAR PRÓXIMA OCORRÊNCIA
        // =========================================================

        private DateTime CalcularProximaOcorrenciaUtc(
            DayOfWeek diaSemana,
            TimeSpan horario,
            DateTime referenciaUtc)
        {
            var fusoBrasil =
                ObterFusoHorarioBrasil();

            var referencia =
                DateTime.SpecifyKind(
                    referenciaUtc,
                    DateTimeKind.Utc);

            var referenciaBrasil =
                TimeZoneInfo.ConvertTimeFromUtc(
                    referencia,
                    fusoBrasil);

            var diferencaDias =
                ((int)diaSemana -
                 (int)referenciaBrasil.DayOfWeek +
                 7) % 7;

            var proximaDataBrasil =
                referenciaBrasil.Date
                    .AddDays(diferencaDias)
                    .Add(horario);

            if (proximaDataBrasil <= referenciaBrasil)
            {
                proximaDataBrasil =
                    proximaDataBrasil.AddDays(7);
            }

            var dataSemFuso =
                DateTime.SpecifyKind(
                    proximaDataBrasil,
                    DateTimeKind.Unspecified);

            return TimeZoneInfo.ConvertTimeToUtc(
                dataSemFuso,
                fusoBrasil);
        }

        // =========================================================
        // FUSO HORÁRIO
        // =========================================================

        private TimeZoneInfo ObterFusoHorarioBrasil()
        {
            try
            {
                // Linux / Railway
                return TimeZoneInfo.FindSystemTimeZoneById(
                    "America/Sao_Paulo");
            }
            catch (TimeZoneNotFoundException)
            {
                // Windows
                return TimeZoneInfo.FindSystemTimeZoneById(
                    "E. South America Standard Time");
            }
        }

        // =========================================================
        // LIMITAR TEXTO
        // =========================================================

        private static string LimitarTexto(
            string? texto,
            int tamanhoMaximo)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return "Erro não informado.";
            }

            texto = texto.Trim();

            if (texto.Length <= tamanhoMaximo)
            {
                return texto;
            }

            return texto.Substring(
                0,
                tamanhoMaximo);
        }
    }
}