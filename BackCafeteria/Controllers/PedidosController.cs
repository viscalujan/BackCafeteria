using BackCafeteria.DTOs;
using BackCafeteria.Helpers;
using BackCafeteria.Models;
using BackCafeteria.Services;
using BackCafeteria.Services;
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
        private readonly EmailService _emailService;

        public PedidosController(CafeteriaDbv2Context context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

  
        [HttpPost]
        // [Authorize(Roles = "alumno")]
        public async Task<IActionResult> CrearPedido([FromBody] PedidoCreateDTO dto)
        {
            if (dto.Detalles == null || !dto.Detalles.Any())
                return BadRequest("Debe incluir al menos un producto.");

            var usuario = await _context.Usuarios.FindAsync(dto.UsuarioId);
            if (usuario == null)
                return NotFound("Usuario no encontrado.");

            decimal totalBruto = 0m;
            var detalles = new List<PedidoDetalle>();

            foreach (var item in dto.Detalles)
            {
                if (item.Cantidad <= 0)
                    return BadRequest($"La cantidad para el producto {item.ProductoId} debe ser mayor a 0.");

                var producto = await _context.Productos.FindAsync(item.ProductoId);
                if (producto == null)
                    return NotFound($"Producto {item.ProductoId} no existe.");

                if (producto.Precio <= 0)
                    return BadRequest($"El precio del producto {producto.Nombre} debe ser mayor a 0.");

                // 👇 Solo validamos stock aquí, NO lo descontamos todavía
                if (producto.CantidadProducto < item.Cantidad)
                    return BadRequest($"No hay stock suficiente de {producto.Nombre}.");

                totalBruto += producto.Precio * item.Cantidad;

                detalles.Add(new PedidoDetalle
                {
                    FkIdProducto = producto.Id,
                    CantidadPdetalles = item.Cantidad,
                    PrecioDetalles = producto.Precio
                });
            }

            if (totalBruto <= 0)
                return BadRequest("El total del pedido debe ser mayor a 0.");

            // 🧮 Calcular comisión estimada para pedidos (no se cobra todavía)
            var (comisionEstimado, totalConComision) = await ComisionHelper.CalcularComisionAsync(
                _context,
                totalBruto,
                "pedido"
            );

            // 🔍 Verificar que el alumno tenga crédito suficiente para total + comisión
            if (usuario.Credito < totalConComision)
            {
                return BadRequest("Crédito insuficiente para crear este pedido (total + comisión).");
            }

            // ✅ Crear el pedido en estado Pendiente, sin tocar crédito, stock ni cuentas
            var pedido = new Pedido
            {
                FkIdUsuario = usuario.IdUsuario,
                FechaPedido = DateTime.Now,
                TotalPedido = totalBruto,           // total de productos
                FkIdEstado = 1,                    // 1 = Pendiente
                PedidoDetalles = detalles
            };

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            // Devolvemos info útil para el front
            return Ok(new
            {
                pedido.IdPedidos,
                TotalProductos = totalBruto,
                ComisionEstimado = comisionEstimado,
                TotalConComision = totalConComision,
                pedido.FechaPedido
            });
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
       // [Authorize(Roles = "ventas")]
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
                    // 2 = ACEPTADO → aquí se cobra TODO, se crea la venta, se baja stock y se mueven cuentas
                    {
                        var totalBruto = pedido.TotalPedido ?? 0m;

                        // 1) Calcular comisión para PEDIDO
                        var (comisionPedido, totalConComisionPedido) = await ComisionHelper.CalcularComisionAsync(
                            _context,
                            totalBruto,
                            "pedido"
                        );

                        // 2) Verificar crédito suficiente (total + comisión)
                        if (usuario.Credito < totalConComisionPedido)
                            return BadRequest("Crédito insuficiente para aceptar el pedido (total + comisión).");

                        // 3) Cobrar al alumno total + comisión
                        usuario.Credito -= totalConComisionPedido;

                        _context.HistorialCreditos.Add(new HistorialCredito
                        {
                            NumeroControlAfectado = usuario.NumeroControl,
                            Monto = -totalConComisionPedido,
                            FechaMovimiento = DateTime.Now,
                            AutCorreo = "Sistema-Pedido-Aceptado"
                        });

                        // 4) Descontar stock AHORA (hasta este momento se hace firme la venta)
                        foreach (var d in pedido.PedidoDetalles)
                        {
                            var producto = await _context.Productos.FindAsync(d.FkIdProducto);
                            if (producto == null)
                                return NotFound($"Producto con ID {d.FkIdProducto} no encontrado.");

                            if (producto.CantidadProducto < d.CantidadPdetalles)
                                return BadRequest($"Stock insuficiente para el producto {producto.Nombre}.");

                            producto.CantidadProducto -= d.CantidadPdetalles;
                        }

                        // 5) Cuentas internas
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

                        // 🔸 Productos: RF → Cafetería
                        recursosLiq.Credito -= totalBruto;
                        cafLiq.Credito += totalBruto;

                        _context.HistorialCreditos.Add(new HistorialCredito
                        {
                            NumeroControlAfectado = "recursos_liquidacion",
                            Monto = -totalBruto,
                            FechaMovimiento = DateTime.Now,
                            AutCorreo = "Sistema-Pedido-Productos"
                        });

                        _context.HistorialCreditos.Add(new HistorialCredito
                        {
                            NumeroControlAfectado = "cafeteria_liquidacion",
                            Monto = totalBruto,
                            FechaMovimiento = DateTime.Now,
                            AutCorreo = "Sistema-Pedido-Productos"
                        });

                        // 🔸 Comisiones: RF → Sistema
                        if (comisionPedido > 0)
                        {
                            recursosCom.Credito -= comisionPedido;
                            sisCom.Credito += comisionPedido;

                            _context.HistorialCreditos.Add(new HistorialCredito
                            {
                                NumeroControlAfectado = "recursos_comisiones",
                                Monto = -comisionPedido,
                                FechaMovimiento = DateTime.Now,
                                AutCorreo = "Sistema-Pedido-Comision"
                            });

                            _context.HistorialCreditos.Add(new HistorialCredito
                            {
                                NumeroControlAfectado = "sistema_comisiones",
                                Monto = comisionPedido,
                                FechaMovimiento = DateTime.Now,
                                AutCorreo = "Sistema-Pedido-Comision"
                            });
                        }

                        // 6) Crear la Venta ligada al Pedido
                        var venta = new Venta
                        {
                            FkIdUsuario = usuario.IdUsuario,
                            MetodoPago = "pedido",
                            FechaVenta = DateTime.Now,
                            TotalVenta = totalBruto,
                            Comision = comisionPedido,
                            TotalConComision = totalConComisionPedido,
                            FkIdPedido = pedido.IdPedidos,
                            VentaDetalles = pedido.PedidoDetalles.Select(d => new VentaDetalle
                            {
                                FkIdProducto = d.FkIdProducto,
                                CantidadProducto = d.CantidadPdetalles,
                                PrecioUnitario = d.PrecioDetalles
                            }).ToList()
                        };

                        _context.Ventas.Add(venta);
                    }
                    break;

                case 3:
                    // 3 = LISTO → solo cambia el estado, sin tocar dinero ni stock
                    break;

                case 4:
                    // 4 = RECHAZADO
                    {
                        var totalPedido = pedido.TotalPedido ?? 0m;

                        // 1) Ver si ya existe una venta ligada a este pedido
                        var ventaPedido = await _context.Ventas
                            .FirstOrDefaultAsync(v => v.FkIdPedido == pedido.IdPedidos && v.MetodoPago == "pedido");

                        if (ventaPedido != null)
                        {
                            var totalBruto = ventaPedido.TotalVenta;
                            var comisionPedido = ventaPedido.Comision;
                            var totalConComisionPedido = ventaPedido.TotalConComision;

                            // 2) Regresar stock de productos
                            foreach (var d in pedido.PedidoDetalles)
                            {
                                var producto = await _context.Productos.FindAsync(d.FkIdProducto);
                                if (producto != null)
                                    producto.CantidadProducto += d.CantidadPdetalles;
                            }

                            // 3) Devolver al alumno lo que pagó (total + comisión)
                            usuario.Credito += totalConComisionPedido;

                            _context.HistorialCreditos.Add(new HistorialCredito
                            {
                                NumeroControlAfectado = usuario.NumeroControl,
                                Monto = totalConComisionPedido,
                                FechaMovimiento = DateTime.Now,
                                AutCorreo = "Sistema-RechazoPedido-Total"
                            });

                            // 4) Revertir liquidaciones internas
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

                            // 🔸 Productos: revertir RF → Cafetería
                            recursosLiq.Credito += totalBruto;
                            cafLiq.Credito -= totalBruto;

                            _context.HistorialCreditos.Add(new HistorialCredito
                            {
                                NumeroControlAfectado = "recursos_liquidacion",
                                Monto = totalBruto,
                                FechaMovimiento = DateTime.Now,
                                AutCorreo = "Sistema-RechazoPedido-Productos"
                            });

                            _context.HistorialCreditos.Add(new HistorialCredito
                            {
                                NumeroControlAfectado = "cafeteria_liquidacion",
                                Monto = -totalBruto,
                                FechaMovimiento = DateTime.Now,
                                AutCorreo = "Sistema-RechazoPedido-Productos"
                            });

                            // 🔸 Comisiones: revertir RF → Sistema
                            if (comisionPedido > 0)
                            {
                                recursosCom.Credito += comisionPedido;
                                sisCom.Credito -= comisionPedido;

                                _context.HistorialCreditos.Add(new HistorialCredito
                                {
                                    NumeroControlAfectado = "recursos_comisiones",
                                    Monto = comisionPedido,
                                    FechaMovimiento = DateTime.Now,
                                    AutCorreo = "Sistema-RechazoPedido-Comision"
                                });

                                _context.HistorialCreditos.Add(new HistorialCredito
                                {
                                    NumeroControlAfectado = "sistema_comisiones",
                                    Monto = -comisionPedido,
                                    FechaMovimiento = DateTime.Now,
                                    AutCorreo = "Sistema-RechazoPedido-Comision"
                                });
                            }

                            // 5) Eliminar la venta ligada
                            _context.Ventas.Remove(ventaPedido);
                        }
                        // Si no hay venta ligada, solo queda marcado como Rechazado
                        // (no había cobro todavía, así que no se toca nada de dinero ni stock)

                        // 6) Guardar motivo y enviar correo
                        pedido.MotivoRechazo = dto.Motivo;

                        await _emailService.EnviarCorreoRechazoPedidoAsync(
                            usuario.CorreoUsuario,
                            pedido.IdPedidos,
                            dto.Motivo ?? "Sin motivo especificado",
                            usuario.NombreUsuario
                        );
                    }
                    break;

                case 5:
                    // 5 = ENTREGADO → solo cierre lógico
                    break;

                default:
                    return BadRequest("Estado inválido.");
            }

            // Actualizar estado del pedido
            pedido.FkIdEstado = dto.NuevoEstado;

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = $"Pedido actualizado a estado {dto.NuevoEstado} correctamente." });
        }


        // 🔹 Obtener todos los pedidos (rol ventas)
        [HttpGet("todos")]
     //   [Authorize(Roles = "ventas")]
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
}