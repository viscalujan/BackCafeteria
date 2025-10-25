using Microsoft.AspNetCore.Mvc;
using BackCafeteria.Models;
using CafeteriaAPI.DTOs;
using System.Linq;

namespace BackCafeteria.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VentasController : ControllerBase
    {
        private readonly CafeteriaDbv2Context _context;

        public VentasController(CafeteriaDbv2Context context)
        {
            _context = context;
        }

        [HttpPost("crear")]
        public IActionResult CrearVenta([FromBody] VentaCreateDTO dto)
        {
            if (dto == null || dto.UsuarioId == null)
                return BadRequest("Usuario no proporcionado.");

            var usuario = _context.Usuarios.Find(dto.UsuarioId);
            if (usuario == null)
                return NotFound("Usuario no encontrado.");

            var venta = new Venta
            {
                FkIdUsuario = usuario.IdUsuario,
                MetodoPago = dto.MetodoPago,
                FechaVenta = DateTime.Now,
                TotalVenta = 0m,
                VentaDetalles = dto.Detalles.Select(d =>
                {
                    var producto = _context.Productos.Find(d.ProductoId);
                    if (producto == null)
                        throw new Exception($"Producto con ID {d.ProductoId} no encontrado.");

                    return new VentaDetalle
                    {
                        FkIdProducto = producto.Id,
                        CantidadProducto = d.Cantidad,
                        PrecioUnitario = producto.Precio
                    };
                }).ToList()
            };

            venta.TotalVenta = venta.VentaDetalles.Sum(vd => vd.CantidadProducto * vd.PrecioUnitario);
            _context.Ventas.Add(venta);
            _context.SaveChanges();

            // Preparar DTO de respuesta
            var response = new VentaResponseDTO
            {
                VentaId = venta.IdVentas,
                UsuarioId = venta.FkIdUsuario,
                MetodoPago = venta.MetodoPago,
                TotalVenta = venta.TotalVenta,
                FechaVenta = venta.FechaVenta,
                Detalles = venta.VentaDetalles.Select(vd => new VentaDetalleResponseDTO
                {
                    ProductoId = vd.FkIdProducto,
                    NombreProducto = _context.Productos.Find(vd.FkIdProducto)?.Nombre ?? "Producto no encontrado",
                    CantidadProducto = vd.CantidadProducto,
                    PrecioUnitario = vd.PrecioUnitario
                }).ToList()
            };

            return Ok(response);

    }
}
}
