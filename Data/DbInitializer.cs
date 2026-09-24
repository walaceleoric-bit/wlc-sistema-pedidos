using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WlcSistemaPedidos.Models;

namespace WlcSistemaPedidos.Data
{
    public static class DbInitializer
    {
        public static async Task InicializarAsync(
            AppDbContext context,
            UserManager<Usuario> userManager)
        {
            // Aplica migrations pendentes
            await context.Database.MigrateAsync();

            // =========================================================
            // ADMINISTRADOR INICIAL
            // =========================================================
            //
            // O administrador inicial só será criado quando NÃO existir
            // nenhum usuário com perfil Administrador.
            //
            // Dessa forma, se o ADM alterar o próprio login depois,
            // o sistema não criará novamente o usuário "adm".
            // =========================================================

            const string loginAdminInicial = "adm";
            const string senhaAdminInicial = "123456";

            var adminExistente =
                await context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        u => u.Perfil ==
                             PerfilUsuario.Administrador);

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

                var resultado =
                    await userManager.CreateAsync(
                        admin,
                        senhaAdminInicial);

                if (!resultado.Succeeded)
                {
                    var erros =
                        string.Join(
                            " | ",
                            resultado.Errors
                                .Select(e => e.Description));

                    throw new Exception(
                        $"Erro ao criar administrador inicial: {erros}");
                }
            }

            // =========================================================
            // CONFIGURAÇÃO INICIAL
            // =========================================================

            if (!await context.ConfiguracoesSistema.AnyAsync())
            {
                var configuracao =
                    new ConfiguracaoSistema
                    {
                        NomeEstabelecimento = "Padaria",
                        AceitarPedidos = true,
                        TaxaEntregaPadrao = 0,
                        MensagemInicial =
                            "Faça seu pedido de forma rápida e fácil.",
                        EmissaoFiscalAtiva = false,
                        DataAtualizacao = DateTime.UtcNow
                    };

                context.ConfiguracoesSistema.Add(
                    configuracao);

                await context.SaveChangesAsync();
            }
        }
    }
}