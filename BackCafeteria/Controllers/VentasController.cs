using BackCafeteria.DTOs;
using BackCafeteria.Models;
using BackCafeteria.Services;
using CafeteriaAPI.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ventas")]
public class VentasController : ControllerBase
{
    private readonly CafeteriaDbv2Context _context;
    private readonly EmailService _emailService;

    public VentasController(CafeteriaDbv2Context context, EmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    [HttpPost]
    public async Task<IActionResult> CrearVenta([FromBody] VentaCreateDTO dto)
    {
        if (dto.Detalles == null || !dto.Detalles.Any())
            return BadRequest("Debe incluir al menos un producto en la venta.");

        decimal total = 0m;
        var detallesVenta = new List<VentaDetalle>();

        // 🔹 Calcular total y restar stock
        foreach (var item in dto.Detalles)
        {
            // Agregar restricción: no permitir cantidades menores o iguales a 0
            if (item.Cantidad <= 0)
                return BadRequest($"La cantidad para el producto {item.ProductoId} debe ser mayor a 0.");

            var producto = await _context.Productos.FindAsync(item.ProductoId);
            if (producto == null)
                return NotFound($"Producto con ID {item.ProductoId} no encontrado.");

            // Agregar restricción: verificar que el precio del producto no sea menor o igual a 0
            if (producto.Precio <= 0)
                return BadRequest($"El precio del producto {producto.Nombre} debe ser mayor a 0.");

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

        // Agregar restricción: el total calculado no debe ser menor o igual a 0
        if (total <= 0)
            return BadRequest("El total de la venta debe ser mayor a 0.");

        Usuario? usuario = null;
        string metodo = dto.MetodoPago.ToLower();

        if (metodo == "credito")
        {
            if (string.IsNullOrWhiteSpace(dto.HashQR) && string.IsNullOrWhiteSpace(dto.NumeroDeControl))
                return BadRequest("El hash del QR o el número de control es obligatorio para pagos con crédito.");

            usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.CodigoQRTexto == dto.HashQR || u.NumeroControl == dto.NumeroDeControl);

            if (usuario == null)
                return NotFound("QR no válido o usuario no encontrado.");

            if (usuario.Credito < total)
                return BadRequest("Crédito insuficiente.");

            usuario.Credito -= total;

            // Registrar en historial del comprador
            _context.HistorialCreditos.Add(new HistorialCredito
            {
                NumeroControlAfectado = usuario.NumeroControl,
                Monto = -total,
                FechaMovimiento = DateTime.Now,
                AutCorreo = "Sistema-VentaCredito"
            });

            // Usuario “liquidacion”
            var usuarioLiquidacion = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.NumeroControl == "liquidacion");

            if (usuarioLiquidacion == null)
            {
                usuarioLiquidacion = new Usuario
                {
                    NombreUsuario = "Liquidación",
                    CorreoUsuario = "liquidacion@cafeteria.com",
                    NumeroControl = "liquidacion",
                    RolUsuario = "0",
                    Credito = 0,
                    CodigoQRTexto = "",
                    ContraUsuario = "123456" // ⚡ Valor obligatorio agregado
                };
                _context.Usuarios.Add(usuarioLiquidacion);
                await _context.SaveChangesAsync(); // Guardar liquidación si se creó nuevo
            }

            usuarioLiquidacion.Credito += total;

            // Historial del usuario "liquidacion"
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
            FkIdUsuario = usuario?.IdUsuario ?? dto.FkIdUsuario,
            TotalVenta = total,
            FechaVenta = DateTime.Now,
            VentaDetalles = detallesVenta
        };

        _context.Ventas.Add(venta);

        // 🔹 Guardar cambios en usuarios, historial y venta en un solo SaveChanges
        await _context.SaveChangesAsync();

        return Ok(new
        {
            IdVenta = venta.IdVentas,
            Total = venta.TotalVenta,
            Fecha = venta.FechaVenta,
            MetodoPago = venta.MetodoPago
        });
    }

