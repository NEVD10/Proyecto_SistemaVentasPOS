using SistemaVentas.Models;
using SistemaVentas.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaVentas.Services
{
    public class ServicioReporte : IServicioReporte
    {
        private readonly IVentaRepositorio _ventaRepositorio;
        private readonly IProductoRepositorio _productoRepositorio;
        private readonly IClienteRepositorio _clienteRepositorio;

        public ServicioReporte(IVentaRepositorio ventaRepositorio, IProductoRepositorio productoRepositorio, IClienteRepositorio clienteRepositorio)
        {
            _ventaRepositorio = ventaRepositorio ?? throw new ArgumentNullException(nameof(ventaRepositorio));
            _productoRepositorio = productoRepositorio ?? throw new ArgumentNullException(nameof(productoRepositorio));
            _clienteRepositorio = clienteRepositorio ?? throw new ArgumentNullException(nameof(clienteRepositorio));
        }

        public async Task<ReporteDiario> GenerarReporteDiario()
        {
            return await GenerarReporteDiario(null);
        }

        public async Task<ReporteDiario> GenerarReporteDiario(DateTime? fecha)
        {
            var ventas = await _ventaRepositorio.ObtenerTodos(1, int.MaxValue);
            var ventasFiltradas = ventas.Elementos.AsQueryable();

            var fechaFiltro = fecha ?? DateTime.Today;
            ventasFiltradas = ventasFiltradas.Where(v => v.FechaVenta.Date == fechaFiltro.Date);

            var reportes = ventasFiltradas.Select(v => new VentaReporte
            {
                IdVenta = v.IdVenta,
                FechaVenta = v.FechaVenta,
                TipoComprobante = v.TipoComprobante,
                MetodoPago = v.MetodoPago,
                IdCliente = v.IdCliente,
                ClienteNombre = v.Cliente != null ? v.Cliente.Nombres + " " + v.Cliente.Apellidos : null,
                MontoTotal = v.MontoTotal,
                DetalleVentas = v.DetalleVentas != null ? v.DetalleVentas.Select(d => new DetalleVenta
                {
                    IdDetalleVenta = d.IdDetalleVenta,
                    IdVenta = d.IdVenta,
                    IdProducto = d.IdProducto,
                    Producto = d.Producto,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    SubtotalLinea = d.SubtotalLinea
                }).ToList() : new List<DetalleVenta>()
            }).ToList();

            var montoTotal = reportes.Sum(r => r.MontoTotal);
            var numeroTransacciones = reportes.Count;
            var ticketPromedio = numeroTransacciones > 0 ? montoTotal / numeroTransacciones : 0;
            var ganancias = reportes.Sum(r => r.DetalleVentas.Sum(d => d.Producto != null ? (d.Producto.PrecioVenta - d.Producto.PrecioCosto) * d.Cantidad : 0));

            var ventasPorHora = new Dictionary<string, decimal>();
            for (int hora = 0; hora < 24; hora++)
            {
                var horaStr = $"{hora:00}:00";
                var montoHora = reportes
                    .Where(r => r.FechaVenta.Hour == hora)
                    .Sum(r => r.MontoTotal);
                ventasPorHora[horaStr] = montoHora;
            }

            var ventasPorCategoria = reportes.SelectMany(r => r.DetalleVentas)
                .GroupBy(d => d.Producto != null && d.Producto.Categoria != null ? d.Producto.Categoria.Nombre : "Sin Categoría")
                .ToDictionary(g => g.Key, g => g.Sum(d => d.PrecioUnitario * d.Cantidad));

            var topProductos = reportes
                .SelectMany(r => r.DetalleVentas)
                .GroupBy(d => d.Producto != null ? d.Producto.Nombre : null)
                .Select(g => new ProductoVendido
                {
                    NombreProducto = g.Key,
                    CantidadVendida = g.Sum(d => d.Cantidad),
                    MontoTotal = g.Sum(d => d.PrecioUnitario * d.Cantidad),
                    Categoria = g.First().Producto != null && g.First().Producto.Categoria != null ? g.First().Producto.Categoria.Nombre : "Sin Categoría"
                })
                .OrderByDescending(p => p.CantidadVendida)
                .Take(5)
                .ToList();

            return new ReporteDiario
            {
                MontoTotalDia = montoTotal,
                NumeroTransacciones = numeroTransacciones,
                TicketPromedio = ticketPromedio,
                GananciasDia = ganancias,
                VentasPorHora = ventasPorHora,
                VentasPorCategoria = ventasPorCategoria,
                TopProductos = topProductos
            };
        }

        public async Task<ReporteMensual> GenerarReporteMensual(DateTime? fechaInicio, DateTime? fechaFin)
        {
            var fechaInicioFiltro = fechaInicio ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var fechaFinFiltro = fechaFin ?? fechaInicioFiltro.AddMonths(1).AddDays(-1);

            var ventas = await _ventaRepositorio.ObtenerTodos(1, int.MaxValue, fechaInicioFiltro, fechaFinFiltro);
            var ventasFiltradas = ventas.Elementos.AsQueryable();

            var reportes = ventasFiltradas.Select(v => new VentaReporte
            {
                IdVenta = v.IdVenta,
                FechaVenta = v.FechaVenta,
                TipoComprobante = v.TipoComprobante,
                MetodoPago = v.MetodoPago,
                IdCliente = v.IdCliente,
                ClienteNombre = v.Cliente != null ? v.Cliente.Nombres + " " + v.Cliente.Apellidos : null,
                MontoTotal = v.MontoTotal,
                DetalleVentas = v.DetalleVentas != null ? v.DetalleVentas.Select(d => new DetalleVenta
                {
                    IdDetalleVenta = d.IdDetalleVenta,
                    IdVenta = d.IdVenta,
                    IdProducto = d.IdProducto,
                    Producto = d.Producto,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    SubtotalLinea = d.SubtotalLinea
                }).ToList() : new List<DetalleVenta>()
            }).ToList();

            var montoTotal = reportes.Sum(r => r.MontoTotal);
            var numeroTransacciones = reportes.Count;
            var ticketPromedio = numeroTransacciones > 0 ? montoTotal / numeroTransacciones : 0;
            var ganancias = reportes.Sum(r => r.DetalleVentas.Sum(d => d.Producto != null ? (d.Producto.PrecioVenta - d.Producto.PrecioCosto) * d.Cantidad : 0));

            // Gráfico de líneas: Tendencia de ventas por día (últimos 31 días)
            var ventasPorMes = reportes
                .GroupBy(r => r.FechaVenta.ToString("yyyy-MM-dd"))
                .ToDictionary(g => g.Key, g => g.Sum(r => r.MontoTotal));

            // Gráfico circular: Ventas por categoría
            var ventasPorCategoria = reportes.SelectMany(r => r.DetalleVentas)
                .GroupBy(d => d.Producto != null && d.Producto.Categoria != null ? d.Producto.Categoria.Nombre : "Sin Categoría")
                .ToDictionary(g => g.Key, g => g.Sum(d => d.PrecioUnitario * d.Cantidad));

            // Gráfico de barras horizontales: Top 10 productos
            var topProductos = reportes
                .SelectMany(r => r.DetalleVentas)
                .GroupBy(d => d.Producto != null ? d.Producto.Nombre : null)
                .Select(g => new ProductoVendido
                {
                    NombreProducto = g.Key,
                    CantidadVendida = g.Sum(d => d.Cantidad),
                    MontoTotal = g.Sum(d => d.PrecioUnitario * d.Cantidad),
                    Categoria = g.First().Producto != null && g.First().Producto.Categoria != null ? g.First().Producto.Categoria.Nombre : "Sin Categoría"
                })
                .OrderByDescending(p => p.MontoTotal)
                .Take(10)
                .ToList();

            return new ReporteMensual
            {
                MontoTotalMes = montoTotal,
                NumeroTransacciones = numeroTransacciones,
                GananciasMes = ganancias,
                VentasPorMes = ventasPorMes,
                VentasPorCategoria = ventasPorCategoria,
                TopProductos = topProductos
            };
        }

        public async Task<List<VentaReporte>> ObtenerVentasFiltradas(DateTime? fechaInicio, DateTime? fechaFin, string metodoPago, string tipoComprobante, int? idCliente)
        {
            var ventas = await _ventaRepositorio.ObtenerTodos(1, int.MaxValue, fechaInicio, fechaFin, idCliente);
            var ventasFiltradas = ventas.Elementos.AsQueryable();

            if (fechaInicio.HasValue)
                ventasFiltradas = ventasFiltradas.Where(v => v.FechaVenta.Date >= fechaInicio.Value.Date);
            if (fechaFin.HasValue)
                ventasFiltradas = ventasFiltradas.Where(v => v.FechaVenta.Date <= fechaFin.Value.Date);
            if (!string.IsNullOrEmpty(metodoPago))
                ventasFiltradas = ventasFiltradas.Where(v => v.MetodoPago == metodoPago);
            if (!string.IsNullOrEmpty(tipoComprobante))
                ventasFiltradas = ventasFiltradas.Where(v => v.TipoComprobante == tipoComprobante);
            if (idCliente.HasValue)
                ventasFiltradas = ventasFiltradas.Where(v => v.IdCliente == idCliente);

            return ventasFiltradas.Select(v => new VentaReporte
            {
                IdVenta = v.IdVenta,
                FechaVenta = v.FechaVenta,
                TipoComprobante = v.TipoComprobante,
                MetodoPago = v.MetodoPago,
                IdCliente = v.IdCliente,
                ClienteNombre = v.Cliente != null ? v.Cliente.Nombres + " " + v.Cliente.Apellidos : null,
                MontoTotal = v.MontoTotal,
                DetalleVentas = v.DetalleVentas != null ? v.DetalleVentas.Select(d => new DetalleVenta
                {
                    IdDetalleVenta = d.IdDetalleVenta,
                    IdVenta = d.IdVenta,
                    IdProducto = d.IdProducto,
                    Producto = d.Producto,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    SubtotalLinea = d.SubtotalLinea
                }).ToList() : new List<DetalleVenta>()
            }).ToList();
        }

        public async Task<Dictionary<string, decimal>> ObtenerVentasPorMesAnual()
        {
            var fechaInicioAnual = DateTime.Today.AddMonths(-24);
            var fechaFinAnual = DateTime.Today;
            var ventasAnual = await _ventaRepositorio.ObtenerTodos(1, int.MaxValue, fechaInicioAnual, fechaFinAnual);
            return ventasAnual.Elementos
                .GroupBy(v => v.FechaVenta.ToString("MM/yyyy"))
                .ToDictionary(g => g.Key, g => g.Sum(v => v.MontoTotal));
        }
    }
}