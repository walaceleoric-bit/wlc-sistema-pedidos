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
    public class ConfiguracoesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Usuario> _userManager;
        private readonly IWebHostEnvironment _environment;

        public ConfiguracoesController(
            AppDbContext context,
            UserManager<Usuario> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        // =========================================================
        // VALIDA ADMINISTRADOR
        // =========================================================

        private async Task<Usuario?> ObterAdministrador()
        {
            var usuario =
                await _userManager.GetUserAsync(User);

            if (usuario == null ||
                !usuario.Ativo ||
                usuario.Perfil != PerfilUsuario.Administrador)
            {
                return null;
            }

            return usuario;
        }

        // =========================================================
        // CONFIGURAÇÕES
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var administrador =
                await ObterAdministrador();

            if (administrador == null)
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            var configuracao =
                await _context.ConfiguracoesSistema
                    .AsNoTracking()
                    .OrderBy(c => c.Id)
                    .FirstOrDefaultAsync();

            if (configuracao == null)
            {
                configuracao =
                    new ConfiguracaoSistema
                    {
                        NomeEstabelecimento = "Padaria",
                        AceitarPedidos = true,
                        TaxaEntregaPadrao = 0,
                        MensagemInicial =
                            "Faça seu pedido de forma rápida e fácil."
                    };
            }

            var model =
                new ConfiguracaoViewModel
                {
                    Id = configuracao.Id,

                    NomeEstabelecimento =
                        configuracao.NomeEstabelecimento,

                    RazaoSocial =
                        configuracao.RazaoSocial,

                    CpfCnpj =
                        configuracao.CpfCnpj,

                    Telefone =
                        configuracao.Telefone,

                    Email =
                        configuracao.Email,

                    WhatsAppPedidos =
                        configuracao.WhatsAppPedidos,

                    UrlSistema =
                        configuracao.UrlSistema,

                    Cep =
                        configuracao.Cep,

                    Endereco =
                        configuracao.Endereco,

                    Bairro =
                        configuracao.Bairro,

                    Cidade =
                        configuracao.Cidade,

                    Estado =
                        configuracao.Estado,

                    TaxaEntregaPadrao =
                        configuracao.TaxaEntregaPadrao,

                    AceitarPedidos =
                        configuracao.AceitarPedidos,

                    MensagemInicial =
                        configuracao.MensagemInicial,

                    EmissaoFiscalAtiva =
                        configuracao.EmissaoFiscalAtiva,

                    AmbienteFiscal =
                        configuracao.AmbienteFiscal,

                    ProvedorFiscal =
                        configuracao.ProvedorFiscal,

                    LogoUrlAtual =
                        configuracao.LogoUrl,

                    BannerUrlAtual =
                        configuracao.BannerUrl,

                    NovoLoginAdministrador =
                        administrador.UserName
                };

            PrepararDadosAdministrador(
                administrador);

            return View(model);
        }

        // =========================================================
        // SALVAR CONFIGURAÇÕES DO ESTABELECIMENTO
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            ConfiguracaoViewModel model)
        {
            var administrador =
                await ObterAdministrador();

            if (administrador == null)
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            model.NomeEstabelecimento =
                model.NomeEstabelecimento?.Trim()
                ?? string.Empty;

            model.RazaoSocial =
                LimparTexto(model.RazaoSocial);

            model.CpfCnpj =
                SomenteNumeros(model.CpfCnpj);

            model.Telefone =
                SomenteNumeros(model.Telefone);

            model.Email =
                LimparTexto(model.Email);

            model.WhatsAppPedidos =
                SomenteNumeros(model.WhatsAppPedidos);

            model.UrlSistema =
                LimparTexto(model.UrlSistema);

            model.Cep =
                SomenteNumeros(model.Cep);

            model.Endereco =
                LimparTexto(model.Endereco);

            model.Bairro =
                LimparTexto(model.Bairro);

            model.Cidade =
                LimparTexto(model.Cidade);

            model.Estado =
                LimparTexto(model.Estado)?
                    .ToUpperInvariant();

            model.MensagemInicial =
                LimparTexto(model.MensagemInicial);

            model.AmbienteFiscal =
                LimparTexto(model.AmbienteFiscal);

            model.ProvedorFiscal =
                LimparTexto(model.ProvedorFiscal);

            if (!string.IsNullOrWhiteSpace(
                    model.WhatsAppPedidos))
            {
                if (model.WhatsAppPedidos.Length < 10 ||
                    model.WhatsAppPedidos.Length > 11)
                {
                    ModelState.AddModelError(
                        nameof(model.WhatsAppPedidos),
                        "Informe um WhatsApp válido com DDD.");
                }
            }

            if (!string.IsNullOrWhiteSpace(model.Cep) &&
                model.Cep.Length != 8)
            {
                ModelState.AddModelError(
                    nameof(model.Cep),
                    "Informe um CEP válido com 8 números.");
            }

            if (!string.IsNullOrWhiteSpace(model.Estado) &&
                model.Estado.Length != 2)
            {
                ModelState.AddModelError(
                    nameof(model.Estado),
                    "Informe a UF com 2 letras.");
            }

            var configuracao =
                await _context.ConfiguracoesSistema
                    .OrderBy(c => c.Id)
                    .FirstOrDefaultAsync();

            if (configuracao != null)
            {
                model.LogoUrlAtual =
                    configuracao.LogoUrl;

                model.BannerUrlAtual =
                    configuracao.BannerUrl;
            }

            // Os campos de acesso pertencem a outros formulários.
            // Eles não devem invalidar o formulário do estabelecimento.
            ModelState.Remove(
                nameof(model.NovoLoginAdministrador));

            ModelState.Remove(
                nameof(model.SenhaAtual));

            ModelState.Remove(
                nameof(model.NovaSenha));

            ModelState.Remove(
                nameof(model.ConfirmarNovaSenha));

            ModelState.Remove(
                nameof(model.LogoArquivo));

            if (!ModelState.IsValid)
            {
                PrepararDadosAdministrador(
                    administrador);

                return View(model);
            }

            if (configuracao == null)
            {
                configuracao =
                    new ConfiguracaoSistema();

                _context.ConfiguracoesSistema.Add(
                    configuracao);
            }

            configuracao.NomeEstabelecimento =
                model.NomeEstabelecimento;

            configuracao.RazaoSocial =
                model.RazaoSocial;

            configuracao.CpfCnpj =
                model.CpfCnpj;

            configuracao.Telefone =
                model.Telefone;

            configuracao.Email =
                model.Email;

            configuracao.WhatsAppPedidos =
                model.WhatsAppPedidos;

            configuracao.UrlSistema =
                model.UrlSistema;

            configuracao.Cep =
                model.Cep;

            configuracao.Endereco =
                model.Endereco;

            configuracao.Bairro =
                model.Bairro;

            configuracao.Cidade =
                model.Cidade;

            configuracao.Estado =
                model.Estado;

            configuracao.TaxaEntregaPadrao =
                model.TaxaEntregaPadrao;

            configuracao.AceitarPedidos =
                model.AceitarPedidos;

            configuracao.MensagemInicial =
                model.MensagemInicial;

            configuracao.EmissaoFiscalAtiva =
                model.EmissaoFiscalAtiva;

            configuracao.AmbienteFiscal =
                model.AmbienteFiscal;

            configuracao.ProvedorFiscal =
                model.ProvedorFiscal;

            configuracao.DataAtualizacao =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Sucesso"] =
                "Configurações salvas com sucesso.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // ALTERAR LOGO DO ESTABELECIMENTO
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlterarLogo(
            IFormFile? logoArquivo,
            bool removerLogo = false)
        {
            var administrador =
                await ObterAdministrador();

            if (administrador == null)
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            var configuracao =
                await _context.ConfiguracoesSistema
                    .OrderBy(c => c.Id)
                    .FirstOrDefaultAsync();

            if (configuracao == null)
            {
                TempData["Erro"] =
                    "As configurações do estabelecimento não foram encontradas.";

                return RedirectToAction(nameof(Index));
            }

            if (removerLogo)
            {
                ExcluirArquivoLogo(
                    configuracao.LogoUrl);

                configuracao.LogoUrl = null;
                configuracao.DataAtualizacao =
                    DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["Sucesso"] =
                    "Logo do estabelecimento removida com sucesso.";

                return RedirectToAction(nameof(Index));
            }

            if (logoArquivo == null ||
                logoArquivo.Length == 0)
            {
                TempData["Erro"] =
                    "Selecione uma imagem para a logo.";

                return RedirectToAction(nameof(Index));
            }

            const long tamanhoMaximo =
                5 * 1024 * 1024;

            if (logoArquivo.Length > tamanhoMaximo)
            {
                TempData["Erro"] =
                    "A imagem deve possuir no máximo 5 MB.";

                return RedirectToAction(nameof(Index));
            }

            var extensao =
                Path.GetExtension(
                    logoArquivo.FileName)
                    .ToLowerInvariant();

            var extensoesPermitidas =
                new[]
                {
                    ".jpg",
                    ".jpeg",
                    ".png",
                    ".webp"
                };

            if (!extensoesPermitidas.Contains(extensao))
            {
                TempData["Erro"] =
                    "Formato inválido. Utilize JPG, JPEG, PNG ou WEBP.";

                return RedirectToAction(nameof(Index));
            }

            var pasta =
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "estabelecimento");

            Directory.CreateDirectory(pasta);

            var nomeArquivo =
                $"logo-{Guid.NewGuid():N}{extensao}";

            var caminhoCompleto =
                Path.Combine(
                    pasta,
                    nomeArquivo);

            await using (
                var stream =
                    new FileStream(
                        caminhoCompleto,
                        FileMode.Create))
            {
                await logoArquivo.CopyToAsync(
                    stream);
            }

            ExcluirArquivoLogo(
                configuracao.LogoUrl);

            configuracao.LogoUrl =
                $"/uploads/estabelecimento/{nomeArquivo}";

            configuracao.DataAtualizacao =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Sucesso"] =
                "Logo do estabelecimento alterada com sucesso.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // ALTERAR LOGIN DO ADMINISTRADOR
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlterarLogin(
            string? novoLoginAdministrador)
        {
            var administrador =
                await ObterAdministrador();

            if (administrador == null)
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            var novoLogin =
                LimparTexto(
                    novoLoginAdministrador);

            if (string.IsNullOrWhiteSpace(
                    novoLogin))
            {
                TempData["Erro"] =
                    "Informe o novo login.";

                return RedirectToAction(nameof(Index));
            }

            if (novoLogin.Length < 3 ||
                novoLogin.Length > 50)
            {
                TempData["Erro"] =
                    "O login deve possuir entre 3 e 50 caracteres.";

                return RedirectToAction(nameof(Index));
            }

            var usuarioExistente =
                await _userManager
                    .FindByNameAsync(novoLogin);

            if (usuarioExistente != null &&
                usuarioExistente.Id !=
                administrador.Id)
            {
                TempData["Erro"] =
                    "Este login já está sendo utilizado.";

                return RedirectToAction(nameof(Index));
            }

            if (string.Equals(
                    administrador.UserName,
                    novoLogin,
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Erro"] =
                    "O novo login é igual ao login atual.";

                return RedirectToAction(nameof(Index));
            }

            var resultado =
                await _userManager.SetUserNameAsync(
                    administrador,
                    novoLogin);

            if (!resultado.Succeeded)
            {
                TempData["Erro"] =
                    MontarErrosIdentity(
                        resultado);

                return RedirectToAction(nameof(Index));
            }

            TempData["Sucesso"] =
                "Login do administrador alterado com sucesso.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // ALTERAR SENHA DO ADMINISTRADOR
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlterarSenha(
            string? senhaAtual,
            string? novaSenha,
            string? confirmarNovaSenha)
        {
            var administrador =
                await ObterAdministrador();

            if (administrador == null)
            {
                return RedirectToAction(
                    "AcessoNegado",
                    "Conta");
            }

            if (string.IsNullOrWhiteSpace(
                    senhaAtual))
            {
                TempData["Erro"] =
                    "Informe a senha atual.";

                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(
                    novaSenha))
            {
                TempData["Erro"] =
                    "Informe a nova senha.";

                return RedirectToAction(nameof(Index));
            }

            if (novaSenha.Length != 6 ||
                !novaSenha.All(char.IsDigit))
            {
                TempData["Erro"] =
                    "A nova senha deve conter exatamente 6 números.";

                return RedirectToAction(nameof(Index));
            }

            if (novaSenha !=
                confirmarNovaSenha)
            {
                TempData["Erro"] =
                    "A confirmação da nova senha não confere.";

                return RedirectToAction(nameof(Index));
            }

            if (senhaAtual == novaSenha)
            {
                TempData["Erro"] =
                    "A nova senha deve ser diferente da senha atual.";

                return RedirectToAction(nameof(Index));
            }

            var resultado =
                await _userManager.ChangePasswordAsync(
                    administrador,
                    senhaAtual,
                    novaSenha);

            if (!resultado.Succeeded)
            {
                if (resultado.Errors.Any(e =>
                        e.Code.Contains(
                            "PasswordMismatch",
                            StringComparison.OrdinalIgnoreCase)))
                {
                    TempData["Erro"] =
                        "A senha atual está incorreta.";
                }
                else
                {
                    TempData["Erro"] =
                        MontarErrosIdentity(
                            resultado);
                }

                return RedirectToAction(nameof(Index));
            }

            TempData["Sucesso"] =
                "Senha do administrador alterada com sucesso.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // MÉTODOS AUXILIARES
        // =========================================================

        private void PrepararDadosAdministrador(
            Usuario administrador)
        {
            ViewBag.LoginAdministrador =
                administrador.UserName;

            ViewBag.NomeAdministrador =
                administrador.Nome;
        }

        private void ExcluirArquivoLogo(
            string? logoUrl)
        {
            if (string.IsNullOrWhiteSpace(
                    logoUrl))
            {
                return;
            }

            if (!logoUrl.StartsWith(
                    "/uploads/estabelecimento/",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var caminhoRelativo =
                logoUrl.TrimStart('/')
                    .Replace(
                        '/',
                        Path.DirectorySeparatorChar);

            var caminhoCompleto =
                Path.Combine(
                    _environment.WebRootPath,
                    caminhoRelativo);

            try
            {
                if (System.IO.File.Exists(
                        caminhoCompleto))
                {
                    System.IO.File.Delete(
                        caminhoCompleto);
                }
            }
            catch (IOException)
            {
                // Mantém o sistema funcionando mesmo
                // se o arquivo físico não puder ser removido.
            }
            catch (UnauthorizedAccessException)
            {
                // Mantém o sistema funcionando mesmo
                // se o arquivo físico estiver protegido.
            }
        }

        private static string MontarErrosIdentity(
            IdentityResult resultado)
        {
            var erros =
                resultado.Errors
                    .Select(e => e.Description)
                    .Where(e =>
                        !string.IsNullOrWhiteSpace(e))
                    .ToList();

            if (erros.Count == 0)
            {
                return "Não foi possível realizar a alteração.";
            }

            return string.Join(
                " | ",
                erros);
        }

        private static string? LimparTexto(
            string? valor)
        {
            if (string.IsNullOrWhiteSpace(
                    valor))
            {
                return null;
            }

            return valor.Trim();
        }

        private static string? SomenteNumeros(
            string? valor)
        {
            if (string.IsNullOrWhiteSpace(
                    valor))
            {
                return null;
            }

            var numeros =
                new string(
                    valor
                        .Where(char.IsDigit)
                        .ToArray());

            return string.IsNullOrWhiteSpace(
                    numeros)
                ? null
                : numeros;
        }
    }
}