using Microsoft.AspNetCore.Mvc;
using BackCafeteria.Models;
using BackCafeteria.Services;
using System;
using System.Text;
using Microsoft.IdentityModel.Tokens;

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
                else if (usuario.Huella != null && usuario.Huella.Length > 0)
                {
                    // Si ya está como byte[], lo usamos directamente
                    huellaParaCorreo = usuario.Huella;
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


