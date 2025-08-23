using System.ComponentModel.DataAnnotations;

namespace SistemaVentas.Models
{
    public class Categoria
    {
        [Key]
        public int IdCategoria { get; set; }

        [Required(ErrorMessage = "El nombre de la categoría es obligatorio")]
        [MinLength(3, ErrorMessage = "Longitud mínima (3) no alcanzada.")]
        [MaxLength(30, ErrorMessage = "Longitud máxima (30) alcanzada.")]
        [Display(Name = "Nombre de la categoría:")]
        public string Nombre { get; set; }
    }
}