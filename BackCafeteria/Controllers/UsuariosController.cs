using BackCafeteria.DTOs;
using BackCafeteria.Helpers;
using BackCafeteria.Models;
using BackCafeteria.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;


namespace BackCafeteria.Controllers
{
   [ApiController]
    [Route("api/[controller]")]
    //[Authorize(Roles = "inventario,ventas")]
    public class UsuariosController : ControllerBase
    {
        private readonly CafeteriaDbv2Context _context;
        private readonly EmailService _email;


        public UsuariosController(CafeteriaDbv2Context context, EmailService email)
        {
            _context = context;
            _email = email;
        }

        // ================= POST: Crear usuario =================
        [HttpPost("crear-usuario")]
        public async Task<IActionResult> PostUsuario([FromBody] UsuarioCreateDTO nuevo)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (await _context.Usuarios.AnyAsync(u => u.NumeroControl == nuevo.NumeroControl))
                return BadRequest("Ya existe un usuario con ese número de control.");

            if (await _context.Usuarios.AnyAsync(u => u.CorreoUsuario == nuevo.Correo))
                return BadRequest("Ya existe un usuario con ese correo.");

            if (nuevo.Credito < 50)
                return BadRequest("El crédito inicial debe ser al menos 50.");

            // 1) Generar QR (hash + PNG)
            var (_, qrPngBytes, hashQR) = QRHelper.GenerarCodigoQR(nuevo.NumeroControl);

            var usuario = new Usuario
            {
                NombreUsuario = nuevo.Nombre,
                CorreoUsuario = nuevo.Correo,
                NumeroControl = nuevo.NumeroControl,
                RolUsuario = "alumno",
                ContraUsuario = BCrypt.Net.BCrypt.HashPassword(nuevo.Contra),
                Credito = nuevo.Credito,
                CodigoQRTexto = hashQR,                          // hash (64 chars)
                Huella = Convert.ToBase64String(qrPngBytes)      // PNG en Base64
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            _context.HistorialCreditos!.Add(new HistorialCredito
            {
                NumeroControlAfectado = usuario.NumeroControl,
                Monto = usuario.Credito,
                FechaMovimiento = DateTime.Now,
                AutCorreo = "Sistema"
            });
            await _context.SaveChangesAsync();

            // 2) Enviar por correo el PNG adjunto
            _email.EnviarCorreoConQR(usuario.CorreoUsuario!, qrPngBytes);

            return Ok(new { mensaje = "Usuario registrado y QR enviado.", usuarioId = usuario.IdUsuario });
        }

        // ================= POST: Aumentar crédito =================
        [HttpPost("aumentar-credito")]
        public async Task<IActionResult> AumentarCredito(AumentoCreditoDTO dto)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.NumeroControl == dto.NumeroControl);

            if (usuario == null)
                return NotFound("Usuario no encontrado.");

            // 🔹 Actualizar crédito
            usuario.Credito += dto.Cantidad;

            // 🔹 Registrar historial
            var historial = new HistorialCredito
            {
                NumeroControlAfectado = usuario.NumeroControl,
                Monto = dto.Cantidad,
                FechaMovimiento = DateTime.Now,
                AutCorreo = "aut@correo.com"
            };

            _context.HistorialCreditos!.Add(historial);

            // 🔹 Guardar cambios en un solo SaveChanges
            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Crédito aumentado",
                creditoActual = usuario.Credito
            });
        }

        // ================= GET: Crédito actual de un usuario =================
        [HttpGet("credito/usuario/{numeroControl}")]
        public async Task<IActionResult> ObtenerCredito(string numeroControl)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.NumeroControl == numeroControl);

            if (usuario == null)
                return NotFound("Usuario no encontrado.");

            return Ok(new
            {
                idUsuario = usuario.IdUsuario,
                nombreUsuario = usuario.NombreUsuario,
                correoUsuario = usuario.CorreoUsuario,
                numeroControl = usuario.NumeroControl,
                credito = usuario.Credito
            });
        }

        // ================= GET: Listar usuarios =================
        [HttpGet("listar-usuarios")]
        public async Task<ActionResult<IEnumerable<UsuarioCreateDTO>>> ListarUsuarios()
        {
            var usuarios = await _context.Usuarios
                .Select(u => new UsuarioCreateDTO
                {
                    Id = u.IdUsuario,
                    Nombre = u.NombreUsuario!,
                    Correo = u.CorreoUsuario!,
                    NumeroControl = u.NumeroControl!,
                    Credito = u.Credito,
                    Rol = u.RolUsuario
                })
                .ToListAsync();

            return Ok(usuarios);
        }

        // ================= GET: Obtener usuario por número de control =================
        [HttpGet("numeroControl/{numeroControl}")]
        public async Task<ActionResult<UsuarioCreateDTO>> GetUsuarioPorNumeroControl(string numeroControl)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.NumeroControl == numeroControl);

            if (usuario == null)
                return NotFound("Usuario no encontrado.");

            return Ok(new UsuarioCreateDTO
            {
                Id = usuario.IdUsuario,
                Nombre = usuario.NombreUsuario!,
                Correo = usuario.CorreoUsuario!,
                NumeroControl = usuario.NumeroControl!,
                Credito = usuario.Credito,
                Rol = usuario.RolUsuario
            });
        }

        // ================= GET: Historial de crédito general =================
        [HttpGet("historial-credito")]
        public async Task<IActionResult> ObtenerHistorialCreditoGeneral()
        {
            var historial = await _context.HistorialCreditos!
                .OrderByDescending(h => h.FechaMovimiento)
                .Select(h => new
                {
                    h.NumeroControlAfectado,
                    h.Monto,
                    h.FechaMovimiento,
                    h.AutCorreo
                })
                .ToListAsync();

            return Ok(historial);
        }

        // ================= GET: Historial de crédito por usuario =================
        [HttpGet("historial-credito/usuario/{numeroControl}")]
        public async Task<IActionResult> ObtenerHistorialCreditoUsuario(string numeroControl)
        {
            var historial = await _context.HistorialCreditos!
                .Where(h => h.NumeroControlAfectado == numeroControl)
                .OrderByDescending(h => h.FechaMovimiento)
                .Select(h => new
                {
                    h.NumeroControlAfectado,
                    h.Monto,
                    h.FechaMovimiento,
                    h.AutCorreo
                })
                .ToListAsync();

            return Ok(historial);
        }

        public class QRValidacionDTO { public string Hash { get; set; } = null!; }

        [HttpPost("validar-qr")]
        public async Task<IActionResult> ValidarQR([FromBody] QRValidacionDTO dto)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.CodigoQRTexto == dto.Hash);

            if (usuario == null) return NotFound("QR inválido.");

            return Ok(new
            {
                usuario.IdUsuario,
                usuario.NombreUsuario,
                usuario.CorreoUsuario,
                usuario.Credito
            });
        }


    }
}
