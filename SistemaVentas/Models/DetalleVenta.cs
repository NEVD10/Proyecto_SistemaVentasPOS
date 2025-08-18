using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaVentas.Models
{
    [Table("DetalleVenta")]
    public class DetalleVenta
    {
        // Claves primarias compuestas (se configuran en el DbContext)
        public int IdVenta { get; set; }
        public int IdProducto { get; set; }

        [Required]
        public int Cantidad { get; set; }

        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal PrecioUnitario { get; set; }

        // Propiedades de navegación
        public virtual Venta Venta { get; set; }
        public virtual Producto Producto { get; set; }
    }
}
