using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaVentas.Models
{
    [Table("Venta")]
    public class Venta
    {
        [Key]
        public int IdVenta { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime FechaVenta { get; set; } = DateTime.Now; // Buen momento para poner un valor por defecto

        [Column(TypeName = "decimal(10, 2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal MontoIGV { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal MontoTotal { get; set; }

        // Propiedades de las llaves foráneas
        public int? IdCliente { get; set; } // Puede ser nulo
        public int IdUsuario { get; set; }

        // Propiedades de navegación
        [ForeignKey("IdCliente")]
        public virtual Cliente? Cliente { get; set; } // Permite que el cliente sea nulo

        [ForeignKey("IdUsuario")]
        public virtual Usuario Usuario { get; set; }

        // Colección para el detalle de la venta
        public virtual ICollection<DetalleVenta> DetalleVentas { get; set; }
    }
}
