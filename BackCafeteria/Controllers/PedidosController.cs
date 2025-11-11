using BackCafeteria.DTOs;
using BackCafeteria.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackCafeteria.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PedidosController : ControllerBase
    {
        private readonly CafeteriaDbv2Context _context;

        public PedidosController(CafeteriaDbv2Context context)
        {
            _context = context;
        }

        // 🔹 Crear pedido (rol alumno)
        [HttpPost]
       // [Authorize(Roles = "alumno")]
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
                var producto = await _context.Productos.FindAsync(item.ProductoId);
                if (producto == null)
                    return NotFound($"Producto {item.ProductoId} no existe.");

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

            if (usuario.Credito < total)
                return BadRequest("Crédito insuficiente para realizar el pedido.");

            usuario.Credito -= total;

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
       // [Authorize(Roles = "alumno")]
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
        //[Authorize(Roles = "ventas")]
        public async Task<IActionResult> CambiarEstado(int idPedido, [FromQuery] int nuevoEstado)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.FkIdUsuarioNavigation)
                .Include(p => p.PedidoDetalles)
                .FirstOrDefaultAsync(p => p.IdPedidos == idPedido);

            if (pedido == null)
                return NotFound("Pedido no encontrado.");

            var usuario = pedido.FkIdUsuarioNavigation;

            // 4 = Rechazado → devolver crédito
            if (nuevoEstado == 4)
            {
                usuario.Credito += pedido.TotalPedido ?? 0;
                _context.HistorialCreditos.Add(new HistorialCredito
                {
                    NumeroControlAfectado = usuario.NumeroControl,
                    Monto = pedido.TotalPedido ?? 0,
                    FechaMovimiento = DateTime.Now,
                    AutCorreo = "Sistema-Devolución"
                });
            }

            // 3 = Listo → crear venta (sin afectar crédito)
            if (nuevoEstado == 3)
            {
                var venta = new Venta
                {
                    FkIdUsuario = usuario.IdUsuario,
                    MetodoPago = "pedido",
                    FechaVenta = DateTime.Now,
                    TotalVenta = pedido.TotalPedido ?? 0,
                    VentaDetalles = pedido.PedidoDetalles.Select(d => new VentaDetalle
                    {
                        FkIdProducto = d.FkIdProducto,
                        CantidadProducto = d.CantidadPdetalles,
                        PrecioUnitario = d.PrecioDetalles
                    }).ToList()
                };
                _context.Ventas.Add(venta);
            }

            // 5 = Entregado → solo marcar final
            if (nuevoEstado == 5)
            {
                // No se modifica nada adicional, solo cierre lógico del pedido
            }

            pedido.FkIdEstado = nuevoEstado;
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = $"Pedido actualizado a estado {nuevoEstado} correctamente." });
        }


        // 🔹 Obtener todos los pedidos (rol ventas)
        [HttpGet("todos")]
        //[Authorize(Roles = "ventas")]
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
