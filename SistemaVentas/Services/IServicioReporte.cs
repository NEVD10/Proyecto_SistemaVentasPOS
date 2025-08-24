using SistemaVentas.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SistemaVentas.Services
{
    public interface IServicioReporte
    {
        Task<ReporteDiario> GenerarReporteDiario();
        Task<ReporteDiario> GenerarReporteDiario(DateTime? fecha);
        Task<ReporteMensual> GenerarReporteMensual(DateTime? fechaInicio, DateTime? fechaFin);
        Task<List<VentaReporte>> ObtenerVentasFiltradas(DateTime? fechaInicio, DateTime? fechaFin, string metodoPago, string tipoComprobante, int? idCliente);
        Task<Dictionary<string, decimal>> ObtenerVentasPorMesAnual();
    }
}