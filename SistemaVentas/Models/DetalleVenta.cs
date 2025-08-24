using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaVentas.Models
{
    public class DetalleVenta
    {
        [Key]
        public int IdDetalleVenta { get; set; }

        [Required(ErrorMessage = "El ID de la venta es requerido.")]
        [ForeignKey("Venta")]
        public int IdVenta { get; set; }
        public Venta Venta { get; set; }

        [Required(ErrorMessage = "El ID del producto es requerido.")]
        [ForeignKey("Producto")]
        public int IdProducto { get; set; }
        public Producto Producto { get; set; }

        [Required(ErrorMessage = "La cantidad es requerida.")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor que 0.")]
        public int Cantidad { get; set; }

        [Required(ErrorMessage = "El precio unitario es requerido.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio unitario debe ser mayor que 0.")]
        public decimal PrecioUnitario { get; set; }

        [Required(ErrorMessage = "El subtotal de línea es requerido.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El subtotal de línea debe ser mayor que 0.")]
        public decimal SubtotalLinea { get; set; }
    }
}