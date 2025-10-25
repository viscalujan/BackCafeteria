using BackCafeteria.Models;
using BackCafeteria.Services;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
using System;
using System.IO;

namespace BackCafeteria.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly CafeteriaDbv2Context _context;

        public UsuariosController(CafeteriaDbv2Context context)
        {
            _context = context;
        }

        [HttpPost("EnviarQR")]
        public IActionResult EnviarQR([FromBody] Usuario usuario)
        {
            if (usuario == null || string.IsNullOrEmpty(usuario.CorreoUsuario))
            {
                return BadRequest("Usuario o correo inválido");
            }

            try
            {
                // Texto que se desea convertir a QR
                string mensajeQR = "Bienvenido!";

                // Generar QR y convertirlo a byte[]
                byte[] qrBytes;
                using (var qrGenerator = new QRCodeGenerator())
                {
                    var qrData = qrGenerator.CreateQrCode(mensajeQR, QRCodeGenerator.ECCLevel.Q);
                    using (var qrCode = new QRCode(qrData))
                    using (var bitmap = qrCode.GetGraphic(20))
                    using (var ms = new MemoryStream())
                    {
                        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        qrBytes = ms.ToArray(); // Ya tenemos byte[]
                    }
                }

                // Enviar correo con QR (ahora sí como byte[])
                EmailService.EnviarCorreoConQR(usuario.CorreoUsuario, qrBytes);

                return Ok("Correo enviado correctamente con QR.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al enviar correo: {ex.Message}");
            }
        }
    }
}
