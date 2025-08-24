using iText.Kernel.Pdf;
using iText.Layout;
using SistemaVentas.Models;
using Microsoft.Extensions.Options;
using System.IO;
using System.Threading.Tasks;

namespace SistemaVentas.Services
{
    public class ServicioFacturacion : IServicioFacturacion
    {
        private readonly FacturaPdfGenerator _facturaGenerator;
        private readonly BoletaPdfGenerator _boletaGenerator;
        private readonly IVentaRepositorio _ventaRepositorio;

        public ServicioFacturacion(FacturaPdfGenerator facturaGenerator, BoletaPdfGenerator boletaGenerator,
            IVentaRepositorio ventaRepositorio)
        {
            _facturaGenerator = facturaGenerator;
            _boletaGenerator = boletaGenerator;
            _ventaRepositorio = ventaRepositorio;
        }

        public byte[] GenerarComprobante(Venta venta, List<Producto> productos)
        {
            byte[] pdfBytes;
            string numeroComprobante = GenerarNumeroComprobante(venta.TipoComprobante).Result; // Espera el resultado

            if (venta.TipoComprobante == "Factura")
            {
                if (!ValidarClienteParaFactura(venta.Cliente))
                {
                    throw new InvalidOperationException("El cliente no tiene un RUC válido para generar una factura.");
                }
                pdfBytes = _facturaGenerator.GeneratePdf(venta, productos);
            }
            else if (venta.TipoComprobante == "Boleta")
            {
                if (!ValidarClienteParaBoleta(venta.Cliente))
                {
                    throw new InvalidOperationException("El cliente no tiene un DNI válido para generar una boleta.");
                }
                pdfBytes = _boletaGenerator.GeneratePdf(venta, productos);
            }
            else
            {
                throw new ArgumentException("Tipo de comprobante no soportado.");
            }

            // Actualizar el número de comprobante en la venta
            venta.NumeroComprobante = numeroComprobante;
            _ventaRepositorio.Actualizar(venta);

            return pdfBytes;
        }

        private bool ValidarClienteParaFactura(Cliente cliente)
        {
            return cliente != null && cliente.TipoDocumento == "RUC" && !string.IsNullOrEmpty(cliente.NumeroDocumento) && cliente.NumeroDocumento.Length == 11;
        }

        private bool ValidarClienteParaBoleta(Cliente cliente)
        {
            return cliente != null && cliente.TipoDocumento == "DNI" && !string.IsNullOrEmpty(cliente.NumeroDocumento) && cliente.NumeroDocumento.Length == 8;
        }

        private async Task<string> GenerarNumeroComprobante(string tipoComprobante)
        {
            int? ultimoNumero = await _ventaRepositorio.ObtenerUltimoNumeroComprobante(tipoComprobante);
            int nuevoNumero = (ultimoNumero ?? 0) + 1;
            string prefijo = tipoComprobante == "Factura" ? "F001-" : "B001-";
            return $"{prefijo}{nuevoNumero:D6}";
        }
    }

    public interface IServicioFacturacion
    {
        byte[] GenerarComprobante(Venta venta, List<Producto> productos);
    }
}