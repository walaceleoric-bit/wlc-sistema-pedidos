using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WlcSistemaPedidos.Data;
using WlcSistemaPedidos.Models;
using WlcSistemaPedidos.ViewModels;

namespace WlcSistemaPedidos.Controllers
{
    [Authorize]
    public class CarrinhoController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        private const string ChaveCarrinho = "Carrinho";

        public CarrinhoController(
            AppDbContext context,
            UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private List<ItemCarrinhoViewModel> ObterCarrinho()
        {
            var carrinhoJson =
                HttpContext.Session.GetString(ChaveCarrinho);

            if (string.IsNullOrWhiteSpace(carrinhoJson))
            {
                return new List<ItemCarrinhoViewModel>();
            }

            return JsonSerializer.Deserialize<List<ItemCarrinhoViewModel>>(
                       carrinhoJson)
                   ?? new List<ItemCarrinhoViewModel>();
        }

        private void SalvarCarrinho(
            List<ItemCarrinhoViewModel> carrinho)
        {
            var carrinhoJson =
                JsonSerializer.Serialize(carrinho);

            HttpContext.Session.SetString(
                ChaveCarrinho,
                carrinhoJson);
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
                .FirstOrDefaultAsync(c =>
                    c.UsuarioId == usuario.Id);
        }

        private async Task CarregarEstabelecimento()
        {
            var configuracao = await _context.ConfiguracoesSistema
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

        private static string? MontarLinkWhatsApp(
            ConfiguracaoSistema? configuracao,
            Pedido pedido)
        {
            if (configuracao == null ||
                string.IsNullOrWhiteSpace(
                    configuracao.WhatsAppPedidos))
            {
                return null;
            }

            var numero =
                new string(
                    configuracao.WhatsAppPedidos
                        .Where(char.IsDigit)
                        .ToArray());

            if (string.IsNullOrWhiteSpace(numero))
            {
                return null;
            }

            if (numero.Length == 10 ||
                numero.Length == 11)
            {
                numero = "55" + numero;
            }

            var cultura =
                new System.Globalization.CultureInfo("pt-BR");

            var mensagem = new StringBuilder();

            mensagem.AppendLine(
                $"*NOVO PEDIDO #{pedido.Id}*");

            mensagem.AppendLine(
                $"Cliente: {pedido.NomeCliente}");

            if (!string.IsNullOrWhiteSpace(
                    pedido.TelefoneCliente))
            {
                mensagem.AppendLine(
                    $"Telefone: {pedido.TelefoneCliente}");
            }

            mensagem.AppendLine();
            mensagem.AppendLine("*ITENS*");

            foreach (var item in pedido.Itens)
            {
                mensagem.AppendLine(
                    $"{item.Quantidade}x {item.NomeProduto} - {item.Subtotal.ToString("C2", cultura)}");
            }

            mensagem.AppendLine();
            mensagem.AppendLine(
                $"Subtotal: {pedido.Subtotal.ToString("C2", cultura)}");

            mensagem.AppendLine(
                $"Taxa de entrega: {pedido.TaxaEntrega.ToString("C2", cultura)}");

            mensagem.AppendLine(
                $"*Total: {pedido.Total.ToString("C2", cultura)}*");

            mensagem.AppendLine();
            mensagem.AppendLine("*ENTREGA*");

            mensagem.AppendLine(
                $"{pedido.EnderecoEntrega}, {pedido.NumeroEntrega}");

            mensagem.AppendLine(
                $"Bairro: {pedido.BairroEntrega}");

            if (!string.IsNullOrWhiteSpace(
                    pedido.ComplementoEntrega))
            {
                mensagem.AppendLine(
                    $"Complemento: {pedido.ComplementoEntrega}");
            }

            if (!string.IsNullOrWhiteSpace(
                    pedido.Observacao))
            {
                mensagem.AppendLine();
                mensagem.AppendLine(
                    $"Observação: {pedido.Observacao}");
            }

            var texto =
                Uri.EscapeDataString(
                    mensagem.ToString());

            return $"https://wa.me/{numero}?text={texto}";
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

            var carrinho = ObterCarrinho();

            ViewBag.TotalCarrinho =
                carrinho.Sum(i => i.Subtotal);

            await CarregarEstabelecimento();

            return View(carrinho);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adicionar(
            int produtoId)
        {
            var cliente = await ObterClienteLogado();

            if (cliente == null || !cliente.Ativo)
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            if (!cliente.PermitirNovosPedidos)
            {
                TempData["Erro"] =
                    "Novos pedidos estão bloqueados para este cliente.";

                return RedirectToAction(
                    "Produtos",
                    "Cliente");
            }

            var configuracao = await _context.ConfiguracoesSistema
                .AsNoTracking()
                .OrderBy(c => c.Id)
                .FirstOrDefaultAsync();

            if (configuracao != null &&
                !configuracao.AceitarPedidos)
            {
                TempData["Erro"] =
                    "O estabelecimento não está aceitando pedidos no momento.";

                return RedirectToAction(
                    "Produtos",
                    "Cliente");
            }

            var produto = await _context.Produtos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(p =>
                    p.Id == produtoId &&
                    p.Ativo &&
                    p.Disponivel &&
                    p.Categoria.Ativa);

            if (produto == null)
            {
                TempData["Erro"] =
                    "Produto não encontrado ou indisponível.";

                return RedirectToAction(
                    "Produtos",
                    "Cliente");
            }

            var carrinho = ObterCarrinho();

            var itemExistente =
                carrinho.FirstOrDefault(i =>
                    i.ProdutoId == produto.Id);

            if (itemExistente != null)
            {
                itemExistente.Quantidade++;
            }
            else
            {
                carrinho.Add(
                    new ItemCarrinhoViewModel
                    {
                        ProdutoId = produto.Id,
                        NomeProduto = produto.Nome,
                        ImagemUrl = produto.ImagemUrl,
                        PrecoUnitario = produto.Preco,
                        Quantidade = 1
                    });
            }

            SalvarCarrinho(carrinho);

            TempData["Sucesso"] =
                $"\"{produto.Nome}\" foi adicionado ao pedido.";

            return RedirectToAction(
                "Produtos",
                "Cliente");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Aumentar(int produtoId)
        {
            var cliente = await ObterClienteLogado();

            if (cliente == null || !cliente.Ativo)
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            if (!cliente.PermitirNovosPedidos)
            {
                TempData["Erro"] =
                    "Novos pedidos estão bloqueados para este cliente.";

                return RedirectToAction(nameof(Index));
            }

            var carrinho = ObterCarrinho();

            var item = carrinho.FirstOrDefault(i =>
                i.ProdutoId == produtoId);

            if (item != null)
            {
                item.Quantidade++;
                SalvarCarrinho(carrinho);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Diminuir(int produtoId)
        {
            var cliente = await ObterClienteLogado();

            if (cliente == null || !cliente.Ativo)
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            var carrinho = ObterCarrinho();

            var item = carrinho.FirstOrDefault(i =>
                i.ProdutoId == produtoId);

            if (item != null)
            {
                item.Quantidade--;

                if (item.Quantidade <= 0)
                {
                    carrinho.Remove(item);
                }

                SalvarCarrinho(carrinho);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remover(int produtoId)
        {
            var cliente = await ObterClienteLogado();

            if (cliente == null || !cliente.Ativo)
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            var carrinho = ObterCarrinho();

            var item = carrinho.FirstOrDefault(i =>
                i.ProdutoId == produtoId);

            if (item != null)
            {
                carrinho.Remove(item);
                SalvarCarrinho(carrinho);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Limpar()
        {
            var cliente = await ObterClienteLogado();

            if (cliente == null || !cliente.Ativo)
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            HttpContext.Session.Remove(ChaveCarrinho);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Finalizar()
        {
            var cliente = await ObterClienteLogado();

            if (cliente == null || !cliente.Ativo)
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            if (!cliente.PermitirNovosPedidos)
            {
                TempData["Erro"] =
                    "Novos pedidos estão bloqueados para este cliente.";

                return RedirectToAction(nameof(Index));
            }

            var carrinho = ObterCarrinho();

            if (carrinho.Count == 0)
            {
                TempData["Erro"] =
                    "Seu carrinho está vazio.";

                return RedirectToAction(nameof(Index));
            }

            var configuracao = await _context.ConfiguracoesSistema
                .AsNoTracking()
                .OrderBy(c => c.Id)
                .FirstOrDefaultAsync();

            if (configuracao != null &&
                !configuracao.AceitarPedidos)
            {
                TempData["Erro"] =
                    "O estabelecimento não está aceitando pedidos no momento.";

                return RedirectToAction(nameof(Index));
            }

            var subtotal = carrinho.Sum(i => i.Subtotal);
            var taxaEntrega = configuracao?.TaxaEntregaPadrao ?? 0m;

            var model = new FinalizarPedidoViewModel
            {
                EnderecoEntrega = cliente.Endereco ?? string.Empty,
                BairroEntrega = cliente.Bairro ?? string.Empty,
                NumeroEntrega = cliente.Numero ?? string.Empty,
                ComplementoEntrega = cliente.Complemento,
                Subtotal = subtotal,
                TaxaEntrega = taxaEntrega,
                Total = subtotal + taxaEntrega
            };

            await CarregarEstabelecimento();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Finalizar(
            FinalizarPedidoViewModel model)
        {
            var cliente = await ObterClienteLogado();

            if (cliente == null || !cliente.Ativo)
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            if (!cliente.PermitirNovosPedidos)
            {
                TempData["Erro"] =
                    "Novos pedidos estão bloqueados para este cliente.";

                return RedirectToAction(nameof(Index));
            }

            var carrinho = ObterCarrinho();

            if (carrinho.Count == 0)
            {
                TempData["Erro"] =
                    "Seu carrinho está vazio.";

                return RedirectToAction(nameof(Index));
            }

            var configuracao = await _context.ConfiguracoesSistema
                .AsNoTracking()
                .OrderBy(c => c.Id)
                .FirstOrDefaultAsync();

            if (configuracao != null &&
                !configuracao.AceitarPedidos)
            {
                TempData["Erro"] =
                    "O estabelecimento não está aceitando pedidos no momento.";

                return RedirectToAction(nameof(Index));
            }

            var idsProdutos = carrinho
                .Select(i => i.ProdutoId)
                .Distinct()
                .ToList();

            var produtos = await _context.Produtos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .Where(p =>
                    idsProdutos.Contains(p.Id))
                .ToListAsync();

            foreach (var itemCarrinho in carrinho)
            {
                var produto = produtos.FirstOrDefault(p =>
                    p.Id == itemCarrinho.ProdutoId);

                if (produto == null ||
                    !produto.Ativo ||
                    !produto.Disponivel ||
                    !produto.Categoria.Ativa)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"O produto \"{itemCarrinho.NomeProduto}\" não está mais disponível.");
                }
            }

            var subtotal = carrinho.Sum(i => i.Subtotal);
            var taxaEntrega = configuracao?.TaxaEntregaPadrao ?? 0m;

            model.Subtotal = subtotal;
            model.TaxaEntrega = taxaEntrega;
            model.Total = subtotal + taxaEntrega;

            if (!ModelState.IsValid)
            {
                await CarregarEstabelecimento();

                return View(model);
            }

            var pedido = new Pedido
            {
                ClienteId = cliente.Id,
                NomeCliente = cliente.Nome,
                TelefoneCliente = cliente.Telefone,
                EnderecoEntrega = model.EnderecoEntrega.Trim(),
                BairroEntrega = model.BairroEntrega.Trim(),
                NumeroEntrega = model.NumeroEntrega.Trim(),
                ComplementoEntrega =
                    string.IsNullOrWhiteSpace(model.ComplementoEntrega)
                        ? null
                        : model.ComplementoEntrega.Trim(),
                Observacao =
                    string.IsNullOrWhiteSpace(model.Observacao)
                        ? null
                        : model.Observacao.Trim(),
                Subtotal = subtotal,
                TaxaEntrega = taxaEntrega,
                Desconto = 0m,
                Total = subtotal + taxaEntrega,
                Status = StatusPedido.Novo,
                DataPedido = DateTime.UtcNow,
                Pago = false,
                ValorPago = 0m
            };

            foreach (var itemCarrinho in carrinho)
            {
                var produto = produtos.First(p =>
                    p.Id == itemCarrinho.ProdutoId);

                pedido.Itens.Add(
                    new ItemPedido
                    {
                        ProdutoId = produto.Id,
                        NomeProduto = produto.Nome,
                        Quantidade = itemCarrinho.Quantidade,
                        PrecoUnitario = produto.Preco,
                        Subtotal =
                            produto.Preco *
                            itemCarrinho.Quantidade
                    });
            }

            // Recalcula os valores usando os preços atuais do banco.
            pedido.Subtotal =
                pedido.Itens.Sum(i => i.Subtotal);

            pedido.Total =
                pedido.Subtotal +
                pedido.TaxaEntrega -
                pedido.Desconto;

            _context.Pedidos.Add(pedido);

            await _context.SaveChangesAsync();

            HttpContext.Session.Remove(ChaveCarrinho);

            TempData["Sucesso"] =
                $"Pedido #{pedido.Id} realizado com sucesso.";

            return RedirectToAction(
                "Sucesso",
                new { id = pedido.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Sucesso(int id)
        {
            var cliente = await ObterClienteLogado();

            if (cliente == null || !cliente.Ativo)
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            var pedido = await _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Itens)
                .FirstOrDefaultAsync(p =>
                    p.Id == id &&
                    p.ClienteId == cliente.Id);

            if (pedido == null)
            {
                return NotFound();
            }

            var configuracao =
                await _context.ConfiguracoesSistema
                    .AsNoTracking()
                    .OrderBy(c => c.Id)
                    .FirstOrDefaultAsync();

            ViewBag.LinkWhatsApp =
                MontarLinkWhatsApp(
                    configuracao,
                    pedido);

            await CarregarEstabelecimento();

            return View(pedido);
        }
    }
}
