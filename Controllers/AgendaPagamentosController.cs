using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WlcSistemaPedidos.Data;
using WlcSistemaPedidos.Models;

namespace WlcSistemaPedidos.Controllers
{
    [Authorize]
    public class AgendaPagamentosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public AgendaPagamentosController(
            AppDbContext context,
            UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================================
        // AGENDA
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var usuario = await ObterAdministradorAsync();

            if (usuario == null)
            {
                return RedirectToAction("Login", "Conta");
            }

            if (usuario.Perfil != PerfilUsuario.Administrador)
            {
                return RedirectToAction("AcessoNegado", "Conta");
            }

            var hoje = ObterAgoraBrasil().Date;

            var inicioMesLocal =
                new DateTime(
                    hoje.Year,
                    hoje.Month,
                    1);

            var inicioMesUtc =
                ConverterDataLocalParaUtc(inicioMesLocal);

            var agendamentos =
                await _context.AgendamentosPagamentos
                    .AsNoTracking()
                    .Where(a =>
                        a.DataVencimento >= inicioMesUtc)
                    .OrderBy(a => a.DataVencimento)
                    .ThenBy(a => a.Beneficiario)
                    .ToListAsync();

            var categorias =
                await _context.CategoriasGastos
                    .AsNoTracking()
                    .Where(c => c.Ativa)
                    .OrderBy(c => c.Nome)
                    .ToListAsync();

            ViewBag.CategoriasGastos = categorias;
            ViewBag.Hoje = hoje;

            return View(agendamentos);
        }

