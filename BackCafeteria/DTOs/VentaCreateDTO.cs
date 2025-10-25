namespace CafeteriaAPI.DTOs
{
    // DTO de creación de venta
    public class VentaCreateDTO
    {
        public string MetodoPago { get; set; } = null!; // "efectivo" o "credito"
        public int UsuarioId { get; set; } // obligatorio siempre
        public List<VentaDetalleDTO> Detalles { get; set; } = new();

        // Solo se usa si es pago por crédito con QR
        public string? NumeroDeControl { get; set; }
        public string? HashQR { get; set; }
    }

    public class VentaDetalleDTO
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
    }

}
