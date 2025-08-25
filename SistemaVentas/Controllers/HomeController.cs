using Microsoft.AspNetCore.Mvc;
using SistemaVentas.Services;
using System;

namespace SistemaVentas.Controllers
{
    public class HomeController : Controller
    {
        private readonly IServicioReporte _servicioReporte;

        public HomeController(IServicioReporte servicioReporte)
        {
            _servicioReporte = servicioReporte ?? throw new ArgumentNullException(nameof(servicioReporte));
        }

        public async Task<IActionResult> Index(DateTime? fecha)
        {
            var reporte = await _servicioReporte.GenerarReporteDiario(fecha);
            ViewData["fecha"] = fecha?.ToString("yyyy-MM-dd");
            return View(reporte);
        }
    }
}