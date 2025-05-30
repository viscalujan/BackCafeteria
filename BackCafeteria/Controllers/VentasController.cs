using BackCafeteria.DTOs;
using CafeteriaAPI.DTOs;
using CafeteriaAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using ZXing;
using ZXing.Common;
using System.Drawing; // Para Bitmap
using System.IO;
using ZXing.Windows.Compatibility;
using BackCafeteria.Models;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ventas")]
public class VentasController : ControllerBase
{
    private readonly CafeteriaContext _context;

    public VentasController(CafeteriaContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CrearVenta([FromBody] VentaCreateDTO dto)
    {
        if (dto.Detalles == null || !dto.Detalles.Any())
            return BadRequest("Debe incluir al menos un producto en la venta.");

        decimal total = 0m;
        var detallesVenta = new List<VentaDetalle>();

        foreach (var item in dto.Detalles)
        {
            var producto = await _context.Productos.FindAsync(item.ProductoId);
            if (producto == null)
                return NotFound($"Producto con ID {item.ProductoId} no encontrado.");

            if (producto.Cantidad < item.Cantidad)
                return BadRequest($"Stock insuficiente para el producto {producto.Nombre}.");

            producto.Cantidad -= item.Cantidad;

            decimal subtotal = producto.Precio * item.Cantidad;
            total += subtotal;

            detallesVenta.Add(new VentaDetalle
            {
                ProductoId = item.ProductoId,
                Cantidad = item.Cantidad,
                PrecioUnitario = producto.Precio
            });
        }

        Usuario? usuario = null;
        string metodo = dto.MetodoPago.ToLower();

        if (metodo == "credito")
        {
            if (string.IsNullOrWhiteSpace(dto.HashQR))
                return BadRequest("El hash del QR es obligatorio para pagos con crédito.");

            usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.CodigoQRTexto == dto.HashQR);
            if (usuario == null)
                return NotFound("QR no válido o usuario no encontrado.");

            if (usuario.Credito < total)
                return BadRequest("Crédito insuficiente.");

            usuario.Credito -= total;

            // Registrar en historial la disminución del usuario comprador
            _context.HistorialCreditos.Add(new HistorialCredito
            {
                NumeroControlAfectado = usuario.Contra,
                Cantidad = -total,
                Fecha = DateTime.Now,
                AutCorreo = "Sistema-VentaCredito"
            });

            // Buscar o crear usuario "liquidacion"
            var usuarioLiquidacion = await _context.Usuarios.FirstOrDefaultAsync(u => u.Contra == "liquidacion");
            if (usuarioLiquidacion == null)
            {
                usuarioLiquidacion = new Usuario
                {
                    Nombre = "Liquidación",
                    Correo = "liquidacion@cafeteria.com",
                    Contra = "liquidacion",
                    Rol = "0",
                    Credito = 0,
                    Huella = Array.Empty<byte>(),
                    CodigoQRTexto = ""
                };
                _context.Usuarios.Add(usuarioLiquidacion);
                await _context.SaveChangesAsync();
            }

            usuarioLiquidacion.Credito += total;

            // Registrar en historial el aumento del usuario "liquidacion"
            _context.HistorialCreditos.Add(new HistorialCredito
            {
                NumeroControlAfectado = "liquidacion",
                Cantidad = total,
                Fecha = DateTime.Now,
                AutCorreo = "Sistema-VentaCredito"
            });
        }
        else if (metodo != "efectivo")
        {
            return BadRequest("Método de pago no válido. Usa 'efectivo' o 'credito'.");
        }

        var venta = new Venta
        {
            MetodoPago = metodo,
            UsuarioId = usuario?.Id,
            Total = total,
            Detalles = detallesVenta,
            Fecha = DateTime.Now
        };

        _context.Ventas.Add(venta);
        await _context.SaveChangesAsync();

        var ticket = new
        {
            Id = venta.Id,
            Total = venta.Total,
            Fecha = venta.Fecha,
            Metodo = metodo
        };

        return Ok(ticket);
    }

    [HttpGet("numeroControl/{numero}")]
    public async Task<ActionResult<Usuario>> GetUsuarioPorNumeroControl(string numero)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Contra == numero);
        if (usuario == null)
            return NotFound();

        return Ok(usuario);
    }


    [HttpGet("/api/reportes/ventas")]
    public async Task<ActionResult> GetVentasFiltradas([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var query = _context.Ventas
            .Include(v => v.Usuario)
            .Include(v => v.Detalles)
            .AsQueryable();

        if (desde.HasValue)
            query = query.Where(v => v.Fecha >= desde.Value);

        if (hasta.HasValue)
            query = query.Where(v => v.Fecha <= hasta.Value);

        var ventas = await query
            .OrderByDescending(v => v.Fecha)
            .ToListAsync();

        return Ok(ventas);
    }

    [HttpGet("reporte-detallado")]
    public async Task<ActionResult> GetReporteDetallado(
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta)
    {
      
        hasta ??= DateTime.Today;
        desde ??= hasta.Value.AddDays(-30);

        var detalles = await _context.VentaDetalles
            .Include(vd => vd.Venta)         
            .Include(vd => vd.Producto)       
            .Where(vd => vd.Venta.Fecha >= desde && vd.Venta.Fecha <= hasta)
            .Select(vd => new
            {
                VentaId = vd.Venta.Id,
                Fecha = vd.Venta.Fecha,
                ProductoId = vd.Producto.Id,
                ProductoNombre = vd.Producto.Nombre,
                Cantidad = vd.Cantidad,
                PrecioUnitario = vd.PrecioUnitario,
                Total = vd.Cantidad * vd.PrecioUnitario,
                MetodoPago = vd.Venta.MetodoPago
            })
            .OrderByDescending(vd => vd.Fecha)
            .ToListAsync();

        return Ok(detalles);
    } 
}