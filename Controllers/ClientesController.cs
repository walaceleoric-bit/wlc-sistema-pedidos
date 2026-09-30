using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WlcSistemaPedidos.Data;
using WlcSistemaPedidos.Models;
using WlcSistemaPedidos.Services;
using WlcSistemaPedidos.ViewModels;

namespace WlcSistemaPedidos.Controllers
{
    [Authorize]
    public class ClientesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Usuario> _userManager;
        private readonly AcessoClienteService _acessoClienteService;

        public ClientesController(
            AppDbContext context,
            UserManager<Usuario> userManager,
            AcessoClienteService acessoClienteService)
        {
            _context = context;
            _userManager = userManager;
            _acessoClienteService = acessoClienteService;
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
        // 10 CLIENTES POR PÁGINA
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index(
            int pagina = 1,
            string? busca = null)
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

            var consulta = _context.Clientes
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(busca))
            {
                busca = busca.Trim();

                consulta = consulta.Where(c =>
                    EF.Functions.ILike(
                        c.Nome,
                        $"%{busca}%") ||

                    (c.CpfCnpj != null &&
                     EF.Functions.ILike(
                         c.CpfCnpj,
                         $"%{busca}%")) ||

                    (c.Telefone != null &&
                     EF.Functions.ILike(
                         c.Telefone,
                         $"%{busca}%")));
            }

            var totalClientes =
                await consulta.CountAsync();

            var totalPaginas =
                (int)Math.Ceiling(
                    totalClientes /
                    (double)itensPorPagina);

            if (totalPaginas > 0 &&
                pagina > totalPaginas)
            {
                pagina = totalPaginas;
            }

            var clientes = await consulta
                .OrderBy(c => c.Nome)
                .Skip((pagina - 1) * itensPorPagina)
                .Take(itensPorPagina)
                .ToListAsync();

            ViewBag.PaginaAtual = pagina;
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.TotalClientes = totalClientes;
            ViewBag.Busca = busca;

            await CarregarEstabelecimento();

