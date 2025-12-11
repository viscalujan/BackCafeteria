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
using BackCafeteria.Helpers;


[ApiController]
[Route("api/[controller]")]
//[Authorize(Roles = "ventas")]
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

        // Variables de comisión
        decimal comision = 0m;
        decimal totalConComision = total;

        if (metodo == "credito")
        {
            // 🔹 1. Buscar alumno por QR o número de control
            if (string.IsNullOrWhiteSpace(dto.HashQR) && string.IsNullOrWhiteSpace(dto.NumeroDeControl))
                return BadRequest("El hash del QR o el número de control es obligatorio para pagos con crédito.");

            usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.CodigoQRTexto == dto.HashQR || u.NumeroControl == dto.NumeroDeControl);

            if (usuario == null)
                return NotFound("QR no válido o usuario no encontrado.");

            // 🔹 2. Calcular comisión dinámica para venta por crédito
            var calc = await ComisionHelper.CalcularComisionAsync(_context, total, "credito");
            comision = calc.comision;
            totalConComision = calc.totalConComision;  // total + comisión

            // 🔹 3. Verificar crédito del alumno (paga productos + comisión)
            if (usuario.Credito < totalConComision)
                return BadRequest("Crédito insuficiente para cubrir la venta y la comisión.");

            // 🔹 4. Descontar al alumno
            usuario.Credito -= totalConComision;

            _context.HistorialCreditos.Add(new HistorialCredito
            {
                NumeroControlAfectado = usuario.NumeroControl,
                Monto = -totalConComision,
                FechaMovimiento = DateTime.Now,
                AutCorreo = "Sistema-VentaCredito"
            });

            // 🔹 5. Cuentas internas (Recursos y Sistema)

            // Recursos Financieros - Liquidación (sale dinero hacia cafetería)
            var recursosLiq = await GetOrCreateCuentaInternaAsync(
                "recursos_liquidacion",
                "RF - Liquidación",
                "recursos_liquidacion@tec.com"
            );

            // Recursos Financieros - Comisiones (paga comisiones al sistema)
            var recursosCom = await GetOrCreateCuentaInternaAsync(
                "recursos_comisiones",
                "RF - Comisiones",
                "recursos_comisiones@tec.com"
            );

            // Cafetería - Liquidación (lo que la cafetería debe cobrar por productos)
            var cafLiq = await GetOrCreateCuentaInternaAsync(
                "cafeteria_liquidacion",
                "Cafetería - Liquidación",
                "cafeteria_liquidacion@tec.com"
            );

            // Sistema - Comisiones (ustedes, admins del sistema)
            var sisCom = await GetOrCreateCuentaInternaAsync(
                "sistema_comisiones",
                "Sistema - Comisiones",
                "sistema_comisiones@tec.com"
            );

            // 🔸 Productos: RF → Cafetería
            recursosLiq.Credito -= total;
            cafLiq.Credito += total;

            _context.HistorialCreditos.Add(new HistorialCredito
            {
                NumeroControlAfectado = "recursos_liquidacion",
                Monto = -total,
                FechaMovimiento = DateTime.Now,
                AutCorreo = "Sistema-VentaCredito-Productos"
            });

            _context.HistorialCreditos.Add(new HistorialCredito
            {
                NumeroControlAfectado = "cafeteria_liquidacion",
                Monto = total,
                FechaMovimiento = DateTime.Now,
                AutCorreo = "Sistema-VentaCredito-Productos"
            });

            // 🔸 Comisiones: RF → Sistema
            if (comision > 0)
            {
                recursosCom.Credito -= comision;
                sisCom.Credito += comision;

                _context.HistorialCreditos.Add(new HistorialCredito
                {
                    NumeroControlAfectado = "recursos_comisiones",
                    Monto = -comision,
                    FechaMovimiento = DateTime.Now,
                    AutCorreo = "Sistema-VentaCredito-Comisiones"
                });

                _context.HistorialCreditos.Add(new HistorialCredito
                {
                    NumeroControlAfectado = "sistema_comisiones",
                    Monto = comision,
                    FechaMovimiento = DateTime.Now,
                    AutCorreo = "Sistema-VentaCredito-Comisiones"
                });
            }
        }
        else if (metodo == "efectivo")
        {
            // Efectivo sin comisión: comision = 0, totalConComision = total
            comision = 0m;
            totalConComision = total;
        }
        else
        {
            return BadRequest("Método de pago no válido. Usa 'efectivo' o 'credito'.");
        }

        var venta = new Venta
        {
            MetodoPago = metodo,
            FkIdUsuario = usuario?.IdUsuario ?? dto.FkIdUsuario,
            TotalVenta = total,                  // total de productos
            Comision = comision,                 // comisión calculada
            TotalConComision = totalConComision, // lo que paga el alumno (si aplica)
            FechaVenta = DateTime.Now,
            VentaDetalles = detallesVenta
        };

        _context.Ventas.Add(venta);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            IdVenta = venta.IdVentas,
            Total = venta.TotalVenta,
            Comision = venta.Comision,
            TotalConComision = venta.TotalConComision,
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
                Comision = v.Comision,
                TotalConComision = v.TotalConComision,
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
            .Include(v => v.FkIdUsuarioNavigation)
            .Include(v => v.VentaDetalles)
                .ThenInclude(d => d.FkIdProductoNavigation)
            .FirstOrDefaultAsync(v => v.IdVentas == idVenta);

        if (venta == null)
            return NotFound("Venta no encontrada.");

        // Restaurar stock de productos (para cualquier método de pago)
        foreach (var d in venta.VentaDetalles)
        {
            var producto = await _context.Productos.FindAsync(d.FkIdProducto);
            if (producto != null)
                producto.CantidadProducto += d.CantidadProducto;
        }

        // Si la venta fue en EFECTIVO, solo regresamos stock y (opcional) marcamos cancela.
        if (venta.MetodoPago == "efectivo")
        {
            // Aquí podrías agregar un campo EstadoVenta = "Cancelada" en lugar de borrar:
            _context.Ventas.Remove(venta);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Venta en efectivo cancelada (solo stock revertido)." });
        }

        // Si la venta fue por CRÉDITO, revertimos todo el flujo contable
        if (venta.MetodoPago == "credito")
        {
            var alumno = venta.FkIdUsuarioNavigation;
            if (alumno == null)
                return BadRequest("La venta no tiene alumno asociado.");

            var total = venta.TotalVenta;
            var comision = venta.Comision;
            var totalConComision = venta.TotalConComision;

            // 1) Devolver al alumno lo que pagó
            alumno.Credito += totalConComision;

            _context.HistorialCreditos.Add(new HistorialCredito
            {
                NumeroControlAfectado = alumno.NumeroControl,
                Monto = totalConComision,
                FechaMovimiento = DateTime.Now,
                AutCorreo = "Sistema-CancelarVentaCredito"
            });

            // 2) Revertir liquidación y comisiones internas
            var recursosLiq = await GetOrCreateCuentaInternaAsync(
                "recursos_liquidacion",
                "RF - Liquidación",
                "recursos_liquidacion@tec.com"
            );

            var recursosCom = await GetOrCreateCuentaInternaAsync(
                "recursos_comisiones",
                "RF - Comisiones",
                "recursos_comisiones@tec.com"
            );

            var cafLiq = await GetOrCreateCuentaInternaAsync(
                "cafeteria_liquidacion",
                "Cafetería - Liquidación",
                "cafeteria_liquidacion@tec.com"
            );

            var sisCom = await GetOrCreateCuentaInternaAsync(
                "sistema_comisiones",
                "Sistema - Comisiones",
                "sistema_comisiones@tec.com"
            );

            // 🔸 Productos: revertir RF → Cafetería (se deshace)
            recursosLiq.Credito += total;
            cafLiq.Credito -= total;

            _context.HistorialCreditos.Add(new HistorialCredito
            {
                NumeroControlAfectado = "recursos_liquidacion",
                Monto = total,
                FechaMovimiento = DateTime.Now,
                AutCorreo = "Sistema-CancelarVentaCredito-Productos"
            });

            _context.HistorialCreditos.Add(new HistorialCredito
            {
                NumeroControlAfectado = "cafeteria_liquidacion",
                Monto = -total,
                FechaMovimiento = DateTime.Now,
                AutCorreo = "Sistema-CancelarVentaCredito-Productos"
            });

            // 🔸 Comisiones: revertir RF → Sistema (se deshace)
            if (comision > 0)
            {
                recursosCom.Credito += comision;
                sisCom.Credito -= comision;

                _context.HistorialCreditos.Add(new HistorialCredito
                {
                    NumeroControlAfectado = "recursos_comisiones",
                    Monto = comision,
                    FechaMovimiento = DateTime.Now,
                    AutCorreo = "Sistema-CancelarVentaCredito-Comisiones"
                });

                _context.HistorialCreditos.Add(new HistorialCredito
                {
                    NumeroControlAfectado = "sistema_comisiones",
                    Monto = -comision,
                    FechaMovimiento = DateTime.Now,
                    AutCorreo = "Sistema-CancelarVentaCredito-Comisiones"
                });
            }

            // 3) Eliminar la venta (o marcar como cancelada si agregas un campo para eso)
            _context.Ventas.Remove(venta);

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Venta a crédito cancelada y movimientos revertidos correctamente." });
        }

        // Si en el futuro agregas otros métodos de pago, aquí los podrías manejar
        return BadRequest("Método de pago no soportado para cancelación.");
    }


    private async Task<Usuario> GetOrCreateCuentaInternaAsync(
        string numeroControl,
        string nombre,
        string correoAlias)
    {
        // 1) Buscar por NumeroControl
        var cuenta = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.NumeroControl == numeroControl);

        if (cuenta != null)
            return cuenta;

        // 2) Buscar por correo (por si ya se creó con ese correo)
        cuenta = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.CorreoUsuario == correoAlias);

        if (cuenta != null)
            return cuenta;

        // 3) Si no existe, crearla
        cuenta = new Usuario
        {
            NombreUsuario = nombre,
            CorreoUsuario = correoAlias,
            NumeroControl = numeroControl,
            RolUsuario = "0",
            Credito = 0,
            CodigoQRTexto = "",
            ContraUsuario = "INTERNAL"
        };

        _context.Usuarios.Add(cuenta);
        await _context.SaveChangesAsync();

        return cuenta;
    }


}