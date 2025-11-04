using BackCafeteria.DTOs;
using BackCafeteria.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackCafeteria.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "inventario,ventas")]
    public class UsuariosController : ControllerBase
    {
        private readonly CafeteriaDbv2Context _context;

        public UsuariosController(CafeteriaDbv2Context context)
        {
            _context = context;
        }

        // ================= POST: Crear usuario =================
        [HttpPost("crear-usuario")]
        public async Task<IActionResult> PostUsuario([FromBody] UsuarioCreateDTO nuevo)
        {
            if (await _context.Usuarios.AnyAsync(u => u.NumeroControl == nuevo.NumeroControl))
                return BadRequest("Ya existe un usuario con ese número de control.");

            if (await _context.Usuarios.AnyAsync(u => u.CorreoUsuario == nuevo.Correo))
                return BadRequest("Ya existe un usuario con ese correo.");

            var usuario = new Usuario
            {
                NombreUsuario = nuevo.Nombre,
                CorreoUsuario = nuevo.Correo,
                NumeroControl = nuevo.NumeroControl,
                RolUsuario = nuevo.Rol,       // rol recibido desde DTO
                ContraUsuario = nuevo.Contra, // contraseña
                Credito = 50                  // crédito inicial
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync(); // Guardamos primero para tener IdUsuario

            // Registrar historial inicial
            var historial = new HistorialCredito
            {
                Monto = usuario.Credito,
                FechaMovimiento = DateTime.Now,
                AutCorreo = "Sistema",
                NumeroControlAfectado = usuario.NumeroControl
            };
            _context.HistorialCreditos!.Add(historial);
            await _context.SaveChangesAsync();


            return Ok(new { mensaje = "Usuario registrado correctamente.", usuarioId = usuario.IdUsuario });
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

        // ================= POST: Aumentar crédito =================
        [HttpPost("aumentar-credito")]
        public async Task<IActionResult> AumentarCredito([FromBody] AumentoCreditoDTO dto)
        {
            if (dto.Cantidad < 50)
                return BadRequest("La cantidad debe ser al menos 50.");

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.NumeroControl == dto.NumeroControl);
            if (usuario == null)
                return NotFound("Usuario no encontrado.");

            usuario.Credito += dto.Cantidad;

            var historial = new HistorialCredito
            {
                NumeroControlAfectado = usuario.NumeroControl,
                Monto = dto.Cantidad,
                FechaMovimiento = DateTime.Now,
                AutCorreo = "Sistema"
            };
            _context.HistorialCreditos!.Add(historial);

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Crédito aumentado correctamente.", creditoActual = usuario.Credito });
        }

        // ================= GET: Historial de crédito general =================
        [HttpGet("historial-credito")]
        public async Task<IActionResult> ObtenerHistorialCreditoGeneral()
        {
            var historial = await _context.HistorialCreditos!
                .OrderByDescending(h => h.FechaMovimiento)
                .ToListAsync();

            return Ok(historial);
        }

        // ================= GET: Historial de crédito por usuario =================
        [HttpGet("historial-credito por usuario/{numeroControl}")]
        public async Task<IActionResult> ObtenerHistorialCreditoUsuario(string numeroControl)
        {
            var historial = await _context.HistorialCreditos!
                .Where(h => h.NumeroControlAfectado == numeroControl)
                .OrderByDescending(h => h.FechaMovimiento)
                .ToListAsync();

            return Ok(historial);
        }

        // ================= GET: Crédito actual de un usuario =================
        [HttpGet("credito de 1 usuario/{numeroControl}")]
        public async Task<IActionResult> ObtenerCredito(string numeroControl)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.NumeroControl == numeroControl);
            if (usuario == null)
                return NotFound("Usuario no encontrado.");

            return Ok(new { credito = usuario.Credito });
        }
    }
}
