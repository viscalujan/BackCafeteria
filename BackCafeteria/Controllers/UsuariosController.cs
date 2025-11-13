using BackCafeteria.DTOs;
using BackCafeteria.Helpers;
using BackCafeteria.Models;
using BackCafeteria.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using OfficeOpenXml;
using System.ComponentModel;


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

        [HttpGet("historial-credito")]
        public async Task<IActionResult> ObtenerHistorialCreditoGeneral()
        {
            var historial = await _context.HistorialCreditos!
                .OrderByDescending(h => h.FechaMovimiento)
                .Select(h => new
                {
                    Id = h.IdHistorialCredito,                 // ← Agregado
                    NumeroControlAfectado = h.NumeroControlAfectado,
                    Cantidad = h.Monto,                 // ← Renombrado
                    Fecha = h.FechaMovimiento,          // ← Renombrado
                    AutCorreo = h.AutCorreo
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

        [HttpGet("historial-credito-filtrado")]
        public async Task<IActionResult> ObtenerHistorialCreditoFiltrado(
          [FromQuery] DateTime? desde,
          [FromQuery] DateTime? hasta,
          [FromQuery] string? numeroControl)
        {
            // último mes por default
            desde ??= DateTime.Today.AddMonths(-1);

            // 👇 Ajuste importante: incluir TODO el día "hasta"
            if (hasta.HasValue)
                hasta = hasta.Value.Date.AddDays(1); // día siguiente a las 00:00
            else
                hasta = DateTime.Today.AddDays(1);   // por defecto: hoy completo

            var query = _context.HistorialCreditos!.AsQueryable();

            if (!string.IsNullOrWhiteSpace(numeroControl))
                query = query.Where(h => h.NumeroControlAfectado == numeroControl);

            // 👇 usamos "< hasta" en lugar de "<="
            query = query.Where(h => h.FechaMovimiento >= desde && h.FechaMovimiento < hasta);

            var historial = await query
                .OrderByDescending(h => h.FechaMovimiento)
                .Select(h => new
                {
                    Id = h.IdHistorialCredito,
                    NumeroControlAfectado = h.NumeroControlAfectado,
                    Cantidad = h.Monto,
                    Fecha = h.FechaMovimiento,
                    AutCorreo = h.AutCorreo
                })
                .ToListAsync();

            return Ok(historial);
        }


        [HttpGet("historial-credito-excel")]
        public async Task<IActionResult> ExportarHistorialExcel(
          [FromQuery] DateTime? desde,
          [FromQuery] DateTime? hasta,
          [FromQuery] string? numeroControl)
        {
            desde ??= DateTime.Today.AddMonths(-1);

            if (hasta.HasValue)
                hasta = hasta.Value.Date.AddDays(1);
            else
                hasta = DateTime.Today.AddDays(1);

            var query = _context.HistorialCreditos!.AsQueryable();

            if (!string.IsNullOrWhiteSpace(numeroControl))
                query = query.Where(h => h.NumeroControlAfectado == numeroControl);

            query = query.Where(h => h.FechaMovimiento >= desde && h.FechaMovimiento < hasta);

            var historial = await query
                .OrderBy(h => h.FechaMovimiento)
                .ToListAsync();

            // EXPORTAR A EXCEL
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("Historial");

            sheet.Cells["A1"].Value = "ID";
            sheet.Cells["B1"].Value = "Número Control";
            sheet.Cells["C1"].Value = "Cantidad";
            sheet.Cells["D1"].Value = "Fecha";
            sheet.Cells["E1"].Value = "Autorizado Por";

            int row = 2;
            foreach (var h in historial)
            {
                sheet.Cells[row, 1].Value = h.IdHistorialCredito;
                sheet.Cells[row, 2].Value = h.NumeroControlAfectado;
                sheet.Cells[row, 3].Value = h.Monto;
                sheet.Cells[row, 4].Value = h.FechaMovimiento;
                sheet.Cells[row, 5].Value = h.AutCorreo;
                row++;
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();

            var bytes = package.GetAsByteArray();

            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"HistorialCredito_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
        }





    }
}
