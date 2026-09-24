using Microsoft.AspNetCore.Identity;
using WlcSistemaPedidos.Models;

namespace WlcSistemaPedidos.Validators
{
    public class SenhaNumericaValidator : IPasswordValidator<Usuario>
    {
        public Task<IdentityResult> ValidateAsync(
            UserManager<Usuario> manager,
            Usuario user,
            string? password)
        {
            if (string.IsNullOrWhiteSpace(password) ||
                password.Length != 6 ||
                !password.All(char.IsDigit))
            {
                return Task.FromResult(
                    IdentityResult.Failed(
                        new IdentityError
                        {
                            Code = "SenhaInvalida",
                            Description = "A senha deve conter exatamente 6 números."
                        }));
            }

            return Task.FromResult(IdentityResult.Success);
        }
    }
}