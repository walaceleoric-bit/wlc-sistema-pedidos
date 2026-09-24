using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WlcSistemaPedidos.Models;

namespace WlcSistemaPedidos.Controllers
{
    public class ContaController : Controller
    {
        private readonly SignInManager<Usuario> _signInManager;
        private readonly UserManager<Usuario> _userManager;

        public ContaController(
            SignInManager<Usuario> signInManager,
            UserManager<Usuario> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        // =========================================================
        // LOGIN
        // =========================================================

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var usuario = await _userManager.GetUserAsync(User);

                if (usuario != null && usuario.Ativo)
                {
                    return RedirecionarPorPerfil(usuario);
                }

                await _signInManager.SignOutAsync();
            }

            ViewBag.ReturnUrl = returnUrl;

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string login,
            string senha,
            string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (string.IsNullOrWhiteSpace(login) ||
                string.IsNullOrWhiteSpace(senha))
            {
                ViewBag.Erro = "Informe o login e a senha.";
                return View();
            }

            login = login.Trim();

            var usuario = await _userManager.FindByNameAsync(login);

            if (usuario == null)
            {
                ViewBag.Erro = "Login ou senha inválidos.";
                return View();
            }

            if (!usuario.Ativo)
            {
                ViewBag.Erro = "Este usuário está bloqueado.";
                return View();
            }

            var resultado = await _signInManager.PasswordSignInAsync(
                usuario,
                senha,
                isPersistent: false,
                lockoutOnFailure: true);

            if (!resultado.Succeeded)
            {
                if (resultado.IsLockedOut)
                {
                    ViewBag.Erro =
                        "Usuário temporariamente bloqueado por excesso de tentativas.";
                }
                else
                {
                    ViewBag.Erro = "Login ou senha inválidos.";
                }

                return View();
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirecionarPorPerfil(usuario);
        }

        // =========================================================
        // SAIR
        // =========================================================

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sair()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction("Index", "Home");
        }

        // =========================================================
        // ACESSO NEGADO
        // =========================================================

        [HttpGet]
        public IActionResult AcessoNegado()
        {
            return View();
        }

        // =========================================================
        // REDIRECIONAMENTO POR PERFIL
        // =========================================================

        private IActionResult RedirecionarPorPerfil(Usuario usuario)
        {
            if (usuario.Perfil == PerfilUsuario.Administrador)
            {
                return RedirectToAction("Index", "Admin");
            }

            if (usuario.Perfil == PerfilUsuario.Cliente)
            {
                return RedirectToAction("Produtos", "Cliente");
            }

            return RedirectToAction("AcessoNegado", "Conta");
        }
    }
}