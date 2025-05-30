using CafeteriaAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using ZXing.QrCode.Internal;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "alumno")]
public class UsuarioNCController : ControllerBase
{
    private readonly CafeteriaContext _context;

    public UsuarioNCController(CafeteriaContext context)
    {
        _context = context;
    }

    // 1. Obtener historial de crédito
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

    // 2. Obtener crédito actual
    [HttpGet("credito/{numeroControl}")]
    public async Task<IActionResult> ObtenerCredito(string numeroControl)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Contra == numeroControl);
        if (usuario == null)
            return NotFound("Usuario no encontrado.");

        return Ok(new { credito = usuario.Credito });
    }

    [HttpGet("qr/{numeroControl}")]
    public async Task<IActionResult> ObtenerCodigoQR(string numeroControl)
    {
        var usuario = await _context.Usuarios
            .Where(u => u.Contra == numeroControl)
            .Select(u => new { u.Huella, u.CodigoQRTexto })
            .FirstOrDefaultAsync();

        if (usuario == null || usuario.Huella == null)
            return NotFound("QR no encontrado para este usuario.");

        return File(usuario.Huella, "image/png", $"QR_{numeroControl}.png");
    }
}
