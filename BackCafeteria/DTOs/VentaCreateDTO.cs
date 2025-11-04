namespace CafeteriaAPI.DTOs
{
    public class VentaCreateDTO
    {
        public string MetodoPago { get; set; } = null!; // "efectivo" o "credito"
        public int? UsuarioId { get; set; } // obligatorio si es con crédito
        public List<VentaDetalleDTO> Detalles { get; set; } = new();
        public string? NumeroDeControl { get; set; } // opcional
        public string? HashQR { get; set; } // opcional
    }

    public class VentaDetalleDTO
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
    }
}
