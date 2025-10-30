using BackCafeteria.DTOs;
using BackCafeteria.Models;
using CafeteriaAPI.DTOs;
using CafeteriaAPI.Models;
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
        [HttpPost]
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
                Credito = 50, // Crédito inicial mínimo
            };

            _context.Usuarios.Add(usuario);

            // Registrar movimiento inicial en HistorialCredito
            var historial = new HistorialCredito
            {
                FkIdUsuario = usuario.IdUsuario,
                Monto = usuario.Credito,
                FechaMovimiento = DateTime.Now
            };
            _context.HistorialCreditos!.Add(historial);

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Usuario registrado correctamente." });
        }

        // ================= GET: Listar usuarios =================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsuarioDTO>>> ListarUsuarios()
        {
            var usuarios = await _context.Usuarios
                .Select(u => new UsuarioDTO
                {
                    Id = u.IdUsuario,
                    Nombre = u.NombreUsuario!,
                    Correo = u.CorreoUsuario!,
                    NumeroControl = u.NumeroControl!,
                    Credito = u.Credito
                })
                .ToListAsync();

            return Ok(usuarios);
        }

        // ================= GET: Obtener usuario por número de control =================
        [HttpGet("numeroControl/{numeroControl}")]
        public async Task<ActionResult<UsuarioDTO>> GetUsuarioPorNumeroControl(string numeroControl)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.NumeroControl == numeroControl);

            if (usuario == null)
                return NotFound("Usuario no encontrado.");

            return Ok(new UsuarioDTO
            {
                Id = usuario.IdUsuario,
                Nombre = usuario.NombreUsuario!,
                Correo = usuario.CorreoUsuario!,
                NumeroControl = usuario.NumeroControl!,
                Credito = usuario.Credito
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

            // Registrar en historial
            var historial = new HistorialCredito
            {
                FkIdUsuario = usuario.IdUsuario,
                Monto = dto.Cantidad,
                FechaMovimiento = DateTime.Now
            };
            _context.HistorialCreditos!.Add(historial);

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Crédito aumentado correctamente." });
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
        [HttpGet("historial-credito/{numeroControl}")]
        public async Task<IActionResult> ObtenerHistorialCreditoUsuario(string numeroControl)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.NumeroControl == numeroControl);
            if (usuario == null)
                return NotFound("Usuario no encontrado.");

            var historial = await _context.HistorialCreditos!
                .Where(h => h.FkIdUsuario == usuario.IdUsuario)
                .OrderByDescending(h => h.FechaMovimiento)
                .ToListAsync();

            return Ok(historial);
        }

        // ================= GET: Obtener crédito actual de un usuario =================
        [HttpGet("credito/{numeroControl}")]
        public async Task<IActionResult> ObtenerCredito(string numeroControl)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.NumeroControl == numeroControl);
            if (usuario == null)
                return NotFound("Usuario no encontrado.");

            return Ok(new { credito = usuario.Credito });
        }

        // ================= GET: Verificar existencia de usuario por número de control =================
        [HttpGet("ExisteNumeroControl")]
        public async Task<ActionResult<bool>> ExisteNumeroControl(string numeroControl)
        {
            return await _context.Usuarios.AnyAsync(u => u.NumeroControl == numeroControl);
        }

        // ================= GET: Verificar existencia de usuario por correo =================
        [HttpGet("ExisteCorreo")]
        public async Task<ActionResult<bool>> ExisteCorreo(string correo)
        {
            return await _context.Usuarios.AnyAsync(u => u.CorreoUsuario == correo);
        }
    }
}
