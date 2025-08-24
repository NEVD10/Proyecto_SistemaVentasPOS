using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using SistemaVentas.Models;
using System.IO;

namespace SistemaVentas.Services
{
    public class FacturaPdfGenerator
    {
        public byte[] GeneratePdf(Venta venta, List<Producto> productos)
        {
            using var memoryStream = new MemoryStream();
            var writer = new PdfWriter(memoryStream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf);

            // Encabezado
            document.Add(new Paragraph("FACTURA ELECTRÓNICA")
                .SetFontSize(20)
                .SetBold()
                .SetTextAlignment(TextAlignment.CENTER));
            document.Add(new Paragraph("Sistema de Ventas S.A.C.")
                .SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER));
            document.Add(new Paragraph("RUC: 12345678901")
                .SetFontSize(12)
                .SetTextAlignment(TextAlignment.CENTER));
            document.Add(new Paragraph($"Nº Factura: F001-{venta.IdVenta:D6}")
                .SetFontSize(12)
                .SetTextAlignment(TextAlignment.CENTER));
            document.Add(new Paragraph($"Fecha de Emisión: {venta.FechaVenta:dd/MM/yyyy HH:mm:ss}")
                .SetFontSize(12)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph("\n"));

            // Datos del Cliente
            var clienteTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 }));
            clienteTable.AddCell(new Cell().Add(new Paragraph("Datos del Cliente").SetBold()));
            clienteTable.AddCell(new Cell().Add(new Paragraph("Datos de la Empresa").SetBold()));
            clienteTable.AddCell(new Cell().Add(new Paragraph(
                venta.Cliente != null
                    ? $"Nombre: {venta.Cliente.Nombres} {venta.Cliente.Apellidos}\nRUC: {venta.Cliente.NumeroDocumento}"
                    : "Cliente: Cliente General\nRUC: -")));
            clienteTable.AddCell(new Cell().Add(new Paragraph(
                "Sistema de Ventas S.A.C.\nRUC: 12345678901\nDirección: Av. Principal 123, Lima")));
            document.Add(clienteTable);

            document.Add(new Paragraph("\n"));

            // Tabla de Detalles
            var table = new Table(UnitValue.CreatePercentArray(new float[] { 40, 20, 20, 20 }));
            table.AddHeaderCell("Producto");
            table.AddHeaderCell("Cantidad");
            table.AddHeaderCell("Precio Unitario");
            table.AddHeaderCell("Subtotal");

            foreach (var detalle in venta.DetalleVentas)
            {
                var producto = productos.FirstOrDefault(p => p.IdProducto == detalle.IdProducto);
                table.AddCell(producto?.Nombre ?? "No encontrado");
                table.AddCell(detalle.Cantidad.ToString());
                table.AddCell($"S/ {detalle.PrecioUnitario:N2}");
                table.AddCell($"S/ {detalle.SubtotalLinea:N2}");
            }

            document.Add(table);

            // Totales
            document.Add(new Paragraph("\n"));
            var totalTable = new Table(UnitValue.CreatePercentArray(new float[] { 80, 20 }));
            totalTable.AddCell(new Cell().Add(new Paragraph("Subtotal:").SetTextAlignment(TextAlignment.RIGHT)));
            totalTable.AddCell(new Cell().Add(new Paragraph($"S/ {venta.Subtotal:N2}")));
            totalTable.AddCell(new Cell().Add(new Paragraph("IGV (18%):").SetTextAlignment(TextAlignment.RIGHT)));
            totalTable.AddCell(new Cell().Add(new Paragraph($"S/ {venta.MontoIGV:N2}")));
            totalTable.AddCell(new Cell().Add(new Paragraph("Total:").SetBold().SetTextAlignment(TextAlignment.RIGHT)));
            totalTable.AddCell(new Cell().Add(new Paragraph($"S/ {venta.MontoTotal:N2}").SetBold()));
            document.Add(totalTable);

            document.Close();
            return memoryStream.ToArray();
        }
    }
}