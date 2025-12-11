using BackCafeteria.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackCafeteria.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ComisionesController : ControllerBase
    {
        private readonly CafeteriaDbv2Context _context;

        public ComisionesController(CafeteriaDbv2Context context)
        {
            _context = context;
        }

        public class ComisionConfigDTO
        {
            public string MetodoPago { get; set; } = null!;
            public decimal Porcentaje { get; set; }
            public decimal Minimo { get; set; }
            public decimal Umbral { get; set; }
        }

        // GET: api/Comisiones/config
        [HttpGet("config")]
        public async Task<ActionResult<IEnumerable<ComisionConfigDTO>>> GetConfigs()
        {
            var configs = await _context.ConfiguracionComisiones
                .Where(c => c.Activo)
                .Select(c => new ComisionConfigDTO
                {
                    MetodoPago = c.MetodoPago,
                    Porcentaje = c.Porcentaje,
                    Minimo = c.Minimo,
                    Umbral = c.Umbral
                })
                .ToListAsync();

            return Ok(configs);
        }

        // GET: api/Comisiones/config/credito
        [HttpGet("config/{metodoPago}")]
        public async Task<ActionResult<ComisionConfigDTO>> GetConfig(string metodoPago)
        {
            var config = await _context.ConfiguracionComisiones
                .FirstOrDefaultAsync(c => c.MetodoPago == metodoPago && c.Activo);

            if (config == null)
                return NotFound("No existe configuración para ese método de pago.");

            var dto = new ComisionConfigDTO
            {
                MetodoPago = config.MetodoPago,
                Porcentaje = config.Porcentaje,
                Minimo = config.Minimo,
                Umbral = config.Umbral
            };

            return Ok(dto);
        }

        // PUT: api/Comisiones/config/credito
        [HttpPut("config/{metodoPago}")]
        public async Task<IActionResult> UpdateConfig(string metodoPago, [FromBody] ComisionConfigDTO dto)
        {
            var config = await _context.ConfiguracionComisiones
                .FirstOrDefaultAsync(c => c.MetodoPago == metodoPago && c.Activo);

            if (config == null)
                return NotFound("No existe configuración para ese método de pago.");

            config.Porcentaje = dto.Porcentaje;
            config.Minimo = dto.Minimo;
            config.Umbral = dto.Umbral;

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Configuración de comisión actualizada correctamente." });
        }
    }
}
