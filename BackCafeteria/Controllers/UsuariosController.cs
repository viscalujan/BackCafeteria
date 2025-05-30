using BackCafeteria.DTOs;
using BackCafeteria.Helpers;
using BackCafeteria.Models;
using BackCafeteria.Services;
using CafeteriaAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeteriaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "inventario,ventas")]
    public class UsuariosController : ControllerBase
    {
        private readonly CafeteriaContext _context;

        public UsuariosController(CafeteriaContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult<Usuario>> PostUsuario([FromBody] UsuarioRegistroDTO nuevo)
        {
            if (await _context.Usuarios.AnyAsync(u => u.Correo == nuevo.Correo))
                return BadRequest("Ya existe un usuario con ese correo.");

            if (nuevo.Credito < 50)
                return BadRequest("El crédito inicial debe ser al menos 50.");

            var (contenidoQR, qrBytes, hashQR) = QRHelper.GenerarCodigoQR(nuevo.NumeroControl);

            var usuario = new Usuario
            {
                Nombre = nuevo.Nombre,
                Correo = nuevo.Correo,
                Contra = nuevo.NumeroControl,
                Rol = "alumno",
                Credito = nuevo.Credito,
                Huella = qrBytes,
                CodigoQRTexto = hashQR,
                Contrasena = "123456"
            };

            _context.Usuarios.Add(usuario);

            var correoAut = User.Identity?.Name ?? "RegistroInicial";
            var historial = new HistorialCredito
            {
                NumeroControlAfectado = nuevo.NumeroControl,
                Cantidad = nuevo.Credito,
                Fecha = DateTime.Now,
                AutCorreo = correoAut
            };
            _context.HistorialCreditos.Add(historial);

            await _context.SaveChangesAsync();

            try
            {
                await EmailService.EnviarCorreoConQR(usuario.Correo, usuario.Nombre, qrBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar correo: {ex.Message}");
            }

            return Ok(new { mensaje = "Usuario registrado correctamente con QR enviado." });
        }



        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsuarioDTO>>> ListarUsuarios()
        {
            var usuarios = await _context.Usuarios
                .Select(u => new UsuarioDTO
                {
                    Id = u.Id,
                    Nombre = u.Nombre,
                    Correo = u.Correo,
                    NumeroControl = u.Contra,
                    Credito = u.Credito
                })
                .ToListAsync();

            return Ok(usuarios);
        }

        [HttpGet("numeroControl/{numeroControl}")]
        public async Task<ActionResult<Usuario>> GetUsuarioPorNumeroControl(string numeroControl)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Contra == numeroControl);

            if (usuario == null)
                return NotFound("Usuario no encontrado.");

            return Ok(usuario);
        }

        [HttpPost("aumentar-credito")]
        public async Task<IActionResult> AumentarCredito([FromBody] AumentoCreditoDTO dto)
        {
            if (dto.Cantidad < 50)
                return BadRequest("La cantidad debe ser al menos 50.");

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Contra == dto.NumeroControl);
            if (usuario == null)
                return NotFound("Usuario no encontrado.");

            usuario.Credito += dto.Cantidad;

            var correoAut = User.Identity?.Name ?? "Desconocido";

            var historial = new HistorialCredito
            {
                NumeroControlAfectado = dto.NumeroControl,
                Cantidad = dto.Cantidad,
                Fecha = DateTime.Now,
                AutCorreo = correoAut
            };
            _context.HistorialCreditos.Add(historial);

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Crédito aumentado y registrado correctamente." });
        }

        [HttpPost("validar-qr")]
        public async Task<ActionResult> ValidarQR([FromBody] QRValidacionDTO dto)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.CodigoQRTexto == dto.Hash);
            if (usuario == null)
                return NotFound("QR inválido.");

            return Ok(new
            {
                usuario.Id,
                usuario.Nombre,
                usuario.Correo,
                usuario.Credito
            });
        }

        /*
        [HttpGet("historial-credito")]
        public async Task<IActionResult> ObtenerHistorialCreditoGeneral()
        {
            var historial = await _context.HistorialCreditos
                .OrderByDescending(h => h.Fecha)
                .ToListAsync();

            return Ok(historial);
        }

        */



        [HttpGet("historial-credito")]
        public async Task<IActionResult> ObtenerHistorialCreditoGeneral()
        {
            var historial = await _context.HistorialCreditos
                .OrderByDescending(h => h.Fecha)
                .ToListAsync();

            return Ok(historial);
        }

        [Authorize(Roles = "alumno")]
        [HttpGet("historial-credito/{numeroControl}")]
        public async Task<IActionResult> ObtenerHistorialCreditoUsuario(string numeroControl)
        {
            var historial = await _context.HistorialCreditos
                .Where(h => h.NumeroControlAfectado == numeroControl)
                .OrderByDescending(h => h.Fecha)
                .ToListAsync();

            if (!historial.Any())
            {
                return NotFound($"No se encontró historial para el número de control: {numeroControl}");
            }

            return Ok(historial);
        }


        [HttpPost("pagar-liquidacion")]
        public async Task<IActionResult> PagarLiquidacion([FromBody] PagoLiquidacionDTO dto)
        {
            // Validar datos básicos
            if (string.IsNullOrWhiteSpace(dto.Correo) || string.IsNullOrWhiteSpace(dto.Contra))
                return BadRequest("Correo y contraseña son obligatorios.");

            if (dto.Monto <= 0)
                return BadRequest("El monto debe ser mayor a cero.");

            // Autenticar usuario en tabla Aut (con BCrypt como en el Login)
            var usuarioAuth = await _context.Aut.FirstOrDefaultAsync(a => a.Correo == dto.Correo);

            if (usuarioAuth == null || !BCrypt.Net.BCrypt.Verify(dto.Contra, usuarioAuth.Contra))
                return Unauthorized("Credenciales incorrectas.");

            // Buscar usuario 'liquidacion'
            var usuarioLiquidacion = await _context.Usuarios.FirstOrDefaultAsync(u => u.Contra == "liquidacion");
            if (usuarioLiquidacion == null)
                return NotFound("Usuario de liquidación no encontrado.");

            // Verificar crédito suficiente
            if (usuarioLiquidacion.Credito < dto.Monto)
                return BadRequest("Crédito insuficiente en cuenta de liquidación.");

            // Descontar el monto
            usuarioLiquidacion.Credito -= dto.Monto;

            // Registrar movimiento en historial
            _context.HistorialCreditos.Add(new HistorialCredito
            {
                NumeroControlAfectado = "liquidacion",
                Cantidad = -dto.Monto,
                Fecha = DateTime.Now,
                AutCorreo = dto.Correo
            });

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Pago de liquidación registrado correctamente." });
        }

        [Authorize(Roles = "alumno")]
        [HttpGet("credito/{numeroControl}")]
        public async Task<IActionResult> ObtenerCredito(string numeroControl)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Contra == numeroControl);
            if (usuario == null)
                return NotFound("Usuario no encontrado.");

            return Ok(new { credito = usuario.Credito });
        }


    }
}
