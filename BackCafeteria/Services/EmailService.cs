using BackCafeteria.Models;
using BackCafeteria.Services;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

namespace BackCafeteria.Services
{
    public class EmailService
    {
        private readonly CafeteriaDbv2Context _context;
        private readonly EmailSettings _settings;

        public EmailService(CafeteriaDbv2Context context, IOptions<EmailSettings> settings)
        {
            _context = context;
            _settings = settings.Value;
        }

        // ===========================
        // 🔧 CONFIGURACIÓN SMTP MULTISERVIDOR
        // ===========================
        private (string host, int port, bool enableSsl) ObtenerConfiguracionSMTP(string correo)
        {
            if (!string.IsNullOrEmpty(_settings.SmtpServidor))
                return (_settings.SmtpServidor, _settings.SmtpPuerto, _settings.UsarSSL);

            if (correo.Contains("gmail.com"))
                return ("smtp.gmail.com", 587, true);

            if (correo.Contains("outlook.com") || correo.Contains("hotmail.com") || correo.Contains("live.com"))
                return ("smtp.office365.com", 587, true);

            if (correo.Contains("yahoo.com"))
                return ("smtp.mail.yahoo.com", 587, true);

            // 🔹 Servidor genérico si no se detecta dominio
            return ("smtp.tuServidor.com", 587, true);
        }

        // ===========================
        // ✉️ MÉTODO PRINCIPAL PARA ENVIAR CORREO CON QR
        // ===========================
        public void EnviarCorreoConQR(string correoDestino, byte[] qrPngBytes)
        {
            var remitente = _settings.CorreoRemitente;
            var clave = _settings.ClaveApp;

            var (host, port, ssl) = ObtenerConfiguracionSMTP(remitente);

            using var ms = new MemoryStream(qrPngBytes);
            using var adj = new Attachment(ms, "QR.png", "image/png");
            using var mail = new MailMessage
            {
                From = new MailAddress(remitente, "Cafetería TEC"),
                Subject = "Tu código QR de acceso",
                Body = "Hola 👋,\n\nAdjuntamos tu código QR generado correctamente.\n\nSaludos,\nCafetería TEC",
                IsBodyHtml = false
            };
            mail.To.Add(correoDestino);
            mail.Attachments.Add(adj);

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = ssl,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(remitente, clave)
            };
            client.Send(mail);
        }

        // ===========================
        // 💳 ENVÍA CORREO AL REALIZAR COMPRA CON CRÉDITO
        // ===========================
        public async Task EnviarCorreoVentaCredito(string correoDestino, decimal totalCredito, string nombreUsuario)
        {
            string remitente = _settings.CorreoRemitente;
            string contrasena = _settings.ClaveApp;
            var (host, port, ssl) = ObtenerConfiguracionSMTP(remitente);

            string asunto = "Compra con crédito registrada";
            string cuerpo = $"Hola {nombreUsuario},\n\n" +
                            $"Se ha registrado tu compra con crédito por un total de ${totalCredito:F2}.\n\n" +
                            $"Gracias por tu compra.\n\n- Cafetería TEC";

            using (var mail = new MailMessage(remitente, correoDestino, asunto, cuerpo))
            using (var client = new SmtpClient(host, port))
            {
                client.EnableSsl = ssl;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(remitente, contrasena);
                await client.SendMailAsync(mail);
            }
        }

        // ===========================
        // 🧾 GENERA UN RESUMEN DE VENTA PARA INCLUIR EN EL CORREO
        // ===========================
        public async Task<string> GenerarResumenVentaAsync(int ventaId)
        {
            var venta = _context.Ventas.FirstOrDefault(v => v.IdVentas == ventaId);
            if (venta == null)
                return "Venta no encontrada";

            var detalles = _context.VentaDetalles
                .Where(d => d.FkIdVenta == ventaId)
                .ToList();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"📄 Resumen de Venta #{ventaId}");
            sb.AppendLine($"Fecha: {venta.FechaVenta:dd/MM/yyyy}");
            sb.AppendLine($"Total: ${venta.TotalVenta:F2}");
            sb.AppendLine("Detalles:");

            foreach (var detalle in detalles)
            {
                sb.AppendLine($"- {detalle.FkIdProductoNavigation.Nombre}: {detalle.CantidadProducto} × ${detalle.PrecioUnitario:F2}");
            }

            return sb.ToString();
        }
    }
}
    // ===========================
    // ⚙️ MODELO DE CONFIGURACIÓN
    // ===========================
