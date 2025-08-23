using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaVentas.Models
{
    public class Producto
    {
        [Key]
        public int IdProducto { get; set; }

        [Required(ErrorMessage = "El nombre del producto es obligatorio")]
        [MinLength(3, ErrorMessage = "Longitud Mínima (3), producto no registrado.")]
        [MaxLength(100, ErrorMessage = "Longitud Máxima (100) alcanzada.")]
        [Display(Name = "Nombre del producto:")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El código de barras es obligatorio")]
        [MinLength(3, ErrorMessage = "Longitud Mínima (3), producto no registrado.")]
        [MaxLength(50, ErrorMessage = "Longitud Máxima (50) alcanzada.")]
        [Display(Name = "Código de barras del producto:")]
        public string CodigoBarras { get; set; }

        [Required(ErrorMessage = "El precio de costo es obligatorio")]
        [Display(Name = "Precio de Costo del producto:")]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal PrecioCosto { get; set; }

        [Required(ErrorMessage = "El precio de venta es obligatorio")]
        [Display(Name = "Precio de Venta del producto:")]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal PrecioVenta { get; set; }

        [Required(ErrorMessage = "El stock es obligatorio")]
        [Display(Name = "Stock del producto:")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock debe ser mayor o igual a 0")]
        public int Stock { get; set; }

        [MaxLength(30, ErrorMessage = "Longitud Máxima (30) alcanzada.")]
        [Display(Name = "Marca del producto:")]
        public string Marca { get; set; }

        [Required(ErrorMessage = "La categoría es obligatoria")]
        [Display(Name = "Categoría del producto:")]
        public int IdCategoria { get; set; } // Cambiado a int no nullable

        [ForeignKey("IdCategoria")]
        public Categoria Categoria { get; set; } // Relación de navegación

        [Display(Name = "Estado del producto:")]
        public bool Estado { get; set; } = true;
    }
}