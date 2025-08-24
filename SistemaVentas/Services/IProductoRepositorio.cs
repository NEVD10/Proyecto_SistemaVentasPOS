using SistemaVentas.Models;

namespace SistemaVentas.Services
{
    public interface IProductoRepositorio
    {
        Task<Paginador<Producto>> ObtenerTodos(int numeroPagina = 1, int tamanoPagina = 10, string filtro = null, bool? estado = null);
        Task<Producto> ObtenerPorId(int id);
        Task<Producto> ObtenerPorCodigoBarras(string codigoBarras);
        Task Actualizar(Producto producto);
        // Otros métodos según necesites
    }
}