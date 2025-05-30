using CafeteriaAPI.DTOs;

namespace BackCafeteria.DTOs
{
    public class VentaQrCreateDTO
    {
        public byte[] QrImageBytes { get; set; }
        public int QrImageWidth { get; set; } = 0; // Opcional
        public int QrImageHeight { get; set; } = 0; // Opcional
        public List<VentaDetalleDTO> Detalles { get; set; }

    }
}
