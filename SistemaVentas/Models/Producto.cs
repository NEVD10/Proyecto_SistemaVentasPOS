using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaVentas.Models
{
    public class Producto
    {
        [Key]
        public int IdProducto { get; set; }

        [Required(ErrorMessage = "El nombre del documento es obligatorio")]
        [MinLength(3, ErrorMessage = "Longitud Mínima (3), producto no registrado.")]
        [MaxLength(100, ErrorMessage = "Longitud Máxima alcanzada.")]
        [Display(Name = "Nombre de producto:")]
        [StringLength(100)]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El código de barras es obligatorio es obligatorio")]
        [MinLength(3, ErrorMessage = "Longitud Mínima (3), producto no registrado.")]
        [MaxLength(13, ErrorMessage = "Longitud Máxima alcanzada.")]
        [Display(Name = "Código de barras del producto:")]
        [StringLength(20)]
        public string CodigoBarras { get; set; }

        [Required(ErrorMessage = "El precio de venta es obligatorio")]
        [Display(Name = "Precio de Venta del producto:")]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal PrecioVenta { get; set; }

        [Required(ErrorMessage = "El stock es obligatorio")]
        [Display(Name = "Stock del producto:")]
        public int Stock { get; set; }

       
        public bool Estado { get; set; }= true;
    }
}