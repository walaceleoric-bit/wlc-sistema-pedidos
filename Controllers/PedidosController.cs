using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WlcSistemaPedidos.Data;
using WlcSistemaPedidos.Models;

namespace WlcSistemaPedidos.Controllers
{
    [Authorize]
    public class PedidosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public PedidosController(
            AppDbContext context,
            UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<bool> UsuarioEhAdministrador()
        {
            var usuario = await _userManager.GetUserAsync(User);

            return usuario != null &&
                   usuario.Ativo &&
                   usuario.Perfil == PerfilUsuario.Administrador;
        }

        private async Task CarregarEstabelecimento()
        {
            var configuracao = await _context.ConfiguracoesSistema
                .AsNoTracking()
                .OrderBy(c => c.Id)
                .FirstOrDefaultAsync();

            ViewBag.NomeEstabelecimento =
                string.IsNullOrWhiteSpace(configuracao?.NomeEstabelecimento)
                    ? "Sistema de Pedidos"
                    : configuracao.NomeEstabelecimento;

            ViewBag.LogoEstabelecimento =
                string.IsNullOrWhiteSpace(configuracao?.LogoUrl)
                    ? "/images/logo-wlc.png"
                    : configuracao.LogoUrl;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            int pagina = 1,
            StatusPedido? status = null)
        {
            if (!await UsuarioEhAdministrador())
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

            var consulta = _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Cliente)
                .AsQueryable();

            if (status.HasValue)
            {
                consulta = consulta.Where(p =>
                    p.Status == status.Value);
            }

            var totalPedidos =
                await consulta.CountAsync();

            var totalPaginas =
                (int)Math.Ceiling(
                    totalPedidos /
                    (double)itensPorPagina);

            if (totalPaginas > 0 &&
                pagina > totalPaginas)
            {
                pagina = totalPaginas;
            }

            var pedidos = await consulta
                .OrderByDescending(p => p.DataPedido)
                .Skip((pagina - 1) * itensPorPagina)
                .Take(itensPorPagina)
                .ToListAsync();

            ViewBag.PaginaAtual = pagina;
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.TotalPedidos = totalPedidos;
            ViewBag.StatusSelecionado = status;

            await CarregarEstabelecimento();

            return View(pedidos);
        }

