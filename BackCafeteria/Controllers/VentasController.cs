using Microsoft.AspNetCore.Mvc;
using BackCafeteria.Models;
using CafeteriaAPI.Models;

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
        public IActionResult CrearVenta(Venta venta)
        {
            if (venta == null)
                return BadRequest();

            _context.Ventas.Add(venta);
            _context.SaveChanges();

            return Ok(venta);
        }
    }
}
