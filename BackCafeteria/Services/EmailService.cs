using BackCafeteria.Models;
using CafeteriaAPI.Models;
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
                    Subject = "Tu QR",
                    Body = "Adjunto tu código QR"
                };
                mail.Attachments.Add(attachment);

                var client = new System.Net.Mail.SmtpClient("smtp.tuServidor.com");
                client.Send(mail);
            }
        }


        public async Task<string> GenerarResumenVentaAsync(int ventaId)
        {
            var venta = _context.Ventas
                .Where(v => v.IdVentas == ventaId)
                .FirstOrDefault();

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
