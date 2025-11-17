using Microsoft.AspNetCore.Mvc;
using BackCafeteria.Models;

namespace BackCafeteria.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
