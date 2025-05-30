using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CafeteriaAPI.Models;
using BCrypt.Net;

namespace CafeteriaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly CafeteriaContext _context;
        private readonly IConfiguration _config;

        public AuthController(CafeteriaContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest login)
        {
            // 🔐 1. Busca en la tabla Aut (admin, venta, inventario...)
            var usuario = await _context.Aut.FirstOrDefaultAsync(u => u.Correo == login.Correo);

            if (usuario != null && BCrypt.Net.BCrypt.Verify(login.Contra, usuario.Contra))
            {
                var key = Encoding.ASCII.GetBytes(_config["settings:secretkey"]);
                var claims = new ClaimsIdentity(new[]
                {
            new Claim(ClaimTypes.NameIdentifier, usuario.Nombre),
            new Claim(ClaimTypes.Role, usuario.Rol)
        });

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = claims,
                    Expires = DateTime.UtcNow.AddHours(2),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var token = tokenHandler.CreateToken(tokenDescriptor);

                // ✅ Solo devuelve token y rol (sin numeroControl para no-alumnos)
                return Ok(new { token = tokenHandler.WriteToken(token), rol = usuario.Rol });
            }

            // 🔐 2. Busca en la tabla Usuarios (alumnos)
            var alumno = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == login.Correo);

            if (alumno != null && BCrypt.Net.BCrypt.Verify(login.Contra, alumno.Contrasena))
            {
                var key = Encoding.ASCII.GetBytes(_config["settings:secretkey"]);
                var claims = new ClaimsIdentity(new[]
                {
            new Claim(ClaimTypes.NameIdentifier, alumno.Nombre),
            new Claim(ClaimTypes.Role, "alumno"),
            new Claim("NumeroControl", alumno.Contra) // "Contra" = número de control
        });

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = claims,
                    Expires = DateTime.UtcNow.AddHours(2),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var token = tokenHandler.CreateToken(tokenDescriptor);

                // ✅ Devuelve token, rol Y numeroControl (solo para alumnos)
                return Ok(new
                {
                    token = tokenHandler.WriteToken(token),
                    rol = "alumno",
                    numeroControl = alumno.Contra // Asegúrate de que esto sea el número de control
                });
            }

            return Unauthorized(new { mensaje = "Credenciales inválidas" });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] Aut nuevoUsuario, [FromQuery] string clave)
        {
            var claveCorrecta = _config["settings:adminRegisterPassword"];
            if (clave != claveCorrecta)
                return Unauthorized("Clave de administrador incorrecta.");

            if (string.IsNullOrWhiteSpace(nuevoUsuario.Nombre) ||
                string.IsNullOrWhiteSpace(nuevoUsuario.Correo) ||
                string.IsNullOrWhiteSpace(nuevoUsuario.Contra) ||
                string.IsNullOrWhiteSpace(nuevoUsuario.Rol))
            {
                return BadRequest("Todos los campos son obligatorios.");
            }

            bool existe = await _context.Aut.AnyAsync(u => u.Correo == nuevoUsuario.Correo);
            if (existe)
                return BadRequest("Ya existe un usuario con ese correo.");

            nuevoUsuario.Contra = BCrypt.Net.BCrypt.HashPassword(nuevoUsuario.Contra);

            _context.Aut.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Usuario registrado correctamente." });
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Home");
        }
    }

}
