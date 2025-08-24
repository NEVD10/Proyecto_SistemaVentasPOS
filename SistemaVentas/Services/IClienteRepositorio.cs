using SistemaVentas.Models;

namespace SistemaVentas.Services
{
    public interface IClienteRepositorio
    {
        Task<Paginador<Cliente>> ObtenerTodos(int numeroPagina = 1, int tamanoPagina = 10, string filtro = null);
        Task<int> Crear(Cliente cliente);
        // Otros métodos según necesites
    }
}