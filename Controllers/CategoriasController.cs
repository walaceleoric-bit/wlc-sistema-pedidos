using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WlcSistemaPedidos.Data;
using WlcSistemaPedidos.Models;

namespace WlcSistemaPedidos.Controllers
{
    [Authorize]
    public class CategoriasController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public CategoriasController(
            AppDbContext context,
            UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================================
        // VALIDA ADMINISTRADOR
        // =========================================================

        private async Task<bool> UsuarioEhAdministrador()
        {
            var usuario = await _userManager.GetUserAsync(User);

            return usuario != null &&
                   usuario.Ativo &&
                   usuario.Perfil == PerfilUsuario.Administrador;
        }

        // =========================================================
        // CARREGA DADOS DO ESTABELECIMENTO
        // =========================================================

        private async Task CarregarEstabelecimento()
        {
            var configuracao = await _context.ConfiguracoesSistema
                .AsNoTracking()
                .OrderBy(c => c.Id)
                .FirstOrDefaultAsync();

            var nomeEstabelecimento = configuracao?.NomeEstabelecimento;

            if (string.IsNullOrWhiteSpace(nomeEstabelecimento))
            {
                nomeEstabelecimento = "Sistema de Pedidos";
            }

            var logoEstabelecimento = configuracao?.LogoUrl;

            if (string.IsNullOrWhiteSpace(logoEstabelecimento))
            {
                logoEstabelecimento = "/images/logo-wlc.png";
            }

            ViewBag.NomeEstabelecimento = nomeEstabelecimento;
            ViewBag.LogoEstabelecimento = logoEstabelecimento;
        }

        // =========================================================
        // LISTAGEM
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!await UsuarioEhAdministrador())
            {
                return RedirectToAction("AcessoNegado", "Conta");
            }

            var categorias = await _context.Categorias
                .AsNoTracking()
                .Include(c => c.Produtos)
                .OrderBy(c => c.OrdemExibicao)
                .ThenBy(c => c.Nome)
                .ToListAsync();

            await CarregarEstabelecimento();

            return View(categorias);
        }

        // =========================================================
        // CRIAR - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Criar()
        {
            if (!await UsuarioEhAdministrador())
            {
                return RedirectToAction("AcessoNegado", "Conta");
            }

            await CarregarEstabelecimento();

            return View(new Categoria
            {
                Ativa = true,
                OrdemExibicao = 0
            });
        }

        // =========================================================
        // CRIAR - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(Categoria categoria)
        {
            if (!await UsuarioEhAdministrador())
            {
                return RedirectToAction("AcessoNegado", "Conta");
            }

            categoria.Nome = categoria.Nome?.Trim() ?? string.Empty;
            categoria.Descricao = categoria.Descricao?.Trim();

            if (!string.IsNullOrWhiteSpace(categoria.Nome))
            {
                var nomeExiste = await _context.Categorias
                    .AnyAsync(c =>
                        c.Nome.ToLower() == categoria.Nome.ToLower());

                if (nomeExiste)
                {
                    ModelState.AddModelError(
                        nameof(categoria.Nome),
                        "Já existe uma categoria com esse nome.");
                }
            }

            if (!ModelState.IsValid)
            {
                await CarregarEstabelecimento();
                return View(categoria);
            }

            categoria.Id = 0;
            categoria.DataCadastro = DateTime.UtcNow;

            _context.Categorias.Add(categoria);

            await _context.SaveChangesAsync();

            TempData["Sucesso"] =
                $"Categoria \"{categoria.Nome}\" cadastrada com sucesso.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // EDITAR - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            if (!await UsuarioEhAdministrador())
            {
                return RedirectToAction("AcessoNegado", "Conta");
            }

            var categoria = await _context.Categorias
                .FindAsync(id);

            if (categoria == null)
            {
                return NotFound();
            }

            await CarregarEstabelecimento();

            return View(categoria);
        }

        // =========================================================
        // EDITAR - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(
            int id,
            Categoria categoria)
        {
            if (!await UsuarioEhAdministrador())
            {
                return RedirectToAction("AcessoNegado", "Conta");
            }

            if (id != categoria.Id)
            {
                return NotFound();
            }

            categoria.Nome = categoria.Nome?.Trim() ?? string.Empty;
            categoria.Descricao = categoria.Descricao?.Trim();

            var categoriaBanco = await _context.Categorias
                .FirstOrDefaultAsync(c => c.Id == id);

            if (categoriaBanco == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(categoria.Nome))
            {
                var nomeExiste = await _context.Categorias
                    .AnyAsync(c =>
                        c.Id != id &&
                        c.Nome.ToLower() == categoria.Nome.ToLower());

                if (nomeExiste)
                {
                    ModelState.AddModelError(
                        nameof(categoria.Nome),
                        "Já existe uma categoria com esse nome.");
                }
            }

            if (!ModelState.IsValid)
            {
                await CarregarEstabelecimento();
                return View(categoria);
            }

            categoriaBanco.Nome = categoria.Nome;
            categoriaBanco.Descricao = categoria.Descricao;
            categoriaBanco.Ativa = categoria.Ativa;
            categoriaBanco.OrdemExibicao = categoria.OrdemExibicao;

            await _context.SaveChangesAsync();

            TempData["Sucesso"] =
                $"Categoria \"{categoriaBanco.Nome}\" atualizada com sucesso.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // EXCLUIR
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id)
        {
            if (!await UsuarioEhAdministrador())
            {
                return RedirectToAction("AcessoNegado", "Conta");
            }

            var categoria = await _context.Categorias
                .Include(c => c.Produtos)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (categoria == null)
            {
                return NotFound();
            }

            if (categoria.Produtos.Any())
            {
                TempData["Erro"] =
                    "Não é possível excluir uma categoria que possui produtos cadastrados.";

                return RedirectToAction(nameof(Index));
            }

            _context.Categorias.Remove(categoria);

            await _context.SaveChangesAsync();

            TempData["Sucesso"] =
                $"Categoria \"{categoria.Nome}\" excluída com sucesso.";

            return RedirectToAction(nameof(Index));
        }
    }
}
