using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace WlcSistemaPedidos.Models
{
    public class Usuario : IdentityUser<int>
    {
        [Required]
        [StringLength(150)]
        public string Nome { get; set; } = string.Empty;

        public PerfilUsuario Perfil { get; set; }

        public bool Ativo { get; set; } = true;

        public DateTime DataCadastro { get; set; } = DateTime.UtcNow;
    }

    public enum PerfilUsuario
    {
        Administrador = 1,
        Cliente = 2
    }
}