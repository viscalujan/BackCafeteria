using BackCafeteria.Models;
using System.Text;
using System.Threading.Tasks;

namespace BackCafeteria.Services
{
    public class EmailService
    {
        private readonly CafeteriaDbv2Context _context;

        public EmailService(CafeteriaDbv2Context context)
        {
            _context = context;
        }

        public static void EnviarCorreoConQR(string correoDestino, byte[] qrBytes)
        {
            using (var ms = new MemoryStream(qrBytes))
            {
                var attachment = new System.Net.Mail.Attachment(ms, "QR.png", "image/png");
                var mail = new System.Net.Mail.MailMessage("tucorreo@dominio.com", correoDestino)
                {
                    Subject = "Tu código QR",
                    Body = "Adjunto tu código QR"
                };
                mail.Attachments.Add(attachment);

                var client = new System.Net.Mail.SmtpClient("smtp.tuServidor.com");
                client.Send(mail);
            }
        }

        // 🔹 Método agregado para corregir error del controlador
        public async Task EnviarCorreoVentaCredito(string correoDestino, decimal totalCredito, string nombreUsuario)
        {
            string asunto = "Compra con crédito registrada";
            string cuerpo = $"Hola {nombreUsuario},\n\nSe ha registrado tu compra con crédito por un total de ${totalCredito}.\nGracias por tu compra.";

            using (var mail = new System.Net.Mail.MailMessage("tucorreo@dominio.com", correoDestino, asunto, cuerpo))
            {
                var client = new System.Net.Mail.SmtpClient("smtp.tuServidor.com")
                {
                    Port = 587,
                    Credentials = new System.Net.NetworkCredential("tucorreo@dominio.com", "tuContraseña"),
                    EnableSsl = true
                };

                await client.SendMailAsync(mail);
            }
        }


        public async Task<string> GenerarResumenVentaAsync(int ventaId)
        {
            var venta = _context.Ventas.FirstOrDefault(v => v.IdVentas == ventaId);
            if (venta == null)
                return "Venta no encontrada";

            var detalles = _context.VentaDetalles
                .Where(d => d.FkIdVenta == ventaId)
                .ToList();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Resumen de Venta #{ventaId}");
            foreach (var detalle in detalles)
            {
                sb.AppendLine($"Producto: {detalle.FkIdProductoNavigation.Nombre} - Cantidad: {detalle.CantidadProducto}");
            }

            return sb.ToString();
        }
    }
}
