using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WlcSistemaPedidos.Services
{
    public class LembretesBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<LembretesBackgroundService> _logger;

        public LembretesBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<LembretesBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            /*
             * Pequena espera inicial para permitir que
             * a aplicação termine de iniciar normalmente.
             */
            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(10),
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    /*
                     * AppDbContext e os serviços relacionados
                     * são Scoped.
                     *
                     * Como BackgroundService é Singleton,
                     * criamos um novo escopo a cada execução.
                     */
                    using var scope =
                        _scopeFactory.CreateScope();

                    var processador =
                        scope.ServiceProvider
                            .GetRequiredService<
                                ProcessadorLembretesService>();

                    await processador.ProcessarPendentesAsync(
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    /*
                     * Uma falha em um ciclo não pode derrubar
                     * permanentemente o serviço automático.
                     */
                    _logger.LogError(
                        ex,
                        "Erro ao processar os lembretes automáticos.");
                }

                /*
                 * Verifica novamente a cada minuto.
                 *
                 * Assim um lembrete marcado para 08:00
                 * será identificado logo após chegar
                 * ao horário programado.
                 */
                try
                {
                    await Task.Delay(
                        TimeSpan.FromMinutes(1),
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}