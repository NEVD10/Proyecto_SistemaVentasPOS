using SistemaVentas.Models;
using System.Net.Mail;
using System.Net;

namespace SistemaVentas.Services
{
    public class ServicioCorreo : IServicioCorreo
    {
        private readonly IConfiguration _configuration;

        public ServicioCorreo(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task EnviarComprobantePorCorreo(string destinatario, Venta venta, byte[] pdfBytes)
        {
            if (string.IsNullOrEmpty(destinatario))
                throw new ArgumentNullException(nameof(destinatario), "El destinatario no puede estar vacío.");

            var configCorreo = _configuration.GetSection("ConfiguracionCorreo").Get<ConfiguracionCorreo>();
            if (configCorreo == null)
                throw new InvalidOperationException("La configuración de correo no está definida en appsettings.json.");

            using var client = new SmtpClient(configCorreo.Servidor, configCorreo.Puerto)
            {
                Credentials = new NetworkCredential(configCorreo.Usuario, configCorreo.Contrasena),
                EnableSsl = true, // Forzar SSL/TLS para el puerto 587
                Timeout = 10000 // Tiempo de espera de 10 segundos
            };

            var mensaje = new MailMessage
            {
                From = new MailAddress(configCorreo.Remitente),
                Subject = $"Comprobante de Venta - {venta.TipoComprobante} {venta.NumeroComprobante}",
                Body = $"Estimado cliente,\n\nAdjunto encontrará el comprobante de su venta realizada el {venta.FechaVenta:dd/MM/yyyy}.\n\nTotal: {venta.MontoTotal:N2}\n\nAtentamente,\nSistema de Ventas",
                IsBodyHtml = false
            };
            mensaje.To.Add(destinatario);

            var attachment = new Attachment(new MemoryStream(pdfBytes), $"Comprobante_{venta.TipoComprobante}_{venta.NumeroComprobante}.pdf");
            mensaje.Attachments.Add(attachment);

            try
            {
                await client.SendMailAsync(mensaje);
            }
            catch (SmtpException ex)
            {
                throw new InvalidOperationException($"Error al enviar el correo a {destinatario}: {ex.Message}. Verifica la configuración SMTP (Puerto: {configCorreo.Puerto}, SSL: {client.EnableSsl}), las credenciales y asegúrate de usar una contraseña de aplicación si es Gmail (Código de error: {ex.StatusCode}).", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ocurrió un error inesperado al enviar el correo a {destinatario}: {ex.Message}", ex);
            }
        }
    }

    public class ConfiguracionCorreo
    {
        public string Servidor { get; set; }
        public int Puerto { get; set; }
        public string Usuario { get; set; }
        public string Contrasena { get; set; }
        public string Remitente { get; set; }
    }
}