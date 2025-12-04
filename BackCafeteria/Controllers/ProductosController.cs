using Microsoft.AspNetCore.Mvc;
using BackCafeteria.Models;
using Microsoft.AspNetCore.Authorization;

namespace BackCafeteria.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "productos")]
    public class ProductosController : ControllerBase
    {
        private readonly CafeteriaDbv2Context _context;

        public ProductosController(CafeteriaDbv2Context context)
        {
            _context = context;
        }

        [HttpGet("productos")]
        public IActionResult ObtenerProductos()
        {
            var productos = _context.Productos.ToList();
            return Ok(productos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Producto>> GetProducto(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();
            return producto;
        }

        [HttpPost]
        public IActionResult CrearProducto([FromBody] Producto producto)
        {
            // Agregar restricciones: no permitir precio menor o igual a 0, ni cantidad negativa
            if (producto.Precio <= 0)
                return BadRequest("El precio del producto debe ser mayor a 0.");
            if (producto.CantidadProducto < 0)
                return BadRequest("La cantidad del producto no puede ser negativa.");

            // Verificar si el ID ya existe
            if (_context.Productos.Any(p => p.Id == producto.Id))
                return BadRequest($"El ID {producto.Id} ya existe. Por favor, utiliza un ID diferente o deja que el sistema lo asigne automáticamente.");

            _context.Productos.Add(producto);
            _context.SaveChanges();
            return Ok(producto);
        }

        [HttpPut("{id}")]
        public IActionResult ActualizarProducto(int id, [FromBody] Producto productoUpdate)
        {
            var producto = _context.Productos.Find(id);
            if (producto == null)
                return NotFound();

            // Agregar restricciones: no permitir precio menor o igual a 0, ni cantidad negativa
            if (productoUpdate.Precio <= 0)
                return BadRequest("El precio del producto debe ser mayor a 0.");
            if (productoUpdate.CantidadProducto < 0)
                return BadRequest("La cantidad del producto no puede ser negativa.");

            producto.Nombre = productoUpdate.Nombre;
            producto.Precio = productoUpdate.Precio;
            producto.CantidadProducto = productoUpdate.CantidadProducto;
            _context.SaveChanges();
            return Ok(producto);
        }

        [HttpDelete("{id}")]
        public IActionResult EliminarProducto(int id)
        {
            var producto = _context.Productos.Find(id);
            if (producto == null)
                return NotFound();

            _context.Productos.Remove(producto);
            _context.SaveChanges();
            return Ok();
        }
    }
}