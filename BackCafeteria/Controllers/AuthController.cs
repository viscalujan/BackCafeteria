using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BackCafeteria.Models;  // Asegúrate de que los modelos estén importados
using BCrypt.Net;

namespace BackCafeteria.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly CafeteriaDbv2Context _context;
        private readonly IConfiguration _config;

        public AuthController(CafeteriaDbv2Context context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // ================= LOGIN =================
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest login)
        {
            // 🔹 1. Buscar en tabla Aut (admin, venta, inventario)
            var usuario = await _context.Aut.FirstOrDefaultAsync(u => u.CorreoAut == login.Correo);
            if (usuario != null && BCrypt.Net.BCrypt.Verify(login.Contra, usuario.ContraAut))
            {
                var token = GenerarToken(usuario.NombreAut, usuario.RolAut ?? "admin");
                return Ok(new { token, rol = usuario.RolAut });
            }

            // 🔹 2. Buscar en tabla Usuarios (alumnos)
            var alumno = await _context.Usuarios.FirstOrDefaultAsync(u => u.CorreoUsuario == login.Correo);
            if (alumno != null && BCrypt.Net.BCrypt.Verify(login.Contra, alumno.ContraUsuario))
            {
                var token = GenerarToken(alumno.NombreUsuario, "alumno", alumno.NumeroControl);
                return Ok(new
                {
                    token,
                    rol = "alumno",
                    numeroControl = alumno.NumeroControl  // Ahora es string, compatible
                });
            }

            return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos" });
        }

        // ================= REGISTRO ADMIN =================
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] Aut nuevoUsuario, [FromQuery] string clave)
        {
            var claveCorrecta = _config["settings:adminRegisterPassword"];
            if (clave != claveCorrecta)
                return Unauthorized("Clave de administrador incorrecta.");

            if (string.IsNullOrWhiteSpace(nuevoUsuario.NombreAut) ||
                string.IsNullOrWhiteSpace(nuevoUsuario.CorreoAut) ||
                string.IsNullOrWhiteSpace(nuevoUsuario.ContraAut) ||
                string.IsNullOrWhiteSpace(nuevoUsuario.RolAut))
            {
                return BadRequest("Todos los campos son obligatorios.");
            }

            bool existe = await _context.Aut.AnyAsync(u => u.CorreoAut == nuevoUsuario.CorreoAut);
            if (existe)
                return BadRequest("Ya existe un usuario con ese correo.");

            // Hashear contraseña
            nuevoUsuario.ContraAut = BCrypt.Net.BCrypt.HashPassword(nuevoUsuario.ContraAut);

            _context.Aut.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Usuario registrado correctamente." });
        }

        // ================= LOGOUT =================
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // Con JWT, cerrar sesión se hace eliminando el token del cliente
            return Ok(new { mensaje = "Sesión cerrada (el token debe eliminarse del cliente)" });
        }

        // ================= MÉTODO PRIVADO PARA GENERAR JWT =================
        private string GenerarToken(string nombre, string rol, string? numeroControl = null)
        {
            var key = Encoding.ASCII.GetBytes(_config["settings:secretkey"]);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, nombre),
                new Claim(ClaimTypes.Role, rol)
            };

            if (!string.IsNullOrEmpty(numeroControl))
                claims.Add(new Claim("NumeroControl", numeroControl));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
        }
    }

    // ================= MODELO LOGIN =================
    public class LoginRequest
    {
        public string Correo { get; set; } = null!;
        public string Contra { get; set; } = null!;
    }
}