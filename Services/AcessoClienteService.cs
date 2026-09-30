using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using WlcSistemaPedidos.Data;
using WlcSistemaPedidos.Models;

namespace WlcSistemaPedidos.Services
{
    public class AcessoClienteService
    {
        private readonly AppDbContext _context;

        public AcessoClienteService(AppDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // CRIAR ACESSO SEGURO
        // =========================================================

        public async Task<string> CriarTokenAsync(
            int clienteId,
            DateTime dataExpiracao)
        {
            var clienteExiste =
                await _context.Clientes
                    .AsNoTracking()
                    .AnyAsync(c =>
                        c.Id == clienteId &&
                        c.Ativo);

            if (!clienteExiste)
            {
                throw new InvalidOperationException(
                    "Cliente não encontrado ou inativo.");
            }

            if (dataExpiracao <= DateTime.UtcNow)
            {
                throw new ArgumentException(
                    "A data de expiração do acesso deve ser futura.",
                    nameof(dataExpiracao));
            }

            /*
             * Gera 32 bytes aleatórios
             * criptograficamente seguros.
             */
            var bytesToken =
                RandomNumberGenerator.GetBytes(32);

            /*
             * Transforma em formato seguro
             * para utilização em URL.
             */
            var token =
                WebEncoders.Base64UrlEncode(
                    bytesToken);

            /*
             * Somente o hash será armazenado
             * no banco de dados.
             */
            var tokenHash =
                GerarHash(token);

            var acesso =
                new AcessoCliente
                {
                    ClienteId =
                        clienteId,

                    TokenHash =
                        tokenHash,

                    DataCriacao =
                        DateTime.UtcNow,

                    DataExpiracao =
                        dataExpiracao,

                    Ativo =
                        true
                };

            _context.AcessosClientes.Add(
                acesso);

            await _context.SaveChangesAsync();

            /*
             * O token puro é devolvido somente
             * para quem precisa montar o link.
             *
             * Ele não é armazenado em AcessoCliente.
             */
            return token;
        }

        // =========================================================
        // VALIDAR TOKEN
        // =========================================================

        public async Task<AcessoCliente?> ValidarTokenAsync(
            string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            var tokenHash =
                GerarHash(token);

            var agora =
                DateTime.UtcNow;

            var acesso =
                await _context.AcessosClientes
                    .Include(a => a.Cliente)
                    .FirstOrDefaultAsync(a =>
                        a.TokenHash == tokenHash &&
                        a.Ativo &&
                        a.DataExpiracao > agora &&
                        a.Cliente.Ativo);

            if (acesso == null)
            {
                return null;
            }

            acesso.DataUltimoAcesso =
                agora;

            await _context.SaveChangesAsync();

            return acesso;
        }

        // =========================================================
        // REVOGAR ACESSO PELO ID
        // =========================================================

        public async Task<bool> RevogarAsync(
            int acessoId)
        {
            var acesso =
                await _context.AcessosClientes
                    .FirstOrDefaultAsync(a =>
                        a.Id == acessoId);

            if (acesso == null)
            {
                return false;
            }

            acesso.Ativo = false;

            await _context.SaveChangesAsync();

            return true;
        }

        // =========================================================
        // REVOGAR ACESSO PELO TOKEN
        // =========================================================

        public async Task<bool> RevogarTokenAsync(
            string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            /*
             * Não precisamos armazenar o token puro
             * para encontrá-lo.
             *
             * Calculamos novamente o SHA-256 e
             * procuramos pelo hash salvo.
             */
            var tokenHash =
                GerarHash(token);

            var acesso =
                await _context.AcessosClientes
                    .FirstOrDefaultAsync(a =>
                        a.TokenHash == tokenHash);

            if (acesso == null)
            {
                return false;
            }

            acesso.Ativo = false;

            await _context.SaveChangesAsync();

            return true;
        }

        // =========================================================
        // HASH SHA-256
        // =========================================================

        private static string GerarHash(
            string token)
        {
            var bytes =
                Encoding.UTF8.GetBytes(token);

            var hash =
                SHA256.HashData(bytes);

            return Convert.ToHexString(hash);
        }
    }
}