using SistemaVentas.Models;
using System;
using System.Threading.Tasks;

namespace SistemaVentas.Services
{
    public interface IVentaRepositorio
    {
        Task<Venta> ObtenerPorId(int id);
        Task<Paginador<Venta>> ObtenerTodos(int numeroPagina = 1, int tamanoPagina = 10, DateTime? fechaInicio = null, DateTime? fechaFin = null, int? clienteId = null);
        Task<int> ContarTotal(DateTime? fechaInicio = null, DateTime? fechaFin = null, int? clienteId = null);
        Task<decimal> SumarMontoTotal(DateTime? fechaInicio = null, DateTime? fechaFin = null, int? clienteId = null);
        Task<int?> ObtenerUltimoNumeroComprobante(string tipoComprobante);
        Task Actualizar(Venta venta);
        Task<int> CrearVentaConDetalles(Venta venta);
    }
}