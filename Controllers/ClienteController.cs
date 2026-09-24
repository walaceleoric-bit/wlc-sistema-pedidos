using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WlcSistemaPedidos.Data;
using WlcSistemaPedidos.Models;

namespace WlcSistemaPedidos.Controllers
{
    [Authorize]
    public class ClienteController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public ClienteController(
            AppDbContext context,
            UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<Cliente?> ObterClienteLogado()
        {
            var usuario = await _userManager.GetUserAsync(User);

            if (usuario == null ||
                !usuario.Ativo ||
                usuario.Perfil != PerfilUsuario.Cliente)
            {
                return null;
            }

            return await _context.Clientes
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.UsuarioId == usuario.Id);
        }

        private async Task CarregarEstabelecimento()
        {
            var configuracao = await _context.ConfiguracoesSistema
                .AsNoTracking()
                .OrderBy(c => c.Id)
                .FirstOrDefaultAsync();

            var nomeEstabelecimento =
                configuracao?.NomeEstabelecimento;

            if (string.IsNullOrWhiteSpace(nomeEstabelecimento))
            {
                nomeEstabelecimento =
                    "Sistema de Pedidos";
            }

            var logoEstabelecimento =
                configuracao?.LogoUrl;

            if (string.IsNullOrWhiteSpace(logoEstabelecimento))
            {
                logoEstabelecimento =
                    "/images/logo-wlc.png";
            }

            ViewBag.NomeEstabelecimento =
                nomeEstabelecimento;

            ViewBag.LogoEstabelecimento =
                logoEstabelecimento;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cliente = await ObterClienteLogado();

            if (cliente == null || !cliente.Ativo)
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            ViewBag.NomeCliente = cliente.Nome;
            ViewBag.SaldoDevedor = cliente.SaldoDevedor;
            ViewBag.PermitirNovosPedidos =
                cliente.PermitirNovosPedidos;

            await CarregarEstabelecimento();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Produtos(
            int pagina = 1,
            string? busca = null,
            int? categoriaId = null)
        {
            var cliente = await ObterClienteLogado();

            if (cliente == null || !cliente.Ativo)
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

            var consulta = _context.Produtos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .Where(p =>
                    p.Ativo &&
                    p.Disponivel &&
                    p.Categoria.Ativa)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(busca))
            {
                busca = busca.Trim();

                consulta = consulta.Where(p =>
                    EF.Functions.ILike(
                        p.Nome,
                        $"%{busca}%") ||
                    (
                        p.Descricao != null &&
                        EF.Functions.ILike(
                            p.Descricao,
                            $"%{busca}%")
                    ));
            }

            if (categoriaId.HasValue)
            {
                consulta = consulta.Where(p =>
                    p.CategoriaId ==
                    categoriaId.Value);
            }

            var totalProdutos =
                await consulta.CountAsync();

            var totalPaginas =
                (int)Math.Ceiling(
                    totalProdutos /
                    (double)itensPorPagina);

            if (totalPaginas > 0 &&
                pagina > totalPaginas)
            {
                pagina = totalPaginas;
            }

            var produtos = await consulta
                .OrderBy(p => p.OrdemExibicao)
                .ThenBy(p => p.Nome)
                .Skip((pagina - 1) * itensPorPagina)
                .Take(itensPorPagina)
                .ToListAsync();

            var categorias = await _context.Categorias
                .AsNoTracking()
                .Where(c => c.Ativa)
                .OrderBy(c => c.OrdemExibicao)
                .ThenBy(c => c.Nome)
                .ToListAsync();

            ViewBag.NomeCliente = cliente.Nome;
            ViewBag.SaldoDevedor = cliente.SaldoDevedor;
            ViewBag.PermitirNovosPedidos =
                cliente.PermitirNovosPedidos;

            ViewBag.Categorias = categorias;
            ViewBag.PaginaAtual = pagina;
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.TotalProdutos = totalProdutos;
            ViewBag.Busca = busca;
            ViewBag.CategoriaId = categoriaId;

            await CarregarEstabelecimento();

            return View(produtos);
        }

        [HttpGet]
        public async Task<IActionResult> Historico(
            int pagina = 1,
            string? busca = null,
            StatusPedido? status = null)
        {
            var cliente = await ObterClienteLogado();

            if (cliente == null || !cliente.Ativo)
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
                .Where(p =>
                    p.ClienteId == cliente.Id)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(busca))
            {
                busca = busca.Trim();

                if (busca.StartsWith("#"))
                {
                    busca = busca.Substring(1).Trim();
                }

                if (int.TryParse(busca, out int numeroPedido))
                {
                    consulta = consulta.Where(p =>
                        p.Id == numeroPedido);
                }
                else
                {
                    consulta = consulta.Where(p => false);
                }
            }

            if (status.HasValue &&
                Enum.IsDefined(typeof(StatusPedido), status.Value))
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

            ViewBag.NomeCliente = cliente.Nome;
            ViewBag.SaldoDevedor = cliente.SaldoDevedor;
            ViewBag.PaginaAtual = pagina;
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.TotalPedidos = totalPedidos;
            ViewBag.Busca = busca;
            ViewBag.StatusSelecionado = status;

            await CarregarEstabelecimento();

            return View(pedidos);
        }
    }
}
