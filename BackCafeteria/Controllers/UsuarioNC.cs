using Microsoft.AspNetCore.Mvc;
using BackCafeteria.Models;
using BackCafeteria.Services;
using QRCoder;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

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
        public IActionResult EnviarQR([FromBody] Usuario usuario)
        {
            try
            {
                if (usuario == null)
                    return BadRequest("Datos del usuario no válidos.");

                if (string.IsNullOrEmpty(usuario.CorreoUsuario))
                    return BadRequest("El correo del usuario es obligatorio.");

                byte[] qrBytes;

                // 🔹 Si la huella ya viene en Base64 (cadena), la convertimos directamente
                if (!string.IsNullOrEmpty(usuario.HuellaBase64))
                {
                    qrBytes = Convert.FromBase64String(usuario.HuellaBase64);
                }
                else if (!string.IsNullOrEmpty(usuario.Huella))
                {
                    // 🔹 Generar código QR con el valor de la huella
                    using (var qrGenerator = new QRCodeGenerator())
                    {
                        var qrData = qrGenerator.CreateQrCode(usuario.Huella, QRCodeGenerator.ECCLevel.Q);
                        using (var qrCode = new QRCode(qrData))
                        using (var bitmap = qrCode.GetGraphic(20))
                        using (var ms = new MemoryStream())
                        {
                            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            qrBytes = ms.ToArray();
                        }
                    }
                }
                else
                {
                    return BadRequest("No se proporcionó una huella válida.");
                }

                // 🔹 Enviar correo con el QR adjunto
                EmailService.EnviarCorreoConQR(usuario.CorreoUsuario, qrBytes);

                return Ok(new { message = "Correo con QR enviado correctamente." });
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
