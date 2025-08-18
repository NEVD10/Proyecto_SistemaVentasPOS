using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaVentas.Models
{
    [Table("Usuario")]
    public class Usuario
    {
        [Key]
        public int IdUsuario { get; set; }


        [Required(ErrorMessage = "El nombre del usuario es obligatorio")]
        [MinLength(3, ErrorMessage = "Longitud Mínima (3), producto no registrado.")]
        [MaxLength(30, ErrorMessage = "Longitud Máxima alcanzada.")]
        [Display(Name = "Nombres: ")]
        [StringLength(30)]
        public string NombreUsuario { get; set; }

        [Required(ErrorMessage = "La contraseña del usuario es obligatorio")]
        [StringLength(40)]
        [Display(Name = "Contraseña: ")]
        public string PasswordHash { get; set; }

        [Required(ErrorMessage = "El nombre del rol es obligatorio")]
        [MinLength(3, ErrorMessage = "Longitud Mínima (3), producto no registrado.")]
        [MaxLength(15, ErrorMessage = "Longitud Máxima alcanzada.")]
        [Display(Name = "Rol: ")]
        [StringLength(15)]
        public string Rol { get; set; }
    }
}