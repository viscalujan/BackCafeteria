using BackCafeteria.DTOs;
using BackCafeteria.Models;
using BackCafeteria.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BackCafeteria.Services;

namespace BackCafeteria.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PedidosController : ControllerBase
    {
        private readonly CafeteriaDbv2Context _context;
        private readonly EmailService _emailService;

        public PedidosController(CafeteriaDbv2Context context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // 🔹 Crear pedido (rol alumno)
        [HttpPost]
        [Authorize(Roles = "alumno")]
        public async Task<IActionResult> CrearPedido([FromBody] PedidoCreateDTO dto)
        {
            if (dto.Detalles == null || !dto.Detalles.Any())
                return BadRequest("Debe incluir al menos un producto.");

            var usuario = await _context.Usuarios.FindAsync(dto.UsuarioId);
            if (usuario == null)
                return NotFound("Usuario no encontrado.");

            decimal total = 0;
            var detalles = new List<PedidoDetalle>();

            foreach (var item in dto.Detalles)
            {
                // Agregar restricción: no permitir cantidades menores o iguales a 0
                if (item.Cantidad <= 0)
                    return BadRequest($"La cantidad para el producto {item.ProductoId} debe ser mayor a 0.");

                var producto = await _context.Productos.FindAsync(item.ProductoId);
                if (producto == null)
                    return NotFound($"Producto {item.ProductoId} no existe.");

                // Agregar restricción: verificar que el precio del producto no sea menor o igual a 0 (aunque idealmente se valide en el modelo)
                if (producto.Precio <= 0)
                    return BadRequest($"El precio del producto {producto.Nombre} debe ser mayor a 0.");

                if (producto.CantidadProducto < item.Cantidad)
                    return BadRequest($"No hay stock suficiente de {producto.Nombre}.");

                total += producto.Precio * item.Cantidad;

                detalles.Add(new PedidoDetalle
                {
                    FkIdProducto = producto.Id,
                    CantidadPdetalles = item.Cantidad,
                    PrecioDetalles = producto.Precio
                });
            }

            // Agregar restricción: el total calculado no debe ser menor o igual a 0
            if (total <= 0)
                return BadRequest("El total del pedido debe ser mayor a 0.");

            if (usuario.Credito < total)
                return BadRequest("Crédito insuficiente para realizar el pedido.");

            usuario.Credito -= total;

            // Usuario liquidación ganha el monto (igual que ventas)
            var liquidacion = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.NumeroControl == "liquidacion");

            if (liquidacion == null)
            {
                liquidacion = new Usuario
                {
                    NombreUsuario = "Liquidación",
                    CorreoUsuario = "liquidacion@cafeteria.com",
                    NumeroControl = "liquidacion",
                    RolUsuario = "0",
                    Credito = 0,
                    CodigoQRTexto = "",
                    ContraUsuario = "123456"
                };
                _context.Usuarios.Add(liquidacion);
                await _context.SaveChangesAsync();
            }

            liquidacion.Credito += total;

            // Registrar en historial de liquidación
            _context.HistorialCreditos.Add(new HistorialCredito
            {
                NumeroControlAfectado = "liquidacion",
                Monto = total,
                FechaMovimiento = DateTime.Now,
                AutCorreo = "Sistema-Pedido"
            });

            var pedido = new Pedido
            {
                FkIdUsuario = usuario.IdUsuario,
                FechaPedido = DateTime.Now,
                TotalPedido = total,
                FkIdEstado = 1, // 1 = Pendiente
                PedidoDetalles = detalles
            };

            _context.Pedidos.Add(pedido);

            // Registrar movimiento en historial de crédito
            _context.HistorialCreditos.Add(new HistorialCredito
            {
                NumeroControlAfectado = usuario.NumeroControl,
                Monto = -total,
                FechaMovimiento = DateTime.Now,
                AutCorreo = "Sistema-Pedido"
            });

            await _context.SaveChangesAsync();

            return Ok(new { pedido.IdPedidos, pedido.TotalPedido, pedido.FechaPedido });
        }

        // 🔹 Obtener pedidos (alumno)
        [HttpGet("usuario/{idUsuario}")]
        [Authorize(Roles = "alumno")]
        public async Task<IActionResult> GetPedidosPorUsuario(int idUsuario)
        {
            var pedidos = await _context.Pedidos
                .Include(p => p.FkIdEstadoNavigation)
                .Include(p => p.PedidoDetalles)
                    .ThenInclude(d => d.FkIdProductoNavigation)
                .Where(p => p.FkIdUsuario == idUsuario)
                .OrderByDescending(p => p.FechaPedido)
                .ToListAsync();

            var result = pedidos.Select(p => new PedidoResponseDTO
            {
                IdPedido = p.IdPedidos,
                Estado = p.FkIdEstadoNavigation.NombrePedido,
                Fecha = p.FechaPedido,
                Total = p.TotalPedido ?? 0,
                Detalles = p.PedidoDetalles.Select(d => new PedidoDetalleResponseDTO
                {
                    Producto = d.FkIdProductoNavigation.Nombre,
                    Cantidad = d.CantidadPdetalles,
                    Precio = d.PrecioDetalles
                }).ToList()
            });

            return Ok(result);
        }

        // 🔹 Cambiar estado del pedido (rol ventas)
        [HttpPut("{idPedido}/estado")]
        [Authorize(Roles = "ventas")]
        public async Task<IActionResult> CambiarEstado(int idPedido, [FromBody] CambioEstadoPedidoDTO dto)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.FkIdUsuarioNavigation)
                .Include(p => p.PedidoDetalles)
                .FirstOrDefaultAsync(p => p.IdPedidos == idPedido);

            if (pedido == null)
                return NotFound("Pedido no encontrado.");

            var usuario = pedido.FkIdUsuarioNavigation;

            switch (dto.NuevoEstado)
            {
                case 2:
                    // 2 = ACEPTADO
                    // No se hace nada, solo cambia el estado
                    break;

                case 3:
                    // 3 = LISTO → generar venta sin afectar crédito
                    var venta = new Venta
                    {
                        FkIdUsuario = usuario.IdUsuario,
                        MetodoPago = "pedido",
                        FechaVenta = DateTime.Now,
                        TotalVenta = pedido.TotalPedido ?? 0,
                        FkIdPedido = pedido.IdPedidos, // 🔥 GUARDAMOS EL ID DEL PEDIDO
                        VentaDetalles = pedido.PedidoDetalles.Select(d => new VentaDetalle
                        {
                            FkIdProducto = d.FkIdProducto,
                            CantidadProducto = d.CantidadPdetalles,
                            PrecioUnitario = d.PrecioDetalles
                        }).ToList()
                    };

                    _context.Ventas.Add(venta);
                    break;

                case 4:
                    // 4 = RECHAZADO → regresar crédito, stock y guardar motivo

                    // Regresar stock
                    foreach (var d in pedido.PedidoDetalles)
                    {
                        var producto = await _context.Productos.FindAsync(d.FkIdProducto);
                        if (producto != null)
                            producto.CantidadProducto += d.CantidadPdetalles;
                    }

                    // Regresar crédito al alumno
                    usuario.Credito += pedido.TotalPedido ?? 0;

                    _context.HistorialCreditos.Add(new HistorialCredito
                    {
                        NumeroControlAfectado = usuario.NumeroControl,
                        Monto = pedido.TotalPedido ?? 0,
                        FechaMovimiento = DateTime.Now,
                        AutCorreo = "Sistema-RechazoPedido"
                    });

                    // Revertir liquidación
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
                            AutCorreo = "Sistema-RechazoPedido"
                        });
                    }

                    // Guardar motivo
                    pedido.MotivoRechazo = dto.Motivo;

                    // Enviar correo al usuario
                    await _emailService.EnviarCorreoRechazoPedidoAsync(
                        usuario.CorreoUsuario,
                        pedido.IdPedidos,
                        dto.Motivo ?? "Sin motivo especificado",
                        usuario.NombreUsuario
                    );

                    break;

                case 5:
                    // 5 = ENTREGADO
                    // No se toca nada más
                    break;

                default:
                    return BadRequest("Estado inválido.");
            }

            // Actualizar estado
            pedido.FkIdEstado = dto.NuevoEstado;

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = $"Pedido actualizado a estado {dto.NuevoEstado} correctamente." });
        }

        // 🔹 Obtener todos los pedidos (rol ventas)
        [HttpGet("todos")]
        [Authorize(Roles = "ventas")]
        public async Task<IActionResult> GetTodosPedidos()
        {
            var pedidos = await _context.Pedidos
                .Include(p => p.FkIdUsuarioNavigation)
                .Include(p => p.FkIdEstadoNavigation)
                .Include(p => p.PedidoDetalles)
                    .ThenInclude(d => d.FkIdProductoNavigation)
                .OrderByDescending(p => p.FechaPedido)
                .ToListAsync();

            var result = pedidos.Select(p => new
            {
                Id = p.IdPedidos,
                Alumno = p.FkIdUsuarioNavigation.NombreUsuario,
                NumeroControl = p.FkIdUsuarioNavigation.NumeroControl,
                Estado = p.FkIdEstadoNavigation.NombrePedido,
                Fecha = p.FechaPedido,
                Total = p.TotalPedido,
                Detalles = p.PedidoDetalles.Select(d => new
                {
                    Producto = d.FkIdProductoNavigation.Nombre,
                    Cantidad = d.CantidadPdetalles,
                    Precio = d.PrecioDetalles
                })
            });

            return Ok(result);
        }
    }
}