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

        // =========================================================
        // ADMINISTRADOR
        // =========================================================

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

        // =========================================================
        // CALCULAR PRÓXIMO ENVIO
        // =========================================================

        private DateTime CalcularProximoEnvioUtc(
            DayOfWeek diaSemana,
            TimeSpan horario)
        {
            var agoraBrasil =
                ObterAgoraBrasil();

            var diferencaDias =
                ((int)diaSemana -
                 (int)agoraBrasil.DayOfWeek +
                 7) % 7;

            var proximaDataBrasil =
                agoraBrasil.Date
                    .AddDays(diferencaDias)
                    .Add(horario);

            /*
             * Se o dia escolhido for hoje, mas o horário
             * já tiver passado, o próximo envio será
             * na semana seguinte.
             */
            if (proximaDataBrasil <= agoraBrasil)
            {
                proximaDataBrasil =
                    proximaDataBrasil.AddDays(7);
            }

            return ConverterBrasilParaUtc(
                proximaDataBrasil);
        }

        // =========================================================
        // ESTABELECIMENTO
        // =========================================================

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

        // =========================================================
        // LISTAGEM
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index(
            int pagina = 1)
        {
            var administrador =
                await ObterAdministrador();

            if (administrador == null)
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            const int itensPorPagina = 10;

            if (pagina < 1)
            {
                pagina = 1;
            }

            /*
             * Lembretes ativos aparecem primeiro.
             * Dentro deles, mostramos primeiro o que
             * possui o próximo envio mais próximo.
             */
            var consulta =
                _context.LembretesPedidos
                    .AsNoTracking()
                    .Include(l => l.Cliente)
                    .OrderByDescending(l => l.Ativo)
                    .ThenBy(l => l.ProximoEnvio);

            var totalItens =
                await consulta.CountAsync();

            var totalPaginas =
                (int)Math.Ceiling(
                    totalItens /
                    (double)itensPorPagina);

            if (totalPaginas > 0 &&
                pagina > totalPaginas)
            {
                pagina = totalPaginas;
            }

            var lembretes =
                await consulta
                    .Skip(
                        (pagina - 1) *
                        itensPorPagina)
                    .Take(itensPorPagina)
                    .ToListAsync();

            /*
             * ProximoEnvio e UltimoEnvio ficam no banco
             * em UTC. Para exibição no painel,
             * convertemos para o horário de Brasília.
             */
            foreach (var lembrete in lembretes)
            {
                lembrete.ProximoEnvio =
                    ConverterUtcParaBrasil(
                        lembrete.ProximoEnvio);

                if (lembrete.UltimoEnvio.HasValue)
                {
                    lembrete.UltimoEnvio =
                        ConverterUtcParaBrasil(
                            lembrete.UltimoEnvio.Value);
                }
            }

            ViewBag.PaginaAtual = pagina;
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.TotalItens = totalItens;

            await CarregarEstabelecimento();

            return View(lembretes);
        }

        // =========================================================
        // CRIAR - GET
        // =========================================================

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
                    DiaSemana =
                        agoraBrasil.DayOfWeek,

                    Horario =
                        new TimeSpan(8, 0, 0),

                    Mensagem =
                        "Olá! Passando para lembrar você de fazer o seu pedido."
                };

            return View(model);
        }

        // =========================================================
        // CRIAR - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(
            LembretePedido model,
            List<DayOfWeek> diasSemana,
            List<TimeSpan> horarios)
        {
            var administrador =
                await ObterAdministrador();

            if (administrador == null)
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            ModelState.Remove(
                nameof(LembretePedido.NomeCliente));

            ModelState.Remove(
                nameof(LembretePedido.Telefone));

            ModelState.Remove(
                nameof(LembretePedido.Cliente));

            ModelState.Remove(
                nameof(LembretePedido.UsuarioResponsavel));

            ModelState.Remove(
                nameof(LembretePedido.ProximoEnvio));

            ModelState.Remove(
                nameof(LembretePedido.DiaSemana));

            ModelState.Remove(
                nameof(LembretePedido.Horario));

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
                string.IsNullOrWhiteSpace(
                    cliente.Telefone))
            {
                ModelState.AddModelError(
                    nameof(model.ClienteId),
                    "O cliente não possui telefone cadastrado.");
            }

            diasSemana ??= new List<DayOfWeek>();
            horarios ??= new List<TimeSpan>();

            if (diasSemana.Count == 0 ||
                horarios.Count == 0 ||
                diasSemana.Count != horarios.Count)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Informe pelo menos um dia da semana e horário.");
            }
            else if (diasSemana.Count > 3)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Você pode programar no máximo 3 lembretes semanais para este cliente.");
            }

            for (int i = 0;
                 i < Math.Min(
                     diasSemana.Count,
                     horarios.Count);
                 i++)
            {
                if (!Enum.IsDefined(
                        typeof(DayOfWeek),
                        diasSemana[i]))
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"O dia do lembrete {i + 1} é inválido.");
                }

                if (horarios[i] < TimeSpan.Zero ||
                    horarios[i] >= TimeSpan.FromDays(1))
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"O horário do lembrete {i + 1} é inválido.");
                }
            }

            if (diasSemana.Count == horarios.Count)
            {
                var combinacoesDuplicadas =
                    diasSemana
                        .Select((dia, indice) =>
                            new
                            {
                                Dia = dia,
                                Horario = horarios[indice]
                            })
                        .GroupBy(x =>
                            new
                            {
                                x.Dia,
                                x.Horario
                            })
                        .Any(g => g.Count() > 1);

                if (combinacoesDuplicadas)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Não repita o mesmo dia e horário no mesmo cadastro.");
                }
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

            var configuracao =
                await _context.ConfiguracoesSistema
                    .AsNoTracking()
                    .OrderBy(c => c.Id)
                    .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(
                    configuracao?.UrlSistema))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "A URL do sistema não está configurada. Configure a URL antes de criar o lembrete.");
            }

            if (cliente != null &&
                diasSemana.Count > 0 &&
                diasSemana.Count == horarios.Count &&
                diasSemana.Count <= 3)
            {
                var lembretesAtivosExistentes =
                    await _context.LembretesPedidos
                        .CountAsync(l =>
                            l.ClienteId == cliente.Id &&
                            l.Ativo);

                if (lembretesAtivosExistentes +
                    diasSemana.Count > 3)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"Este cliente já possui {lembretesAtivosExistentes} lembrete(s) ativo(s). O limite é de 3 lembretes ativos por cliente.");
                }

                var combinacoesNovas =
                    diasSemana
                        .Select((dia, indice) =>
                            new
                            {
                                Dia = dia,
                                Horario = horarios[indice]
                            })
                        .ToList();

                var lembretesExistentes =
                    await _context.LembretesPedidos
                        .AsNoTracking()
                        .Where(l =>
                            l.ClienteId == cliente.Id &&
                            l.Ativo)
                        .Select(l =>
                            new
                            {
                                l.DiaSemana,
                                l.Horario
                            })
                        .ToListAsync();

                if (combinacoesNovas.Any(novo =>
                    lembretesExistentes.Any(existente =>
                        existente.DiaSemana == novo.Dia &&
                        existente.Horario == novo.Horario)))
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Este cliente já possui um lembrete ativo com um dos dias e horários informados.");
                }
            }

            if (!ModelState.IsValid)
            {
                await CarregarClientes();
                await CarregarEstabelecimento();

                ViewBag.DiasSemanaSelecionados =
                    diasSemana;

                ViewBag.HorariosSelecionados =
                    horarios;

                return View(model);
            }

            for (int i = 0;
                 i < diasSemana.Count;
                 i++)
            {
                var proximoEnvioUtc =
                    CalcularProximoEnvioUtc(
                        diasSemana[i],
                        horarios[i]);

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

                        DiaSemana =
                            diasSemana[i],

                        Horario =
                            horarios[i],

                        ProximoEnvio =
                            proximoEnvioUtc,

                        DataCadastro =
                            DateTime.UtcNow,

                        UltimoEnvio =
                            null,

                        Ativo =
                            true,

                        UsuarioResponsavelId =
                            administrador.Id,

                        ErroEnvio =
                            null
                    };

                _context.LembretesPedidos.Add(
                    lembrete);
            }

            await _context.SaveChangesAsync();

            TempData["Sucesso"] =
                diasSemana.Count == 1
                    ? "Lembrete recorrente criado com sucesso."
                    : $"{diasSemana.Count} lembretes recorrentes criados com sucesso.";

            return RedirectToAction(
                nameof(Index));
        }

        // =========================================================
        // CANCELAR / DESATIVAR
        // =========================================================

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

            if (!lembrete.Ativo)
            {
                TempData["Erro"] =
                    "Este lembrete já está desativado.";

                return RedirectToAction(
                    nameof(Index));
            }

            lembrete.Ativo = false;

            await _context.SaveChangesAsync();

            TempData["Sucesso"] =
                "Lembrete desativado com sucesso.";

            return RedirectToAction(
                nameof(Index));
        }

        // =========================================================
        // REATIVAR
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reativar(
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

            if (lembrete.Ativo)
            {
                TempData["Erro"] =
                    "Este lembrete já está ativo.";

                return RedirectToAction(
                    nameof(Index));
            }

            /*
             * Ao reativar, recalculamos a próxima
             * ocorrência semanal.
             */
            lembrete.ProximoEnvio =
                CalcularProximoEnvioUtc(
                    lembrete.DiaSemana,
                    lembrete.Horario);

            lembrete.Ativo = true;
            lembrete.ErroEnvio = null;

            await _context.SaveChangesAsync();

            TempData["Sucesso"] =
                "Lembrete reativado com sucesso.";

            return RedirectToAction(
                nameof(Index));
        }

        // =========================================================
        // EXCLUIR
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(
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

            /*
             * Para evitar exclusão acidental,
             * um lembrete ativo precisa primeiro
             * ser desativado.
             */
            if (lembrete.Ativo)
            {
                TempData["Erro"] =
                    "Desative o lembrete antes de excluí-lo.";

                return RedirectToAction(
                    nameof(Index));
            }

            _context.LembretesPedidos.Remove(
                lembrete);

            await _context.SaveChangesAsync();

            TempData["Sucesso"] =
                "Lembrete excluído com sucesso.";

            return RedirectToAction(
                nameof(Index));
        }

        // =========================================================
        // CLIENTES
        // =========================================================

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