            return View(clientes);
        }

        // =========================================================
        // ACESSO DO CLIENTE
        // GERA O LINK REAL DE ACESSO
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Acesso(int id)
        {
            if (!await UsuarioEhAdministrador())
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            var cliente = await _context.Clientes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cliente == null)
            {
                return NotFound();
            }

            if (!cliente.Ativo)
            {
                TempData["Erro"] =
                    $"O cliente \"{cliente.Nome}\" está inativo e não pode receber um link de acesso.";

                return RedirectToAction(nameof(Index));
            }

            var dataExpiracao =
                DateTime.UtcNow.AddDays(30);

            var token =
                await _acessoClienteService.CriarTokenAsync(
                    cliente.Id,
                    dataExpiracao);

            var linkAcesso =
                Url.Action(
                    "EntrarPorLink",
                    "Cliente",
                    new
                    {
                        token
                    },
                    Request.Scheme);

            if (string.IsNullOrWhiteSpace(linkAcesso))
            {
                TempData["Erro"] =
                    "Não foi possível gerar o link de acesso do cliente.";

                return RedirectToAction(nameof(Index));
            }

            ViewBag.ClienteId = cliente.Id;
            ViewBag.NomeCliente = cliente.Nome;
            ViewBag.TelefoneCliente = cliente.Telefone;
            ViewBag.LinkAcesso = linkAcesso;
            ViewBag.DataExpiracao = dataExpiracao;

            await CarregarEstabelecimento();

            return View();
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

            await CarregarEstabelecimento();

            return View(new ClienteCriarViewModel
            {
                Ativo = true,
                PermitirNovosPedidos = true
            });
        }

        // =========================================================
        // CRIAR - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(
            ClienteCriarViewModel model)
        {
            if (!await UsuarioEhAdministrador())
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            PrepararModelCriacao(model);

            if (!ModelState.IsValid)
            {
                await CarregarEstabelecimento();
                return View(model);
            }

            // =====================================================
            // CRIA CLIENTE
            // =====================================================

            var cliente = new Cliente
            {
                Nome = model.Nome,
                CpfCnpj = model.CpfCnpj,
                Telefone = model.Telefone,
                Email = model.Email,
                Endereco = model.Endereco,
                Bairro = model.Bairro,
                Cidade = model.Cidade,
                Numero = model.Numero,
                Complemento = model.Complemento,
                Cep = model.Cep,

                Ativo = model.Ativo,

                PermitirNovosPedidos =
                    model.PermitirNovosPedidos,

                SaldoDevedor = 0,
                DataCadastro = DateTime.UtcNow,

                UsuarioId = null
            };

            _context.Clientes.Add(cliente);

            await _context.SaveChangesAsync();

            TempData["Sucesso"] =
                $"Cliente \"{cliente.Nome}\" cadastrado com sucesso.";

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
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            var cliente = await _context.Clientes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cliente == null)
            {
                return NotFound();
            }

            await CarregarEstabelecimento();

            return View(cliente);
        }

        // =========================================================
        // EDITAR - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(
            int id,
            Cliente cliente)
        {
            if (!await UsuarioEhAdministrador())
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            if (id != cliente.Id)
            {
                return NotFound();
            }

            var clienteBanco = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Id == id);

            if (clienteBanco == null)
            {
                return NotFound();
            }

            PrepararCliente(cliente);

            ModelState.Remove(nameof(Cliente.Usuario));

            if (cliente.SaldoDevedor < 0)
            {
                ModelState.AddModelError(
                    nameof(cliente.SaldoDevedor),
                    "O saldo devedor não pode ser negativo.");
            }

            if (!ModelState.IsValid)
            {
                cliente.UsuarioId =
                    clienteBanco.UsuarioId;

                cliente.DataCadastro =
                    clienteBanco.DataCadastro;

                cliente.SaldoDevedor =
                    clienteBanco.SaldoDevedor;

                await CarregarEstabelecimento();

                return View(cliente);
            }

            clienteBanco.Nome =
                cliente.Nome;

            clienteBanco.CpfCnpj =
                cliente.CpfCnpj;

            clienteBanco.Telefone =
                cliente.Telefone;

            clienteBanco.Email =
                cliente.Email;

            clienteBanco.Endereco =
                cliente.Endereco;

            clienteBanco.Bairro =
                cliente.Bairro;

            clienteBanco.Cidade =
                cliente.Cidade;

            clienteBanco.Numero =
                cliente.Numero;

            clienteBanco.Complemento =
                cliente.Complemento;

            clienteBanco.Cep =
                cliente.Cep;

            clienteBanco.Ativo =
                cliente.Ativo;

            clienteBanco.PermitirNovosPedidos =
                cliente.PermitirNovosPedidos;

            await _context.SaveChangesAsync();

            TempData["Sucesso"] =
                $"Cliente \"{clienteBanco.Nome}\" atualizado com sucesso.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // FINANCEIRO DO CLIENTE - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Financeiro(
            int id,
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

            var cliente = await _context.Clientes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cliente == null)
            {
                return NotFound();
            }

            var consultaMovimentacoes =
                _context.MovimentacoesFinanceiras
                    .AsNoTracking()
                    .Include(m => m.UsuarioResponsavel)
                    .Where(m => m.ClienteId == id);

            var totalMovimentacoes =
                await consultaMovimentacoes.CountAsync();

            var totalPaginas =
                (int)Math.Ceiling(
                    totalMovimentacoes /
                    (double)itensPorPagina);

            if (totalPaginas > 0 &&
                pagina > totalPaginas)
            {
                pagina = totalPaginas;
            }

            var movimentacoes =
                await consultaMovimentacoes
                    .OrderByDescending(m => m.DataMovimentacao)
                    .ThenByDescending(m => m.Id)
                    .Skip((pagina - 1) * itensPorPagina)
                    .Take(itensPorPagina)
                    .ToListAsync();

            var model = new ClienteFinanceiroViewModel
            {
                ClienteId = cliente.Id,
                NomeCliente = cliente.Nome,
                SaldoDevedor = cliente.SaldoDevedor,

                PermitirNovosPedidos =
                    cliente.PermitirNovosPedidos,

                Tipo =
                    TipoMovimentacaoFinanceira.Pagamento,

                Movimentacoes = movimentacoes
            };

            ViewBag.PaginaAtual = pagina;
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.TotalMovimentacoes = totalMovimentacoes;

            await CarregarEstabelecimento();

            return View(model);
        }

        // =========================================================
        // FINANCEIRO DO CLIENTE - NOVA MOVIMENTAÇÃO
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Financeiro(
            ClienteFinanceiroViewModel model)
        {
            if (!await UsuarioEhAdministrador())
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c =>
                    c.Id == model.ClienteId);

            if (cliente == null)
            {
                return NotFound();
            }

            // Os dados abaixo sempre vêm do banco.
            model.NomeCliente = cliente.Nome;
            model.SaldoDevedor = cliente.SaldoDevedor;
            model.PermitirNovosPedidos =
                cliente.PermitirNovosPedidos;

            // =====================================================
            // VALIDAÇÕES DA MOVIMENTAÇÃO
            // =====================================================

            if (model.Valor <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.Valor),
                    "Informe um valor maior que zero.");
            }

            var tipoPermitido =
                model.Tipo ==
                    TipoMovimentacaoFinanceira.Debito ||

                model.Tipo ==
                    TipoMovimentacaoFinanceira.Pagamento ||

                model.Tipo ==
                    TipoMovimentacaoFinanceira.AjusteCredito ||

                model.Tipo ==
                    TipoMovimentacaoFinanceira.AjusteDebito;

            if (!tipoPermitido)
            {
                ModelState.AddModelError(
                    nameof(model.Tipo),
                    "Selecione uma movimentação financeira válida.");
            }

            // Pagamento ou crédito não podem deixar
            // o saldo devedor negativo.

            var reduzSaldo =
                model.Tipo ==
                    TipoMovimentacaoFinanceira.Pagamento ||

                model.Tipo ==
                    TipoMovimentacaoFinanceira.AjusteCredito;

            if (reduzSaldo &&
                model.Valor > cliente.SaldoDevedor)
            {
                ModelState.AddModelError(
                    nameof(model.Valor),
                    "O valor não pode ser maior que o saldo devedor atual.");
            }

            if (!ModelState.IsValid)
            {
                model.Movimentacoes =
                    await _context.MovimentacoesFinanceiras
                        .AsNoTracking()
                        .Include(m => m.UsuarioResponsavel)
                        .Where(m =>
                            m.ClienteId == cliente.Id)
                        .OrderByDescending(m =>
                            m.DataMovimentacao)
                        .ThenByDescending(m => m.Id)
                        .ToListAsync();

                await CarregarEstabelecimento();

                return View(model);
            }

            var usuarioResponsavel =
                await _userManager.GetUserAsync(User);

            if (usuarioResponsavel == null)
            {
                return RedirectToAction(
                    "Login",
                    "Conta");
            }

            // =====================================================
            // CALCULA NOVO SALDO
            // =====================================================

            decimal novoSaldo =
                cliente.SaldoDevedor;

            switch (model.Tipo)
            {
                case TipoMovimentacaoFinanceira.Debito:

                    novoSaldo += model.Valor;

                    break;

                case TipoMovimentacaoFinanceira.Pagamento:

                    novoSaldo -= model.Valor;

                    break;

                case TipoMovimentacaoFinanceira.AjusteCredito:

                    novoSaldo -= model.Valor;

                    break;

                case TipoMovimentacaoFinanceira.AjusteDebito:

                    novoSaldo += model.Valor;

                    break;

                default:

                    ModelState.AddModelError(
                        nameof(model.Tipo),
                        "Tipo de movimentação inválido.");

                    model.Movimentacoes =
                        await _context.MovimentacoesFinanceiras
                            .AsNoTracking()
                            .Include(m => m.UsuarioResponsavel)
                            .Where(m =>
                                m.ClienteId == cliente.Id)
                            .OrderByDescending(m =>
                                m.DataMovimentacao)
                            .ThenByDescending(m => m.Id)
                            .ToListAsync();

                    await CarregarEstabelecimento();

                    return View(model);
            }

            // Proteção adicional.
            if (novoSaldo < 0)
            {
                ModelState.AddModelError(
                    nameof(model.Valor),
                    "A movimentação deixaria o saldo do cliente negativo.");

                model.Movimentacoes =
                    await _context.MovimentacoesFinanceiras
                        .AsNoTracking()
                        .Include(m => m.UsuarioResponsavel)
                        .Where(m =>
                            m.ClienteId == cliente.Id)
                        .OrderByDescending(m =>
                            m.DataMovimentacao)
                        .ThenByDescending(m => m.Id)
                        .ToListAsync();

                await CarregarEstabelecimento();

                return View(model);
            }

            // =====================================================
            // REGISTRA MOVIMENTAÇÃO
            // =====================================================

            var movimentacao =
                new MovimentacaoFinanceira
                {
                    ClienteId = cliente.Id,

                    Tipo = model.Tipo,

                    Valor = model.Valor,

                    Observacao =
                        string.IsNullOrWhiteSpace(
                            model.Observacao)
                            ? null
                            : model.Observacao.Trim(),

                    DataMovimentacao =
                        DateTime.UtcNow,

                    UsuarioResponsavelId =
                        usuarioResponsavel.Id
                };

            // =====================================================
            // TRANSAÇÃO
            // =====================================================

            await using var transacao =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                cliente.SaldoDevedor =
                    novoSaldo;

                _context.MovimentacoesFinanceiras
                    .Add(movimentacao);

                await _context.SaveChangesAsync();

                await transacao.CommitAsync();
            }
            catch
            {
                await transacao.RollbackAsync();
                throw;
            }

            // =====================================================
            // MENSAGEM
            // =====================================================

            var nomeTipo =
                model.Tipo switch
                {
                    TipoMovimentacaoFinanceira.Debito =>
                        "Débito",

                    TipoMovimentacaoFinanceira.Pagamento =>
                        "Pagamento",

                    TipoMovimentacaoFinanceira.AjusteCredito =>
                        "Ajuste de crédito",

                    TipoMovimentacaoFinanceira.AjusteDebito =>
                        "Ajuste de débito",

                    _ =>
                        "Movimentação"
                };

            TempData["Sucesso"] =
                $"{nomeTipo} registrado com sucesso para \"{cliente.Nome}\". Novo saldo devedor: {cliente.SaldoDevedor:C2}.";

            return RedirectToAction(
                nameof(Financeiro),
                new
                {
                    id = cliente.Id
                });
        }

        // =========================================================
        // ALTERAR LIBERAÇÃO DE NOVOS PEDIDOS
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlterarLiberacaoPedidos(
            int id)
        {
            if (!await UsuarioEhAdministrador())
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c =>
                    c.Id == id);

            if (cliente == null)
            {
                return NotFound();
            }

            cliente.PermitirNovosPedidos =
                !cliente.PermitirNovosPedidos;

            await _context.SaveChangesAsync();

            TempData["Sucesso"] =
                cliente.PermitirNovosPedidos
                    ? $"Novos pedidos liberados para \"{cliente.Nome}\"."
                    : $"Novos pedidos bloqueados para \"{cliente.Nome}\".";

            return RedirectToAction(
                nameof(Financeiro),
                new
                {
                    id = cliente.Id
                });
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
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cliente == null)
            {
                return NotFound();
            }

            var possuiPedidos =
                await _context.Pedidos
                    .AnyAsync(p =>
                        p.ClienteId == id);

            var possuiMovimentacoes =
                await _context.MovimentacoesFinanceiras
                    .AnyAsync(m =>
                        m.ClienteId == id);

            if (possuiPedidos ||
                possuiMovimentacoes)
            {
                TempData["Erro"] =
                    "Este cliente possui histórico de pedidos ou financeiro e não pode ser excluído. Desative o cliente para preservar o histórico.";

                return RedirectToAction(nameof(Index));
            }

            var nomeCliente = cliente.Nome;

            _context.Clientes.Remove(cliente);

            await _context.SaveChangesAsync();

            TempData["Sucesso"] =
                $"Cliente \"{nomeCliente}\" excluído com sucesso.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // PREPARA VIEWMODEL DE CRIAÇÃO
        // =========================================================

        private static void PrepararModelCriacao(
            ClienteCriarViewModel model)
        {
            model.Nome =
                model.Nome?.Trim()
                ?? string.Empty;

            model.CpfCnpj =
                LimparOuRetornarNulo(
                    model.CpfCnpj);

            model.Telefone =
                LimparOuRetornarNulo(
                    model.Telefone);

            model.Email =
                LimparOuRetornarNulo(
                    model.Email);

            model.Endereco =
                LimparOuRetornarNulo(
                    model.Endereco);

            model.Bairro =
                LimparOuRetornarNulo(
                    model.Bairro);

            model.Cidade =
                LimparOuRetornarNulo(
                    model.Cidade);

            model.Numero =
                LimparOuRetornarNulo(
                    model.Numero);

            model.Complemento =
                LimparOuRetornarNulo(
                    model.Complemento);

            model.Cep =
                LimparOuRetornarNulo(
                    model.Cep);
        }

        // =========================================================
        // PREPARA CLIENTE
        // =========================================================

        private static void PrepararCliente(
            Cliente cliente)
        {
            cliente.Nome =
                cliente.Nome?.Trim()
                ?? string.Empty;

            cliente.CpfCnpj =
                LimparOuRetornarNulo(
                    cliente.CpfCnpj);

            cliente.Telefone =
                LimparOuRetornarNulo(
                    cliente.Telefone);

            cliente.Email =
                LimparOuRetornarNulo(
                    cliente.Email);

            cliente.Endereco =
                LimparOuRetornarNulo(
                    cliente.Endereco);

            cliente.Bairro =
                LimparOuRetornarNulo(
                    cliente.Bairro);

            cliente.Cidade =
                LimparOuRetornarNulo(
                    cliente.Cidade);

            cliente.Numero =
                LimparOuRetornarNulo(
                    cliente.Numero);

            cliente.Complemento =
                LimparOuRetornarNulo(
                    cliente.Complemento);

            cliente.Cep =
                LimparOuRetornarNulo(
                    cliente.Cep);
        }

        // =========================================================
        // LIMPA TEXTO
        // =========================================================

        private static string? LimparOuRetornarNulo(
            string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return null;
            }

            return valor.Trim();
        }
    }
}