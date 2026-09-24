using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using WlcSistemaPedidos.Data;
using WlcSistemaPedidos.Models;

namespace WlcSistemaPedidos.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(
            ILogger<HomeController> logger,
            AppDbContext context)
        {
            _logger = logger;
            _context = context;
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
                nomeEstabelecimento = "Sistema de Pedidos";
            }

            var logoEstabelecimento =
                configuracao?.LogoUrl;

            if (string.IsNullOrWhiteSpace(logoEstabelecimento))
            {
                logoEstabelecimento = "/images/logo-wlc.png";
            }

            ViewBag.NomeEstabelecimento =
                nomeEstabelecimento;

            ViewBag.LogoEstabelecimento =
                logoEstabelecimento;
        }

        public async Task<IActionResult> Index()
        {
            await CarregarEstabelecimento();
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId =
                    Activity.Current?.Id ??
                    HttpContext.TraceIdentifier
            });
        }
    }
}
