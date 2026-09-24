using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WlcSistemaPedidos.Data;
using WlcSistemaPedidos.Models;

namespace WlcSistemaPedidos.Controllers
{
    [Authorize]
    public class LembretesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public LembretesController(
            AppDbContext context,
            UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<Usuario?> ObterAdministrador()
        {
            var usuario =
                await _userManager.GetUserAsync(User);

            if (usuario == null ||
                !usuario.Ativo ||
                usuario.Perfil != PerfilUsuario.Administrador)
            {
                return null;
            }

            return usuario;
        }

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

        private DateTime ObterAgoraBrasil()
        {
            var fusoBrasil =
                ObterFusoHorarioBrasil();

            return TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                fusoBrasil);
        }

        private DateTime ConverterBrasilParaUtc(
            DateTime dataHoraBrasil)
        {
            var fusoBrasil =
                ObterFusoHorarioBrasil();

            var dataSemFuso =
                DateTime.SpecifyKind(
                    dataHoraBrasil,
                    DateTimeKind.Unspecified);

            return TimeZoneInfo.ConvertTimeToUtc(
                dataSemFuso,
                fusoBrasil);
        }

        private DateTime ConverterUtcParaBrasil(
            DateTime dataHoraUtc)
        {
            var fusoBrasil =
                ObterFusoHorarioBrasil();

            var utc =
                DateTime.SpecifyKind(
                    dataHoraUtc,
                    DateTimeKind.Utc);

            return TimeZoneInfo.ConvertTimeFromUtc(
                utc,
                fusoBrasil);
        }

        private async Task CarregarEstabelecimento()
        {
            var configuracao =
                await _context.ConfiguracoesSistema
                    .AsNoTracking()
                    .OrderBy(c => c.Id)
                    .FirstOrDefaultAsync();

            ViewBag.NomeEstabelecimento =
                string.IsNullOrWhiteSpace(
                    configuracao?.NomeEstabelecimento)
                    ? "Sistema de Pedidos"
                    : configuracao.NomeEstabelecimento;

            ViewBag.LogoEstabelecimento =
                string.IsNullOrWhiteSpace(
                    configuracao?.LogoUrl)
                    ? "/images/logo-wlc.png"
                    : configuracao.LogoUrl;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var administrador =
                await ObterAdministrador();

            if (administrador == null)
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            var lembretes =
                await _context.LembretesPedidos
                    .AsNoTracking()
                    .Include(l => l.Cliente)
                    .OrderBy(l => l.Enviado)
                    .ThenBy(l => l.DataHoraAgendada)
                    .ToListAsync();

            /*
             * No banco os horários ficam em UTC.
             * Para exibição, convertemos para o horário
             * de Brasília.
             */
            foreach (var lembrete in lembretes)
            {
                lembrete.DataHoraAgendada =
                    ConverterUtcParaBrasil(
                        lembrete.DataHoraAgendada);

                if (lembrete.DataEnvio.HasValue)
                {
                    lembrete.DataEnvio =
                        ConverterUtcParaBrasil(
                            lembrete.DataEnvio.Value);
                }
            }

            await CarregarEstabelecimento();

            return View(lembretes);
        }

        [HttpGet]
        public async Task<IActionResult> Criar()
        {
            var administrador =
                await ObterAdministrador();

            if (administrador == null)
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            await CarregarClientes();
            await CarregarEstabelecimento();

            var agoraBrasil =
                ObterAgoraBrasil();

            var model =
                new LembretePedido
                {
                    DataHoraAgendada =
                        agoraBrasil.AddHours(1),

                    Mensagem =
                        "Olá! Passando para lembrar do seu pedido. " +
                        "Acesse nosso sistema para fazer seu pedido."
                };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(
            LembretePedido model)
        {
            var administrador =
                await ObterAdministrador();

            if (administrador == null)
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            /*
             * Estes campos não são digitados
             * diretamente no formulário.
             */
            ModelState.Remove(
                nameof(LembretePedido.NomeCliente));

            ModelState.Remove(
                nameof(LembretePedido.Telefone));

            ModelState.Remove(
                nameof(LembretePedido.Cliente));

            ModelState.Remove(
                nameof(LembretePedido.UsuarioResponsavel));

            var cliente =
                await _context.Clientes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c =>
                        c.Id == model.ClienteId &&
                        c.Ativo);

            if (cliente == null)
            {
                ModelState.AddModelError(
                    nameof(model.ClienteId),
                    "Cliente não encontrado ou inativo.");
            }

            if (cliente != null &&
                string.IsNullOrWhiteSpace(cliente.Telefone))
            {
                ModelState.AddModelError(
                    nameof(model.ClienteId),
                    "O cliente não possui telefone cadastrado.");
            }

            var agoraBrasil =
                ObterAgoraBrasil();

            if (model.DataHoraAgendada <= agoraBrasil)
            {
                ModelState.AddModelError(
                    nameof(model.DataHoraAgendada),
                    "Informe uma data e horário futuros.");
            }

            model.Mensagem =
                model.Mensagem?.Trim()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(
                model.Mensagem))
            {
                ModelState.AddModelError(
                    nameof(model.Mensagem),
                    "Informe a mensagem do lembrete.");
            }

            if (!ModelState.IsValid)
            {
                await CarregarClientes();
                await CarregarEstabelecimento();

                return View(model);
            }

            /*
             * O ADM informa o horário de Brasília.
             * Antes de salvar no PostgreSQL,
             * convertemos para UTC.
             */
            var dataHoraUtc =
                ConverterBrasilParaUtc(
                    model.DataHoraAgendada);

            var lembrete =
                new LembretePedido
                {
                    ClienteId =
                        cliente!.Id,

                    NomeCliente =
                        cliente.Nome,

                    Telefone =
                        cliente.Telefone!,

                    Mensagem =
                        model.Mensagem,

                    DataHoraAgendada =
                        dataHoraUtc,

                    DataCadastro =
                        DateTime.UtcNow,

                    DataEnvio =
                        null,

                    Enviado =
                        false,

                    Ativo =
                        true,

                    UsuarioResponsavelId =
                        administrador.Id,

                    ErroEnvio =
                        null
                };

            _context.LembretesPedidos.Add(
                lembrete);

            await _context.SaveChangesAsync();

            TempData["Sucesso"] =
                "Lembrete agendado com sucesso.";

            return RedirectToAction(
                nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(
            int id)
        {
            var administrador =
                await ObterAdministrador();

            if (administrador == null)
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            var lembrete =
                await _context.LembretesPedidos
                    .FirstOrDefaultAsync(l =>
                        l.Id == id);

            if (lembrete == null)
            {
                return NotFound();
            }

            if (lembrete.Enviado)
            {
                TempData["Erro"] =
                    "Este lembrete já foi enviado.";

                return RedirectToAction(
                    nameof(Index));
            }

            if (!lembrete.Ativo)
            {
                TempData["Erro"] =
                    "Este lembrete já está cancelado.";

                return RedirectToAction(
                    nameof(Index));
            }

            lembrete.Ativo = false;

            await _context.SaveChangesAsync();

            TempData["Sucesso"] =
                "Lembrete cancelado com sucesso.";

            return RedirectToAction(
                nameof(Index));
        }

        private async Task CarregarClientes()
        {
            ViewBag.Clientes =
                await _context.Clientes
                    .AsNoTracking()
                    .Where(c =>
                        c.Ativo &&
                        c.Telefone != null &&
                        c.Telefone != "")
                    .OrderBy(c => c.Nome)
                    .ToListAsync();
        }
    }
}