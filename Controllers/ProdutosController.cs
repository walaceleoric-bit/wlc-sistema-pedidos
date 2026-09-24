using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WlcSistemaPedidos.Data;
using WlcSistemaPedidos.Models;

namespace WlcSistemaPedidos.Controllers
{
    [Authorize]
    public class ProdutosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Usuario> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProdutosController(
            AppDbContext context,
            UserManager<Usuario> userManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // =========================================================
        // VALIDA SE É ADMINISTRADOR
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
            var configuracao =
                await _context.ConfiguracoesSistema
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

        // =========================================================
        // LISTAGEM
        // 10 PRODUTOS POR PÁGINA
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index(
            int pagina = 1,
            string? busca = null,
            int? categoriaId = null)
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

            var consulta = _context.Produtos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(busca))
            {
                busca = busca.Trim();

                consulta = consulta.Where(p =>
                    EF.Functions.ILike(
                        p.Nome,
                        $"%{busca}%"));
            }

            if (categoriaId.HasValue)
            {
                consulta = consulta.Where(p =>
                    p.CategoriaId == categoriaId.Value);
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

            var produtos =
                await consulta
                    .OrderBy(p => p.OrdemExibicao)
                    .ThenBy(p => p.Nome)
                    .Skip((pagina - 1) * itensPorPagina)
                    .Take(itensPorPagina)
                    .ToListAsync();

            var categorias =
                await _context.Categorias
                    .AsNoTracking()
                    .OrderBy(c => c.OrdemExibicao)
                    .ThenBy(c => c.Nome)
                    .ToListAsync();

            await CarregarEstabelecimento();

            ViewBag.PaginaAtual =
                pagina;

            ViewBag.TotalPaginas =
                totalPaginas;

            ViewBag.TotalProdutos =
                totalProdutos;

            ViewBag.Busca =
                busca;

            ViewBag.CategoriaId =
                categoriaId;

            ViewBag.Categorias =
                categorias;

            return View(produtos);
        }

        // =========================================================
        // CRIAR - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Criar()
        {
            if (!await UsuarioEhAdministrador())
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            await CarregarCategorias();
            await CarregarEstabelecimento();

            return View(new Produto
            {
                Ativo = true,
                Disponivel = true,
                OrdemExibicao = 0
            });
        }

