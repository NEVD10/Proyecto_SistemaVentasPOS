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

            ViewData["fechaInicio"] = fechaInicio?.ToString("dd-MM-yyyy");
            ViewData["fechaFin"] = fechaFin?.ToString("dd-MM-yyyy");
            ViewData["metodoPago"] = metodoPago;
            ViewData["tipoComprobante"] = tipoComprobante;
            ViewData["idCliente"] = idCliente;

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

            ViewData["fecha"] = fecha?.ToString("dd-MM-yyyy");

            return View(reporte);
        }

        public async Task<IActionResult> Mensual(DateTime? fechaInicio, DateTime? fechaFin)
        {
            var reporte = await _servicioReporte.GenerarReporteMensual(fechaInicio, fechaFin);

            ViewData["fechaInicio"] = fechaInicio?.ToString("dd-MM-yyyy") ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).ToString("dd-MM-yyyy");
            ViewData["fechaFin"] = fechaFin?.ToString("dd-MM-yyyy") ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1).AddDays(-1).ToString("dd-MM-yyyy");
            ViewBag.VentasPorMesAnual = await _servicioReporte.ObtenerVentasPorMesAnual();

            return View(reporte);
        }
    }
}