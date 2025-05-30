using System.Net;
using System.Net.Mail;
using System.Net.Mime;

namespace BackCafeteria.Services
{
    public class EmailService
    {
        public static async Task EnviarCorreoConQR(string correoDestino, string nombreUsuario, byte[] qrBytes)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(correoDestino))
                    throw new ArgumentException("El correo no puede estar vacío");

                if (qrBytes == null || qrBytes.Length == 0)
                    throw new ArgumentException("Los bytes del QR no pueden estar vacíos");

                Console.WriteLine($"Preparando correo para {correoDestino}...");

                var mensaje = new MailMessage
                {
                    From = new MailAddress("robertoisaacmc@gmail.com", "Cafetería"),
                    Subject = "Tu código QR de acceso",
                    IsBodyHtml = true,
                    Body = $@"
                <h3>Hola {nombreUsuario},</h3>
                <p>Este es tu código QR para realizar pagos en la cafetería:</p>
                <img src='cid:qrImage' style='width:200px;height:200px;'/>
                <p>No compartas este código con nadie.</p>"
                };

                // Usamos el parámetro 'correo' directamente
                mensaje.To.Add(correoDestino);

                using var qrStream = new MemoryStream(qrBytes);
                var qrResource = new LinkedResource(qrStream, "image/png") // Asegurando que es PNG
                {
                    ContentId = "qrImage",
                    TransferEncoding = TransferEncoding.Base64
                };

                var htmlView = AlternateView.CreateAlternateViewFromString(mensaje.Body, null, MediaTypeNames.Text.Html);
                htmlView.LinkedResources.Add(qrResource);
                mensaje.AlternateViews.Add(htmlView);

                using var smtp = new SmtpClient("smtp.gmail.com", 587)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential("robertoisaacmc@gmail.com", "rwbs tjhx hcvd pzje"),
                    Timeout = 10000
                };

                await smtp.SendMailAsync(mensaje);
                Console.WriteLine($"Correo enviado correctamente a {correoDestino}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR al enviar correo a {correoDestino}: {ex.Message}");
                throw; // Relanzamos la excepción para manejo superior
            }
        }
    }
}