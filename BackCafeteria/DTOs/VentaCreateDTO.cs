namespace CafeteriaAPI.DTOs
{
    public class VentaCreateDTO
    {
        public string MetodoPago { get; set; } = null!; // "efectivo" o "credito"
        public int? UsuarioId { get; set; } // obligatorio si es con crédito
        public List<VentaDetalleDTO> Detalles { get; set; } = new();
        public string? NumeroDeControl { get; set; } // Para crédito

        public string? HashQR { get; set; } // solo se usa si el método es crédito por QR



    }

    public class VentaDetalleDTO
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }

    }

}
