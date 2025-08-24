using System.ComponentModel.DataAnnotations;

namespace SistemaVentas.Models
{
    public class Usuario
    {
        [Key]
        public int IdUsuario { get; set; }

        [Required(ErrorMessage = "El nombre de usuario es requerido.")]
        [StringLength(20, ErrorMessage = "El nombre de usuario no puede exceder los 20 caracteres.")]
        public string NombreUsuario { get; set; }

        [Required(ErrorMessage = "La contraseña es requerida.")]
        [StringLength(255, ErrorMessage = "La contraseña no puede exceder los 255 caracteres.")]
        public string PasswordHash { get; set; }

        [Required(ErrorMessage = "El nombre completo es requerido.")]
        [StringLength(100, ErrorMessage = "El nombre completo no puede exceder los 100 caracteres.")]
        public string NombreCompleto { get; set; }

        [Required(ErrorMessage = "El rol es requerido.")]
        [StringLength(15, ErrorMessage = "El rol no puede exceder los 15 caracteres.")]
        [RegularExpression(@"^(Administrador|Vendedor|Almacen)$", ErrorMessage = "El rol debe ser 'Administrador', 'Vendedor' o 'Almacen'.")]
        public string Rol { get; set; }

        [StringLength(100, ErrorMessage = "El email no puede exceder los 100 caracteres.")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido.")]
        public string Email { get; set; }

        public bool Estado { get; set; } = true;
    }
}