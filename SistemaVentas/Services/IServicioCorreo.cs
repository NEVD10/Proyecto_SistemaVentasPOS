using SistemaVentas.Models;
using System.Threading.Tasks;

namespace SistemaVentas.Services
{
    public interface IServicioCorreo
    {
        Task EnviarComprobantePorCorreo(string destinatario, Venta venta, byte[] pdfBytes);
    }
}