using System;
using System.Collections.Generic;

namespace CafeteriaAPI.DTOs
{
    public class VentaResponseDTO
    {
        public int VentaId { get; set; }
        public int UsuarioId { get; set; }
        public string MetodoPago { get; set; } = null!;
        public decimal TotalVenta { get; set; }
        public DateTime FechaVenta { get; set; }

        public List<VentaDetalleResponseDTO> Detalles { get; set; } = new();
    }

    public class VentaDetalleResponseDTO
    {
        public int ProductoId { get; set; }
        public string NombreProducto { get; set; } = null!;
        public int CantidadProducto { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal TotalDetalle => CantidadProducto * PrecioUnitario;
    }
}
