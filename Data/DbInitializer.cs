
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using WlcSistemaPedidos.Models;

namespace WlcSistemaPedidos.Data
{
    public static class DbInitializer
    {
        public static async Task InicializarAsync(
            AppDbContext context,
            UserManager<Usuario> userManager,
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            await context.Database.MigrateAsync();

            const string loginAdminInicial = "adm";
            const string senhaAdminInicial = "123456";

            var adminExistente = await context.Users
                .FirstOrDefaultAsync(u =>
                    u.Perfil == PerfilUsuario.Administrador);

            if (adminExistente == null)
            {
                var admin = new Usuario
                {
                    UserName = loginAdminInicial,
                    Nome = "Administrador",
                    Perfil = PerfilUsuario.Administrador,
                    Ativo = true,
                    EmailConfirmed = true
                };

                var resultado = await userManager.CreateAsync(
                    admin, senhaAdminInicial);

                if (!resultado.Succeeded)
                {
                    throw new Exception(string.Join(" | ",
                        resultado.Errors.Select(e => e.Description)));
                }
            }
            else if (environment.IsDevelopment())
            {
                // Recuperação temporária por User Secrets.
                // Executa somente quando a chave Executar é true.
                bool executarReset = configuration.GetValue<bool>(
                    "AdminReset:Executar");

                if (executarReset)
                {
                    string? novoLogin =
                        configuration["AdminReset:Login"];

                    string? novaSenha =
                        configuration["AdminReset:Senha"];

                    if (string.IsNullOrWhiteSpace(novoLogin) ||
                        string.IsNullOrWhiteSpace(novaSenha))
                    {
                        throw new InvalidOperationException(
                            "Informe AdminReset:Login e AdminReset:Senha nos Segredos do Usuário.");
                    }

                    novoLogin = novoLogin.Trim();

                    // Evita utilizar um login pertencente a outro usuário.
                    var usuarioComLogin = await userManager
                        .FindByNameAsync(novoLogin);

                    if (usuarioComLogin != null &&
                        usuarioComLogin.Id != adminExistente.Id)
                    {
                        throw new InvalidOperationException(
                            "O login informado já pertence a outro usuário.");
                    }

                    if (adminExistente.UserName != novoLogin)
                    {
                        var resultadoLogin = await userManager
                            .SetUserNameAsync(
                                adminExistente, novoLogin);

                        if (!resultadoLogin.Succeeded)
                        {
                            throw new Exception(string.Join(" | ",
                                resultadoLogin.Errors
                                    .Select(e => e.Description)));
                        }
                    }

                    var token = await userManager
                        .GeneratePasswordResetTokenAsync(adminExistente);

                    var resultadoSenha = await userManager
                        .ResetPasswordAsync(
                            adminExistente, token, novaSenha);

                    if (!resultadoSenha.Succeeded)
                    {
                        throw new Exception(string.Join(" | ",
                            resultadoSenha.Errors
                                .Select(e => e.Description)));
                    }

                    await userManager.SetLockoutEndDateAsync(
                        adminExistente, null);

                    await userManager.ResetAccessFailedCountAsync(
                        adminExistente);

                    Console.WriteLine(
                        $"Recuperação concluída. Login: {novoLogin}");
                }
            }

            if (!await context.ConfiguracoesSistema.AnyAsync())
            {
                var configuracao = new ConfiguracaoSistema
                {
                    NomeEstabelecimento = "Padaria",
                    AceitarPedidos = true,
                    TaxaEntregaPadrao = 0,
                    MensagemInicial =
                        "Faça seu pedido de forma rápida e fácil.",
                    EmissaoFiscalAtiva = false,
                    DataAtualizacao = DateTime.UtcNow
                };

                context.ConfiguracoesSistema.Add(configuracao);
                await context.SaveChangesAsync();
            }
        }
    }
}
