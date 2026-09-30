using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WlcSistemaPedidos.Data;
using WlcSistemaPedidos.Models;
using WlcSistemaPedidos.Services;

namespace WlcSistemaPedidos.Controllers
{
    [AllowAnonymous]
    public class ClienteController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AcessoClienteService _acessoClienteService;

        private const string ChaveClienteId =
            "ClienteIdentificadoId";

        private const string ChaveTelefoneCadastro =
            "TelefoneNovoCliente";

        public ClienteController(
            AppDbContext context,
            AcessoClienteService acessoClienteService)
        {
            _context = context;
            _acessoClienteService = acessoClienteService;
        }

        // =========================================================
        // CLIENTE IDENTIFICADO NA SESSÃO
        // =========================================================

        private async Task<Cliente?> ObterClienteIdentificado()
        {
            var clienteId =
                HttpContext.Session.GetInt32(
                    ChaveClienteId);

            if (!clienteId.HasValue)
            {
                return null;
            }

            return await _context.Clientes
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.Id == clienteId.Value &&
                    c.Ativo);
        }

        // =========================================================
        // NORMALIZAR TELEFONE
        // =========================================================

        private static string SomenteNumeros(
            string valor)
        {
            return new string(
                valor
                    .Where(char.IsDigit)
                    .ToArray());
        }

        // =========================================================
        // LIMPAR TEXTO
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

        // =========================================================
        // ESTABELECIMENTO
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

            if (string.IsNullOrWhiteSpace(
                    nomeEstabelecimento))
            {
                nomeEstabelecimento =
                    "Sistema de Pedidos";
            }

            var logoEstabelecimento =
                configuracao?.LogoUrl;

            if (string.IsNullOrWhiteSpace(
                    logoEstabelecimento))
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
        // ACESSO DO CLIENTE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Acesso()
        {
            var cliente =
                await ObterClienteIdentificado();

            if (cliente != null)
            {
                return RedirectToAction(
                    nameof(Produtos));
            }

            await CarregarEstabelecimento();

            return View();
        }

        // =========================================================
        // ACESSO POR LINK SEGURO
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> EntrarPorLink(
            string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                TempData["ErroAcesso"] =
                    "O link de acesso é inválido.";

                return RedirectToAction(
                    nameof(Acesso));
            }

            var acesso =
                await _acessoClienteService
                    .ValidarTokenAsync(token);

            if (acesso == null)
            {
                TempData["ErroAcesso"] =
                    "Este link de acesso é inválido ou expirou.";

                return RedirectToAction(
                    nameof(Acesso));
            }

            HttpContext.Session.Remove(
                "Carrinho");

            HttpContext.Session.Remove(
                ChaveTelefoneCadastro);

            HttpContext.Session.SetInt32(
                ChaveClienteId,
                acesso.ClienteId);

            return RedirectToAction(
                nameof(Produtos));
        }

        // =========================================================
        // IDENTIFICAR PELO TELEFONE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Acesso(
            string telefone)
        {
            if (string.IsNullOrWhiteSpace(
                    telefone))
            {
                ViewBag.Erro =
                    "Informe seu número de telefone.";

                await CarregarEstabelecimento();

                return View();
            }

            var telefoneInformado =
                SomenteNumeros(telefone);

            if (telefoneInformado.Length < 10 ||
                telefoneInformado.Length > 11)
            {
                ViewBag.Erro =
                    "Informe um número de telefone válido com DDD.";

                await CarregarEstabelecimento();

                return View();
            }

            var clientes =
                await _context.Clientes
                    .AsNoTracking()
                    .Where(c =>
                        c.Ativo &&
                        c.Telefone != null)
                    .Select(c => new
                    {
                        c.Id,
                        c.Telefone
                    })
                    .ToListAsync();

            var clienteEncontrado =
                clientes.FirstOrDefault(c =>
                    SomenteNumeros(
                        c.Telefone ?? string.Empty)
                    == telefoneInformado);

            // =====================================================
            // CLIENTE JÁ CADASTRADO
            // =====================================================

            if (clienteEncontrado != null)
            {
                HttpContext.Session.Remove(
                    "Carrinho");

                HttpContext.Session.Remove(
                    ChaveTelefoneCadastro);

                HttpContext.Session.SetInt32(
                    ChaveClienteId,
                    clienteEncontrado.Id);

                return RedirectToAction(
                    nameof(Produtos));
            }

            // =====================================================
            // CLIENTE NOVO
            // GUARDA O TELEFONE TEMPORARIAMENTE NA SESSÃO
            // =====================================================

            HttpContext.Session.Remove(
                ChaveClienteId);

            HttpContext.Session.Remove(
                "Carrinho");

            HttpContext.Session.SetString(
                ChaveTelefoneCadastro,
                telefoneInformado);

            return RedirectToAction(
                nameof(Cadastro));
        }

        // =========================================================
        // CADASTRO DO NOVO CLIENTE - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Cadastro()
        {
            var cliente =
                await ObterClienteIdentificado();

            if (cliente != null)
            {
                return RedirectToAction(
                    nameof(Produtos));
            }

            var telefone =
                HttpContext.Session.GetString(
                    ChaveTelefoneCadastro);

            if (string.IsNullOrWhiteSpace(telefone))
            {
                return RedirectToAction(
                    nameof(Acesso));
            }

            ViewBag.Telefone = telefone;

            await CarregarEstabelecimento();

            return View();
        }

        // =========================================================
        // CADASTRO DO NOVO CLIENTE - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cadastro(
            string nome,
            string telefone,
            string endereco,
            string numero,
            string bairro,
            string cidade,
            string? cep,
            string? complemento)
        {
            var telefoneSessao =
                HttpContext.Session.GetString(
                    ChaveTelefoneCadastro);

            if (string.IsNullOrWhiteSpace(
                    telefoneSessao))
            {
                return RedirectToAction(
                    nameof(Acesso));
            }

            telefoneSessao =
                SomenteNumeros(telefoneSessao);

            var telefoneInformado =
                SomenteNumeros(
                    telefone ?? string.Empty);

            // O telefone não pode ser trocado durante
            // o cadastro iniciado na tela anterior.
            if (telefoneInformado != telefoneSessao)
            {
                ViewBag.Erro =
                    "O telefone informado não corresponde ao telefone usado para iniciar o cadastro.";

                ViewBag.Telefone =
                    telefoneSessao;

                await CarregarEstabelecimento();

                return View();
            }

            nome = nome?.Trim() ?? string.Empty;
            endereco = endereco?.Trim() ?? string.Empty;
            numero = numero?.Trim() ?? string.Empty;
            bairro = bairro?.Trim() ?? string.Empty;
            cidade = cidade?.Trim() ?? string.Empty;

            cep =
                LimparOuRetornarNulo(cep);

            complemento =
                LimparOuRetornarNulo(
                    complemento);

            if (string.IsNullOrWhiteSpace(nome))
            {
                ViewBag.Erro =
                    "Informe seu nome.";

                ViewBag.Telefone =
                    telefoneSessao;

                await CarregarEstabelecimento();

                return View();
            }

            if (nome.Length > 150)
            {
                ViewBag.Erro =
                    "O nome informado é muito longo.";

                ViewBag.Telefone =
                    telefoneSessao;

                await CarregarEstabelecimento();

                return View();
            }

            if (string.IsNullOrWhiteSpace(endereco))
            {
                ViewBag.Erro =
                    "Informe seu endereço.";

                ViewBag.Telefone =
                    telefoneSessao;

                await CarregarEstabelecimento();

                return View();
            }

            if (endereco.Length > 250)
            {
                ViewBag.Erro =
                    "O endereço informado é muito longo.";

                ViewBag.Telefone =
                    telefoneSessao;

                await CarregarEstabelecimento();

                return View();
            }

            if (string.IsNullOrWhiteSpace(numero))
            {
                ViewBag.Erro =
                    "Informe o número do endereço.";

                ViewBag.Telefone =
                    telefoneSessao;

                await CarregarEstabelecimento();

                return View();
            }

            if (numero.Length > 10)
            {
                ViewBag.Erro =
                    "O número do endereço é muito longo.";

                ViewBag.Telefone =
                    telefoneSessao;

                await CarregarEstabelecimento();

                return View();
            }

            if (string.IsNullOrWhiteSpace(bairro))
            {
                ViewBag.Erro =
                    "Informe seu bairro.";

                ViewBag.Telefone =
                    telefoneSessao;

                await CarregarEstabelecimento();

                return View();
            }

            if (bairro.Length > 100)
            {
                ViewBag.Erro =
                    "O bairro informado é muito longo.";

                ViewBag.Telefone =
                    telefoneSessao;

                await CarregarEstabelecimento();

                return View();
            }

            if (string.IsNullOrWhiteSpace(cidade))
            {
                ViewBag.Erro =
                    "Informe sua cidade.";

                ViewBag.Telefone =
                    telefoneSessao;

                await CarregarEstabelecimento();

                return View();
            }

            if (cidade.Length > 100)
            {
                ViewBag.Erro =
                    "A cidade informada é muito longa.";

                ViewBag.Telefone =
                    telefoneSessao;

                await CarregarEstabelecimento();

                return View();
            }

            if (cep != null &&
                cep.Length > 10)
            {
                ViewBag.Erro =
                    "O CEP informado é inválido.";

                ViewBag.Telefone =
                    telefoneSessao;

                await CarregarEstabelecimento();

                return View();
            }

            if (complemento != null &&
                complemento.Length > 250)
            {
                ViewBag.Erro =
                    "O complemento informado é muito longo.";

                ViewBag.Telefone =
                    telefoneSessao;

                await CarregarEstabelecimento();

                return View();
            }

            // =====================================================
            // CONFERE NOVAMENTE SE O TELEFONE JÁ FOI CADASTRADO
            // =====================================================

            var clientesComTelefone =
                await _context.Clientes
                    .Where(c =>
                        c.Telefone != null)
                    .ToListAsync();

            var clienteExistente =
                clientesComTelefone.FirstOrDefault(c =>
                    SomenteNumeros(
                        c.Telefone ?? string.Empty)
                    == telefoneSessao);

            if (clienteExistente != null)
            {
                if (!clienteExistente.Ativo)
                {
                    ViewBag.Erro =
                        "Este telefone já possui um cadastro inativo. Entre em contato com o estabelecimento.";

                    ViewBag.Telefone =
                        telefoneSessao;

                    await CarregarEstabelecimento();

                    return View();
                }

                HttpContext.Session.Remove(
                    "Carrinho");

                HttpContext.Session.Remove(
                    ChaveTelefoneCadastro);

                HttpContext.Session.SetInt32(
                    ChaveClienteId,
                    clienteExistente.Id);

                return RedirectToAction(
                    nameof(Produtos));
            }

            // =====================================================
            // CRIA O NOVO CLIENTE
            // =====================================================

            var novoCliente =
                new Cliente
                {
                    Nome = nome,
                    Telefone = telefoneSessao,

                    Endereco = endereco,
                    Numero = numero,
                    Bairro = bairro,
                    Cidade = cidade,
                    Cep = cep,
                    Complemento = complemento,

                    Ativo = true,
                    PermitirNovosPedidos = true,

                    SaldoDevedor = 0,
                    DataCadastro = DateTime.UtcNow,

                    UsuarioId = null
                };

            _context.Clientes.Add(
                novoCliente);

            await _context.SaveChangesAsync();

            // =====================================================
            // IDENTIFICA AUTOMATICAMENTE O NOVO CLIENTE
            // =====================================================

            HttpContext.Session.Remove(
                "Carrinho");

            HttpContext.Session.Remove(
                ChaveTelefoneCadastro);

            HttpContext.Session.SetInt32(
                ChaveClienteId,
                novoCliente.Id);

            return RedirectToAction(
                nameof(Produtos));
        }

        // =========================================================
        // TROCAR CLIENTE / SAIR
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Sair()
        {
            HttpContext.Session.Remove(
                ChaveClienteId);

            HttpContext.Session.Remove(
                ChaveTelefoneCadastro);

            HttpContext.Session.Remove(
                "Carrinho");

            return RedirectToAction(
                nameof(Acesso));
        }

        // =========================================================
        // INÍCIO DO CLIENTE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cliente =
                await ObterClienteIdentificado();

            if (cliente == null)
            {
                return RedirectToAction(
                    nameof(Acesso));
            }

            ViewBag.NomeCliente =
                cliente.Nome;

            ViewBag.SaldoDevedor =
                cliente.SaldoDevedor;

            ViewBag.PermitirNovosPedidos =
                cliente.PermitirNovosPedidos;

            await CarregarEstabelecimento();

            return View();
        }

        // =========================================================
        // PRODUTOS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Produtos(
            int pagina = 1,
            string? busca = null,
            int? categoriaId = null)
        {
            var cliente =
                await ObterClienteIdentificado();

            if (cliente == null)
            {
                return RedirectToAction(
                    nameof(Acesso));
            }

            const int itensPorPagina = 10;

            if (pagina < 1)
            {
                pagina = 1;
            }

            var consulta =
                _context.Produtos
                    .AsNoTracking()
                    .Include(p => p.Categoria)
                    .Where(p =>
                        p.Ativo &&
                        p.Disponivel &&
                        p.Categoria.Ativa)
                    .AsQueryable();

            if (!string.IsNullOrWhiteSpace(
                    busca))
            {
                busca = busca.Trim();

                consulta =
                    consulta.Where(p =>
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
                consulta =
                    consulta.Where(p =>
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

            var produtos =
                await consulta
                    .OrderBy(p =>
                        p.OrdemExibicao)
                    .ThenBy(p =>
                        p.Nome)
                    .Skip(
                        (pagina - 1) *
                        itensPorPagina)
                    .Take(itensPorPagina)
                    .ToListAsync();

            var categorias =
                await _context.Categorias
                    .AsNoTracking()
                    .Where(c =>
                        c.Ativa)
                    .OrderBy(c =>
                        c.OrdemExibicao)
                    .ThenBy(c =>
                        c.Nome)
                    .ToListAsync();

            ViewBag.NomeCliente =
                cliente.Nome;

            ViewBag.SaldoDevedor =
                cliente.SaldoDevedor;

            ViewBag.PermitirNovosPedidos =
                cliente.PermitirNovosPedidos;

            ViewBag.Categorias =
                categorias;

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

            await CarregarEstabelecimento();

            return View(produtos);
        }

        // =========================================================
        // HISTÓRICO
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Historico(
            int pagina = 1,
            string? busca = null,
            StatusPedido? status = null)
        {
            var cliente =
                await ObterClienteIdentificado();

            if (cliente == null)
            {
                return RedirectToAction(
                    nameof(Acesso));
            }

            const int itensPorPagina = 10;

            if (pagina < 1)
            {
                pagina = 1;
            }

            var consulta =
                _context.Pedidos
                    .AsNoTracking()
                    .Where(p =>
                        p.ClienteId ==
                        cliente.Id)
                    .AsQueryable();

            if (!string.IsNullOrWhiteSpace(
                    busca))
            {
                busca = busca.Trim();

                if (busca.StartsWith("#"))
                {
                    busca =
                        busca.Substring(1)
                            .Trim();
                }

                if (int.TryParse(
                    busca,
                    out int numeroPedido))
                {
                    consulta =
                        consulta.Where(p =>
                            p.Id ==
                            numeroPedido);
                }
                else
                {
                    consulta =
                        consulta.Where(p =>
                            false);
                }
            }

            if (status.HasValue &&
                Enum.IsDefined(
                    typeof(StatusPedido),
                    status.Value))
            {
                consulta =
                    consulta.Where(p =>
                        p.Status ==
                        status.Value);
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

            var pedidos =
                await consulta
                    .OrderByDescending(p =>
                        p.DataPedido)
                    .Skip(
                        (pagina - 1) *
                        itensPorPagina)
                    .Take(itensPorPagina)
                    .ToListAsync();

            ViewBag.NomeCliente =
                cliente.Nome;

            ViewBag.SaldoDevedor =
                cliente.SaldoDevedor;

            ViewBag.PaginaAtual =
                pagina;

            ViewBag.TotalPaginas =
                totalPaginas;

            ViewBag.TotalPedidos =
                totalPedidos;

            ViewBag.Busca =
                busca;

            ViewBag.StatusSelecionado =
                status;

            await CarregarEstabelecimento();

            return View(pedidos);
        }

        // =========================================================
        // DETALHES DO PEDIDO DO CLIENTE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> DetalhesPedido(
            int id)
        {
            var cliente =
                await ObterClienteIdentificado();

            if (cliente == null)
            {
                return RedirectToAction(
                    nameof(Acesso));
            }

            /*
             * Segurança:
             *
             * Não buscamos somente pelo Id do pedido.
             * O pedido também precisa pertencer ao cliente
             * atualmente identificado na sessão.
             *
             * Portanto, alterar manualmente o número do pedido
             * na URL não permite visualizar pedidos de outro
             * cliente.
             */

            var pedido =
                await _context.Pedidos
                    .AsNoTracking()
                    .Include(p => p.Itens)
                    .FirstOrDefaultAsync(p =>
                        p.Id == id &&
                        p.ClienteId == cliente.Id);

            if (pedido == null)
            {
                return NotFound();
            }

            ViewBag.NomeCliente =
                cliente.Nome;

            ViewBag.SaldoDevedor =
                cliente.SaldoDevedor;

            await CarregarEstabelecimento();

            return View(pedido);
        }
    }
}