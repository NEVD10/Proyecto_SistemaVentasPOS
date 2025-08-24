using Microsoft.AspNetCore.Mvc;
using SistemaVentas.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaVentas.Controllers
{
    public class ReportesController : Controller
    {
        private readonly IServicioReporte _servicioReporte;

        public ReportesController(IServicioReporte servicioReporte)
        {
            _servicioReporte = servicioReporte ?? throw new ArgumentNullException(nameof(servicioReporte));
        }

        public async Task<IActionResult> Index(DateTime? fechaInicio, DateTime? fechaFin, string metodoPago, string tipoComprobante, int? idCliente)
        {
            var ventas = await _servicioReporte.ObtenerVentasFiltradas(fechaInicio, fechaFin, metodoPago, tipoComprobante, idCliente);

            // Guardar filtros en ViewData para mantenerlos en el formulario
            ViewData["fechaInicio"] = fechaInicio?.ToString("yyyy-MM-dd");
            ViewData["fechaFin"] = fechaFin?.ToString("yyyy-MM-dd");
            ViewData["metodoPago"] = metodoPago;
            ViewData["tipoComprobante"] = tipoComprobante;
            ViewData["idCliente"] = idCliente;

            // Preparar datos para gráficos
            var datosBarras = ventas
                .GroupBy(v => v.FechaVenta.Date.ToString("dd/MM/yyyy"))
                .ToDictionary(g => g.Key, g => g.Sum(v => v.MontoTotal));
            var datosLineas = ventas
                .GroupBy(v => v.FechaVenta.ToString("MM/yyyy"))
                .ToDictionary(g => g.Key, g => g.Sum(v => v.MontoTotal));

            ViewBag.DatosBarras = datosBarras;
            ViewBag.DatosLineas = datosLineas;

            return View(ventas);
        }

        public async Task<IActionResult> Diario(DateTime? fecha)
        {
            var reporte = await _servicioReporte.GenerarReporteDiario(fecha);

            // Guardar filtro en ViewData para mantenerlo en el formulario
            ViewData["fecha"] = fecha?.ToString("yyyy-MM-dd");

            return View(reporte);
        }
    }
}