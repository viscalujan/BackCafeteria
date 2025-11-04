namespace CafeteriaAPI.DTOs
{
    public class VentaCreateDTO
    {
        public int FkIdUsuario { get; set; } // ID del usuario que hace la compra
        public string MetodoPago { get; set; } = null!; // "efectivo" o "credito"
        public string? NumeroDeControl { get; set; } // opcional
        public string? HashQR { get; set; } // opcional
        public DateTime FechaVenta { get; set; }
        public List<VentaDetalleDTO> Detalles { get; set; } = new();
    }

    public class VentaDetalleDTO
    {
        public int ProductoId { get; set; } // referencia al producto
        public int Cantidad { get; set; } // cantidad a vender
    }
}
