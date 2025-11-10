using BackCafeteria.Models;
using BackCafeteria.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace BackCafeteria.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "alumno")]
    public class UsuarioNCController : ControllerBase
    {
        private readonly CafeteriaDbv2Context _context;

        public UsuarioNCController(CafeteriaDbv2Context context)
        {
            _context = context;
        }

        // ✅ POST: api/UsuarioNC/EnviarQR
        [HttpPost("EnviarQR")]
        [AllowAnonymous] // puedes quitarlo si quieres mantener la autenticación
        public IActionResult EnviarQR([FromBody] CorreoQRRequest datos)
        {
            try
            {
                if (datos == null)
                    return BadRequest("No se recibieron datos válidos.");

                if (string.IsNullOrEmpty(datos.CorreoRemitente) || string.IsNullOrEmpty(datos.ClaveApp))
                    return BadRequest("Faltan datos de autenticación del remitente.");

                if (string.IsNullOrEmpty(datos.CorreoDestino))
                    return BadRequest("El correo destino es obligatorio.");

                byte[] qrBytes;

                // 🔹 Generar QR
                if (!string.IsNullOrEmpty(datos.HuellaBase64))
                {
                    try
                    {
                        qrBytes = Convert.FromBase64String(datos.HuellaBase64);
                    }
                    catch
                    {
                        using (var qrGen = new QRCoder.QRCodeGenerator())
                        {
                            var qrData = qrGen.CreateQrCode(datos.HuellaBase64, QRCoder.QRCodeGenerator.ECCLevel.Q);
                            using (var qr = new QRCoder.QRCode(qrData))
                            using (var bmp = qr.GetGraphic(20))
                            using (var ms = new MemoryStream())
                            {
                                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                qrBytes = ms.ToArray();
                            }
                        }
                    }
                }
                else
                {
                    using (var qrGen = new QRCoder.QRCodeGenerator())
                    {
                        var qrData = qrGen.CreateQrCode(datos.Huella ?? "HUELLA_NO_PROPORCIONADA", QRCoder.QRCodeGenerator.ECCLevel.Q);
                        using (var qr = new QRCoder.QRCode(qrData))
                        using (var bmp = qr.GetGraphic(20))
                        using (var ms = new MemoryStream())
                        {
                            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            qrBytes = ms.ToArray();
                        }
                    }
                }

                // 🔹 Configurar cliente SMTP con datos del remitente
                using (var client = new System.Net.Mail.SmtpClient(datos.SmtpServidor, datos.SmtpPuerto))
                {
                    client.EnableSsl = datos.UsarSSL;
                    client.Credentials = new System.Net.NetworkCredential(datos.CorreoRemitente, datos.ClaveApp);

                    var mail = new System.Net.Mail.MailMessage
                    {
                        From = new System.Net.Mail.MailAddress(datos.CorreoRemitente),
                        Subject = "Código QR - Cafetería",
                        Body = "Aquí tienes tu código QR generado automáticamente.",
                        IsBodyHtml = true
                    };

                    mail.To.Add(datos.CorreoDestino);
                    mail.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(qrBytes), "codigoQR.png", "image/png"));

                    client.Send(mail);
                }

                return Ok(new { message = "Correo enviado correctamente al destinatario." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error al enviar QR: " + ex.Message });
            }
        }


        // ✅ GET: api/UsuarioNC/historial-credito/{numeroControl}
        [HttpGet("historial-credito/{numeroControl}")]
        public async Task<IActionResult> ObtenerHistorialCreditoUsuario(string numeroControl)
        {
            var historial = await _context.HistorialCreditos
                .Where(h => h.NumeroControlAfectado == numeroControl)
                .OrderByDescending(h => h.FechaMovimiento)
                .ToListAsync();

            if (!historial.Any())
            {
                return NotFound($"No se encontró historial para el número de control: {numeroControl}");
            }

            return Ok(historial);
        }

        // ✅ GET: api/UsuarioNC/credito/{numeroControl}
        [HttpGet("credito/{numeroControl}")]
        public async Task<IActionResult> ObtenerCredito(string numeroControl)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.NumeroControl == numeroControl);

            if (usuario == null)
                return NotFound("Usuario no encontrado.");

            return Ok(new { credito = usuario.Credito });
        }

        // ✅ GET: api/UsuarioNC/qr/{numeroControl}
        [HttpGet("qr/{numeroControl}")]
        public async Task<IActionResult> ObtenerCodigoQR(string numeroControl)
        {
            var usuario = await _context.Usuarios
                .Where(u => u.NumeroControl == numeroControl)
                .Select(u => new { u.Huella, u.CodigoQRTexto })
                .FirstOrDefaultAsync();

            if (usuario == null || usuario.Huella == null)
                return NotFound("QR no encontrado para este usuario.");

            // Convertir la huella (si es base64) en bytes para devolver como imagen
            byte[] qrBytes;
            try
            {
                qrBytes = Convert.FromBase64String(usuario.Huella);
            }
            catch
            {
                return BadRequest("La huella no es una imagen válida en Base64.");
            }

            return File(qrBytes, "image/png", $"QR_{numeroControl}.png");
        }
    }
}