        // =========================================================
        // CRIAR - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(
            Produto produto,
            IFormFile? imagemProduto)
        {
            if (!await UsuarioEhAdministrador())
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            produto.Nome =
                produto.Nome?.Trim()
                ?? string.Empty;

            produto.Descricao =
                produto.Descricao?.Trim();

            produto.ImagemUrl = null;

            ModelState.Remove(
                nameof(Produto.Categoria));

            ModelState.Remove(
                nameof(Produto.ImagemUrl));

            if (produto.Preco < 0)
            {
                ModelState.AddModelError(
                    nameof(produto.Preco),
                    "O preço não pode ser negativo.");
            }

            var categoriaExiste =
                await _context.Categorias
                    .AnyAsync(c =>
                        c.Id == produto.CategoriaId &&
                        c.Ativa);

            if (!categoriaExiste)
            {
                ModelState.AddModelError(
                    nameof(produto.CategoriaId),
                    "Selecione uma categoria válida.");
            }

            ValidarImagem(imagemProduto);

            if (!ModelState.IsValid)
            {
                await CarregarCategorias();
                await CarregarEstabelecimento();

                return View(produto);
            }

            if (imagemProduto != null &&
                imagemProduto.Length > 0)
            {
                try
                {
                    produto.ImagemUrl =
                        await SalvarImagemProduto(
                            imagemProduto);
                }
                catch
                {
                    ModelState.AddModelError(
                        "imagemProduto",
                        "Não foi possível salvar a imagem. Tente novamente.");

                    await CarregarCategorias();
                    await CarregarEstabelecimento();

                    return View(produto);
                }
            }

            produto.Id = 0;
            produto.DataCadastro =
                DateTime.UtcNow;

            produto.DataAtualizacao =
                null;

            _context.Produtos.Add(
                produto);

            await _context.SaveChangesAsync();

            TempData["Sucesso"] =
                $"Produto \"{produto.Nome}\" cadastrado com sucesso.";

            return RedirectToAction(
                nameof(Index));
        }

        // =========================================================
        // EDITAR - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Editar(
            int id)
        {
            if (!await UsuarioEhAdministrador())
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            var produto =
                await _context.Produtos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        p => p.Id == id);

            if (produto == null)
            {
                return NotFound();
            }

            await CarregarCategorias();
            await CarregarEstabelecimento();

            return View(produto);
        }

        // =========================================================
        // EDITAR - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(
            int id,
            Produto produto,
            IFormFile? imagemProduto,
            bool removerImagem = false)
        {
            if (!await UsuarioEhAdministrador())
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            if (id != produto.Id)
            {
                return NotFound();
            }

            var produtoBanco =
                await _context.Produtos
                    .FirstOrDefaultAsync(
                        p => p.Id == id);

            if (produtoBanco == null)
            {
                return NotFound();
            }

            produto.Nome =
                produto.Nome?.Trim()
                ?? string.Empty;

            produto.Descricao =
                produto.Descricao?.Trim();

            ModelState.Remove(
                nameof(Produto.Categoria));

            ModelState.Remove(
                nameof(Produto.ImagemUrl));

            if (produto.Preco < 0)
            {
                ModelState.AddModelError(
                    nameof(produto.Preco),
                    "O preço não pode ser negativo.");
            }

            var categoriaExiste =
                await _context.Categorias
                    .AnyAsync(c =>
                        c.Id == produto.CategoriaId &&
                        c.Ativa);

            if (!categoriaExiste)
            {
                ModelState.AddModelError(
                    nameof(produto.CategoriaId),
                    "Selecione uma categoria válida.");
            }

            ValidarImagem(imagemProduto);

            if (!ModelState.IsValid)
            {
                produto.ImagemUrl =
                    produtoBanco.ImagemUrl;

                await CarregarCategorias();
                await CarregarEstabelecimento();

                return View(produto);
            }

            var imagemAntiga =
                produtoBanco.ImagemUrl;

            string? novaImagem = null;

            if (imagemProduto != null &&
                imagemProduto.Length > 0)
            {
                try
                {
                    novaImagem =
                        await SalvarImagemProduto(
                            imagemProduto);
                }
                catch
                {
                    produto.ImagemUrl =
                        produtoBanco.ImagemUrl;

                    ModelState.AddModelError(
                        "imagemProduto",
                        "Não foi possível salvar a nova imagem. Tente novamente.");

                    await CarregarCategorias();
                    await CarregarEstabelecimento();

                    return View(produto);
                }
            }

            produtoBanco.Nome =
                produto.Nome;

            produtoBanco.Descricao =
                produto.Descricao;

            produtoBanco.Preco =
                produto.Preco;

            produtoBanco.CategoriaId =
                produto.CategoriaId;

            produtoBanco.OrdemExibicao =
                produto.OrdemExibicao;

            produtoBanco.Ativo =
                produto.Ativo;

            produtoBanco.Disponivel =
                produto.Disponivel;

            produtoBanco.DataAtualizacao =
                DateTime.UtcNow;

            var deveExcluirImagemAntiga =
                false;

            if (novaImagem != null)
            {
                produtoBanco.ImagemUrl =
                    novaImagem;

                deveExcluirImagemAntiga =
                    true;
            }
            else if (removerImagem)
            {
                produtoBanco.ImagemUrl =
                    null;

                deveExcluirImagemAntiga =
                    true;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                if (novaImagem != null)
                {
                    ExcluirImagemFisica(
                        novaImagem);
                }

                throw;
            }

            if (deveExcluirImagemAntiga)
            {
                ExcluirImagemFisica(
                    imagemAntiga);
            }

            TempData["Sucesso"] =
                $"Produto \"{produtoBanco.Nome}\" atualizado com sucesso.";

            return RedirectToAction(
                nameof(Index));
        }

        // =========================================================
        // EXCLUIR - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(
            int id)
        {
            if (!await UsuarioEhAdministrador())
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            var produto =
                await _context.Produtos
                    .FirstOrDefaultAsync(
                        p => p.Id == id);

            if (produto == null)
            {
                return NotFound();
            }

            var possuiPedidos =
                await _context.ItensPedido
                    .AnyAsync(i =>
                        i.ProdutoId == id);

            if (possuiPedidos)
            {
                TempData["Erro"] =
                    "Este produto já possui histórico de pedidos e não pode ser excluído. Desative o produto para que ele deixe de aparecer para novos pedidos.";

                return RedirectToAction(
                    nameof(Index));
            }

            var imagemProduto =
                produto.ImagemUrl;

            var nomeProduto =
                produto.Nome;

            _context.Produtos.Remove(
                produto);

            await _context.SaveChangesAsync();

            ExcluirImagemFisica(
                imagemProduto);

            TempData["Sucesso"] =
                $"Produto \"{nomeProduto}\" excluído com sucesso.";

            return RedirectToAction(
                nameof(Index));
        }

        // =========================================================
        // CARREGA CATEGORIAS
        // =========================================================

        private async Task CarregarCategorias()
        {
            ViewBag.Categorias =
                await _context.Categorias
                    .AsNoTracking()
                    .Where(c => c.Ativa)
                    .OrderBy(c =>
                        c.OrdemExibicao)
                    .ThenBy(c =>
                        c.Nome)
                    .ToListAsync();
        }

        // =========================================================
        // VALIDA IMAGEM
        // =========================================================

        private void ValidarImagem(
            IFormFile? imagemProduto)
        {
            if (imagemProduto == null ||
                imagemProduto.Length == 0)
            {
                return;
            }

            const long tamanhoMaximo =
                5 * 1024 * 1024;

            if (imagemProduto.Length >
                tamanhoMaximo)
            {
                ModelState.AddModelError(
                    "imagemProduto",
                    "A imagem deve ter no máximo 5 MB.");
            }

            var extensao =
                Path.GetExtension(
                    imagemProduto.FileName)
                .ToLowerInvariant();

            var extensoesPermitidas =
                new[]
                {
                    ".jpg",
                    ".jpeg",
                    ".png",
                    ".webp"
                };

            if (!extensoesPermitidas
                .Contains(extensao))
            {
                ModelState.AddModelError(
                    "imagemProduto",
                    "Selecione uma imagem JPG, JPEG, PNG ou WEBP.");
            }

            var tiposPermitidos =
                new[]
                {
                    "image/jpeg",
                    "image/png",
                    "image/webp"
                };

            var contentType =
                imagemProduto.ContentType?
                    .ToLowerInvariant()
                ?? string.Empty;

            if (!tiposPermitidos
                .Contains(contentType))
            {
                ModelState.AddModelError(
                    "imagemProduto",
                    "O arquivo selecionado não é uma imagem válida.");
            }
        }

        // =========================================================
        // SALVA IMAGEM
        // =========================================================

        private async Task<string>
            SalvarImagemProduto(
                IFormFile imagemProduto)
        {
            var extensao =
                Path.GetExtension(
                    imagemProduto.FileName)
                .ToLowerInvariant();

            var nomeArquivo =
                $"{Guid.NewGuid():N}{extensao}";

            var pastaUploads =
                Path.Combine(
                    _webHostEnvironment.WebRootPath,
                    "uploads",
                    "produtos");

            if (!Directory.Exists(
                pastaUploads))
            {
                Directory.CreateDirectory(
                    pastaUploads);
            }

            var caminhoCompleto =
                Path.Combine(
                    pastaUploads,
                    nomeArquivo);

            await using var stream =
                new FileStream(
                    caminhoCompleto,
                    FileMode.Create);

            await imagemProduto
                .CopyToAsync(stream);

            return
                $"/uploads/produtos/{nomeArquivo}";
        }

        // =========================================================
        // EXCLUI IMAGEM FÍSICA COM SEGURANÇA
        // =========================================================

        private void ExcluirImagemFisica(
            string? imagemUrl)
        {
            if (string.IsNullOrWhiteSpace(
                imagemUrl))
            {
                return;
            }

            if (!imagemUrl.StartsWith(
                "/uploads/produtos/",
                StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                var caminhoRelativo =
                    imagemUrl
                        .TrimStart('/')
                        .Replace(
                            '/',
                            Path.DirectorySeparatorChar);

                var caminhoCompleto =
                    Path.Combine(
                        _webHostEnvironment.WebRootPath,
                        caminhoRelativo);

                if (!System.IO.File.Exists(
                    caminhoCompleto))
                {
                    return;
                }

                var atributos =
                    System.IO.File.GetAttributes(
                        caminhoCompleto);

                if ((atributos &
                     FileAttributes.ReadOnly) ==
                    FileAttributes.ReadOnly)
                {
                    System.IO.File.SetAttributes(
                        caminhoCompleto,
                        atributos &
                        ~FileAttributes.ReadOnly);
                }

                System.IO.File.Delete(
                    caminhoCompleto);
            }
            catch (UnauthorizedAccessException)
            {
                // Não interrompe o sistema caso
                // o Windows bloqueie temporariamente o arquivo.
            }
            catch (IOException)
            {
                // Não interrompe o sistema caso
                // o arquivo esteja temporariamente em uso.
            }
        }
    }
}