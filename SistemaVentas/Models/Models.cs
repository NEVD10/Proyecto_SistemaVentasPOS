using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SistemaVentas.Models
{
    public class ReporteDiario
    {
        public decimal MontoTotalDia { get; set; }
        public int NumeroTransacciones { get; set; }
        public decimal TicketPromedio { get; set; }
        public decimal GananciasDia { get; set; }
        public Dictionary<string, decimal> VentasPorHora { get; set; }
        public Dictionary<string, decimal> VentasPorCategoria { get; set; }
        public List<ProductoVendido> TopProductos { get; set; }
    }

    public class VentaReporte
    {
        public int IdVenta { get; set; }
        public DateTime FechaVenta { get; set; }
        public string TipoComprobante { get; set; }
        public string MetodoPago { get; set; }
        public int? IdCliente { get; set; }
        public string ClienteNombre { get; set; }
        public decimal MontoTotal { get; set; }
        public List<DetalleVenta> DetalleVentas { get; set; }
    }

    public class ProductoVendido
    {
        public string NombreProducto { get; set; }
        public int CantidadVendida { get; set; }
        public decimal MontoTotal { get; set; }
        public string Categoria { get; set; }
    }


    // Nota: No redefiniré DetalleVenta aquí, ya que existe en tu proyecto.
}