        [HttpGet]
        public async Task<IActionResult> Historico(
            int pagina = 1,
            string? busca = null,
            StatusPedido? status = null)
        {
            if (!await UsuarioEhAdministrador())
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

            var consulta = _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Cliente)
                .Where(p =>
                    p.Status == StatusPedido.Finalizado ||
                    p.Status == StatusPedido.Cancelado)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(busca))
            {
                busca = busca.Trim();

                var buscaSemCerquilha =
                    busca.StartsWith("#")
                        ? busca.Substring(1).Trim()
                        : busca;

                if (int.TryParse(
                    buscaSemCerquilha,
                    out int numeroPedido))
                {
                    consulta = consulta.Where(p =>
                        p.Id == numeroPedido);
                }
                else
                {
                    consulta = consulta.Where(p =>
                        EF.Functions.ILike(
                            p.NomeCliente,
                            $"%{busca}%"));
                }
            }

            if (status.HasValue &&
                (status.Value == StatusPedido.Finalizado ||
                 status.Value == StatusPedido.Cancelado))
            {
                consulta = consulta.Where(p =>
                    p.Status == status.Value);
            }

            var totalPedidos =
                await consulta.CountAsync();

            var totalPaginas =
                (int)Math.Ceiling(
                    totalPedidos /
                    (double)itensPorPagina);

            if (totalPaginas > 0 &&
                pagina > totalPaginas)
            {
                pagina = totalPaginas;
            }

            var pedidos = await consulta
                .OrderByDescending(p => p.DataPedido)
                .Skip((pagina - 1) * itensPorPagina)
                .Take(itensPorPagina)
                .ToListAsync();

            ViewBag.PaginaAtual = pagina;
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.TotalPedidos = totalPedidos;
            ViewBag.Busca = busca;
            ViewBag.StatusSelecionado = status;

            await CarregarEstabelecimento();

            return View(pedidos);
        }

        [HttpGet]
        public async Task<IActionResult> Detalhes(int id)
        {
            if (!await UsuarioEhAdministrador())
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            var pedido = await _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Cliente)
                .Include(p => p.Itens)
                .FirstOrDefaultAsync(p =>
                    p.Id == id);

            if (pedido == null)
            {
                return NotFound();
            }

            await CarregarEstabelecimento();

            return View(pedido);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlterarStatus(
            int id,
            StatusPedido status)
        {
            var usuario = await _userManager.GetUserAsync(User);

            if (usuario == null ||
                !usuario.Ativo ||
                usuario.Perfil != PerfilUsuario.Administrador)
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            if (!Enum.IsDefined(typeof(StatusPedido), status))
            {
                TempData["Erro"] =
                    "Status do pedido inválido.";

                return RedirectToAction(
                    nameof(Detalhes),
                    new { id });
            }

            var pedido = await _context.Pedidos
                .Include(p => p.Cliente)
                .FirstOrDefaultAsync(p =>
                    p.Id == id);

            if (pedido == null)
            {
                return NotFound();
            }

            if (pedido.Status == StatusPedido.Cancelado)
            {
                TempData["Erro"] =
                    "Um pedido cancelado está encerrado e não pode ter o status alterado.";

                return RedirectToAction(
                    nameof(Detalhes),
                    new { id });
            }

            if (pedido.Status == StatusPedido.Finalizado)
            {
                TempData["Erro"] =
                    "Um pedido finalizado está encerrado e não pode ter o status alterado.";

                return RedirectToAction(
                    nameof(Detalhes),
                    new { id });
            }

            if (pedido.Status == status)
            {
                TempData["Erro"] =
                    "O pedido já está nesse status.";

                return RedirectToAction(
                    nameof(Detalhes),
                    new { id });
            }

            bool transicaoValida =
                (pedido.Status == StatusPedido.Novo &&
                    (status == StatusPedido.Confirmado ||
                     status == StatusPedido.Cancelado)) ||

                (pedido.Status == StatusPedido.Confirmado &&
                    (status == StatusPedido.Preparando ||
                     status == StatusPedido.Cancelado)) ||

                (pedido.Status == StatusPedido.Preparando &&
                    (status == StatusPedido.SaiuParaEntrega ||
                     status == StatusPedido.Cancelado)) ||

                (pedido.Status == StatusPedido.SaiuParaEntrega &&
                    (status == StatusPedido.Finalizado ||
                     status == StatusPedido.Cancelado));

            if (!transicaoValida)
            {
                TempData["Erro"] =
                    "Alteração de status não permitida. Siga a sequência normal do pedido.";

                return RedirectToAction(
                    nameof(Detalhes),
                    new { id });
            }

            await using var transacao =
                await _context.Database.BeginTransactionAsync();

            try
            {
                bool debitoJaLancado =
                    await _context.MovimentacoesFinanceiras
                        .AnyAsync(m =>
                            m.PedidoId == pedido.Id &&
                            m.Tipo ==
                                TipoMovimentacaoFinanceira.Debito);

                bool estornoJaLancado =
                    await _context.MovimentacoesFinanceiras
                        .AnyAsync(m =>
                            m.PedidoId == pedido.Id &&
                            m.Tipo ==
                                TipoMovimentacaoFinanceira.Estorno);

                if (status == StatusPedido.Confirmado &&
                    !debitoJaLancado)
                {
                    if (pedido.ClienteId == null ||
                        pedido.Cliente == null)
                    {
                        await transacao.RollbackAsync();

                        TempData["Erro"] =
                            "Não foi possível confirmar o pedido porque ele não possui um cliente vinculado.";

                        return RedirectToAction(
                            nameof(Detalhes),
                            new { id });
                    }

                    pedido.Cliente.SaldoDevedor +=
                        pedido.Total;

                    _context.MovimentacoesFinanceiras.Add(
                        new MovimentacaoFinanceira
                        {
                            ClienteId =
                                pedido.Cliente.Id,

                            PedidoId =
                                pedido.Id,

                            Tipo =
                                TipoMovimentacaoFinanceira.Debito,

                            Valor =
                                pedido.Total,

                            Observacao =
                                $"Débito automático referente ao pedido #{pedido.Id}.",

                            DataMovimentacao =
                                DateTime.UtcNow,

                            UsuarioResponsavelId =
                                usuario.Id
                        });
                }

                if (status == StatusPedido.Cancelado &&
                    debitoJaLancado &&
                    !estornoJaLancado)
                {
                    if (pedido.ClienteId == null ||
                        pedido.Cliente == null)
                    {
                        await transacao.RollbackAsync();

                        TempData["Erro"] =
                            "Não foi possível cancelar o pedido porque o cliente vinculado não foi encontrado.";

                        return RedirectToAction(
                            nameof(Detalhes),
                            new { id });
                    }

                    pedido.Cliente.SaldoDevedor =
                        Math.Max(
                            0m,
                            pedido.Cliente.SaldoDevedor -
                            pedido.Total);

                    _context.MovimentacoesFinanceiras.Add(
                        new MovimentacaoFinanceira
                        {
                            ClienteId =
                                pedido.Cliente.Id,

                            PedidoId =
                                pedido.Id,

                            Tipo =
                                TipoMovimentacaoFinanceira.Estorno,

                            Valor =
                                pedido.Total,

                            Observacao =
                                $"Estorno automático referente ao cancelamento do pedido #{pedido.Id}.",

                            DataMovimentacao =
                                DateTime.UtcNow,

                            UsuarioResponsavelId =
                                usuario.Id
                        });
                }

                pedido.Status = status;

                if (status == StatusPedido.Finalizado)
                {
                    pedido.DataFinalizacao ??=
                        DateTime.UtcNow;
                }
                else
                {
                    pedido.DataFinalizacao = null;
                }

                await _context.SaveChangesAsync();
                await transacao.CommitAsync();

                if (status == StatusPedido.Cancelado &&
                    debitoJaLancado &&
                    !estornoJaLancado)
                {
                    TempData["Sucesso"] =
                        $"Pedido #{pedido.Id} cancelado e valor estornado do financeiro.";
                }
                else
                {
                    TempData["Sucesso"] =
                        $"Status do pedido #{pedido.Id} atualizado.";
                }

                return RedirectToAction(
                    nameof(Detalhes),
                    new { id = pedido.Id });
            }
            catch
            {
                await transacao.RollbackAsync();

                TempData["Erro"] =
                    "Não foi possível atualizar o pedido. Nenhuma alteração financeira foi realizada.";

                return RedirectToAction(
                    nameof(Detalhes),
                    new { id });
            }
        }
    }
}
