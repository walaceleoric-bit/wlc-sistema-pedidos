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
    public class CaixaController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public CaixaController(
            AppDbContext context,
            UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================================
        // CAIXA
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index(
            string periodo = "hoje",
            int pagina = 1)
        {
            var usuario =
                await ObterAdministradorAsync();

            if (usuario == null)
            {
                return RedirectToAction(
                    "Login",
                    "Conta");
            }

            if (usuario.Perfil !=
                PerfilUsuario.Administrador)
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            if (pagina < 1)
            {
                pagina = 1;
            }

            // =====================================================
            // PERÍODO
            // =====================================================

            var hoje =
                DateTime.Today;

            DateTime inicioLocal;
            DateTime fimLocal;

            switch (periodo.ToLower())
            {
                case "semana":

                    var diferenca =
                        ((int)hoje.DayOfWeek + 6) % 7;

                    inicioLocal =
                        hoje.AddDays(-diferenca);

                    fimLocal =
                        inicioLocal.AddDays(7);

                    break;

                case "mes":

                    inicioLocal =
                        new DateTime(
                            hoje.Year,
                            hoje.Month,
                            1);

                    fimLocal =
                        inicioLocal.AddMonths(1);

                    break;

                default:

                    periodo = "hoje";

                    inicioLocal =
                        hoje;

                    fimLocal =
                        hoje.AddDays(1);

                    break;
            }

            var inicioUtc =
                inicioLocal.ToUniversalTime();

            var fimUtc =
                fimLocal.ToUniversalTime();

            // =====================================================
            // RECEBIMENTOS DO PERÍODO
            // =====================================================

            var recebimentos =
                await _context
                    .MovimentacoesFinanceiras
                    .AsNoTracking()
                    .Where(m =>
                        m.Tipo ==
                            TipoMovimentacaoFinanceira.Pagamento &&
                        m.DataMovimentacao >= inicioUtc &&
                        m.DataMovimentacao < fimUtc)
                    .SumAsync(m =>
                        (decimal?)m.Valor)
                ?? 0m;

            // =====================================================
            // RECEBIMENTOS REGISTRADOS - TOTAL GERAL
            // =====================================================

            var faturamentoRegistrado =
                await _context
                    .MovimentacoesFinanceiras
                    .AsNoTracking()
                    .Where(m =>
                        m.Tipo ==
                            TipoMovimentacaoFinanceira.Pagamento)
                    .SumAsync(m =>
                        (decimal?)m.Valor)
                ?? 0m;

            // =====================================================
            // CLIENTES COM SALDO DEVEDOR
            // =====================================================

            var clientesComSaldoDevedor =
                await _context.Clientes
                    .AsNoTracking()
                    .CountAsync(c =>
                        c.SaldoDevedor > 0);

            // =====================================================
            // TOTAL A RECEBER - SALDO DEVEDOR DOS CLIENTES
            // =====================================================

            var totalAReceber =
                await _context.Clientes
                    .AsNoTracking()
                    .Where(c =>
                        c.SaldoDevedor > 0)
                    .SumAsync(c =>
                        (decimal?)c.SaldoDevedor)
                ?? 0m;

            // =====================================================
            // GASTOS
            // =====================================================

            var totalGastos =
                await _context.GastosCaixa
                    .AsNoTracking()
                    .Where(g =>
                        g.DataGasto >= inicioUtc &&
                        g.DataGasto < fimUtc)
                    .SumAsync(g =>
                        (decimal?)g.Valor)
                ?? 0m;

            var lucro =
                recebimentos - totalGastos;

            // =====================================================
            // TOTAL POR CATEGORIA
            // =====================================================

            var gastosPorCategoria =
                await _context.GastosCaixa
                    .AsNoTracking()
                    .Where(g =>
                        g.DataGasto >= inicioUtc &&
                        g.DataGasto < fimUtc)
                    .GroupBy(g => g.Categoria)
                    .Select(g => new
                    {
                        Categoria = g.Key,
                        Total = g.Sum(x => x.Valor)
                    })
                    .OrderByDescending(g => g.Total)
                    .ToListAsync();

            // =====================================================
            // CATEGORIAS DE GASTOS
            // =====================================================

            var categorias =
                await _context.CategoriasGastos
                    .AsNoTracking()
                    .Where(c => c.Ativa)
                    .OrderBy(c => c.Nome)
                    .ToListAsync();

            ViewBag.CategoriasGastos =
                categorias;

            // =====================================================
            // PAGINAÇÃO - 10 REGISTROS
            // =====================================================

            const int tamanhoPagina = 10;

            var consulta =
                _context.GastosCaixa
                    .AsNoTracking()
                    .Where(g =>
                        g.DataGasto >= inicioUtc &&
                        g.DataGasto < fimUtc)
                    .OrderByDescending(g =>
                        g.DataGasto);

            var totalRegistros =
                await consulta.CountAsync();

            var totalPaginas =
                (int)Math.Ceiling(
                    totalRegistros /
                    (double)tamanhoPagina);

            if (totalPaginas > 0 &&
                pagina > totalPaginas)
            {
                pagina = totalPaginas;
            }

            var gastos =
                await consulta
                    .Skip(
                        (pagina - 1) *
                        tamanhoPagina)
                    .Take(tamanhoPagina)
                    .ToListAsync();

            // =====================================================
            // VIEWBAG
            // =====================================================

            ViewBag.Periodo =
                periodo;

            ViewBag.Recebimentos =
                recebimentos;

            ViewBag.FaturamentoRegistrado =
                faturamentoRegistrado;

            ViewBag.ClientesComSaldoDevedor =
                clientesComSaldoDevedor;

            ViewBag.TotalAReceber =
                totalAReceber;

            ViewBag.TotalGastos =
                totalGastos;

            ViewBag.Lucro =
                lucro;

            ViewBag.GastosPorCategoria =
                gastosPorCategoria;

            ViewBag.PaginaAtual =
                pagina;

            ViewBag.TotalPaginas =
                totalPaginas;

            ViewBag.TotalRegistros =
                totalRegistros;

            return View(gastos);
        }

        // =========================================================
        // REGISTRAR GASTO
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarGasto(
            string categoria,
            string descricao,
            decimal valor)
        {
            var usuario =
                await ObterAdministradorAsync();

            if (usuario == null)
            {
                return RedirectToAction(
                    "Login",
                    "Conta");
            }

            if (usuario.Perfil !=
                PerfilUsuario.Administrador)
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            categoria =
                categoria?.Trim()
                ?? string.Empty;

            descricao =
                descricao?.Trim()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(
                    categoria))
            {
                TempData["ErroCaixa"] =
                    "Selecione a categoria do gasto.";

                return RedirectToAction(
                    nameof(Index));
            }

            var categoriaExiste =
                await _context.CategoriasGastos
                    .AsNoTracking()
                    .AnyAsync(c =>
                        c.Ativa &&
                        c.Nome == categoria);

            if (!categoriaExiste)
            {
                TempData["ErroCaixa"] =
                    "A categoria selecionada não existe.";

                return RedirectToAction(
                    nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(
                    descricao))
            {
                TempData["ErroCaixa"] =
                    "Informe a descrição do gasto.";

                return RedirectToAction(
                    nameof(Index));
            }

            if (valor <= 0)
            {
                TempData["ErroCaixa"] =
                    "Informe um valor maior que zero.";

                return RedirectToAction(
                    nameof(Index));
            }

            var gasto =
                new GastoCaixa
                {
                    Categoria = categoria,
                    Descricao = descricao,
                    Valor = valor,
                    DataGasto = DateTime.UtcNow,
                    UsuarioResponsavelId =
                        usuario.Id
                };

            _context.GastosCaixa.Add(gasto);

            await _context.SaveChangesAsync();

            TempData["SucessoCaixa"] =
                "Gasto registrado com sucesso.";

            return RedirectToAction(
                nameof(Index));
        }

        // =========================================================
        // ADICIONAR CATEGORIA
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdicionarCategoria(
            string nome)
        {
            var usuario =
                await ObterAdministradorAsync();

            if (usuario == null)
            {
                return RedirectToAction(
                    "Login",
                    "Conta");
            }

            if (usuario.Perfil !=
                PerfilUsuario.Administrador)
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            nome =
                nome?.Trim()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(nome))
            {
                TempData["ErroCaixa"] =
                    "Informe o nome da categoria.";

                return RedirectToAction(
                    nameof(Index));
            }

            if (nome.Length > 100)
            {
                TempData["ErroCaixa"] =
                    "O nome da categoria é muito grande.";

                return RedirectToAction(
                    nameof(Index));
            }

            var categoriaExistente =
                await _context.CategoriasGastos
                    .FirstOrDefaultAsync(c =>
                        c.Nome.ToLower() ==
                        nome.ToLower());

            if (categoriaExistente != null)
            {
                if (!categoriaExistente.Ativa)
                {
                    categoriaExistente.Ativa = true;

                    await _context.SaveChangesAsync();

                    TempData["SucessoCaixa"] =
                        "Categoria adicionada novamente com sucesso.";

                    return RedirectToAction(
                        nameof(Index));
                }

                TempData["ErroCaixa"] =
                    "Essa categoria já está cadastrada.";

                return RedirectToAction(
                    nameof(Index));
            }

            var categoria =
                new CategoriaGasto
                {
                    Nome = nome,
                    Ativa = true,
                    DataCadastro = DateTime.UtcNow
                };

            _context.CategoriasGastos.Add(
                categoria);

            await _context.SaveChangesAsync();

            TempData["SucessoCaixa"] =
                "Categoria adicionada com sucesso.";

            return RedirectToAction(
                nameof(Index));
        }

        // =========================================================
        // EXCLUIR CATEGORIA
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirCategoria(
            int id)
        {
            var usuario =
                await ObterAdministradorAsync();

            if (usuario == null)
            {
                return RedirectToAction(
                    "Login",
                    "Conta");
            }

            if (usuario.Perfil !=
                PerfilUsuario.Administrador)
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            var categoria =
                await _context.CategoriasGastos
                    .FirstOrDefaultAsync(c =>
                        c.Id == id);

            if (categoria == null)
            {
                TempData["ErroCaixa"] =
                    "Categoria não encontrada.";

                return RedirectToAction(
                    nameof(Index));
            }

            var possuiGastos =
                await _context.GastosCaixa
                    .AsNoTracking()
                    .AnyAsync(g =>
                        g.Categoria ==
                        categoria.Nome);

            if (possuiGastos)
            {
                TempData["ErroCaixa"] =
                    "Essa categoria possui gastos registrados e não pode ser excluída.";

                return RedirectToAction(
                    nameof(Index));
            }

            _context.CategoriasGastos.Remove(
                categoria);

            await _context.SaveChangesAsync();

            TempData["SucessoCaixa"] =
                "Categoria excluída com sucesso.";

            return RedirectToAction(
                nameof(Index));
        }

        // =========================================================
        // EXCLUIR GASTO
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(
            int id)
        {
            var usuario =
                await ObterAdministradorAsync();

            if (usuario == null)
            {
                return RedirectToAction(
                    "Login",
                    "Conta");
            }

            if (usuario.Perfil !=
                PerfilUsuario.Administrador)
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            var gasto =
                await _context.GastosCaixa
                    .FirstOrDefaultAsync(g =>
                        g.Id == id);

            if (gasto == null)
            {
                return RedirectToAction(
                    nameof(Index));
            }

            _context.GastosCaixa.Remove(gasto);

            await _context.SaveChangesAsync();

            TempData["SucessoCaixa"] =
                "Gasto excluído com sucesso.";

            return RedirectToAction(
                nameof(Index));
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
                    IdentityConstants
                        .ApplicationScheme);

                return null;
            }

            return usuario;
        }
    }
}