    [HttpGet("numeroControl/{numero}")]
    public async Task<ActionResult<Usuario>> GetUsuarioPorNumeroControl(string numero)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.NumeroControl == numero);
        if (usuario == null)
            return NotFound();

        return Ok(usuario);
    }

    [HttpGet("/api/reportes/ventas")]
    public async Task<ActionResult<List<VentaResponseDTO>>> GetVentasFiltradas([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var query = _context.Ventas
            .Include(v => v.FkIdUsuarioNavigation)
            .Include(v => v.VentaDetalles)
                .ThenInclude(d => d.FkIdProductoNavigation)
            .AsQueryable();

        if (desde.HasValue)
            query = query.Where(v => v.FechaVenta >= desde.Value);

        if (hasta.HasValue)
            query = query.Where(v => v.FechaVenta <= hasta.Value);

        var ventas = await query
            .OrderByDescending(v => v.FechaVenta)
            .Select(v => new VentaResponseDTO
            {
                VentaId = v.IdVentas,
                UsuarioId = v.FkIdUsuarioNavigation.IdUsuario,
                MetodoPago = v.MetodoPago,
                TotalVenta = v.TotalVenta,
                FechaVenta = v.FechaVenta,
                Detalles = v.VentaDetalles.Select(d => new VentaDetalleResponseDTO
                {
                    ProductoId = d.FkIdProducto,
                    NombreProducto = d.FkIdProductoNavigation.Nombre,
                    CantidadProducto = d.CantidadProducto,
                    PrecioUnitario = d.PrecioUnitario
                }).ToList()
            })
            .ToListAsync();

        return Ok(ventas);
    }

    [HttpGet("reporte-detallado")]
    public async Task<ActionResult> GetReporteDetallado([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
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

    [HttpPut("{idVenta}/cancelar")]
    public async Task<IActionResult> CancelarVenta(int idVenta)
    {
        var venta = await _context.Ventas
            .Include(v => v.VentaDetalles)
            .Include(v => v.FkIdUsuarioNavigation)
            .FirstOrDefaultAsync(v => v.IdVentas == idVenta);

        if (venta == null)
            return NotFound("Venta no encontrada.");

        if (venta.MetodoPago == "cancelado")
            return BadRequest("La venta ya está cancelada.");

        // 🚨 SI LA VENTA PROVIENE DE UN PEDIDO
        if (venta.FkIdPedido != null)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.PedidoDetalles)
                .Include(p => p.FkIdUsuarioNavigation)
                .FirstOrDefaultAsync(p => p.IdPedidos == venta.FkIdPedido);

            if (pedido != null)
            {
                // Cambiar estado a RECHAZADO (4)
                pedido.FkIdEstado = 4;

                // Devolver stock
                foreach (var d in pedido.PedidoDetalles)
                {
                    var producto = await _context.Productos.FindAsync(d.FkIdProducto);
                    if (producto != null)
                        producto.CantidadProducto += d.CantidadPdetalles;
                }

                // Devolver crédito al alumno
                pedido.FkIdUsuarioNavigation.Credito += pedido.TotalPedido ?? 0;

                _context.HistorialCreditos.Add(new HistorialCredito
                {
                    NumeroControlAfectado = pedido.FkIdUsuarioNavigation.NumeroControl,
                    Monto = pedido.TotalPedido ?? 0,
                    FechaMovimiento = DateTime.Now,
                    AutCorreo = "Sistema-CancelacionPedido"
                });

                // Quitar crédito a liquidación
                var liquidacion = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.NumeroControl == "liquidacion");

                if (liquidacion != null)
                {
                    liquidacion.Credito -= pedido.TotalPedido ?? 0;

                    _context.HistorialCreditos.Add(new HistorialCredito
                    {
                        NumeroControlAfectado = "liquidacion",
                        Monto = -(pedido.TotalPedido ?? 0),
                        FechaMovimiento = DateTime.Now,
                        AutCorreo = "Sistema-CancelacionPedido"
                    });
                }
            }

            // Marcar venta como cancelada (sin revertir stock ni crédito)
            venta.MetodoPago = "cancelado";

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Pedido y venta cancelados correctamente." });
        }

        // 🚨 SI ES UNA VENTA NORMAL — Lógica original
        foreach (var d in venta.VentaDetalles)
        {
            var producto = await _context.Productos.FindAsync(d.FkIdProducto);
            if (producto != null)
                producto.CantidadProducto += d.CantidadProducto;
        }

        if (venta.MetodoPago == "credito")
        {
            var usuario = venta.FkIdUsuarioNavigation;

            if (usuario != null)
            {
                usuario.Credito += venta.TotalVenta;

                _context.HistorialCreditos.Add(new HistorialCredito
                {
                    NumeroControlAfectado = usuario.NumeroControl,
                    Monto = venta.TotalVenta,
                    FechaMovimiento = DateTime.Now,
                    AutCorreo = "Sistema-DevolucionVenta"
                });
            }

            var liquidacion = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.NumeroControl == "liquidacion");

            if (liquidacion != null)
            {
                liquidacion.Credito -= venta.TotalVenta;

                _context.HistorialCreditos.Add(new HistorialCredito
                {
                    NumeroControlAfectado = "liquidacion",
                    Monto = -venta.TotalVenta,
                    FechaMovimiento = DateTime.Now,
                    AutCorreo = "Sistema-DevolucionVenta"
                });
            }
        }

        venta.MetodoPago = "cancelado";
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Venta cancelada correctamente." });
    }
}