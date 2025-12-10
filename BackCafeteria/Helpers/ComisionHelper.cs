using System;
using System.Threading.Tasks;
using BackCafeteria.Models;
using Microsoft.EntityFrameworkCore;

namespace BackCafeteria.Helpers
{
    public static class ComisionHelper
    {
        public static async Task<(decimal comision, decimal totalConComision)> CalcularComisionAsync(
            CafeteriaDbv2Context context,
            decimal totalBruto,
            string metodoPago)
        {
            // Efectivo NO tiene comisión
            if (metodoPago == "efectivo")
                return (0m, totalBruto);

            // Buscar configuración dinámica
            var config = await context.ConfiguracionComisiones
                .FirstOrDefaultAsync(c => c.MetodoPago == metodoPago && c.Activo);

            if (config == null)
                return (0m, totalBruto); // si no hay config, no se aplica comisión

            decimal comisionBase;

            if (metodoPago == "pedido")
            {
                // Pedido: respeta umbral y mínimo
                if (config.Umbral > 0 && totalBruto <= config.Umbral)
                {
                    comisionBase = config.Minimo;
                }
                else
                {
                    comisionBase = totalBruto * (config.Porcentaje / 100m);

                    if (config.Minimo > 0 && comisionBase < config.Minimo)
                        comisionBase = config.Minimo;
                }
            }
            else
            {
                // Otros métodos (ej. 'credito')
                comisionBase = totalBruto * (config.Porcentaje / 100m);

                if (config.Minimo > 0 && comisionBase < config.Minimo)
                    comisionBase = config.Minimo;
            }

            // Redondear hacia arriba y dejar solo enteros
            var comisionEntera = (decimal)Math.Ceiling(comisionBase);

            // El alumno paga total_bruto + comisión (la cafetería no pierde)
            var totalConComision = totalBruto + comisionEntera;

            return (comisionEntera, totalConComision);
        }
    }
}
