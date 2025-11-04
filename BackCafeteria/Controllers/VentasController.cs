using BackCafeteria.DTOs;
using BackCafeteria.Models;
using BackCafeteria.Services;
using CafeteriaAPI.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZXing;
using ZXing.Common;
using System.Drawing;
using System.IO;
using ZXing.Windows.Compatibility;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ventas")]
public class VentasController : ControllerBase
{
    private readonly CafeteriaDbv2Context _context;
    private readonly EmailService _emailService;

    public VentasController(CafeteriaDbv2Context context)
    {
        _context = context;
        _emailService = new EmailService(context);
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

            if (producto.CantidadProducto < item.Cantidad)
                return BadRequest($"Stock insuficiente para el producto {producto.Nombre}.");

            producto.CantidadProducto -= item.Cantidad;

            decimal subtotal = producto.Precio * item.Cantidad;
            total += subtotal;

            detallesVenta.Add(new VentaDetalle
            {
                FkIdProducto = item.ProductoId,
                CantidadProducto = item.Cantidad,
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

            // 📧 Enviar correo de confirmación (ajustado a tu EmailService)
            string resumen = await _emailService.GenerarResumenVentaAsync(usuario.IdUsuario);
            // Aquí podrías enviar correo si lo implementas
            // await _emailService.EnviarCorreoConQR(usuario.CorreoUsuario, qrBytes);

            usuario.Credito -= total;

            // Registrar en historial (disminución del comprador)
            _context.HistorialCreditos.Add(new HistorialCredito
            {
                NumeroControlAfectado = usuario.NumeroControl,
                Monto = -total,
                FechaMovimiento = DateTime.Now,
                AutCorreo = "Sistema-VentaCredito"
            });

            // Buscar o crear usuario “liquidacion”
            var usuarioLiquidacion = await _context.Usuarios.FirstOrDefaultAsync(u => u.NumeroControl == "liquidacion");
            if (usuarioLiquidacion == null)
            {
                usuarioLiquidacion = new Usuario
                {
                    NombreUsuario = "Liquidación",
                    CorreoUsuario = "liquidacion@cafeteria.com",
                    NumeroControl = "liquidacion",
                    RolUsuario = "0",
                    Credito = 0,
                    Huella = null,
                    CodigoQRTexto = ""
                };
                _context.Usuarios.Add(usuarioLiquidacion);
                await _context.SaveChangesAsync();
            }

            usuarioLiquidacion.Credito += total;

            // Registrar aumento en historial del usuario "liquidacion"
            _context.HistorialCreditos.Add(new HistorialCredito
            {
                NumeroControlAfectado = "liquidacion",
                Monto = total,
                FechaMovimiento = DateTime.Now,
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
            FkIdUsuario = usuario?.IdUsuario ?? 0,
            TotalVenta = total,
            FechaVenta = DateTime.Now,
            VentaDetalles = detallesVenta
        };

        _context.Ventas.Add(venta);
        await _context.SaveChangesAsync();

        var ticket = new
        {
            Id = venta.IdVentas,
            Total = venta.TotalVenta,
            Fecha = venta.FechaVenta,
            Metodo = metodo
        };

        return Ok(ticket);
    }

    // 🔹 GET por número de control
    [HttpGet("numeroControl/{numero}")]
    public async Task<ActionResult<Usuario>> GetUsuarioPorNumeroControl(string numero)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.NumeroControl == numero);
        if (usuario == null)
            return NotFound();

        return Ok(usuario);
    }

    // 🔹 Reporte general
    [HttpGet("/api/reportes/ventas")]
    public async Task<ActionResult> GetVentasFiltradas([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var query = _context.Ventas
            .Include(v => v.FkIdUsuarioNavigation)
            .Include(v => v.VentaDetalles)
            .AsQueryable();

        if (desde.HasValue)
            query = query.Where(v => v.FechaVenta >= desde.Value);

        if (hasta.HasValue)
            query = query.Where(v => v.FechaVenta <= hasta.Value);

        var ventas = await query
            .OrderByDescending(v => v.FechaVenta)
            .ToListAsync();

        return Ok(ventas);
    }

    // 🔹 Reporte detallado
    [HttpGet("reporte-detallado")]
    public async Task<ActionResult> GetReporteDetallado(
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta)
    {
        hasta ??= DateTime.Today;
        desde ??= hasta.Value.AddDays(-30);

        var detalles = await _context.VentaDetalles
            .Include(vd => vd.FkIdVentaNavigation)
            .Include(vd => vd.FkIdProductoNavigation)
            .Where(vd => vd.FkIdVentaNavigation.FechaVenta >= desde && vd.FkIdVentaNavigation.FechaVenta <= hasta)
            .Select(vd => new
            {
                VentaId = vd.FkIdVenta,
                Fecha = vd.FkIdVentaNavigation.FechaVenta,
                ProductoId = vd.FkIdProducto,
                ProductoNombre = vd.FkIdProductoNavigation.Nombre,
                Cantidad = vd.CantidadProducto,
                PrecioUnitario = vd.PrecioUnitario,
                Total = vd.CantidadProducto * vd.PrecioUnitario,
                MetodoPago = vd.FkIdVentaNavigation.MetodoPago
            })
            .OrderByDescending(vd => vd.Fecha)
            .ToListAsync();

        return Ok(detalles);
    }
}
