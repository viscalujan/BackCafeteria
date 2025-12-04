using BackCafeteria.DTOs;
using BackCafeteria.Helpers;
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
        [AllowAnonymous]
        public async Task<IActionResult> EnviarQR([FromBody] CorreoQRRequest datos)
        {
            try
            {
                if (datos == null)
                    return BadRequest("No se recibieron datos válidos.");

                if (string.IsNullOrEmpty(datos.CorreoRemitente) || string.IsNullOrEmpty(datos.ClaveApp))
                    return BadRequest("Faltan datos de autenticación del remitente.");

                if (string.IsNullOrEmpty(datos.CorreoDestino))
                    return BadRequest("El correo destino es obligatorio.");

                if (string.IsNullOrEmpty(datos.NumeroControl))
                    return BadRequest("Debe especificar el número de control del usuario.");

                byte[] qrBytes;

                // 🔹 Generar QR a partir de la huella o texto
                string textoQR = datos.Huella ?? datos.NumeroControl;

                using (var qrGen = new QRCoder.QRCodeGenerator())
                {
                    var qrData = qrGen.CreateQrCode(textoQR, QRCoder.QRCodeGenerator.ECCLevel.Q);
                    using (var qr = new QRCoder.QRCode(qrData))
                    using (var bmp = qr.GetGraphic(20))
                    using (var ms = new MemoryStream())
                    {
                        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        qrBytes = ms.ToArray();
                    }
                }

                // 🔹 Guardar el QR en la base de datos del usuario
                var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.NumeroControl == datos.NumeroControl);
                if (usuario != null)
                {
                    usuario.Huella = Convert.ToBase64String(qrBytes);
                    usuario.CodigoQRTexto = textoQR;
                    await _context.SaveChangesAsync();
                }
                else
                {
                    return NotFound($"No se encontró un usuario con número de control {datos.NumeroControl}");
                }

                // 🔹 Configurar cliente SMTP
                using (var client = new System.Net.Mail.SmtpClient(datos.SmtpServidor, datos.SmtpPuerto))
                {
                    client.EnableSsl = datos.UsarSSL;
                    client.Credentials = new System.Net.NetworkCredential(datos.CorreoRemitente, datos.ClaveApp);

                    var mail = new System.Net.Mail.MailMessage
                    {
                        From = new System.Net.Mail.MailAddress(datos.CorreoRemitente),
                        Subject = "Código QR - Cafetería TEC",
                        Body = $"Hola 👋,\n\nAdjuntamos tu código QR de acceso.\n\nNúmero de control: {datos.NumeroControl}\n\nSaludos,\nCafetería TEC",
                        IsBodyHtml = false
                    };

                    mail.To.Add(datos.CorreoDestino);
                    mail.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(qrBytes), "codigoQR.png", "image/png"));

                    await client.SendMailAsync(mail);
                }

                return Ok(new
                {
                    message = "QR enviado y guardado correctamente.",
                    numeroControl = datos.NumeroControl
                });
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


        [HttpPost("transferir-credito")]
        public async Task<IActionResult> TransferirCredito([FromBody] TransferenciaCreditoDTO dto)
        {
            if (dto.Cantidad <= 0)
                return BadRequest("La cantidad debe ser mayor a cero.");

            // 🔹 Buscar emisor y receptor
            var emisor = await _context.Usuarios.FirstOrDefaultAsync(u => u.NumeroControl == dto.NumeroControlEmisor);
            var receptor = await _context.Usuarios.FirstOrDefaultAsync(u => u.NumeroControl == dto.NumeroControlReceptor);

            if (emisor == null)
                return NotFound("No se encontró el usuario emisor.");

            if (receptor == null)
                return NotFound("No se encontró el usuario receptor.");

            // 🔹 Validar contraseña (hash BCrypt)
            var passwordValida = BCrypt.Net.BCrypt.Verify(dto.ContrasenaEmisor, emisor.ContraUsuario);
            if (!passwordValida)
                return Unauthorized("Contraseña incorrecta.");

            // 🔹 Verificar crédito disponible
            if (emisor.Credito < dto.Cantidad)
                return BadRequest("Crédito insuficiente para realizar la transferencia.");

            // 🔹 Actualizar créditos
            emisor.Credito -= dto.Cantidad;
            receptor.Credito += dto.Cantidad;

            // 🔹 Registrar en historial (positivo y negativo)
            var historialEmisor = new HistorialCredito
            {
                NumeroControlAfectado = emisor.NumeroControl!,
                Monto = -dto.Cantidad,
                FechaMovimiento = DateTime.Now,
                AutCorreo = $"Transferencia a {receptor.NumeroControl}"
            };

            var historialReceptor = new HistorialCredito
            {
                NumeroControlAfectado = receptor.NumeroControl!,
                Monto = dto.Cantidad,
                FechaMovimiento = DateTime.Now,
                AutCorreo = $"Transferencia recibida de {emisor.NumeroControl}"
            };

            _context.HistorialCreditos!.Add(historialEmisor);
            _context.HistorialCreditos!.Add(historialReceptor);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = $"Transferencia completada. {dto.Cantidad:C} transferidos de {emisor.NumeroControl} a {receptor.NumeroControl}",
                creditoEmisor = emisor.Credito,
                creditoReceptor = receptor.Credito
            });
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

        [HttpGet("qr/{numeroControl}")]
        public async Task<IActionResult> ObtenerCodigoQR(string numeroControl)
        {
            var usuario = await _context.Usuarios
                .Where(u => u.NumeroControl == numeroControl)
                .Select(u => new { u.Huella })
                .FirstOrDefaultAsync();

            if (usuario == null || string.IsNullOrEmpty(usuario.Huella))
                return NotFound("QR no encontrado para este usuario.");

            byte[] pngBytes;
            try { pngBytes = Convert.FromBase64String(usuario.Huella); }
            catch { return BadRequest("La huella no es una imagen válida."); }

            return File(pngBytes, "image/png", $"QR_{numeroControl}.png");
        }

        [HttpGet("qr/base64/{numeroControl}")]
        public async Task<IActionResult> ObtenerCodigoQRBase64(string numeroControl)
        {
            var usuario = await _context.Usuarios
                .Where(u => u.NumeroControl == numeroControl)
                .Select(u => new { u.Huella })
                .FirstOrDefaultAsync();

            if (usuario == null || string.IsNullOrEmpty(usuario.Huella))
                return NotFound("QR no encontrado para este usuario.");

            return Ok(new { base64 = usuario.Huella });
        }

        [HttpPost("regenerar-qr/{numeroControl}")]
        public async Task<IActionResult> RegenerarQR(string numeroControl)
        {
            // 1) Buscar usuario
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.NumeroControl == numeroControl);

            if (usuario == null)
                return NotFound("Usuario no encontrado.");

            // 2) Generar un nuevo QR basado en NC + timestamp
            var (contenidoQR, qrBytes, hashQR) = QRHelper.GenerarCodigoQR(numeroControl);

            // 3) Guardar Base64 y hash en la BD
            usuario.Huella = Convert.ToBase64String(qrBytes);
            usuario.CodigoQRTexto = hashQR;

            await _context.SaveChangesAsync();

            // (Opcional) Enviar correo con el nuevo QR
            try
            {
                var emailService = HttpContext.RequestServices.GetRequiredService<EmailService>();
                emailService.EnviarCorreoConQR(usuario.CorreoUsuario, qrBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al enviar correo: " + ex.Message);
            }

            // 4) Responder con datos del QR (por si el front quiere actualizar sin recargar toda la vista)
            return Ok(new
            {
                mensaje = "QR regenerado correctamente.",
                qrBase64 = Convert.ToBase64String(qrBytes),
                hash = hashQR
            });
        }
    }
}