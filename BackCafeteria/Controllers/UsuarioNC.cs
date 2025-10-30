using Microsoft.AspNetCore.Mvc;
using BackCafeteria.Models;
using BackCafeteria.Services;
using QRCoder;  // Asegúrate de tener QRCoder instalado (NuGet)
using System;
using System.IO;
using System.Text;

namespace BackCafeteria.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuarioNC : ControllerBase
    {
        private readonly CafeteriaDbv2Context _context;

        public UsuarioNC(CafeteriaDbv2Context context)
        {
            _context = context;
        }

        [HttpPost("EnviarQR")]
        public IActionResult EnviarQR([FromBody] Usuario usuario)
        {
            try
            {
                byte[] huellaParaCorreo;

                if (!string.IsNullOrEmpty(usuario.HuellaBase64))
                {
                    // Convertimos el Base64 a byte[] para enviarlo
                    huellaParaCorreo = Convert.FromBase64String(usuario.HuellaBase64);
                }
                else if (!string.IsNullOrEmpty(usuario.Huella))
                {
                    // Si Huella es una cadena Base64, conviértela
                    try
                    {
                        huellaParaCorreo = Convert.FromBase64String(usuario.Huella);
                    }
                    catch (FormatException)
                    {
                        // Si no es Base64 válido, trata como texto plano y genera un QR simple
                        string mensajeQR = usuario.Huella;
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

                // Ahora enviamos el byte[]
                EmailService.EnviarCorreoConQR(usuario.CorreoUsuario, huellaParaCorreo);

                return Ok(new { message = "Correo enviado correctamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
