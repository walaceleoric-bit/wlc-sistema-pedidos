using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WlcSistemaPedidos.Data;
using WlcSistemaPedidos.Models;

namespace WlcSistemaPedidos.Controllers
{
    [Authorize]
    public class NotasFiscaisController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public NotasFiscaisController(
            AppDbContext context,
            UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<bool> UsuarioEhAdministrador()
        {
            var usuario =
                await _userManager.GetUserAsync(User);

            return usuario != null &&
                   usuario.Ativo &&
                   usuario.Perfil ==
                       PerfilUsuario.Administrador;
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

            ViewBag.EmissaoFiscalAtiva =
                configuracao?.EmissaoFiscalAtiva ?? false;

            ViewBag.AmbienteFiscal =
                configuracao?.AmbienteFiscal;

            ViewBag.ProvedorFiscal =
                configuracao?.ProvedorFiscal;
        }

        // ==========================================
        // LISTAGEM
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Index(
            int pagina = 1,
            StatusNotaFiscal? status = null)
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

            var consulta =
                _context.NotasFiscais
                    .AsNoTracking()
                    .Include(n => n.Pedido)
                    .ThenInclude(p => p.Cliente)
                    .AsQueryable();

            if (status.HasValue)
            {
                consulta = consulta.Where(n =>
                    n.Status == status.Value);
            }

            var totalNotas =
                await consulta.CountAsync();

            var totalPaginas =
                (int)Math.Ceiling(
                    totalNotas /
                    (double)itensPorPagina);

            if (totalPaginas > 0 &&
                pagina > totalPaginas)
            {
                pagina = totalPaginas;
            }

            var notas =
                await consulta
                    .OrderByDescending(n =>
                        n.DataCriacao)
                    .Skip(
                        (pagina - 1) *
                        itensPorPagina)
                    .Take(itensPorPagina)
                    .ToListAsync();

            ViewBag.PaginaAtual =
                pagina;

            ViewBag.TotalPaginas =
                totalPaginas;

            ViewBag.TotalNotas =
                totalNotas;

            ViewBag.StatusSelecionado =
                status;

            await CarregarEstabelecimento();

            return View(notas);
        }

        // ==========================================
        // PEDIDOS DISPONÍVEIS PARA NOTA
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> PedidosDisponiveis(
            int pagina = 1)
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

            var consulta =
                _context.Pedidos
                    .AsNoTracking()
                    .Include(p => p.Cliente)
                    .Where(p =>
                        p.Status ==
                            StatusPedido.Finalizado &&
                        p.NotaFiscal == null);

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

            var pedidos =
                await consulta
                    .OrderByDescending(p =>
                        p.DataFinalizacao ??
                        p.DataPedido)
                    .Skip(
                        (pagina - 1) *
                        itensPorPagina)
                    .Take(itensPorPagina)
                    .ToListAsync();

            ViewBag.PaginaAtual =
                pagina;

            ViewBag.TotalPaginas =
                totalPaginas;

            ViewBag.TotalPedidos =
                totalPedidos;

            await CarregarEstabelecimento();

            return View(pedidos);
        }

        // ==========================================
        // PREPARAR NOTA
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Preparar(
            int pedidoId)
        {
            var usuario =
                await _userManager.GetUserAsync(User);

            if (usuario == null ||
                !usuario.Ativo ||
                usuario.Perfil !=
                    PerfilUsuario.Administrador)
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            var pedido =
                await _context.Pedidos
                    .Include(p => p.NotaFiscal)
                    .FirstOrDefaultAsync(p =>
                        p.Id == pedidoId);

            if (pedido == null)
            {
                return NotFound();
            }

            if (pedido.Status !=
                StatusPedido.Finalizado)
            {
                TempData["Erro"] =
                    "Somente pedidos finalizados podem ser preparados para emissão fiscal.";

                return RedirectToAction(
                    nameof(PedidosDisponiveis));
            }

            if (pedido.NotaFiscal != null)
            {
                TempData["Erro"] =
                    $"O pedido #{pedido.Id} já possui registro fiscal.";

                return RedirectToAction(
                    nameof(Index));
            }

            var notaFiscal =
                new NotaFiscal
                {
                    PedidoId =
                        pedido.Id,

                    Status =
                        StatusNotaFiscal.Pendente,

                    DataCriacao =
                        DateTime.UtcNow,

                    UsuarioResponsavelId =
                        usuario.Id
                };

            _context.NotasFiscais.Add(
                notaFiscal);

            await _context.SaveChangesAsync();

            TempData["Sucesso"] =
                $"Pedido #{pedido.Id} preparado para emissão fiscal.";

            return RedirectToAction(
                nameof(Index));
        }

        // ==========================================
        // DETALHES
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Detalhes(
            int id)
        {
            if (!await UsuarioEhAdministrador())
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            var nota =
                await _context.NotasFiscais
                    .AsNoTracking()
                    .Include(n => n.Pedido)
                        .ThenInclude(p =>
                            p.Cliente)
                    .Include(n => n.Pedido)
                        .ThenInclude(p =>
                            p.Itens)
                    .Include(n =>
                        n.UsuarioResponsavel)
                    .FirstOrDefaultAsync(n =>
                        n.Id == id);

            if (nota == null)
            {
                return NotFound();
            }

            await CarregarEstabelecimento();

            return View(nota);
        }
    }
}