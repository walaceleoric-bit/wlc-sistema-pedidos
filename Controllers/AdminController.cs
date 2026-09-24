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
    public class AdminController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly AppDbContext _context;

        public AdminController(
            UserManager<Usuario> userManager,
            AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // =====================================================
            // USUÁRIO LOGADO
            // =====================================================

            var usuario =
                await _userManager.GetUserAsync(User);

            if (usuario == null)
            {
                return RedirectToAction(
                    "Login",
                    "Conta");
            }

            // =====================================================
            // USUÁRIO BLOQUEADO
            // =====================================================

            if (!usuario.Ativo)
            {
                await HttpContext.SignOutAsync(
                    IdentityConstants.ApplicationScheme);

                return RedirectToAction(
                    "Login",
                    "Conta");
            }

            // =====================================================
            // SOMENTE ADMINISTRADOR
            // =====================================================

            if (usuario.Perfil !=
                PerfilUsuario.Administrador)
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            // =====================================================
            // CONFIGURAÇÃO DO ESTABELECIMENTO
            // =====================================================

            var configuracao =
                await _context.ConfiguracoesSistema
                    .AsNoTracking()
                    .OrderBy(c => c.Id)
                    .FirstOrDefaultAsync();

            var nomeEstabelecimento =
                configuracao?.NomeEstabelecimento;

            if (string.IsNullOrWhiteSpace(
                    nomeEstabelecimento))
            {
                nomeEstabelecimento =
                    "Sistema de Pedidos";
            }

            var logoEstabelecimento =
                configuracao?.LogoUrl;

            // Se ainda não existir uma logo do estabelecimento,
            // usamos a WLC temporariamente como imagem padrão.
            if (string.IsNullOrWhiteSpace(
                    logoEstabelecimento))
            {
                logoEstabelecimento =
                    "/images/logo-wlc.png";
            }

            // =====================================================
            // DATA DE HOJE
            // =====================================================

            var hojeLocal =
                DateTime.Today;

            var inicioHojeUtc =
                hojeLocal.ToUniversalTime();

            var inicioAmanhaUtc =
                hojeLocal
                    .AddDays(1)
                    .ToUniversalTime();

            // =====================================================
            // PEDIDOS HOJE
            // =====================================================

            var pedidosHoje =
                await _context.Pedidos
                    .AsNoTracking()
                    .CountAsync(p =>
                        p.DataPedido >= inicioHojeUtc &&
                        p.DataPedido < inicioAmanhaUtc);

            // =====================================================
            // RECEBIMENTOS REGISTRADOS
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
            // TOTAL A RECEBER
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
            // DADOS PARA A VIEW
            // =====================================================

            ViewBag.NomeUsuario =
                usuario.Nome;

            ViewBag.NomeEstabelecimento =
                nomeEstabelecimento;

            ViewBag.LogoEstabelecimento =
                logoEstabelecimento;

            ViewBag.PedidosHoje =
                pedidosHoje;

            ViewBag.FaturamentoRegistrado =
                faturamentoRegistrado;

            ViewBag.ClientesComSaldoDevedor =
                clientesComSaldoDevedor;

            ViewBag.TotalAReceber =
                totalAReceber;

            return View();
        }
    }
}