// Models/Usuario.cs
using System.ComponentModel.DataAnnotations;

namespace SistemaVentas.Models
{
    public class Usuario
    {
        [Key]
        public int IdUsuario { get; set; }

        [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "El nombre de usuario debe tener entre 3 y 20 caracteres")]
        [Display(Name = "Nombre de usuario")]
        public string NombreUsuario { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(255, ErrorMessage = "La contraseña debe tener como máximo 255 caracteres")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string PasswordHash { get; set; } // Se almacenará encriptado con BCrypt

        [Required(ErrorMessage = "El nombre completo es obligatorio")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre completo debe tener entre 3 y 100 caracteres")]
        [Display(Name = "Nombre completo")]
        public string NombreCompleto { get; set; }

        [Required(ErrorMessage = "El rol es obligatorio")]
        [StringLength(15, ErrorMessage = "El rol debe tener como máximo 15 caracteres")]
        [RegularExpression("^(Administrador|Vendedor|Almacen)$", ErrorMessage = "El rol debe ser 'Administrador', 'Vendedor' o 'Almacen'")]
        [Display(Name = "Rol")]
        public string Rol { get; set; }

        [Required(ErrorMessage = "El estado es obligatorio")]
        [Display(Name = "Estado")]
        public bool Estado { get; set; } = true;
    }
}