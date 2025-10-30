using Microsoft.AspNetCore.Mvc;
using BackCafeteria.Models;
using BackCafeteria.Services;
using QRCoder;  // Asegúrate de tener QRCoder instalado (NuGet)
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

        // 1️⃣ Obtener historial de crédito del usuario
        [HttpGet("historial-credito/{numeroControl}")]
        public async Task<IActionResult> ObtenerHistorialCreditoUsuario(string numeroControl)
        {
            var historial = await _context.HistorialCreditos
                .Where(h => h.FkIdUsuarioNavigation.NumeroControl == numeroControl)
                .OrderByDescending(h => h.FechaMovimiento)
                .ToListAsync();

            if (!historial.Any())
                return NotFound($"No se encontró historial para el número de control: {numeroControl}");

            return Ok(historial);
        }

        // 2️⃣ Obtener crédito actual del usuario
        [HttpGet("credito/{numeroControl}")]
        public async Task<IActionResult> ObtenerCredito(string numeroControl)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.NumeroControl == numeroControl);

            if (usuario == null)
                return NotFound("Usuario no encontrado.");

            return Ok(new { credito = usuario.Credito });
        }

        // 3️⃣ Obtener QR del usuario
        [HttpGet("qr/{numeroControl}")]
        public async Task<IActionResult> ObtenerCodigoQR(string numeroControl)
        {
            var usuario = await _context.Usuarios
                .Where(u => u.NumeroControl == numeroControl)
                .Select(u => new { u.Huella, u.Codigqrtexto })
                .FirstOrDefaultAsync();

            if (usuario == null || usuario.Huella == null)
                return NotFound("QR no encontrado para este usuario.");

            return File(usuario.Huella, "image/png", $"QR_{numeroControl}.png");
        }

        // 4️⃣ Enviar QR por correo
        [HttpPost("EnviarQR")]
        public IActionResult EnviarQR([FromBody] EnviarQRDTO dto)
        {
            try
            {
                if (string.IsNullOrEmpty(dto.Correo))
                    return BadRequest("Correo no proporcionado.");

                byte[] huellaParaCorreo;

                if (!string.IsNullOrEmpty(dto.HuellaBase64))
                {
                    huellaParaCorreo = Convert.FromBase64String(dto.HuellaBase64);
                }
                else if (!string.IsNullOrEmpty(dto.Huella))
                {
                    try
                    {
                        huellaParaCorreo = Convert.FromBase64String(dto.Huella);
                    }
                    catch (FormatException)
                    {
                        string mensajeQR = dto.Huella;
                        using (var qrGenerator = new QRCodeGenerator())
                        {
                            var qrData = qrGenerator.CreateQrCode(mensajeQR, QRCodeGenerator.ECCLevel.Q);
                            using (var qrCode = new QRCode(qrData))
                            using (var bitmap = qrCode.GetGraphic(20))
                            using (var ms = new MemoryStream())
                            {
                                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                huellaParaCorreo = ms.ToArray();
                            }
                        }
                    }
                }
                else
                {
                    return BadRequest("No se proporcionó huella válida para enviar por correo.");
                }

                // 🔹 Ajuste: Solo enviamos 2 argumentos según tu servicio actual
                EmailService.EnviarCorreoConQR(dto.Correo, huellaParaCorreo);

                return Ok(new { message = "Correo enviado correctamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    // DTO recomendado para EnviarQR
    public class EnviarQRDTO
    {
        public string Correo { get; set; } = null!;
        public string NombreUsuario { get; set; } = null!;
        public string? HuellaBase64 { get; set; }
        public string? Huella { get; set; }
    }
}