        // =========================================================
        // NOVO AGENDAMENTO
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(
            string beneficiario,
            string categoria,
            string descricao,
            decimal valor,
            DateTime dataVencimento,
            string? observacao,
            bool recorrente,
            FrequenciaAgendamentoPagamento? frequencia)
        {
            var usuario = await ObterAdministradorAsync();

            if (usuario == null)
            {
                return RedirectToAction("Login", "Conta");
            }

            if (usuario.Perfil != PerfilUsuario.Administrador)
            {
                return RedirectToAction("AcessoNegado", "Conta");
            }

            beneficiario =
                beneficiario?.Trim()
                ?? string.Empty;

            categoria =
                categoria?.Trim()
                ?? string.Empty;

            descricao =
                descricao?.Trim()
                ?? string.Empty;

            observacao =
                observacao?.Trim();

            if (string.IsNullOrWhiteSpace(beneficiario))
            {
                TempData["ErroAgenda"] =
                    "Informe quem precisa receber o pagamento.";

                return RedirectToAction(nameof(Index));
            }

            if (beneficiario.Length > 150)
            {
                TempData["ErroAgenda"] =
                    "O nome informado é muito grande.";

                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(categoria))
            {
                TempData["ErroAgenda"] =
                    "Selecione a categoria do pagamento.";

                return RedirectToAction(nameof(Index));
            }

            var categoriaExiste =
                await _context.CategoriasGastos
                    .AsNoTracking()
                    .AnyAsync(c =>
                        c.Ativa &&
                        c.Nome == categoria);

            if (!categoriaExiste)
            {
                TempData["ErroAgenda"] =
                    "A categoria selecionada não existe.";

                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(descricao))
            {
                TempData["ErroAgenda"] =
                    "Informe a descrição do pagamento.";

                return RedirectToAction(nameof(Index));
            }

            if (descricao.Length > 250)
            {
                TempData["ErroAgenda"] =
                    "A descrição informada é muito grande.";

                return RedirectToAction(nameof(Index));
            }

            if (valor <= 0)
            {
                TempData["ErroAgenda"] =
                    "Informe um valor maior que zero.";

                return RedirectToAction(nameof(Index));
            }

            if (dataVencimento == default)
            {
                TempData["ErroAgenda"] =
                    "Informe a data do pagamento.";

                return RedirectToAction(nameof(Index));
            }

            if (observacao?.Length > 500)
            {
                TempData["ErroAgenda"] =
                    "A observação é muito grande.";

                return RedirectToAction(nameof(Index));
            }

            if (recorrente && frequencia == null)
            {
                TempData["ErroAgenda"] =
                    "Escolha se a repetição será semanal ou mensal.";

                return RedirectToAction(nameof(Index));
            }

            if (!recorrente)
            {
                frequencia = null;
            }

            var agendamento =
                new AgendamentoPagamento
                {
                    Beneficiario = beneficiario,
                    Categoria = categoria,
                    Descricao = descricao,
                    Valor = valor,

                    DataVencimento =
                        ConverterDataLocalParaUtc(
                            dataVencimento.Date),

                    Observacao =
                        string.IsNullOrWhiteSpace(observacao)
                            ? null
                            : observacao,

                    Recorrente = recorrente,
                    Frequencia = frequencia,

                    Ativo = true,
                    Pago = false,

                    DataPagamento = null,
                    DataCadastro = DateTime.UtcNow,

                    UsuarioResponsavelId =
                        usuario.Id
                };

            _context.AgendamentosPagamentos.Add(
                agendamento);

            await _context.SaveChangesAsync();

            TempData["SucessoAgenda"] =
                "Pagamento agendado com sucesso.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // ATIVAR / PAUSAR
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlternarAtivo(
            int id)
        {
            var usuario = await ObterAdministradorAsync();

            if (usuario == null)
            {
                return RedirectToAction("Login", "Conta");
            }

            if (usuario.Perfil != PerfilUsuario.Administrador)
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            var agendamento =
                await _context.AgendamentosPagamentos
                    .FirstOrDefaultAsync(a =>
                        a.Id == id);

            if (agendamento == null)
            {
                TempData["ErroAgenda"] =
                    "Agendamento não encontrado.";

                return RedirectToAction(nameof(Index));
            }

            if (agendamento.Pago &&
                !agendamento.Recorrente)
            {
                TempData["ErroAgenda"] =
                    "Esse pagamento já foi concluído.";

                return RedirectToAction(nameof(Index));
            }

            agendamento.Ativo =
                !agendamento.Ativo;

            await _context.SaveChangesAsync();

            TempData["SucessoAgenda"] =
                agendamento.Ativo
                    ? "Agendamento ativado."
                    : "Agendamento pausado.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // MARCAR COMO PAGO
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarComoPago(
            int id)
        {
            var usuario = await ObterAdministradorAsync();

            if (usuario == null)
            {
                return RedirectToAction("Login", "Conta");
            }

            if (usuario.Perfil != PerfilUsuario.Administrador)
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            var agendamento =
                await _context.AgendamentosPagamentos
                    .FirstOrDefaultAsync(a =>
                        a.Id == id);

            if (agendamento == null)
            {
                TempData["ErroAgenda"] =
                    "Agendamento não encontrado.";

                return RedirectToAction(nameof(Index));
            }

            if (!agendamento.Ativo)
            {
                TempData["ErroAgenda"] =
                    "Ative o agendamento antes de marcar como pago.";

                return RedirectToAction(nameof(Index));
            }

            if (agendamento.Pago &&
                !agendamento.Recorrente)
            {
                TempData["ErroAgenda"] =
                    "Esse pagamento já foi concluído.";

                return RedirectToAction(nameof(Index));
            }

            agendamento.DataPagamento =
                DateTime.UtcNow;

            // =============================================
            // RECORRENTE
            // =============================================

            if (agendamento.Recorrente &&
                agendamento.Frequencia.HasValue)
            {
                var vencimentoLocal =
                    ConverterUtcParaLocal(
                        agendamento.DataVencimento)
                    .Date;

                if (agendamento.Frequencia.Value ==
                    FrequenciaAgendamentoPagamento.Semanal)
                {
                    vencimentoLocal =
                        vencimentoLocal.AddDays(7);
                }
                else
                {
                    vencimentoLocal =
                        vencimentoLocal.AddMonths(1);
                }

                agendamento.DataVencimento =
                    ConverterDataLocalParaUtc(
                        vencimentoLocal);

                // Continua ativo para o próximo vencimento.
                agendamento.Pago = false;
                agendamento.Ativo = true;

                await _context.SaveChangesAsync();

                TempData["SucessoAgenda"] =
                    agendamento.Frequencia.Value ==
                    FrequenciaAgendamentoPagamento.Semanal
                        ? "Pagamento concluído. Próximo lembrete agendado para a próxima semana."
                        : "Pagamento concluído. Próximo lembrete agendado para o próximo mês.";
            }
            else
            {
                // Pagamento único: ao concluir, ele deixa a agenda.
                // Nenhum gasto é criado no Caixa.
                _context.AgendamentosPagamentos.Remove(
                    agendamento);

                await _context.SaveChangesAsync();

                TempData["SucessoAgenda"] =
                    "Pagamento concluído e retirado da agenda.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // EXCLUIR
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(
            int id)
        {
            var usuario = await ObterAdministradorAsync();

            if (usuario == null)
            {
                return RedirectToAction("Login", "Conta");
            }

            if (usuario.Perfil != PerfilUsuario.Administrador)
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            var agendamento =
                await _context.AgendamentosPagamentos
                    .FirstOrDefaultAsync(a =>
                        a.Id == id);

            if (agendamento == null)
            {
                TempData["ErroAgenda"] =
                    "Agendamento não encontrado.";

                return RedirectToAction(nameof(Index));
            }

            _context.AgendamentosPagamentos.Remove(
                agendamento);

            await _context.SaveChangesAsync();

            TempData["SucessoAgenda"] =
                "Agendamento excluído.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // ADMINISTRADOR
        // =========================================================

        private async Task<Usuario?>
            ObterAdministradorAsync()
        {
            var usuario =
                await _userManager
                    .GetUserAsync(User);

            if (usuario == null)
            {
                return null;
            }

            if (!usuario.Ativo)
            {
                await HttpContext.SignOutAsync(
                    IdentityConstants.ApplicationScheme);

                return null;
            }

            return usuario;
        }

        // =========================================================
        // HORÁRIO DO BRASIL
        // =========================================================

        private static TimeZoneInfo
            ObterFusoHorarioBrasil()
        {
            try
            {
                return TimeZoneInfo
                    .FindSystemTimeZoneById(
                        "America/Sao_Paulo");
            }
            catch
            {
                return TimeZoneInfo
                    .FindSystemTimeZoneById(
                        "E. South America Standard Time");
            }
        }

        private static DateTime
            ObterAgoraBrasil()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                ObterFusoHorarioBrasil());
        }

        private static DateTime
            ConverterDataLocalParaUtc(
                DateTime dataLocal)
        {
            var dataSemFuso =
                DateTime.SpecifyKind(
                    dataLocal,
                    DateTimeKind.Unspecified);

            return TimeZoneInfo.ConvertTimeToUtc(
                dataSemFuso,
                ObterFusoHorarioBrasil());
        }

        private static DateTime
            ConverterUtcParaLocal(
                DateTime dataUtc)
        {
            var utc =
                dataUtc.Kind == DateTimeKind.Utc
                    ? dataUtc
                    : DateTime.SpecifyKind(
                        dataUtc,
                        DateTimeKind.Utc);

            return TimeZoneInfo.ConvertTimeFromUtc(
                utc,
                ObterFusoHorarioBrasil());
        }
    }
}