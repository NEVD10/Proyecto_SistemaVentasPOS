using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaVentas.Models
{
    public class Venta
    {
        [Key]
        public int IdVenta { get; set; }

        [Required(ErrorMessage = "La fecha de venta es requerida.")]
        public DateTime FechaVenta { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "El tipo de comprobante es requerido.")]
        [StringLength(20, ErrorMessage = "El tipo de comprobante no puede exceder los 20 caracteres.")]
        [RegularExpression(@"^(Boleta|Factura)$", ErrorMessage = "El tipo de comprobante debe ser 'Boleta' o 'Factura'.")]
        public string TipoComprobante { get; set; }

        [StringLength(20, ErrorMessage = "El número de comprobante no puede exceder los 20 caracteres.")]
        public string NumeroComprobante { get; set; }

        [Required(ErrorMessage = "El subtotal es requerido.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El subtotal debe ser mayor que 0.")]
        public decimal Subtotal { get; set; }

        [Required(ErrorMessage = "El monto de IGV es requerido.")]
        [Range(0.00, double.MaxValue, ErrorMessage = "El monto de IGV no puede ser negativo.")]
        public decimal MontoIGV { get; set; }

        [Required(ErrorMessage = "El monto total es requerido.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto total debe ser mayor que 0.")]
        public decimal MontoTotal { get; set; }

        [Required(ErrorMessage = "El método de pago es requerido.")]
        [StringLength(20, ErrorMessage = "El método de pago no puede exceder los 20 caracteres.")]
        [RegularExpression(@"^(Efectivo|Tarjeta|Yape|Transferencia)$", ErrorMessage = "El método de pago debe ser 'Efectivo', 'Tarjeta', 'Yape' o 'Transferencia'.")]
        public string MetodoPago { get; set; } = "Efectivo";

        [ForeignKey("Cliente")]
        public int? IdCliente { get; set; }
        public Cliente Cliente { get; set; }

        [Required(ErrorMessage = "El ID del usuario es requerido.")]
        [ForeignKey("Usuario")]
        public int IdUsuario { get; set; }
        public Usuario Usuario { get; set; }

        // Propiedad de navegación para la relación uno-a-muchos con DetalleVenta
        public ICollection<DetalleVenta> DetalleVentas { get; set; }
    }
}