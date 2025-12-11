using BackCafeteria.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackCafeteria.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContabilidadController : ControllerBase
    {
        private readonly CafeteriaDbv2Context _context;

        public ContabilidadController(CafeteriaDbv2Context context)
        {
            _context = context;
        }

        // 🔹 1) Ver todas las cuentas internas (con saldo actual)
        [HttpGet("cuentas-internas")]
        public async Task<IActionResult> GetCuentasInternas()
        {
            string[] codigos =
            {
                "recursos_liquidacion",
                "recursos_comisiones",
                "cafeteria_liquidacion",
                "sistema_comisiones"
            };

            var cuentas = await _context.Usuarios
                .Where(u => codigos.Contains(u.NumeroControl))
                .Select(u => new
                {
                    u.NumeroControl,
                    u.NombreUsuario,
                    Saldo = u.Credito
                })
                .ToListAsync();

            return Ok(cuentas);
        }

        // 🔹 2) Resumen amigable: qué significa cada saldo
        [HttpGet("resumen")]
        public async Task<IActionResult> GetResumenContable()
        {
            string[] codigos =
            {
                "recursos_liquidacion",
                "recursos_comisiones",
                "cafeteria_liquidacion",
                "sistema_comisiones"
            };

            var cuentas = await _context.Usuarios
                .Where(u => codigos.Contains(u.NumeroControl))
                .ToListAsync();

            decimal recursosLiq = cuentas.FirstOrDefault(c => c.NumeroControl == "recursos_liquidacion")?.Credito ?? 0;
            decimal recursosCom = cuentas.FirstOrDefault(c => c.NumeroControl == "recursos_comisiones")?.Credito ?? 0;
            decimal cafLiq = cuentas.FirstOrDefault(c => c.NumeroControl == "cafeteria_liquidacion")?.Credito ?? 0;
            decimal sisCom = cuentas.FirstOrDefault(c => c.NumeroControl == "sistema_comisiones")?.Credito ?? 0;

            var resumen = new
            {
                RecursosLiquidacion = recursosLiq,
                RecursosComisiones = recursosCom,
                CafeteriaLiquidacion = cafLiq,
                SistemaComisiones = sisCom,

                // Interpretaciones útiles:
                Explicacion = new
                {
                    RecursosLiquidacion = "Dinero de RF que respalda ventas de productos hechas con crédito.",
                    RecursosComisiones = "Dinero de RF reservado para pagar comisiones al sistema.",
                    CafeteriaLiquidacion = "Lo que la cafetería tiene derecho a cobrar por ventas de productos.",
                    SistemaComisiones = "Lo que el sistema ha ganado por comisiones."
                }
            };

            return Ok(resumen);
        }

        // 🔹 3) Ver historial de movimientos por cuenta (ej. sistema_comisiones)
        [HttpGet("historial")]
        public async Task<IActionResult> GetHistorial(
            [FromQuery] string? cuenta,
            [FromQuery] DateTime? desde,
            [FromQuery] DateTime? hasta)
        {
            var query = _context.HistorialCreditos.AsQueryable();

            if (!string.IsNullOrWhiteSpace(cuenta))
                query = query.Where(h => h.NumeroControlAfectado == cuenta);

            if (desde.HasValue)
                query = query.Where(h => h.FechaMovimiento >= desde.Value);

            if (hasta.HasValue)
                query = query.Where(h => h.FechaMovimiento <= hasta.Value);

            var movimientos = await query
                .OrderByDescending(h => h.FechaMovimiento)
                .Take(200) // para no explotar el grid del front
                .Select(h => new
                {
                    h.IdHistorialCredito,
                    h.NumeroControlAfectado,
                    h.Monto,
                    h.FechaMovimiento,
                    h.AutCorreo
                })
                .ToListAsync();

            return Ok(movimientos);
        }
    }
}